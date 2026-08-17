using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ShopApi.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ShopCRM"] = "Server=192.168.122.226,1433;Database=ShopCRM;User Id=shopcrm;Password=ShopCrm_Win_Pwd_2026!;TrustServerCertificate=True",
                ["Jwt:Key"] = "test-jwt-key-for-integration-tests-at-least-32-chars!!",
                ["Jwt:Issuer"] = "ShopApi",
                ["Jwt:Audience"] = "ShopApi",
                ["Jwt:ExpiryMinutes"] = "20",
            });
        });
    }
}