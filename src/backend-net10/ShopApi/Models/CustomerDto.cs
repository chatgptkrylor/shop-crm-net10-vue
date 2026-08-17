using System.ComponentModel.DataAnnotations;

namespace ShopApi.Models;

public class CustomerDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? Email { get; set; }

    [StringLength(30, ErrorMessage = "Phone cannot exceed 30 characters")]
    public string? Phone { get; set; }

    [StringLength(100, ErrorMessage = "Company cannot exceed 100 characters")]
    public string? Company { get; set; }

    [Required(ErrorMessage = "Status is required")]
    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class CustomerDetailDto
{
    public CustomerDto Customer { get; set; } = new();
    public List<InteractionDto> Interactions { get; set; } = new();
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}