using System.Security.Claims;

namespace Kerpackie.Discord.Auth.Models;

/// <summary>
/// Represents the data collected during a Discord OAuth2 login flow.
/// </summary>
/// <remarks>
/// This DTO consolidates the most commonly used fields extracted from the Discord OAuth
/// claims and token response, along with the raw <see cref="OriginalClaims"/> provided
/// by the authentication middleware. It's used by the library to communicate login state
/// to application code (for example, to create or link local user accounts).
/// </remarks>
public class DiscordLoginContext
{
    /// <summary>
    /// The Discord user identifier (snowflake) for the authenticated user.
    /// </summary>
    /// <remarks>
    /// This field is required and cannot be null.
    /// </remarks>
    public required string DiscordId { get; set; }

    /// <summary>
    /// The user's email address as returned by Discord. Only present when the "email" scope is granted.
    /// </summary>
    /// <remarks>
    /// This field is required and cannot be null.
    /// </remarks>
    public required string Email { get; set; }

    /// <summary>
    /// The user's username (display name) as returned by Discord.
    /// </summary>
    /// <remarks>
    /// This field is required and cannot be null.
    /// </remarks>
    public required string Username { get; set; }

    /// <summary>
    /// A URL to the user's avatar image, if available. May be <c>null</c> when the user has no avatar set.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// The OAuth2 access token issued by Discord for the authenticated user.
    /// </summary>
    /// <remarks>
    /// This token can be used to call Discord endpoints on behalf of the user (for example, to list guilds).
    /// Treat this value as a secret; do not log it in plaintext.
    /// </remarks>
    /// <remarks>
    /// This field is required and cannot be null.
    /// </remarks>
    public required string AccessToken { get; set; }

    /// <summary>
    /// The user's locale (language tag) as provided by Discord, for example "en-US". May be <c>null</c>.
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Indicates whether the user's email address has been verified by Discord. May be <c>null</c> when email is not provided.
    /// </summary>
    public bool? Verified { get; set; }

    /// <summary>
    /// The raw claims collection produced by the Discord OAuth authentication middleware.
    /// </summary>
    /// <remarks>
    /// Include claims such as the user's id, username, email, and any other mapped fields.
    /// This allows consumers to access the original claim values beyond the simplified properties
    /// included on this context object.
    /// </remarks>
    /// <remarks>
    /// This field is required and cannot be null.
    /// </remarks>
    public required IEnumerable<Claim> OriginalClaims { get; set; }
}