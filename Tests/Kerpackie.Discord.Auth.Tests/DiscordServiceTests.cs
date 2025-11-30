// csharp

using System.Net;
using System.Text.Json;
using Kerpackie.Discord.Auth.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Kerpackie.Discord.Auth.Tests;

public class DiscordServiceTests
{
    // A helper to create a mock HttpClient that returns what we want
    private IHttpClientFactory CreateMockHttpClientFactory(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://discord.com/api/")
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(x => x.CreateClient("DiscordApi")).Returns(client);

        return factoryMock.Object;
    }

    [Fact]
    public async Task GetGuilds_ReturnsJson_WhenApiSucceeds()
    {
        // Arrange
        var expectedJson = "[{\"id\": \"123\", \"name\": \"Test Guild\"}]";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedJson)
        };

        var factory = CreateMockHttpClientFactory(mockResponse);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new DiscordService(factory, memoryCache, NullLogger<DiscordService>.Instance);

        // Act
        var result = await service.GetGuildsJsonAsync("fake-token");

        // Assert
        Assert.Equal(expectedJson, result);
    }

    [Fact]
    public async Task ExecuteAsync_DeserializesGenericObject_Correctly()
    {
        // Arrange: We simulate a custom Discord endpoint returning a complex object
        var apiResponse = new { id = "999", type = "twitch", name = "MyConnection" };
        var jsonResponse = JsonSerializer.Serialize(apiResponse);

        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        var factory = CreateMockHttpClientFactory(mockResponse);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new DiscordService(factory, memoryCache, NullLogger<DiscordService>.Instance);

        // Act: Use a test helper to perform the request and deserialize the response
        var result = await ExecuteGenericFromFactory<TestConnectionModel>(
            factory,
            "token",
            HttpMethod.Get,
            "users/@me/connections");

        // Assert
        Assert.NotNull(result);
        Assert.Equal<string>("999", result.id);
        Assert.Equal<string>("twitch", result.type);
    }

    // Test helper: sends request using the IHttpClientFactory and deserializes the JSON
    private static async Task<T?> ExecuteGenericFromFactory<T>(IHttpClientFactory factory, string token, HttpMethod method, string path)
    {
        var client = factory.CreateClient("DiscordApi");
        using var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Helper class for the test
    public class TestConnectionModel { public string id { get; set; } public string type { get; set; } }
}
