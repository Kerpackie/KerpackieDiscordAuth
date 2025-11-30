using AspNet.Security.OAuth.Discord;
using Kerpackie.Discord.Auth.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kerpackie.Discord.Auth.Identity;

/// <summary>
/// Provides convenience extension methods to register Discord authentication integrated with ASP.NET Core Identity.
/// </summary>
/// <remarks>
/// These helpers wire up the Discord OAuth provider while integrating with the Identity system's existing
/// external cookie and user management. They are intended to be called during service registration (Startup/Program).
/// </remarks>
public static class IdentityDiscordExtensions
{
    /// <summary>
    /// Adds Discord external authentication and a default Identity-based handler into the DI container.
    /// </summary>
    /// <typeparam name="TUser">
    /// The application's Identity user type. This must derive from <see cref="IdentityUser"/> and have a parameterless constructor.
    /// </typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="config">The application's configuration used to retrieve Discord-related settings.</param>
    /// <param name="configureOptions">
    /// Optional callback to configure <see cref="DiscordAuthenticationOptions"/> provided by AspNet.Security.OAuth.Discord.
    /// This allows callers to set client id/secret, scopes, events, etc.
    /// </param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining.</returns>
    /// <remarks>
    /// This method registers the library's Identity-specific handler <c>IdentityDiscordHandler{TUser}</c> by delegating
    /// to the general <c>AddDiscordAuth{THandler}</c> extension. It intentionally avoids re-registering the "Identity.External"
    /// proxy cookie because ASP.NET Core Identity already registers that scheme; adding it again would cause a runtime conflict.
    /// </remarks>
    public static IServiceCollection AddDiscordAuthWithIdentity<TUser>(
        this IServiceCollection services, 
        IConfiguration config,
        Action<DiscordAuthenticationOptions>? configureOptions = null) 
        where TUser : IdentityUser, new()
    {
        // We pass 'false' because Identity already adds the "Identity.External" cookie scheme.
        // Re-adding or duplicating that scheme would cause a startup/runtime error.
        return services.AddDiscordAuth<IdentityDiscordHandler<TUser>>(
            config, 
            configureOptions, 
            setupProxyCookie: false);
    }
}
