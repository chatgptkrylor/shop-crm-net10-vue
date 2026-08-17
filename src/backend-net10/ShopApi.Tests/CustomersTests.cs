using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ShopApi.Tests;

public class CustomersTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CustomersTests(TestWebApplicationFactory factory)
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

    private async Task<HttpClient> GetAuthenticatedClientWithHeaderAsync()
    {
        var client = await GetAuthenticatedClientAsync();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        return client;
    }

    [Fact]
    public async Task ListCustomers_ReturnsPagedResult()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/customers?page=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResultBody>();
        Assert.NotNull(body);
        Assert.True(body!.TotalCount >= 10);
        Assert.NotEmpty(body.Items);
        Assert.Equal(1, body.Page);
        Assert.Equal(10, body.PageSize);
    }

    [Fact]
    public async Task CreateCustomer_Returns201()
    {
        var client = await GetAuthenticatedClientWithHeaderAsync();
        var dto = new
        {
            name = "Test Customer",
            email = "test@example.com",
            phone = "555-9999",
            company = "TestCo",
            status = "Lead",
        };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(dto),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/customers", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CustomerBody>();
        Assert.NotNull(body);
        Assert.True(body!.Id > 0);
        Assert.Equal("Test Customer", body.Name);
    }

    [Fact]
    public async Task GetCustomer_Returns200Or404()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/customers/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var notFound = await client.GetAsync("/api/customers/99999");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UpdateCustomer_Returns200()
    {
        var client = await GetAuthenticatedClientWithHeaderAsync();
        // First create
        var createDto = new { name = "Update Test", status = "Lead" };
        var createContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(createDto),
            Encoding.UTF8,
            "application/json");
        var createResponse = await client.PostAsync("/api/customers", createContent);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerBody>();

        // Then update
        var updateDto = new { id = created!.Id, name = "Updated Name", status = "Customer" };
        var updateContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(updateDto),
            Encoding.UTF8,
            "application/json");
        var updateResponse = await client.PutAsync($"/api/customers/{created.Id}", updateContent);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Verify
        var getResponse = await client.GetAsync($"/api/customers/{created.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<CustomerBody>();
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal("Customer", updated.Status);
    }

    [Fact]
    public async Task DeleteCustomer_Returns204Then404()
    {
        var client = await GetAuthenticatedClientWithHeaderAsync();
        // Create
        var createDto = new { name = "Delete Test", status = "Lead" };
        var createContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(createDto),
            Encoding.UTF8,
            "application/json");
        var createResponse = await client.PostAsync("/api/customers", createContent);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerBody>();

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/customers/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify gone
        var getResponse = await client.GetAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_Invalid_Returns400()
    {
        var client = await GetAuthenticatedClientWithHeaderAsync();
        var dto = new { name = "", status = "" }; // invalid
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(dto),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/customers", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private class PagedResultBody
    {
        public List<CustomerBody> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }

    private class CustomerBody
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}