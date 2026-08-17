using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ShopApi.Tests;

public class HealthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_Returns200_AndHealthyStatus()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("healthy", body!.Status);
    }

    private class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
    }
}