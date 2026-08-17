using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApi.Models;
using ShopApi.Repository;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ICustomerRepository _customerRepo;
    private readonly IInteractionRepository _interactionRepo;

    public DashboardController(ICustomerRepository customerRepo, IInteractionRepository interactionRepo)
    {
        _customerRepo = customerRepo;
        _interactionRepo = interactionRepo;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var totalCustomers = await _customerRepo.GetTotalCountAsync();
        var statusCounts = await _customerRepo.GetCountByStatusAsync();
        var recentInteractions = await _interactionRepo.GetRecentAsync(5);
        var username = User.FindFirst("username")?.Value ?? string.Empty;

        var dto = new DashboardDto
        {
            TotalCustomers = totalCustomers,
            StatusCounts = statusCounts.Select(s => new StatusCountDto { Status = s.Status, Count = s.Count }).ToList(),
            RecentInteractions = recentInteractions.Select(i => new InteractionDto
            {
                Id = i.Id,
                CustomerId = i.CustomerId,
                Type = i.Type,
                Note = i.Note,
                LoggedAt = i.LoggedAt,
                LoggedByUserId = i.LoggedByUserId,
                LoggedByUsername = i.LoggedByUsername,
            }).ToList(),
            Username = username,
        };

        return Ok(dto);
    }
}