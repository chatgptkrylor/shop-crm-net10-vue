using Microsoft.Data.SqlClient;
using ShopApi.Infrastructure;
using ShopApi.Models;

namespace ShopApi.Repository;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(string id);
    Task CreateAsync(Session session);
    Task ExtendAsync(string id, DateTime newExpiresAt);
    Task DeleteAsync(string id);
}

public class SessionRepository : ISessionRepository
{
    private readonly IDbConnectionFactory _factory;

    public SessionRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Session?> GetByIdAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT Id, UserId, Username, Role, CreatedAt, ExpiresAt " +
            "FROM dbo.Sessions WHERE Id = @Id AND ExpiresAt > SYSUTCDATETIME()", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new Session
        {
            Id = (string)reader["Id"],
            UserId = (int)reader["UserId"],
            Username = (string)reader["Username"],
            Role = (string)reader["Role"],
            CreatedAt = (DateTime)reader["CreatedAt"],
            ExpiresAt = (DateTime)reader["ExpiresAt"],
        };
    }

    public async Task CreateAsync(Session session)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "INSERT INTO dbo.Sessions (Id, UserId, Username, Role, CreatedAt, ExpiresAt) " +
            "VALUES (@Id, @UserId, @Username, @Role, @CreatedAt, @ExpiresAt)", conn);
        cmd.Parameters.AddWithValue("@Id", session.Id);
        cmd.Parameters.AddWithValue("@UserId", session.UserId);
        cmd.Parameters.AddWithValue("@Username", session.Username);
        cmd.Parameters.AddWithValue("@Role", session.Role);
        cmd.Parameters.AddWithValue("@CreatedAt", session.CreatedAt);
        cmd.Parameters.AddWithValue("@ExpiresAt", session.ExpiresAt);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ExtendAsync(string id, DateTime newExpiresAt)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "UPDATE dbo.Sessions SET ExpiresAt = @ExpiresAt WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@ExpiresAt", newExpiresAt);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("DELETE FROM dbo.Sessions WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}
