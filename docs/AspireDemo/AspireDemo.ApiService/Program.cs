using Kerpackie.Discord.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using AspNet.Security.OAuth.Discord;

using Kerpackie.Discord.Auth.Interfaces;
using Kerpackie.Discord.Auth.Services;
using Kerpackie.Discord.Auth.Extensions;
using Kerpackie.Discord.Auth.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AspireDemo.ApiService.Data;
using AspireDemo.ApiService.Models;
using Kerpackie.Discord.Auth.Identity;
using Kerpackie.Discord.Auth.OpenTelemetry;
using OpenTelemetry.Trace;
using AspireDemo.ApiService;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("AppDb"));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add Discord Auth with Identity
builder.Services.AddDiscordAuthWithIdentity<ApplicationUser>(builder.Configuration);

// Add OpenTelemetry Instrumentation
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddDiscordAuthInstrumentation());

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

app.MapDiscordAuth();

app.MapGet("/logout", async (HttpContext context, SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
});

app.MapGet("/user", async (HttpContext context, [FromServices] IDiscordService discordService, [FromServices] UserManager<ApplicationUser> userManager) =>
{
    var user = await userManager.GetUserAsync(context.User);
    if (user == null) return Results.Unauthorized();

    var token = await userManager.GetAuthenticationTokenAsync(user, "Discord", "access_token");
    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
    
    return Results.Ok(await discordService.GetUserAsync(token));
});

app.MapGet("/guilds", async (HttpContext context, [FromServices] IDiscordService discordService, [FromServices] UserManager<ApplicationUser> userManager) =>
{
    var user = await userManager.GetUserAsync(context.User);
    if (user == null) return Results.Unauthorized();

    var token = await userManager.GetAuthenticationTokenAsync(user, "Discord", "access_token");
    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

    return Results.Ok(await discordService.GetGuildsAsync(token));
});

app.MapDefaultEndpoints();

app.Run();

