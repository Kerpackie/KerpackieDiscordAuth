using System.Security.Claims;
using Kerpackie.Discord.Auth.Models;

namespace Kerpackie.Discord.Auth.Interfaces;

/// <summary>
/// Abstraction for handling the completion of a Discord OAuth login flow.
/// </summary>
/// <remarks>
/// Implementations of this interface are responsible for turning the data available in
/// <see cref="DiscordLoginContext"/> (for example user information and tokens) into a
/// <see cref="ClaimsPrincipal"/> that represents the authenticated user for the application.
///
/// The authentication middleware calls this method when a Discord login completes and expects
/// a fully-populated <see cref="ClaimsPrincipal"/> to establish the application's identity.
/// Implementations may consult application services, persist or update user records, and add
/// custom claims as needed.
/// </remarks>
public interface IDiscordAuthHandler
{
    /// <summary>
    /// Called when a Discord login flow completes and a login context is available.
    /// </summary>
    /// <param name="context">
    /// The <see cref="DiscordLoginContext"/> containing the HTTP context, Discord user information,
    /// tokens, and any data produced by the authentication middleware during the OAuth flow.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> whose result is a non-null <see cref="ClaimsPrincipal"/>
    /// representing the authenticated user. The returned principal will be used by the framework
    /// to sign in the user or establish the current user identity.
    /// </returns>
    /// <remarks>
    /// - The returned <see cref="ClaimsPrincipal"/> should not be null. If the login cannot be
    ///   completed or should be rejected, throw an appropriate exception or return a principal
    ///   that reflects an unauthenticated identity per your application's convention.
    /// - Keep implementations asynchronous and avoid blocking calls. Long-running work (such as
    ///   expensive network or database operations) should be awaited.
    /// - Add only the claims you need; avoid leaking sensitive data as claims.
    /// - This method may be invoked once per authentication attempt; implementations should not
    ///   assume it will be called multiple times for the same request.
    /// </remarks>
    /// <example>
    /// A minimal example implementation:
    /// <code language="csharp">
    /// public async Task&lt;ClaimsPrincipal&gt; OnDiscordLoginAsync(DiscordLoginContext context)
    /// {
    ///     var id = new ClaimsIdentity("Discord");
    ///     id.AddClaim(new Claim(ClaimTypes.NameIdentifier, context.User.Id));
    ///     id.AddClaim(new Claim(ClaimTypes.Name, context.User.Username));
    ///     // Optionally add roles or other claims, persist user, etc.
    ///     return new ClaimsPrincipal(id);
    /// }
    /// </code>
    /// </example>
    Task<ClaimsPrincipal> OnDiscordLoginAsync(DiscordLoginContext context);
}
