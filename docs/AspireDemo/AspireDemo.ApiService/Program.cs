using Kerpackie.Discord.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using AspNet.Security.OAuth.Discord;

using Kerpackie.Discord.Auth.Interfaces;
using Kerpackie.Discord.Auth.Interfaces;
using Kerpackie.Discord.Auth.Services;
using Kerpackie.Discord.Auth.Extensions;
using AspireDemo.ApiService;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add Discord Auth
builder.Services.AddDiscordAuth<DemoDiscordAuthHandler>(builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "API service is running.");

app.MapGet("/login-discord", async (HttpContext context, string returnUrl = "/") =>
{
    await context.ChallengeAsync(DiscordAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = returnUrl
    });
});

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

