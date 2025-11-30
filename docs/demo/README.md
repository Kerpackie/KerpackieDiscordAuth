# Kerpackie.Discord.Auth Aspire Demo

This project demonstrates how to use the `Kerpackie.Discord.Auth` library in a .NET Aspire application with a React frontend.

## Prerequisites

- .NET 10.0 SDK
- Node.js (LTS recommended)
- Docker (for Aspire orchestration, though not strictly required for this specific demo as it uses no containers)

## Project Structure

- **AspireDemo.AppHost**: The Aspire orchestrator project.
- **AspireDemo.ApiService**: A Minimal API backend using `Kerpackie.Discord.Auth`.
- **AspireDemo.Web**: A React (Vite) frontend.

## Configuration

1.  Open `AspireDemo.AppHost/appsettings.json` (or `UserSecrets`).
2.  Ensure you have your Discord Application credentials configured in the `Kerpackie.Discord.Auth` section (usually in the `ApiService` or shared configuration).

**Note**: The `ApiService` expects the following configuration:
```json
"Kerpackie.Discord.Auth": {
  "ClientId": "YOUR_CLIENT_ID",
  "ClientSecret": "YOUR_CLIENT_SECRET"
}
```

## Running the Demo

1.  Open the solution in your IDE (Rider/VS).
2.  Set `AspireDemo.AppHost` as the startup project.
3.  Run the project.
4.  The Aspire Dashboard will launch.
5.  Click the endpoint link for `webfrontend` to open the React app.
6.  Click "Login with Discord" to authenticate.

## Features

- **Discord Login**: Authenticates using OAuth2.
- **User Info**: Displays logged-in user's username, avatar, and ID.
- **Guilds**: Fetches and displays the user's guilds.
