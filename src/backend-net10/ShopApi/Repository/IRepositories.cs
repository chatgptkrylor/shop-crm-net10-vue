using ShopApi.Models;

namespace ShopApi.Repository;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
}

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<int> CreateAsync(Customer customer);
    Task<bool> UpdateAsync(Customer customer);
    Task<bool> DeleteAsync(int id);
    Task<List<StatusCount>> GetCountByStatusAsync();
}

public interface IInteractionRepository
{
    Task<List<Interaction>> GetByCustomerIdAsync(int customerId);
    Task<List<Interaction>> GetRecentAsync(int count);
    Task<int> CreateAsync(Interaction interaction);
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedByUserId { get; set; }
}

public class Interaction
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime LoggedAt { get; set; }
    public int LoggedByUserId { get; set; }
    public string LoggedByUsername { get; set; } = string.Empty;
}

public class StatusCount
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}