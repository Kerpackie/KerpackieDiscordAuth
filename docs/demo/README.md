# Kerpackie.Discord.Auth Demos

This folder contains demo applications showcasing the `Kerpackie.Discord.Auth` library.

## Available Demos

### 1. Full Integration Demo (`AspireDemo`)
A complete example demonstrating:
- **ASP.NET Core Identity**: User persistence using Entity Framework Core (InMemory).
- **OpenTelemetry**: Distributed tracing for authentication flows.
- **React Frontend**: A modern UI to display user info and guilds.
- **Aspire Orchestration**: Manages the API and Frontend.

**Location**: `docs/AspireDemo/AspireDemo.AppHost`

### 2. Basic Demo (`AspireDemo.Basic`)
A simplified example demonstrating:
- **Basic Authentication**: Simple in-memory handler without Identity.
- **Minimal Setup**: The bare minimum required to get Discord Auth working.
- **React Frontend**: Reuses the same UI.

**Location**: `docs/AspireDemo.Basic/AspireDemo.Basic.AppHost`

## Getting Started

1.  **Prerequisites**:
    - .NET 10.0 SDK
    - Node.js (LTS)
    - Docker (optional, for Aspire)

2.  **Configuration**:
    Ensure `appsettings.json` (or User Secrets) in the AppHost project contains your Discord credentials:
    ```json
    "Kerpackie.Discord.Auth": {
      "ClientId": "YOUR_CLIENT_ID",
      "ClientSecret": "YOUR_CLIENT_SECRET"
    }
    ```

3.  **Running**:
    - Open the solution in your IDE.
    - Set the desired AppHost project as the startup project.
    - Run the application.
    - Use the Aspire Dashboard to launch the `webfrontend`.

## Documentation

- [**Integration Guide**](INTEGRATION_GUIDE.md): Step-by-step instructions for adding Discord Auth to your own project.
- [**Architecture**](ARCHITECTURE.md): High-level overview of the demo architecture.
