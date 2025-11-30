using System.Text.Json.Serialization;

namespace Kerpackie.Discord.Auth.Models;

/// <summary>
/// Represents a Discord guild (server) object as returned by the Discord API.
/// Contains basic guild metadata used by the authentication flows (for example,
/// the list of guilds a user belongs to when using Discord OAuth scopes).
/// </summary>
public class DiscordGuild
{
    /// <summary>
    /// The guild's unique identifier (Snowflake).
    /// This value is required.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The guild's name.
    /// This value is required.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The hash of the guild's icon, or <c>null</c> if the guild has no icon.
    /// When present, it's used together with <see cref="Id"/> to construct the icon CDN URL.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Whether the current user is the owner of the guild.
    /// </summary>
    [JsonPropertyName("owner")]
    public bool Owner { get; set; }

    /// <summary>
    /// The permissions for the current user in this guild encoded as a string.
    /// Discord typically provides permissions as a bitfield; this property stores
    /// the raw representation returned by the API (commonly convertible to a numeric type).
    /// </summary>
    [JsonPropertyName("permissions")]
    public string? Permissions { get; set; }

    /// <summary>
    /// A list of feature flags enabled for the guild (for example, "VIP_REGIONS", "COMMUNITY").
    /// </summary>
    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();
        
    /// <summary>
    /// Builds the CDN URL for the guild icon if an icon hash exists.
    /// </summary>
    /// <returns>
    /// The full CDN URL for the guild icon (png or gif depending on animation),
    /// or an empty string if the guild has no icon.
    /// </returns>
    /// <remarks>
    /// Discord's animated icons start with the prefix "a_". If the icon hash begins
    /// with "a_", this method uses the "gif" extension; otherwise it uses "png".
    /// The returned URL follows Discord's CDN pattern:
    /// https://cdn.discordapp.com/icons/{guildId}/{iconHash}.{ext}
    /// </remarks>
    public string GetIconUrl()
    {
        if (string.IsNullOrEmpty(Icon)) return string.Empty;
            
        var extension = Icon.StartsWith("a_") ? "gif" : "png";
        return $"https://cdn.discordapp.com/icons/{Id}/{Icon}.{extension}";
    }
}
