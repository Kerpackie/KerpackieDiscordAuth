using System.Text.Json.Serialization;

namespace Kerpackie.Discord.Auth.Models;

/// <summary>
/// Represents a Discord user as returned by the Discord REST API (for example `GET users/@me`).
/// </summary>
/// <remarks>
/// Properties are mapped to the Discord JSON payload using <see cref="JsonPropertyNameAttribute"/>.
/// Some fields are only present when the OAuth2 token includes the appropriate scopes (for example, <see cref="Email"/> requires the "email" scope).
/// </remarks>
public class DiscordUser
{
    /// <summary>
    /// The user's Discord snowflake identifier.
    /// </summary>
    /// <remarks>
    /// This value is required and uniquely identifies the user across Discord.
    /// </remarks>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The user's username (may not include discriminators depending on API version).
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; set; }

    /// <summary>
    /// The user's global display name, if set. May be <c>null</c>.
    /// </summary>
    [JsonPropertyName("global_name")]
    public string? GlobalName { get; set; }

    /// <summary>
    /// The user's email address. Present only when the access token includes the "email" scope.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// The user's avatar hash. Use this value to construct a CDN URL for the avatar image.
    /// </summary>
    /// <remarks>
    /// If <c>null</c>, the user has no avatar and the default avatar should be used.
    /// Example avatar URL format: https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.png
    /// </remarks>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
    
    /// <summary>
    /// The user's locale (language tag), for example "en-US".
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// Indicates whether the user's email address has been verified. Meaningful only when <see cref="Email"/> is present.
    /// </summary>
    [JsonPropertyName("verified")]
    public bool Verified { get; set; }
}