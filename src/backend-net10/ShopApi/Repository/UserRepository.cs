using Microsoft.Data.SqlClient;
using ShopApi.Infrastructure;
using ShopApi.Models;

namespace ShopApi.Repository;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _factory;

    public UserRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT Id, Username, PasswordHash, Role, CreatedAt FROM dbo.Users WHERE Username = @Username", conn);
        cmd.Parameters.AddWithValue("@Username", username);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = (int)reader["Id"],
                Username = (string)reader["Username"],
                PasswordHash = (string)reader["PasswordHash"],
                Role = (string)reader["Role"],
                CreatedAt = (DateTime)reader["CreatedAt"],
            };
        }
        return null;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT Id, Username, PasswordHash, Role, CreatedAt FROM dbo.Users WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = (int)reader["Id"],
                Username = (string)reader["Username"],
                PasswordHash = (string)reader["PasswordHash"],
                Role = (string)reader["Role"],
                CreatedAt = (DateTime)reader["CreatedAt"],
            };
        }
        return null;
    }
}