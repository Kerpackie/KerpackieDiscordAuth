// filepath: /Users/kerpackie/RiderProjects/KerpackieDiscordAuth/src/Kerpackie.Discord.Auth/Endpoints/DiscordLoginEndpoint.cs
using Kerpackie.Discord.Auth.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Kerpackie.Discord.Auth.Endpoints;

/// <summary>
/// Provides endpoint mapping helpers for initiating a Discord authentication flow.
/// </summary>
/// <remarks>
/// This static helper wires a GET endpoint (at <see cref="DiscordAuthSettings.LoginPath"/>) which challenges the
/// authentication middleware using the "Discord" scheme. The resulting challenge will redirect the user to Discord's
/// OAuth2 authorization endpoint. After authentication, Discord will redirect back to the configured callback path.
/// </remarks>
public static class DiscordLoginEndpoint
{
    /// <summary>
    /// Maps a GET endpoint that initiates a Discord sign-in flow.
    /// </summary>
    /// <param name="app">The endpoint route builder used to register the route.</param>
    /// <param name="settings">Discord authentication settings containing the login path, callback path and default return url.</param>
    /// <param name="logger">Logger used to record diagnostic information for the login flow.</param>
    /// <remarks>
    /// When the mapped endpoint is invoked, it:
    /// 1. Logs the incoming request and optional return URL.
    /// 2. Determines the final target URL to return the user to after authentication (either the provided returnUrl query parameter or <see cref="DiscordAuthSettings.DefaultReturnUrl"/>).
    /// 3. Creates <see cref="AuthenticationProperties"/> with a RedirectUri pointing to <see cref="DiscordAuthSettings.CallbackPath"/>.
    /// 4. Stores the chosen target URL in the AuthenticationProperties <c>Items</c> collection under the "returnUrl" key so it can be retrieved later by the callback handler.
    /// 5. Issues a Challenge using the "Discord" authentication scheme which triggers the external OAuth2 redirect.
    /// 
    /// This method does not throw exceptions itself but the underlying authentication system may produce errors that should
    /// be handled by the caller application's error handling/middleware.
    /// </remarks>
    /// <example>
    /// // Example usage during application startup:
    /// // app.MapDiscordLogin(discordAuthSettings, logger);
    /// </example>
    public static void MapDiscordLogin(this IEndpointRouteBuilder app, DiscordAuthSettings settings, ILogger logger)
    {
        app.MapGet(settings.LoginPath, (string? returnUrl) =>
        {
            // Log the incoming login attempt and the optional returnUrl query parameter.
            logger.LogInformation("Initiating Discord Login. ReturnUrl: {ReturnUrl}", returnUrl);

            // Determine where to send the user after a successful login:
            // prefer the provided returnUrl query parameter; otherwise use the configured default.
            var targetUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : settings.DefaultReturnUrl;

            // Build authentication properties:
            // - RedirectUri is where the external provider (Discord) should send the user back to.
            // - Items can carry arbitrary data (here we store the final return URL so the callback handler can read it).
            var props = new AuthenticationProperties { RedirectUri = settings.CallbackPath };
            props.Items["returnUrl"] = targetUrl;

            // Issue an authentication challenge using the "Discord" scheme.
            // This instructs the authentication middleware to redirect the client to Discord's OAuth2 authorization endpoint.
            return Results.Challenge(props, authenticationSchemes: new[] { "Discord" });
        });
    }
}
