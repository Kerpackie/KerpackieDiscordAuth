# Kerpackie.Discord.Auth

[![NuGet](https://img.shields.io/nuget/v/Kerpackie.Discord.Auth.svg)](https://www.nuget.org/packages/Kerpackie.Discord.Auth)
[![Build Status](https://github.com/kerpackie/KerpackieDiscordAuth/actions/workflows/dotnet.yml/badge.svg)](https://github.com/kerpackie/KerpackieDiscordAuth/actions)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**Kerpackie.Discord.Auth** is a robust, developer-friendly library for integrating Discord OAuth2 authentication into your ASP.NET Core applications. Whether you need simple "Login with Discord" functionality or deep integration with ASP.NET Core Identity, this library has you covered.

## ✨ Features

- **🚀 Seamless Integration**: Get up and running with just a few lines of code.
- **🔐 Identity Support**: Full compatibility with ASP.NET Core Identity for user persistence and management.
- **📊 OpenTelemetry**: Built-in instrumentation for tracing authentication flows and diagnosing issues.
- **🛠️ Flexible Configuration**: Customize redirect paths, scopes, and claim mapping to fit your needs.
- **📦 Modular Design**: Use only what you need—core auth, identity integration, or observability.

## 📦 Installation

Install the packages via NuGet:

```bash
# Core Library
dotnet add package Kerpackie.Discord.Auth

# Identity Integration (Optional)
dotnet add package Kerpackie.Discord.Auth.Identity

# OpenTelemetry Instrumentation (Optional)
dotnet add package Kerpackie.Discord.Auth.OpenTelemetry
```

## 🚀 Quick Start

### Scenario A: Basic Login (No Database)

Ideal for simple apps or tools where you just need to verify a Discord user.

```csharp
// Program.cs
using Kerpackie.Discord.Auth.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Discord Auth
builder.Services.AddDiscordAuth<MyAuthHandler>(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// 2. Map Endpoints (/auth/discord/login, /auth/discord/callback)
app.MapDiscordAuth();

app.Run();
```

**Implement the Handler:**

```csharp
public class MyAuthHandler : IDiscordAuthHandler
{
    public Task<ClaimsPrincipal> OnDiscordLoginAsync(DiscordLoginContext context)
    {
        var identity = new ClaimsIdentity("Cookies");
        identity.AddClaim(new Claim(ClaimTypes.Name, context.Username));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, context.DiscordId));
        return Task.FromResult(new ClaimsPrincipal(identity));
    }
}
```

### Scenario B: ASP.NET Core Identity

Perfect for full-stack applications requiring user accounts and database persistence.

```csharp
// Program.cs
using Kerpackie.Discord.Auth.Identity;

var builder = WebApplication.CreateBuilder(args);

// 1. Setup Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 2. Add Discord Auth with Identity
builder.Services.AddDiscordAuthWithIdentity<ApplicationUser>(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapDiscordAuth();

app.Run();
```

## ⚙️ Configuration

Add your Discord credentials to `appsettings.json`:

```json
{
  "Kerpackie.Discord.Auth": {
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "LoginPath": "/auth/discord/login",
    "CallbackPath": "/auth/discord/callback"
  }
}
```

## 📊 Observability

Enable OpenTelemetry tracing to monitor your authentication flows:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => 
    {
        tracing.AddDiscordAuthInstrumentation();
    });
```

## 📚 Documentation

For detailed guides, advanced configuration, and examples, check out the [Documentation](docs/README.md).

## 📄 License

Licensed under the [MIT License](LICENSE).
