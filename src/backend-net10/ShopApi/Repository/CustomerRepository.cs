using Microsoft.Data.SqlClient;
using ShopApi.Infrastructure;
using ShopApi.Models;

namespace ShopApi.Repository;

public class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnectionFactory _factory;

    public CustomerRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Customer>> GetAllAsync(int page, int pageSize)
    {
        var customers = new List<Customer>();
        int offset = (page - 1) * pageSize;

        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT Id, Name, Email, Phone, Company, Status, CreatedAt, UpdatedAt, CreatedByUserId " +
            "FROM dbo.Customers ORDER BY Id OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", conn);
        cmd.Parameters.AddWithValue("@Offset", offset);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            customers.Add(MapCustomer(reader));
        }
        return customers;
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Customers", conn);
        return (int)await cmd.ExecuteScalarAsync()!;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT Id, Name, Email, Phone, Company, Status, CreatedAt, UpdatedAt, CreatedByUserId " +
            "FROM dbo.Customers WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapCustomer(reader);
        }
        return null;
    }

    public async Task<int> CreateAsync(Customer customer)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "INSERT INTO dbo.Customers (Name, Email, Phone, Company, Status, CreatedByUserId) " +
            "VALUES (@Name, @Email, @Phone, @Company, @Status, @CreatedByUserId); " +
            "SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
        cmd.Parameters.AddWithValue("@Name", customer.Name);
        cmd.Parameters.AddWithValue("@Email", (object?)customer.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Phone", (object?)customer.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Company", (object?)customer.Company ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", customer.Status);
        cmd.Parameters.AddWithValue("@CreatedByUserId", customer.CreatedByUserId);
        return (int)await cmd.ExecuteScalarAsync()!;
    }

    public async Task<bool> UpdateAsync(Customer customer)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "UPDATE dbo.Customers SET Name = @Name, Email = @Email, Phone = @Phone, " +
            "Company = @Company, Status = @Status, UpdatedAt = SYSUTCDATETIME() " +
            "WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", customer.Id);
        cmd.Parameters.AddWithValue("@Name", customer.Name);
        cmd.Parameters.AddWithValue("@Email", (object?)customer.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Phone", (object?)customer.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Company", (object?)customer.Company ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", customer.Status);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("DELETE FROM dbo.Customers WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<StatusCount>> GetCountByStatusAsync()
    {
        var counts = new List<StatusCount>();
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT Status, COUNT(*) AS Count FROM dbo.Customers GROUP BY Status ORDER BY Status", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            counts.Add(new StatusCount
            {
                Status = (string)reader["Status"],
                Count = (int)reader["Count"],
            });
        }
        return counts;
    }

    private static Customer MapCustomer(SqlDataReader reader)
    {
        return new Customer
        {
            Id = (int)reader["Id"],
            Name = (string)reader["Name"],
            Email = reader["Email"] as string,
            Phone = reader["Phone"] as string,
            Company = reader["Company"] as string,
            Status = (string)reader["Status"],
            CreatedAt = (DateTime)reader["CreatedAt"],
            UpdatedAt = reader["UpdatedAt"] as DateTime?,
            CreatedByUserId = (int)reader["CreatedByUserId"],
        };
    }
}