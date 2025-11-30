using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kerpackie.Discord.Auth.Diagnostics;
using Kerpackie.Discord.Auth.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Kerpackie.Discord.Auth.Services;

/// <summary>
/// Service responsible for calling the Discord HTTP API and returning user and guild information.
/// </summary>
/// <remarks>
/// This implementation uses an <see cref="IHttpClientFactory"/> named "DiscordApi" and expects
/// callers to provide a valid OAuth2 access token. It includes light caching for guild lists
/// and basic handling for rate limits (429) with a single short retry attempt.
/// </remarks>
public class DiscordService : IDiscordService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DiscordService> _logger; 

    /// <summary>
    /// Creates a new instance of <see cref="DiscordService"/>.
    /// </summary>
    /// <param name="httpClientFactory">Factory used to create configured <see cref="System.Net.Http.HttpClient"/> instances (expects a client named "DiscordApi").</param>
    /// <param name="cache">Memory cache used to store guild lists for a short duration to reduce API calls.</param>
    /// <param name="logger">Logger instance for diagnostic messages and error reporting.</param>
    public DiscordService(
        IHttpClientFactory httpClientFactory, 
        IMemoryCache cache,
        ILogger<DiscordService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the raw JSON response for the authenticated user's guilds.
    /// </summary>
    /// <param name="accessToken">OAuth2 access token for the user.</param>
    /// <returns>
    /// A JSON string containing the first page of the user's guilds (up to 200 entries),
    /// or <c>null</c> if the request fails.
    /// </returns>
    /// <remarks>
    /// This method intentionally returns only the first page as raw JSON because concatenating
    /// JSON arrays across pages is error prone; use <see cref="GetGuildsAsync"/> to obtain a
    /// fully paginated and deserialized list of guilds.
    /// </remarks>
    public async Task<string?> GetGuildsJsonAsync(string accessToken)
    {
        // Note: We do NOT paginate raw JSON calls because merging JSON arrays 
        // string-wise is messy. This returns the first page (max 200) only.
        return await ExecuteDiscordRequestAsync<string>(accessToken, "users/@me/guilds?limit=200");
    }

    /// <summary>
    /// Retrieves the authenticated user's guilds as a deserialized list of <see cref="DiscordGuild"/>.
    /// </summary>
    /// <param name="accessToken">OAuth2 access token for the user.</param>
    /// <returns>
    /// A list of <see cref="DiscordGuild"/> representing the user's guilds, or <c>null</c> if the request fails.
    /// </returns>
    /// <remarks>
    /// This method paginates through Discord's guilds endpoints (200 per page) and caches the
    /// full result for a short duration (default: 2 minutes) using a cache key derived from the token's hash.
    /// Caching is intended to reduce duplicate API requests for the same authenticated session.
    /// </remarks>
    public async Task<List<DiscordGuild>?> GetGuildsAsync(string accessToken)
    {
        // Cache Key strategy: "discord_guilds_{ShortHashOfToken}"
        // We use the token hash so different users (or re-logins) get their own cache entries.
        var cacheKey = $"discord_guilds_{accessToken.GetHashCode()}";

        if (_cache.TryGetValue(cacheKey, out List<DiscordGuild>? cachedGuilds) && cachedGuilds != null)
        {
            _logger.LogDebug("Returning {Count} guilds from cache.", cachedGuilds.Count);
            return cachedGuilds;
        }

        // Start fetching (with pagination)
        var allGuilds = new List<DiscordGuild>();
        string? lastId = null;
        
        while (true)
        {
            var endpoint = "users/@me/guilds?limit=200";
            if (!string.IsNullOrEmpty(lastId))
            {
                endpoint += $"&after={lastId}";
            }

            var batch = await ExecuteDiscordRequestAsync<List<DiscordGuild>>(accessToken, endpoint);
            
            if (batch == null || batch.Count == 0) break;

            allGuilds.AddRange(batch);

            // Discord returns max 200 per page. If we got fewer, we are done.
            if (batch.Count < 200) break;

            // Prepare for next page
            lastId = batch.Last().Id;
        }

        // Cache the full list for a short duration (e.g., 2 minutes)
        // Guild memberships don't change that often.
        _cache.Set(cacheKey, allGuilds, TimeSpan.FromMinutes(2));

        return allGuilds;
    }

    /// <summary>
    /// Retrieves the authenticated user's profile as a deserialized <see cref="DiscordUser"/> object.
    /// </summary>
    /// <param name="accessToken">OAuth2 access token for the user.</param>
    /// <returns>A <see cref="DiscordUser"/> instance, or <c>null</c> if the request fails.</returns>
    public async Task<DiscordUser?> GetUserAsync(string accessToken)
    {
        return await ExecuteDiscordRequestAsync<DiscordUser>(accessToken, "users/@me");
    }

    /// <summary>
    /// Retrieves the authenticated user's profile as raw JSON.
    /// </summary>
    /// <param name="accessToken">OAuth2 access token for the user.</param>
    /// <returns>A JSON string of the user's profile, or <c>null</c> if the request fails.</returns>
    public async Task<string?> GetUserJsonAsync(string accessToken)
    {
        return await ExecuteDiscordRequestAsync<string>(accessToken, "users/@me");
    }

    /// <summary>
    /// Executes a GET request against a Discord API endpoint and deserializes the response to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type. Use <see cref="string"/> to receive raw JSON text.</typeparam>
    /// <param name="accessToken">OAuth2 access token used in the Authorization header.</param>
    /// <param name="endpoint">Discord API endpoint (relative to the configured base address). May include query string.</param>
    /// <returns>
    /// The deserialized response of type <typeparamref name="T"/>, or <c>null</c> if the request fails or the response cannot be deserialized.
    /// </returns>
    /// <remarks>
    /// Behavior notes:
    /// - Adds an Activity using <see cref="DiscordAuthDiagnostics.ActivitySource"/> for tracing.
    /// - Sets the Authorization header to "Bearer {accessToken}".
    /// - Performs a single retry for HTTP 429 (rate limit) responses if the server indicates a short retry-after.
    /// - Logs warnings for forbidden (403) or unauthorized (401) responses and returns <c>null</c> in those cases.
    /// - Rethrows unexpected exceptions after logging and marking the Activity as errored.
    /// </remarks>
    /// <exception cref="Exception">Any unexpected exception during HTTP communication is logged and rethrown.</exception>
    public async Task<T?> ExecuteDiscordRequestAsync<T>(string accessToken, string endpoint)
    {
        using var activity = DiscordAuthDiagnostics.ActivitySource.StartActivity($"DiscordRequest: {endpoint}");
        activity?.SetTag("discord.endpoint", endpoint);

        try 
        {
            var client = _httpClientFactory.CreateClient("DiscordApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var path = endpoint.StartsWith("/") ? endpoint.Substring(1) : endpoint;
            
            // Retry Loop for Rate Limits
            // We allow 1 retry if we hit a 429 with a short wait time.
            int maxRetries = 1;
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                var response = await client.GetAsync(path);

                if (response.IsSuccessStatusCode)
                {
                    if (typeof(T) == typeof(string))
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        return (T)(object)json;
                    }
                    return await response.Content.ReadFromJsonAsync<T>();
                }

                // Handle Rate Limiting (429)
                if ((int)response.StatusCode == 429)
                {
                    var retryAfterSeconds = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 5;
                    
                    _logger.LogWarning("Rate limited on {Endpoint}. Retry after: {Seconds}s", endpoint, retryAfterSeconds);
                    activity?.AddEvent(new ActivityEvent("RateLimited"));

                    // If wait is short (< 3s) and we haven't retried yet, wait and try again.
                    if (attempt < maxRetries && retryAfterSeconds < 3)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds + 0.5)); // +0.5s buffer
                        continue;
                    }
                }

                // Handle Missing Scopes (403) or Invalid Token (401)
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("Access Forbidden to {Endpoint}. Ensure the Access Token has the required scopes (e.g. 'guilds').", endpoint);
                    return default;
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Access Unauthorized. Token may be expired or invalid.");
                    return default;
                }

                // General Error
                _logger.LogError("Discord API Error: {Status} for {Endpoint}", response.StatusCode, endpoint);
                activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {response.StatusCode}");
                return default;
            }

            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Discord API call to {Endpoint}", endpoint);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}