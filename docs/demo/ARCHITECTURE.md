# Architecture Overview

This document describes the architecture of the Aspire Demo project, which showcases how to integrate `Kerpackie.Discord.Auth` into a modern .NET application orchestrated by Aspire.

## System Components

The solution consists of three main projects:

1.  **AspireDemo.AppHost** (Orchestrator)
2.  **AspireDemo.ApiService** (Backend)
3.  **AspireDemo.Web** (Frontend)

```mermaid
graph TD
    User[User Browser] -->|HTTP :5173| Web[AspireDemo.Web (React)]
    Web -->|Proxy /api| API[AspireDemo.ApiService (.NET 10)]
    API -->|OAuth2| Discord[Discord API]
    
    subgraph Aspire Orchestration
        AppHost[AspireDemo.AppHost] -.->|Manages| Web
        AppHost -.->|Manages| API
    end
```

### 1. AspireDemo.AppHost
This is the entry point for the Aspire application. It defines the resources and their relationships.
- **Role**: Orchestrator.
- **Configuration**:
    - Defines `apiservice` (Backend) running on port 5001.
    - Defines `webfrontend` (Frontend) as an NPM app running the `dev` script.
    - Injects the `apiservice` URL into the frontend environment variables.

### 2. AspireDemo.ApiService
A .NET 10 Minimal API that handles authentication and data retrieval.
- **Role**: Backend API & Auth Provider.
- **Key Features**:
    - Uses `Kerpackie.Discord.Auth` for Discord OAuth2.
    - Implements `IDiscordAuthHandler` to map Discord user data to ASP.NET Core Claims.
    - Exposes endpoints for user info and guilds.
    - Uses `IDiscordService` to fetch data from Discord using the user's access token.

### 3. AspireDemo.Web
A React application built with Vite and TypeScript.
- **Role**: User Interface.
- **Key Features**:
    - Proxies `/api` requests to the backend.
    - Handles the login flow by redirecting to `/api/login-discord`.
    - Displays user profile and guilds using data fetched from the API.
