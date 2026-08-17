using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApi.Models;
using ShopApi.Repository;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ICustomerRepository _repo;

    public ReportsController(ICustomerRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var statusCounts = await _repo.GetCountByStatusAsync();
        var total = await _repo.GetTotalCountAsync();

        var dto = new ReportDto
        {
            StatusCounts = statusCounts.Select(s => new StatusCountDto
            {
                Status = s.Status,
                Count = s.Count,
            }).ToList(),
            TotalCustomers = total,
        };

        return Ok(dto);
    }
}
