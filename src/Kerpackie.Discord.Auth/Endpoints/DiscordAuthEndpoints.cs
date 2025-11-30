using Kerpackie.Discord.Auth.Constants;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Kerpackie.Discord.Auth.Models;

namespace Kerpackie.Discord.Auth.Endpoints;

/// <summary>
/// Helper that registers the Discord authentication-related endpoints on an <see cref="IEndpointRouteBuilder"/>.
/// </summary>
/// <remarks>
/// This class provides a single convenience extension method that resolves the shared settings and logger factory
/// from the application's DI container and registers the two concrete endpoints: the login initiator and the OAuth callback.
/// Use <see cref="MapDiscordAuth"/> during application startup to wire the Discord authentication endpoints into the routing pipeline.
/// </remarks>
public static class DiscordAuthEndpoints
{
    /// <summary>
    /// Maps the Discord authentication endpoints (login and callback) into the provided endpoint route builder.
    /// </summary>
    /// <param name="app">The application's endpoint route builder used to register routes.</param>
    /// <remarks>
    /// Behavior:
    /// - Resolves <see cref="DiscordAuthSettings"/> from DI (<see cref="IOptions{T}"/>).
    /// - Resolves an <see cref="ILoggerFactory"/> and creates two typed loggers using the categories defined in <c>DiscordAuthConstants</c>.
    /// - Calls <c>MapDiscordLogin</c> and <c>MapDiscordCallback</c> with the resolved settings and their respective loggers.
    ///
    /// Error modes:
    /// - If required services are not registered in DI (settings or logger factory), this method will throw when attempting to resolve them.
    /// - This method performs registration-time resolution (not per-request), which surfaces misconfiguration early during app startup.
    /// </remarks>
    public static void MapDiscordAuth(this IEndpointRouteBuilder app)
    {
        // Resolve shared dependencies once at registration time and pass them into the specific mappers
        var settings = app.ServiceProvider.GetRequiredService<IOptions<DiscordAuthSettings>>().Value;
        var loggerFactory = app.ServiceProvider.GetRequiredService<ILoggerFactory>();

        // Orchestrator: register both endpoints with typed/specific logger categories
        // The login endpoint initiates the OAuth flow and the callback endpoint completes it.
        app.MapDiscordLogin(settings, loggerFactory.CreateLogger(DiscordAuthConstants.LoggerCategoryLogin));
        app.MapDiscordCallback(settings, loggerFactory.CreateLogger(DiscordAuthConstants.LoggerCategoryCallback));
    }
}