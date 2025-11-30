namespace Kerpackie.Discord.Auth.Models;

/// <summary>
/// Configuration settings used to configure Discord authentication behavior.
/// </summary>
/// <remarks>
/// This type is intended to be bound from configuration (for example, from an "Discord" section
/// in appsettings.json or a secret store). It contains the OAuth client credentials and URL
/// endpoints used by the authentication middleware/handlers in this library.
/// </remarks>
/// <example>
/// Example appsettings.json:
/// {
///   "Discord": {
///     "ClientId": "your-client-id",
///     "ClientSecret": "your-client-secret",
///     "LoginPath": "/auth/discord/login",
///     "CallbackPath": "/auth/discord/callback",
///     "DefaultReturnUrl": "/"
///   }
/// }
///
/// </example>
public class DiscordAuthSettings
{
    /// <summary>
    /// The configuration section name that contains the Discord authentication settings.
    /// Use this constant when binding IConfiguration to <see cref="DiscordAuthSettings"/>.
    /// </summary>
    public const string SectionName = "Discord";

    /// <summary>
    /// The Discord OAuth2 application Client ID.
    /// </summary>
    /// <remarks>
    /// This property is required. Provide the public client identifier from your Discord application.
    /// Keep this value configured for your environment (do not hardcode in source).
    /// </remarks>
    public required string ClientId { get; set; }

    /// <summary>
    /// The Discord OAuth2 application Client Secret.
    /// </summary>
    /// <remarks>
    /// This property is required. Treat the client secret as sensitive data and store it securely
    /// (for example, in environment variables, Azure Key Vault, AWS Secrets Manager, or user secrets).
    /// Do not check the client secret into source control.
    /// </remarks>
    public required string ClientSecret { get; set; }

    /// <summary>
    /// The local path used to initiate a Discord login redirect.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>/auth/discord/login</c>. This path is intended to be served by the library's
    /// endpoint that starts the OAuth flow (redirects to Discord's authorize endpoint).
    /// Customize if you need a different routing convention.
    /// </remarks>
    public string LoginPath { get; set; } = "/auth/discord/login";

    /// <summary>
    /// The local callback path that Discord will redirect to after the user authorizes the app.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>/auth/discord/callback</c>. The application should expose an endpoint
    /// at this path to complete the OAuth flow, exchange the authorization code for tokens,
    /// and sign the user in / establish a session.
    /// Ensure your Discord application configuration lists the full redirect URI that maps
    /// to this path (including scheme and host) when running in production.
    /// </remarks>
    public string CallbackPath { get; set; } = "/auth/discord/callback";
    
    /// <summary>
    /// The default URL to return the user to after a successful login, when no return URL was specified.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>/</c>. This value is used as a safe fallback return destination following authentication.
    /// Validate or sanitize return URLs if your application accepts a returnUrl parameter to avoid open redirects.
    /// </remarks>
    public string DefaultReturnUrl { get; set; } = "/";
}
