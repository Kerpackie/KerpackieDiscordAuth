using OpenTelemetry.Trace;
using Kerpackie.Discord.Auth.Diagnostics;

namespace Kerpackie.Discord.Auth.OpenTelemetry;

/// <summary>
/// Extension methods for setting up Discord Auth OpenTelemetry instrumentation.
/// </summary>
public static class DiscordAuthInstrumentationExtensions
{
    /// <summary>
    /// Adds the Discord Auth instrumentation to the TracerProviderBuilder.
    /// </summary>
    /// <param name="builder">The TracerProviderBuilder to add the instrumentation to.</param>
    /// <returns>The TracerProviderBuilder for chaining.</returns>
    public static TracerProviderBuilder AddDiscordAuthInstrumentation(this TracerProviderBuilder builder)
    {
        builder.AddSource(DiscordAuthDiagnostics.ActivitySourceName);
        return builder;
    }
}
