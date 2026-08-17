using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApi.Infrastructure;
using ShopApi.Models;
using ShopApi.Repository;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InteractionsController : ControllerBase
{
    private readonly IInteractionRepository _repo;

    public InteractionsController(IInteractionRepository repo)
    {
        _repo = repo;
    }

    [HttpPost]
    [ServiceFilter(typeof(XRequestedWithFilter))]
    public async Task<IActionResult> Create([FromBody] CreateInteractionRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userIdStr = User.FindFirst("userId")?.Value;
        var username = User.FindFirst("username")?.Value;
        if (userIdStr == null || username == null) return Unauthorized();

        var interaction = new Interaction
        {
            CustomerId = request.CustomerId,
            Type = request.Type,
            Note = request.Note,
            LoggedByUserId = int.Parse(userIdStr),
            LoggedByUsername = username,
        };

        var id = await _repo.CreateAsync(interaction);
        interaction.Id = id;
        interaction.LoggedAt = DateTime.UtcNow;

        var dto = new InteractionDto
        {
            Id = interaction.Id,
            CustomerId = interaction.CustomerId,
            Type = interaction.Type,
            Note = interaction.Note,
            LoggedAt = interaction.LoggedAt,
            LoggedByUserId = interaction.LoggedByUserId,
            LoggedByUsername = interaction.LoggedByUsername,
        };
        return CreatedAtAction(nameof(Create), new { id }, dto);
    }

    [HttpGet("~/api/customers/{customerId}/interactions")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var interactions = await _repo.GetByCustomerIdAsync(customerId);
        var dtos = interactions.Select(i => new InteractionDto
        {
            Id = i.Id,
            CustomerId = i.CustomerId,
            Type = i.Type,
            Note = i.Note,
            LoggedAt = i.LoggedAt,
            LoggedByUserId = i.LoggedByUserId,
            LoggedByUsername = i.LoggedByUsername,
        }).ToList();
        return Ok(dtos);
    }
}
