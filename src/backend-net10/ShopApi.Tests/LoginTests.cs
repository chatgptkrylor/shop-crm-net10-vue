using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ShopApi.Infrastructure;
using ShopApi.Repository;
using Xunit;

namespace ShopApi.Tests;

public class LoginTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200AndSetsCookie()
    {
        var loginRequest = new { username = "admin", password = "Admin@123" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");
        content.Headers.Add("X-Requested-With", "XMLHttpRequest");

        var response = await _client.PostAsync("/api/account/login", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseBody>();
        Assert.NotNull(body);
        Assert.Equal("admin", body!.Username);
        Assert.Equal("Admin", body.Role);
        Assert.True(response.Headers.Contains("Set-Cookie"));
        Assert.Contains("shopcrm_token", string.Join(";", response.Headers.GetValues("Set-Cookie")));
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var loginRequest = new { username = "admin", password = "wrongpassword" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");
        content.Headers.Add("X-Requested-With", "XMLHttpRequest");

        var response = await _client.PostAsync("/api/account/login", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingXRequestedWith_Returns400()
    {
        var loginRequest = new { username = "admin", password = "Admin@123" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/account/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private class LoginResponseBody
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}

public class AuthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
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
        // Extract cookie from response and add it manually as a header
        if (loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var tokenCookie = cookies.FirstOrDefault();
            if (tokenCookie != null)
            {
                var tokenPart = tokenCookie.Split(';')[0];
                client.DefaultRequestHeaders.Add("Cookie", tokenPart);
            }
        }
        return client;
    }

    [Fact]
    public async Task Me_WithoutCookie_Returns401()
    {
        var response = await _client.GetAsync("/api/account/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithCookie_Returns200AndUser()
    {
        var authedClient = await GetAuthenticatedClientAsync();
        var response = await authedClient.GetAsync("/api/account/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MeResponseBody>();
        Assert.NotNull(body);
        Assert.Equal("admin", body!.Username);
        Assert.Equal("Admin", body.Role);
    }

    private class MeResponseBody
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}