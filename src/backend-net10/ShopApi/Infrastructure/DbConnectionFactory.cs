using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ShopApi.Infrastructure;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ShopCRM")
            ?? throw new InvalidOperationException("ShopCRM connection string not found");
    }

    public SqlConnection CreateConnection()
    {
        var conn = new SqlConnection(_connectionString);
        return conn;
    }
}

public static class DbConnectionExtensions
{
    public static IServiceCollection AddDbConnectionFactory(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        return services;
    }
}