using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace ShopApi.Tests;

public class ReportsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ReportsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var loginRequest = new { username = "admin", password = "Admin@123" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");
        content.Headers.Add("X-Requested-With", "XMLHttpRequest");
        var loginResponse = await client.PostAsync("/api/account/login", content);
        loginResponse.EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task Reports_Authed_Returns200WithData()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/reports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReportBody>();
        Assert.NotNull(body);
        Assert.True(body!.TotalCustomers >= 10);
        Assert.NotEmpty(body.StatusCounts);
    }

    [Fact]
    public async Task Reports_Unauthed_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/reports");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private class ReportBody
    {
        public List<StatusCount> StatusCounts { get; set; } = new();
        public int TotalCustomers { get; set; }
    }

    private class StatusCount
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
