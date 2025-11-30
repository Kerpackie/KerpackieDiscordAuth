using System.Security.Claims;
using Kerpackie.Discord.Auth.Interfaces;
using Kerpackie.Discord.Auth.Models;
using Microsoft.AspNetCore.Identity;

namespace Kerpackie.Discord.Auth.Identity;

/// <summary>
/// A reusable handler that maps Discord authentication results into ASP.NET Core Identity users.
/// </summary>
/// <remarks>
/// This class implements <see cref="IDiscordAuthHandler"/> and provides a default strategy for:
/// - locating an existing local user by email,
/// - creating a new local user when none exists,
/// - linking the external Discord login to the local user,
/// - storing the Discord access token as an authentication token,
/// - synchronizing a small set of Discord claims onto the local user,
/// and finally returning a <see cref="ClaimsPrincipal"/> for the local Identity user.
///
/// TUser is expected to be an <see cref="IdentityUser"/>-derived type registered with ASP.NET Core Identity.
/// This implementation performs user creation and claim updates through <see cref="UserManager{TUser}"/>,
/// and produces the final principal via <see cref="IUserClaimsPrincipalFactory{TUser}"/>.
/// </remarks>
public class IdentityDiscordHandler<TUser> : IDiscordAuthHandler 
    where TUser : IdentityUser, new()
{
    private readonly UserManager<TUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<TUser> _principalFactory;

    /// <summary>
    /// Constructs the handler with the required Identity services.
    /// </summary>
    /// <param name="userManager">UserManager used to query/create/update users and claims.</param>
    /// <param name="principalFactory">Factory used to create a <see cref="ClaimsPrincipal"/> for the local user.</param>
    public IdentityDiscordHandler(
        UserManager<TUser> userManager,
        IUserClaimsPrincipalFactory<TUser> principalFactory)
    {
        _userManager = userManager;
        _principalFactory = principalFactory;
    }

    /// <summary>
    /// Process a Discord login: ensure a corresponding local user exists, link the external login,
    /// persist the provider token, synchronize selected claims, and return a local <see cref="ClaimsPrincipal"/>.
    /// </summary>
    /// <param name="context">Context containing Discord-provided claims and the access token.</param>
    /// <returns>
    /// A <see cref="ClaimsPrincipal"/> representing the local Identity user that should be signed in.
    /// </returns>
    /// <remarks>
    /// Behavior & side-effects:
    /// - Looks up the local user by email (uses <see cref="UserManager{TUser}.FindByEmailAsync"/>).
    /// - If no user exists, creates a new TUser with EmailConfirmed = true.
    /// - Ensures a login entry exists for provider "Discord" (adds one via <see cref="UserManager{TUser}.AddLoginAsync"/>).
    /// - Persists the Discord access token via <see cref="UserManager{TUser}.SetAuthenticationTokenAsync"/>.
    /// - Calls <see cref="SyncClaims"/> to add/update a small set of claim types on the local user.
    /// - Finally, builds and returns a <see cref="ClaimsPrincipal"/> from the local user using the configured principal factory.
    ///
    /// Error handling:
    /// - This method relies on <see cref="UserManager{TUser}"/> to perform persistence; failures (IdentityResult errors)
    ///   are not explicitly thrown here but will surface via returned failed results from UserManager calls if they occur.
    /// - Caller should ensure that Identity services are configured and that required DB migrations are applied.
    /// </remarks>
    public async Task<ClaimsPrincipal> OnDiscordLoginAsync(DiscordLoginContext context)
    {
        // Try to find an existing user by their email address (Discord often provides email).
        var user = await _userManager.FindByEmailAsync(context.Email);

        // Create User if not exists
        if (user == null)
        {
            // Create a minimal local user using the email as username. Mark email as confirmed because Discord already verified it.
            user = new TUser 
            { 
                UserName = context.Email, 
                Email = context.Email, 
                EmailConfirmed = true 
            };
            await _userManager.CreateAsync(user);
            // Note: production code may want to check the IdentityResult and handle failures (logging, retries, etc.).
        }

        // Link Login
        // Ensure the external login info for Discord is associated with this local user.
        var logins = await _userManager.GetLoginsAsync(user);
        if (!logins.Any(x => x.LoginProvider == "Discord"))
        {
            await _userManager.AddLoginAsync(user, new UserLoginInfo("Discord", context.DiscordId, "Discord"));
            // Again, consider checking the returned IdentityResult in a robust implementation.
        }

        // Update Token & Claims
        // Store the access token provided by Discord so it can be used for API calls on behalf of the user.
        await _userManager.SetAuthenticationTokenAsync(user, "Discord", "access_token", context.AccessToken);
        // Synchronize a small set of claims from Discord onto the local user record.
        await SyncClaims(user, context);

        // Create and return a ClaimsPrincipal for the local user (sign-in will use this principal).
        return await _principalFactory.CreateAsync(user);
    }

    /// <summary>
    /// Synchronize a short list of provider claims from the Discord login context onto the local user.
    /// </summary>
    /// <param name="user">The local Identity user to update.</param>
    /// <param name="context">The Discord login context containing original claims and derived fields.</param>
    /// <remarks>
    /// This method:
    /// - Defines a fixed list of claim types to synchronize (avatar URL and global name).
    /// - For each claim type, reads the value from the Discord context and compares it against existing user claims.
    /// - If the claim exists and the value changed, it replaces the claim. If it is missing and a value is present, it adds it.
    ///
    /// Notes:
    /// - Only non-empty values are applied. Null/empty values will not remove an existing claim.
    /// - This approach avoids making large claim modifications; extend the claim list as needed for additional fields.
    /// - Claim types used here mirror the Discord-specific URNs used elsewhere in the library.
    /// </remarks>
    private async Task SyncClaims(TUser user, DiscordLoginContext context)
    {
        // Define the claim types we want to keep in sync on the local user.
        var claimsToSync = new[] { "urn:discord:avatar:url", "urn:discord:global_name" };
        var userClaims = await _userManager.GetClaimsAsync(user);

        // Helper to map a claim type to the value available in the context.
        string? GetValue(string type) => type switch {
            "urn:discord:avatar:url" => context.AvatarUrl,
            "urn:discord:global_name" => context.OriginalClaims.FirstOrDefault(c => c.Type == "urn:discord:global_name")?.Value,
            _ => null
        };

        foreach (var claimType in claimsToSync)
        {
            var newValue = GetValue(claimType);
            var existingClaim = userClaims.FirstOrDefault(c => c.Type == claimType);

            // If there's an existing claim but the value changed, replace it.
            if (existingClaim != null && existingClaim.Value != newValue && !string.IsNullOrEmpty(newValue))
            {
                await _userManager.RemoveClaimAsync(user, existingClaim);
                await _userManager.AddClaimAsync(user, new Claim(claimType, newValue));
            }
            // If there is no existing claim but we have a non-empty value, add the claim.
            else if (existingClaim == null && !string.IsNullOrEmpty(newValue))
            {
                await _userManager.AddClaimAsync(user, new Claim(claimType, newValue));
            }

            // Intentionally do not remove claims when newValue is null/empty to avoid unexpected data loss.
            // If removal is desired, implement explicit logic and an administrator-approved policy.
        }
    }
}
