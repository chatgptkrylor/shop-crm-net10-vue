using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace ShopApi.Tests;

public class InteractionsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public InteractionsTests(TestWebApplicationFactory factory)
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
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        return client;
    }

    [Fact]
    public async Task CreateInteraction_Returns201WithLoggedByUsername()
    {
        var client = await GetAuthenticatedClientAsync();
        var request = new { customerId = 2, type = "Call", note = "Test interaction note" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/interactions", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<InteractionBody>();
        Assert.NotNull(body);
        Assert.True(body!.Id > 0);
        Assert.Equal("admin", body.LoggedByUsername);
        Assert.Equal("Call", body.Type);
    }

    [Fact]
    public async Task GetByCustomer_ReturnsInteractions()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/customers/1/interactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<InteractionBody>>();
        Assert.NotNull(body);
        Assert.NotEmpty(body!);
    }

    [Fact]
    public async Task CreateInteraction_Invalid_Returns400()
    {
        var client = await GetAuthenticatedClientAsync();
        var request = new { customerId = 1, type = "", note = "" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/interactions", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private class InteractionBody
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string LoggedByUsername { get; set; } = string.Empty;
    }
}
