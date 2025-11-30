using System.Diagnostics;

namespace Kerpackie.Discord.Auth.Diagnostics;

/// <summary>
/// Provides shared diagnostic primitives for the Discord authentication library.
/// </summary>
/// <remarks>
/// This class exposes a constant name for the activity source and a shared <see cref="ActivitySource"/>
/// instance that other parts of the library (or consuming applications) can use to create and start
/// <see cref="Activity"/> instances for tracing and diagnostics (for example, with OpenTelemetry).
/// </remarks>
public static class DiscordAuthDiagnostics
{
    /// <summary>
    /// The name of the activity source used by the Kerpackie Discord authentication components.
    /// </summary>
    /// <remarks>
    /// Prefer using this constant when creating or configuring telemetry providers so the same source
    /// name is used consistently across the library and consumer instrumentation.
    /// Example provider configuration could filter or subscribe to this source name to capture activity data.
    /// </remarks>
    public const string ActivitySourceName = "Kerpackie.Auth.Discord";

    /// <summary>
    /// Activity name used when handling the OAuth callback from Discord.
    /// </summary>
    /// <remarks>
    /// Use this name for activities that represent the entry point where the external provider redirects
    /// back to the application (e.g., when the application processes the external cookie and tokens).
    /// </remarks>
    public const string ActivityCallback = "DiscordCallback";

    /// <summary>
    /// Activity name used when invoking the application-level handler for a successful Discord login.
    /// </summary>
    /// <remarks>
    /// Use this name for activities that wrap the logic inside the <c>OnDiscordLogin</c> handler, such as
    /// creating or mapping a local user and signing them in.
    /// </remarks>
    public const string ActivityHandlerOnDiscordLogin = "Handler.OnDiscordLogin";

    /// <summary>
    /// Activity name used when retrieving the user's guilds.
    /// </summary>
    public const string ActivityGetGuilds = "GetGuilds";

    /// <summary>
    /// Activity name used when initiating the login flow.
    /// </summary>
    public const string ActivityInitiateLogin = "InitiateLogin";

    // Tag names
    /// <summary>
    /// Tag key for the Discord API endpoint being called.
    /// </summary>
    public const string TagDiscordEndpoint = "discord.endpoint";

    /// <summary>
    /// Tag key for the HTTP status code returned by Discord.
    /// </summary>
    public const string TagDiscordStatusCode = "discord.status_code";

    /// <summary>
    /// Tag key for the number of guilds retrieved.
    /// </summary>
    public const string TagDiscordGuildCount = "discord.guild_count";

    /// <summary>
    /// Tag key for the Discord user ID.
    /// </summary>
    public const string TagDiscordUserId = "discord.user_id";

    /// <summary>
    /// Tag key indicating if the Discord user is verified.
    /// </summary>
    public const string TagDiscordVerified = "discord.verified";

    /// <summary>
    /// Tag key for the Discord user's locale.
    /// </summary>
    public const string TagDiscordLocale = "discord.locale";

    /// <summary>
    /// Tag key for the return URL after login.
    /// </summary>
    public const string TagDiscordReturnUrl = "discord.return_url";

    // Event names
    /// <summary>
    /// Event name for a cache hit.
    /// </summary>
    public const string EventCacheHit = "CacheHit";

    /// <summary>
    /// Event name for a cache miss.
    /// </summary>
    public const string EventCacheMiss = "CacheMiss";

    /// <summary>
    /// Event name when rate limited.
    /// </summary>
    public const string EventRateLimited = "RateLimited";

    /// <summary>
    /// The shared <see cref="ActivitySource"/> instance created with <see cref="ActivitySourceName"/>.
    /// </summary>
    /// <remarks>
    /// Use this <see cref="ActivitySource"/> to start activities for tracing authentication flows,
    /// HTTP calls, token exchanges, or other diagnostic events in the Discord auth code paths.
    ///
    /// Example:
    /// <code>
    /// using var activity = DiscordAuthDiagnostics.ActivitySource.StartActivity(DiscordAuthDiagnostics.ActivityCallback);
    /// activity?.SetTag("client_id", settings.ClientId);
    /// </code>
    ///
    /// Notes:
    /// - <see cref="ActivitySource"/> instances are lightweight and intended to be shared; creating one per class is unnecessary.
    /// - Consumers should configure an OpenTelemetry or other telemetry provider that listens for this source name
    ///   to capture and export the activities produced by the library.
    /// - The ActivitySource is static and does not need to be disposed in normal use; leave disposal to process shutdown if required by hosting environment.
    /// </remarks>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}