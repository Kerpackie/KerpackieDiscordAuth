using Kerpackie.Discord.Auth.Extensions;
using Kerpackie.Discord.Auth.Endpoints;
using Kerpackie.Discord.Auth.Services;
using AspireDemo.Basic.ApiService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add Discord Auth (Basic)
builder.Services.AddDiscordAuth<DemoDiscordAuthHandler>(builder.Configuration);

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Basic API service is running.");

app.MapDiscordAuth();

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync("Identity.External");
    return Results.Redirect("/");
});

app.MapGet("/user", async (HttpContext context, [FromServices] IDiscordService discordService) =>
{
    var token = await context.GetTokenAsync("Identity.External", "access_token");
    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
    return Results.Ok(await discordService.GetUserAsync(token));
});

app.MapGet("/guilds", async (HttpContext context, [FromServices] IDiscordService discordService) =>
{
    var token = await context.GetTokenAsync("Identity.External", "access_token");
    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
    return Results.Ok(await discordService.GetGuildsAsync(token));
});

app.MapDefaultEndpoints();

app.Run();
