# Integration Guide

This guide provides step-by-step instructions for integrating the `Kerpackie.Discord.Auth` library into your ASP.NET Core application.

## Prerequisites

1.  **Add the Package**: Ensure your project references the `Kerpackie.Discord.Auth` library.
    ```xml
    <ProjectReference Include="..\..\src\Kerpackie.Discord.Auth\Kerpackie.Discord.Auth.csproj" />
    ```

2.  **Configure Credentials**: Add your Discord Application credentials to `appsettings.json` (or User Secrets).
    ```json
    "Kerpackie.Discord.Auth": {
      "ClientId": "YOUR_CLIENT_ID",
      "ClientSecret": "YOUR_CLIENT_SECRET"
    }
    ```

---

## Scenario A: Basic Integration (No Identity)

Use this approach for simple applications that do not require a user database or ASP.NET Core Identity.

### Step 1: Create an Auth Handler
Create a class that implements `IDiscordAuthHandler`. This maps the Discord user to a `ClaimsPrincipal`.

```csharp
public class MyDiscordAuthHandler : IDiscordAuthHandler
{
    public Task<ClaimsPrincipal> OnDiscordLoginAsync(DiscordLoginContext context)
    {
        var identity = new ClaimsIdentity("Identity.External");
        
        // Add claims from Discord context
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

### Step 2: Register Services
In `Program.cs`, register the handler and authorization services.

```csharp
using Kerpackie.Discord.Auth.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register Discord Auth with your handler
builder.Services.AddDiscordAuth<MyDiscordAuthHandler>(builder.Configuration);

builder.Services.AddAuthorization();
```

### Step 3: Configure Middleware & Endpoints
In `Program.cs`, set up the pipeline.

```csharp
using Kerpackie.Discord.Auth.Endpoints;

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Registers endpoints: /auth/discord/login and /auth/discord/callback
app.MapDiscordAuth();

app.Run();
```

---

## Scenario B: Integration with ASP.NET Core Identity

Use this approach if you are using ASP.NET Core Identity (e.g., with Entity Framework Core) and want to persist users in a database.

### Step 1: Add Identity Dependency
Reference the Identity integration library.
```xml
<ProjectReference Include="..\..\src\Kerpackie.Discord.Auth.Identity\Kerpackie.Discord.Auth.Identity.csproj" />
```

### Step 2: Configure Identity & Auth
In `Program.cs`, configure your DbContext, Identity, and then the Discord integration.

```csharp
using Kerpackie.Discord.Auth.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure EF Core (example using InMemory)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("AppDb"));

// 2. Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. Register Discord Auth with Identity Integration
// This automatically handles user creation and linking.
builder.Services.AddDiscordAuthWithIdentity<ApplicationUser>(builder.Configuration);

builder.Services.AddAuthorization();
```

### Step 3: Configure Middleware & Endpoints
In `Program.cs`, set up the pipeline.

```csharp
using Kerpackie.Discord.Auth.Endpoints;

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDiscordAuth();

app.Run();
```

### Step 4: Accessing Tokens
To access the Discord Access Token (e.g., to call the API), retrieve it from the `UserManager`.

```csharp
app.MapGet("/user", async (HttpContext context, 
    [FromServices] IDiscordService discordService,
    [FromServices] UserManager<ApplicationUser> userManager) =>
{
    var user = await userManager.GetUserAsync(context.User);
    if (user == null) return Results.Unauthorized();

    // Retrieve the token stored by the library
    var token = await userManager.GetAuthenticationTokenAsync(user, "Discord", "access_token");
    
    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
    
    return Results.Ok(await discordService.GetUserAsync(token));
});
```

---

## Advanced Topics

### Handling Redirects

After a successful login, the user needs to be redirected back to your application (e.g., a dashboard or the page they came from).

#### Option 1: Client-Side Control (Recommended for SPAs)
The frontend can specify the return URL by passing a `returnUrl` query parameter to the login endpoint.

**Example (React):**
```typescript
const handleLogin = () => {
  // Redirect to backend login, instructing it to return to the current page or a specific route
  window.location.href = `/api/auth/discord/login?returnUrl=${encodeURIComponent(window.location.href)}`;
};
```

#### Option 2: Server-Side Default
If no `returnUrl` is provided, the library defaults to the root path (`/`). You can customize this default behavior by configuring the `DiscordAuthSettings` or by manually handling the login endpoint mapping if you need complex logic.

### Configuring DiscordAuthSettings

You can customize the library's behavior by adding properties to the `Kerpackie.Discord.Auth` section in `appsettings.json`.

```json
"Kerpackie.Discord.Auth": {
  "ClientId": "YOUR_CLIENT_ID",
  "ClientSecret": "YOUR_CLIENT_SECRET",
  "LoginPath": "/auth/discord/login",      // Default: /auth/discord/login
  "CallbackPath": "/auth/discord/callback", // Default: /auth/discord/callback
  "DefaultReturnUrl": "/dashboard"          // Default: /
}
```

- **LoginPath**: The endpoint where the login flow starts.
- **CallbackPath**: The endpoint Discord redirects back to. **Must match** the Redirect URI configured in the Discord Developer Portal.
- **DefaultReturnUrl**: Where to redirect the user after login if no `returnUrl` query parameter is provided.
    - **Relative URL** (e.g., `/dashboard`): Redirects to the **Backend** (API) domain.
    - **Absolute URL** (e.g., `http://localhost:5173/dashboard`): Redirects to that specific URL (use this if your Frontend is on a different port/domain).

### Organizing Endpoints

For cleaner code, especially in larger projects, avoid defining all endpoints in `Program.cs`. You can organize them into dedicated static classes.

**Example: `UserEndpoints.cs`**
```csharp
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/user").RequireAuthorization();

        group.MapGet("/", GetUser);
        group.MapGet("/guilds", GetGuilds);
    }

    private static async Task<IResult> GetUser(
        HttpContext context,
        [FromServices] IDiscordService discordService,
        [FromServices] UserManager<ApplicationUser> userManager)
    {
        var token = await GetTokenAsync(context, userManager);
        if (token == null) return Results.Unauthorized();
        return Results.Ok(await discordService.GetUserAsync(token));
    }

    private static async Task<IResult> GetGuilds(
        HttpContext context,
        [FromServices] IDiscordService discordService,
        [FromServices] UserManager<ApplicationUser> userManager)
    {
        var token = await GetTokenAsync(context, userManager);
        if (token == null) return Results.Unauthorized();
        return Results.Ok(await discordService.GetGuildsAsync(token));
    }

    private static async Task<string?> GetTokenAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(context.User);
        if (user == null) return null;
        return await userManager.GetAuthenticationTokenAsync(user, "Discord", "access_token");
    }
}
```

**Usage in `Program.cs`:**
```csharp
app.MapUserEndpoints();
```

---

## Adding OpenTelemetry

To enable tracing for Discord authentication flows (works with both scenarios):

### Step 1: Add Dependency
Reference the OpenTelemetry integration library.
```xml
<ProjectReference Include="..\..\src\Kerpackie.Discord.Auth.OpenTelemetry\Kerpackie.Discord.Auth.OpenTelemetry.csproj" />
```

### Step 2: Configure Tracing
In `Program.cs`, add the instrumentation to your OpenTelemetry setup.

```csharp
using Kerpackie.Discord.Auth.OpenTelemetry;
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => 
    {
        // Add Discord Auth instrumentation
        tracing.AddDiscordAuthInstrumentation();
        
        // Add other instrumentation (e.g., ASP.NET Core, HttpClient)
        tracing.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation();
    });
```
