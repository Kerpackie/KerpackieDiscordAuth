namespace Kerpackie.Discord.Auth.Constants;

/// <summary>
/// Project-wide constants used across the Kerpackie Discord Auth library.
/// Keep short, stable string values here (logger categories, claim names, etc.).
/// </summary>
public static class DiscordAuthConstants
{
    /// <summary>
    /// Logger category name used for logging messages during the login flow.
    /// Use this to create or filter loggers related to the initial login request handling.
    /// </summary>
    public const string LoggerCategoryLogin = "Kerpackie.Auth.Discord.Login";

    /// <summary>
    /// Logger category name used for logging messages during the callback/authorization flow.
    /// Use this to create or filter loggers related to processing the OAuth callback.
    /// </summary>
    public const string LoggerCategoryCallback = "Kerpackie.Auth.Discord.Callback";

    // Claim name constants used by the library

    /// <summary>
    /// Claim key that stores the user's Discord avatar URL.
    /// Value format: a fully-qualified URL returned by Discord's API for the user's avatar image.
    /// </summary>
    public const string ClaimDiscordAvatarUrl = "urn:discord:avatar:url";

    /// <summary>
    /// Claim key that stores the user's locale (language/region) as provided by Discord.
    /// Typical values are language tags such as "en-US", "fr", etc.
    /// </summary>
    public const string ClaimDiscordLocale = "urn:discord:locale";

    /// <summary>
    /// Claim key that indicates whether the Discord account email has been verified.
    /// Expected values are the string representation of a boolean ("true"/"false").
    /// </summary>
    public const string ClaimDiscordVerified = "urn:discord:verified";
}