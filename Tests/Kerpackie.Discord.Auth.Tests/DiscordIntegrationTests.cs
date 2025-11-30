using Kerpackie.Discord.Auth.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WireMock.ResponseBuilders;
using WireMock.RequestBuilders;
using WireMock.Server;

namespace Kerpackie.Discord.Auth.Tests;

public class DiscordIntegrationTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly string _baseUrl;

    public DiscordIntegrationTests()
    {
        // 1. Start a fake HTTP server on a random free port
        _server = WireMockServer.Start();
        _baseUrl = _server.Url!;
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    [Fact]
    public async Task Service_SendsCorrectHeaders_To_SimulatedDiscord()
    {
        // Arrange
        var testToken = "super-secret-access-token";

        // 2. Program the Fake Server to expect a specific request
        _server
            .Given(Request.Create()
                .WithPath("/users/@me/guilds")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {testToken}"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("[{\"name\": \"Real Integration Guild\"}]"));

        // 3. Configure the REAL DiscordService, but point it to the FAKE server
        var httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(x => x.CreateClient("DiscordApi")).Returns(httpClient);

        // Pass a NullLogger<DiscordService>.Instance to satisfy the new constructor dependency
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new DiscordService(factoryMock.Object, memoryCache, NullLogger<DiscordService>.Instance);

        // Act
        var result = await service.GetGuildsJsonAsync(testToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Real Integration Guild", result);

        // Double Check: Ask WireMock if it actually received the request
        var logs = _server.LogEntries;
        Assert.Single(logs);
    }
}
