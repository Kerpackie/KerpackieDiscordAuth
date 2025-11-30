using System.Globalization;
using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using Kerpackie.Discord.Auth.Interfaces;
using Kerpackie.Discord.Auth.Models;
using Kerpackie.Discord.Auth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kerpackie.Discord.Auth.Extensions;

/// <summary>
/// Provides extension methods for configuring Discord authentication in ASP.NET Core applications.
/// </summary>
public static class DiscordAuthExtensions
{
    /// <summary>
    /// Adds and configures Discord authentication services, including settings binding, HTTP client, caching, and authentication schemes.
    /// </summary>
    /// <typeparam name="THandler">The custom handler implementing <see cref="IDiscordAuthHandler"/> for Discord authentication events.</typeparam>
    /// <param name="services">The service collection to add authentication services to.</param>
    /// <param name="configuration">The application configuration containing Discord settings.</param>
    /// <param name="configureOptions">Optional delegate to further configure <see cref="DiscordAuthenticationOptions"/>.</param>
    /// <param name="setupProxyCookie">Whether to configure proxy cookie settings for external identity authentication.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if Discord ClientId or ClientSecret is missing in configuration.</exception>
    public static IServiceCollection AddDiscordAuth<THandler>(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<DiscordAuthenticationOptions>? configureOptions = null,
        bool setupProxyCookie = true) 
        where THandler : class, IDiscordAuthHandler
    {
        // Bind & Validate Settings
        var settings = new DiscordAuthSettings
        {
            ClientId = "", 
            ClientSecret = ""
        };
        
        var section = configuration.GetSection(DiscordAuthSettings.SectionName);
        section.Bind(settings);

        if (string.IsNullOrEmpty(settings.ClientId) || string.IsNullOrEmpty(settings.ClientSecret))
        {
            throw new InvalidOperationException($"Missing Discord Configuration. Ensure your appsettings.json has a '{DiscordAuthSettings.SectionName}' section.");
        }

        services.AddSingleton(settings); 
        services.AddScoped<IDiscordAuthHandler, THandler>();
        
        // Add Memory Cache for Guilds/User data caching
        services.AddMemoryCache();

        services.AddHttpClient("DiscordApi", c => 
        {
            c.BaseAddress = new Uri("https://discord.com/api/");
            // Discord API requires a User-Agent header. 
            // See: https://discord.com/developers/docs/reference#user-agent
            c.DefaultRequestHeaders.UserAgent.ParseAdd("Kerpackie.Discord.Auth/1.0");
        });
        
        services.AddScoped<IDiscordService, DiscordService>();

        // Configure Authentication
        var authBuilder = services.AddAuthentication();

        if (setupProxyCookie)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
            });

            authBuilder.AddCookie("Identity.External", options =>
            {
                options.Cookie.Name = "Identity.External";
                options.Cookie.SameSite = SameSiteMode.Unspecified;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            });
        }

        authBuilder.AddDiscord(options => {
            options.ClientId = settings.ClientId;         
            options.ClientSecret = settings.ClientSecret; 

            // Security: Enable Proof Key for Code Exchange (PKCE)
            options.UsePkce = true;

            // UX: Handle "Cancel" on consent screen
            options.AccessDeniedPath = "/login?error=access_denied";

            options.Scope.Add("identify");
            options.Scope.Add("email");
            options.Scope.Add("guilds");

            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey("urn:discord:global_name", "global_name");
            options.ClaimActions.MapJsonKey("urn:discord:locale", "locale");
            options.ClaimActions.MapJsonKey("urn:discord:verified", "verified");
            
            options.ClaimActions.MapCustomJson("urn:discord:avatar:url", user =>
            {
                var userId = user.GetString("id");
                var avatarHash = user.GetString("avatar");
                if (string.IsNullOrEmpty(avatarHash)) return null;
                var extension = avatarHash.StartsWith("a_") ? "gif" : "png";
                return string.Format(CultureInfo.InvariantCulture, "https://cdn.discordapp.com/avatars/{0}/{1}.{2}", userId, avatarHash, extension);
            });

            configureOptions?.Invoke(options);

            options.SaveTokens = true;
            options.SignInScheme = "Identity.External"; 
            options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
            options.CorrelationCookie.IsEssential = true;
        });
            
        return services;
    }
}