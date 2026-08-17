using System.ComponentModel.DataAnnotations;

namespace ShopApi.Models;

public class DashboardDto
{
    public int TotalCustomers { get; set; }
    public List<StatusCountDto> StatusCounts { get; set; } = new();
    public List<InteractionDto> RecentInteractions { get; set; } = new();
    public string Username { get; set; } = string.Empty;
}

public class StatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class InteractionDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime LoggedAt { get; set; }
    public int LoggedByUserId { get; set; }
    public string LoggedByUsername { get; set; } = string.Empty;
}

public class CreateInteractionRequest
{
    [Required(ErrorMessage = "CustomerId is required")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Type is required")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "Note is required")]
    [MinLength(1, ErrorMessage = "Note cannot be empty")]
    public string Note { get; set; } = string.Empty;
}

public class ReportDto
{
    public List<StatusCountDto> StatusCounts { get; set; } = new();
    public int TotalCustomers { get; set; }
}