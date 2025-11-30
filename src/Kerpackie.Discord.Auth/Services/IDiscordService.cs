using Kerpackie.Discord.Auth.Models;

namespace Kerpackie.Discord.Auth.Services;

/// <summary>
/// Abstraction for making requests to the Discord API and returning
/// user/guild information used by the authentication flows.
/// Implementations should handle HTTP details (Authorization header,
/// retries, deserialization) and surface domain objects or raw JSON.
/// </summary>
public interface IDiscordService
{
    /// <summary>
    /// Retrieves the raw JSON payload representing the list of guilds
    /// the user is a member of from the Discord API.
    /// </summary>
    /// <param name="accessToken">
    /// OAuth2 access token (typically a bearer token) for the user whose guilds are requested.
    /// The implementation is expected to add the appropriate Authorization header:
    /// "Authorization: Bearer {accessToken}".
    /// </param>
    /// <returns>
    /// A task that resolves to the raw JSON string containing the guilds array,
    /// or <c>null</c> if the request failed or no data was returned.
    /// </returns>
    /// <remarks>
    /// Implementations should not modify the JSON; use <see cref="GetGuildsAsync(string)"/>
    /// to get parsed <see cref="DiscordGuild"/> instances. Common failure modes include
    /// unauthorized tokens or network errors; implementations may throw exceptions
    /// such as <see cref="System.Net.Http.HttpRequestException"/>, <see cref="System.Threading.Tasks.TaskCanceledException"/>,
    /// or JSON parsing exceptions from consumers if they attempt to deserialize an invalid payload.
    /// </remarks>
    Task<string?> GetGuildsJsonAsync(string accessToken);

    /// <summary>
    /// Retrieves the list of guilds the user is a member of and deserializes them
    /// into <see cref="DiscordGuild"/> objects.
    /// </summary>
    /// <param name="accessToken">OAuth2 access token for the target user.</param>
    /// <returns>
    /// A task that resolves to a list of <see cref="DiscordGuild"/> or <c>null</c> if the request fails.
    /// The returned list may be empty if the user belongs to no guilds.
    /// </returns>
    /// <remarks>
    /// Implementations should perform JSON deserialization and return strongly-typed models.
    /// If the API returns an unexpected shape, implementations may either return <c>null</c>
    /// or throw a descriptive exception — prefer returning <c>null</c> for transient failures
    /// and throwing for programming errors/unrecoverable conditions.
    /// </remarks>
    Task<List<DiscordGuild>?> GetGuildsAsync(string accessToken);

    /// <summary>
    /// Retrieves the Discord user profile for the provided access token and returns
    /// it as a <see cref="DiscordUser"/> instance.
    /// </summary>
    /// <param name="accessToken">OAuth2 access token for the user to fetch.</param>
    /// <returns>
    /// A task that resolves to a <see cref="DiscordUser"/> instance, or <c>null</c>
    /// if the request fails or the user could not be retrieved.
    /// </returns>
    /// <remarks>
    /// Typical implementation calls the "/users/@me" endpoint. Implementations should
    /// map fields like id, username, discriminator, avatar, etc., into <see cref="DiscordUser"/>.
    /// </remarks>
    Task<DiscordUser?> GetUserAsync(string accessToken);

    /// <summary>
    /// Retrieves the raw JSON payload representing the Discord user profile
    /// for the provided access token.
    /// </summary>
    /// <param name="accessToken">OAuth2 access token for the user to fetch.</param>
    /// <returns>
    /// A task that resolves to the raw JSON user object, or <c>null</c> if unavailable.
    /// </returns>
    /// <remarks>
    /// Use this when consumers want to handle deserialization themselves or inspect
    /// Discord's raw response directly.
    /// </remarks>
    Task<string?> GetUserJsonAsync(string accessToken);

    /// <summary>
    /// Executes a generic request against a Discord API endpoint and deserializes
    /// the response to the requested type.
    /// </summary>
    /// <typeparam name="T">The expected return type to deserialize the JSON response to.</typeparam>
    /// <param name="accessToken">OAuth2 access token for authorization.</param>
    /// <param name="endpoint">
    /// The Discord API endpoint path (for example, "/users/@me" or "/users/@me/guilds").
    /// Implementations should combine this with the base API URL (e.g., "https://discord.com/api")
    /// as needed.
    /// </param>
    /// <returns>
    /// A task that resolves to an instance of <typeparamref name="T"/> deserialized from the response,
    /// or <c>null</c> if the request failed or the response body was empty.
    /// </returns>
    /// <remarks>
    /// Implementations are expected to:
    /// - Add the Authorization header using the provided <paramref name="accessToken"/>.
    /// - Perform proper HTTP error handling and optionally translate non-success HTTP
    ///   responses into <c>null</c> or throw exceptions depending on policy.
    /// - Deserialize JSON to <typeparamref name="T"/> using a consistent serializer.
    /// Common exceptions include <see cref="System.Net.Http.HttpRequestException"/>,
    /// <see cref="System.Text.Json.JsonException"/>, and <see cref="System.Threading.Tasks.TaskCanceledException"/>.
    /// </remarks>
    Task<T?> ExecuteDiscordRequestAsync<T>(string accessToken, string endpoint);
}
