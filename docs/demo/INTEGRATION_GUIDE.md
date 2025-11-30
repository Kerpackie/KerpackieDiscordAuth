# Integration Guide

This guide details how to integrate the `Kerpackie.Discord.Auth` library into your ASP.NET Core application.

## Prerequisites

Ensure your project references the `Kerpackie.Discord.Auth` library.

```xml
<ProjectReference Include="..\..\src\Kerpackie.Discord.Auth\Kerpackie.Discord.Auth.csproj" />
```

## Configuration

Ensure your `appsettings.json` (or User Secrets) contains the Discord credentials:

```json
"Kerpackie.Discord.Auth": {
  "ClientId": "YOUR_CLIENT_ID",
  "ClientSecret": "YOUR_CLIENT_SECRET"
}
```

---

## Option 1: Basic Integration (No Identity)

Use this option if you want simple Discord authentication without ASP.NET Core Identity's user management database.

### 1. Implement the Handler

Create a class that implements `IDiscordAuthHandler`. This handler is called after a successful Discord login to create a `ClaimsPrincipal`.

```csharp
public class MyDiscordAuthHandler : IDiscordAuthHandler
{
    public Task<ClaimsPrincipal> OnDiscordLoginAsync(DiscordLoginContext context)
    {
        var identity = new ClaimsIdentity("Identity.External");
        
        // Map Discord properties to Claims
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, context.DiscordId));
        identity.AddClaim(new Claim(ClaimTypes.Name, context.Username));
        
        if (!string.IsNullOrEmpty(context.AvatarUrl))
        {
            identity.AddClaim(new Claim("urn:discord:avatar:url", context.AvatarUrl));
        }

        return Task.FromResult(new ClaimsPrincipal(identity));
    }
}
```

### 2. Register Services

In `Program.cs`:

```csharp
using Kerpackie.Discord.Auth.Extensions;

builder.Services.AddDiscordAuth<MyDiscordAuthHandler>(builder.Configuration);
builder.Services.AddAuthorization();
```

### 3. Configure Middleware & Endpoints

```csharp
using Kerpackie.Discord.Auth.Endpoints;

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Registers /auth/discord/login and /auth/discord/callback
app.MapDiscordAuth();

app.Run();
```

---

## Option 2: Integration with ASP.NET Core Identity

Use this option if you are using ASP.NET Core Identity (e.g., with Entity Framework Core) and want to link Discord logins to your Identity users.

### 1. Add Dependencies

Reference `Kerpackie.Discord.Auth.Identity`.

```xml
<ProjectReference Include="..\..\src\Kerpackie.Discord.Auth.Identity\Kerpackie.Discord.Auth.Identity.csproj" />
```

### 2. Register Services

In `Program.cs`, configure Identity and then use `AddDiscordAuthWithIdentity`.

```csharp
using Kerpackie.Discord.Auth.Identity;

// 1. Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("AppDb"));

// 2. Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. Add Discord Auth with Identity Integration
// This automatically handles user creation/linking based on the Discord login.
builder.Services.AddDiscordAuthWithIdentity<ApplicationUser>(builder.Configuration);

builder.Services.AddAuthorization();
```

### 3. Configure Middleware & Endpoints

```csharp
using Kerpackie.Discord.Auth.Endpoints;

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDiscordAuth();

app.Run();
```

---

## OpenTelemetry Integration

To enable tracing for Discord authentication flows:

1.  Reference `Kerpackie.Discord.Auth.OpenTelemetry`.
2.  Configure OpenTelemetry in `Program.cs`:

```csharp
using Kerpackie.Discord.Auth.OpenTelemetry;
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => 
    {
        tracing.AddDiscordAuthInstrumentation();
        // ... other instrumentation
    });
```

## Using IDiscordService

Regardless of the integration method, you can inject `IDiscordService` to interact with the Discord API using the user's access token.

```csharp
app.MapGet("/user", async (HttpContext context, [FromServices] IDiscordService discordService) =>
{
    // Retrieve the access token stored by the library
    var token = await context.GetTokenAsync("Identity.External", "access_token");
    
    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
    
    // Fetch user data
    var user = await discordService.GetUserAsync(token);
    return Results.Ok(user);
});
```
