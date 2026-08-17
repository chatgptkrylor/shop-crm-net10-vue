using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ShopApi.Tests;

public class DashboardTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DashboardTests(TestWebApplicationFactory factory)
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
    public async Task Dashboard_Authed_Returns200WithData()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DashboardBody>();
        Assert.NotNull(body);
        Assert.True(body!.TotalCustomers >= 10);
        Assert.NotEmpty(body.StatusCounts);
        Assert.NotEmpty(body.RecentInteractions);
        Assert.Equal("admin", body.Username);
    }

    [Fact]
    public async Task Dashboard_Unauthed_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private class DashboardBody
    {
        public int TotalCustomers { get; set; }
        public List<StatusCount> StatusCounts { get; set; } = new();
        public List<RecentInteraction> RecentInteractions { get; set; } = new();
        public string Username { get; set; } = string.Empty;
    }

    private class StatusCount
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    private class RecentInteraction
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }
}