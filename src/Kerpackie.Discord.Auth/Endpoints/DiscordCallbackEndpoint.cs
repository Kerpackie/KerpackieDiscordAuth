using System.Diagnostics;
using System.Security.Claims;
using Kerpackie.Discord.Auth.Constants;
using Kerpackie.Discord.Auth.Diagnostics;
using Kerpackie.Discord.Auth.Interfaces;
using Kerpackie.Discord.Auth.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Kerpackie.Discord.Auth.Endpoints;

/// <summary>
/// Registers an HTTP endpoint that handles the OAuth2 callback from Discord.
///
/// This helper wires the configured callback path to read the temporary external
/// identity produced by the authentication middleware (the external cookie),
/// transform that into a <see cref="DiscordLoginContext"/>, and delegate to an
/// <see cref="IDiscordAuthHandler"/> implementation to create/lookup a local
/// application principal. The external cookie is then cleared and the user is
/// signed-in to the local application scheme and redirected to the configured
/// return URL.
/// </summary>
/// <remarks>
/// - Expects the external authentication result to be available via the
///   "Identity.External" scheme (set by the external provider middleware).
/// - Reads tokens and claims from the external authentication result, including
///   access token and Discord-specific claims.
/// - Calls through to <see cref="IDiscordAuthHandler.OnDiscordLoginAsync"/> to
///   obtain a local <see cref="System.Security.Claims.ClaimsPrincipal"/> to sign in.
/// - Issues a 302 redirect to the originally requested return URL (stored in
///   the external auth properties) or to <see cref="DiscordAuthSettings.DefaultReturnUrl"/>.
/// </remarks>
public static class DiscordCallbackEndpoint
{
    /// <summary>
    /// Maps the Discord callback path to a GET handler that completes the external sign-in flow.
    /// </summary>
    /// <param name="app">The application's endpoint route builder used to register the route.</param>
    /// <param name="settings">Configuration/settings for Discord authentication (login/callback paths, default return URL).</param>
    /// <param name="logger">Logger used to record diagnostics for the callback and login flow.</param>
    /// <remarks>
    /// The mapped endpoint:
    /// 1. Authenticates the external cookie (scheme "Identity.External"). If it fails, returns 401.
    /// 2. Builds a <see cref="DiscordLoginContext"/> from claims and tokens provided by Discord.
    /// 3. Invokes <see cref="IDiscordAuthHandler.OnDiscordLoginAsync"/> to obtain a local principal.
    /// 4. Signs out the external cookie, signs in the local application principal, and redirects to the return URL.
    /// </remarks>
    public static void MapDiscordCallback(this IEndpointRouteBuilder app, DiscordAuthSettings settings, ILogger logger)
    {
        app.MapGet(settings.CallbackPath, async (
            HttpContext context,
            IDiscordAuthHandler authHandler) =>
        {
            // Start a diagnostic activity for the callback handling (helps tracing distributed ops).
            using (DiscordAuthDiagnostics.ActivitySource.StartActivity(DiscordAuthDiagnostics.ActivityCallback))
            {
                // Attempt to read the external authentication result produced by the OAuth middleware.
                // The external middleware stores the result in the "Identity.External" cookie/scheme.
                var result = await context.AuthenticateAsync("Identity.External");

                // If we couldn't authenticate the external cookie, log and return 401 Unauthorized.
                if (!result.Succeeded)
                {
                    logger.LogWarning("Discord Callback Failed: External cookie not found or invalid.");
                    Activity.Current?.SetStatus(ActivityStatusCode.Error, "Auth Failed");
                    return Results.Unauthorized();
                }

                // Extract the external principal (claims from Discord). If missing, treat as unauthorized.
                var p = result.Principal;
                if (p == null) return Results.Unauthorized();

                // Log basic identifying information for diagnostics (Discord user ID).
                logger.LogInformation("Discord Callback Received for User: {DiscordId}", p.FindFirstValue(ClaimTypes.NameIdentifier));

                // Parse the "verified" Discord-specific claim (true/false).
                bool.TryParse(p.FindFirstValue(DiscordAuthConstants.ClaimDiscordVerified), out var isVerified);

                // Build the context object that will be passed to the application-level handler.
                // This gathers standard claims (ID, email, username), Discord-specific fields and the access token.
                var info = new DiscordLoginContext
                {
                    DiscordId = p.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    Email = p.FindFirstValue(ClaimTypes.Email)!,
                    Username = p.FindFirstValue(ClaimTypes.Name)!,
                    AvatarUrl = p.FindFirstValue(DiscordAuthConstants.ClaimDiscordAvatarUrl),
                    Locale = p.FindFirstValue(DiscordAuthConstants.ClaimDiscordLocale),
                    Verified = isVerified,
                    AccessToken = result.Properties?.GetTokenValue("access_token")!,
                    OriginalClaims = p.Claims
                };

                Activity.Current?.SetTag(DiscordAuthDiagnostics.TagDiscordUserId, info.DiscordId);
                Activity.Current?.SetTag(DiscordAuthDiagnostics.TagDiscordVerified, info.Verified);
                Activity.Current?.SetTag(DiscordAuthDiagnostics.TagDiscordLocale, info.Locale);

                // Start a nested diagnostic activity around the application handler invocation.
                using (DiscordAuthDiagnostics.ActivitySource.StartActivity(DiscordAuthDiagnostics.ActivityHandlerOnDiscordLogin))
                {
                    // Let the application-level handler process the Discord login:
                    // - Create or lookup a local user.
                    // - Return a ClaimsPrincipal representing the local identity to sign in.
                    var localPrincipal = await authHandler.OnDiscordLoginAsync(info);

                    // Clear the temporary external cookie used by the external authentication middleware.
                    await context.SignOutAsync("Identity.External");

                    // Prepare an HTTP redirect (302) back to the original return URL.
                    context.Response.StatusCode = 302;
                    var returnUrl = result.Properties?.Items["returnUrl"] ?? settings.DefaultReturnUrl;
                    context.Response.Headers.Location = returnUrl;

                    // Sign in the local application principal (usually the Identity application cookie).
                    await context.SignInAsync(IdentityConstants.ApplicationScheme, localPrincipal);

                    // Log the successful sign-in and redirection target.
                    logger.LogInformation("Discord Login Successful. Redirecting to {ReturnUrl}", returnUrl);
                    return Results.Empty;
                }
            }
        });
    }
}