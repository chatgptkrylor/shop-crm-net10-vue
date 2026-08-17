using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApi.Models;
using ShopApi.Repository;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _repo;
    private const int PageSize = 10;

    public CustomersController(ICustomerRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] int page = 1)
    {
        var customers = await _repo.GetAllAsync(page, PageSize);
        var total = await _repo.GetTotalCountAsync();
        var totalPages = (int)Math.Ceiling((double)total / PageSize);

        var items = customers.Select(c => new CustomerDto
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Phone = c.Phone,
            Company = c.Company,
            Status = c.Status,
        }).ToList();

        return Ok(new PagedResult<CustomerDto>
        {
            Items = items,
            Page = page,
            PageSize = PageSize,
            TotalPages = totalPages,
            TotalCount = total,
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _repo.GetByIdAsync(id);
        if (c == null) return NotFound();

        var dto = new CustomerDto
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Phone = c.Phone,
            Company = c.Company,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
        };
        return Ok(dto);
    }

    [HttpPost]
    [ServiceFilter(typeof(Infrastructure.XRequestedWithFilter))]
    public async Task<IActionResult> Create([FromBody] CustomerDto model)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userIdStr = User.FindFirst("userId")?.Value;
        if (userIdStr == null) return Unauthorized();
        var userId = int.Parse(userIdStr);

        var customer = new Customer
        {
            Name = model.Name,
            Email = model.Email,
            Phone = model.Phone,
            Company = model.Company,
            Status = model.Status,
            CreatedByUserId = userId,
        };
        var id = await _repo.CreateAsync(customer);
        model.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, model);
    }

    [HttpPut("{id}")]
    [ServiceFilter(typeof(Infrastructure.XRequestedWithFilter))]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerDto model)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.Name = model.Name;
        existing.Email = model.Email;
        existing.Phone = model.Phone;
        existing.Company = model.Company;
        existing.Status = model.Status;

        await _repo.UpdateAsync(existing);
        return Ok(model);
    }

    [HttpDelete("{id}")]
    [ServiceFilter(typeof(Infrastructure.XRequestedWithFilter))]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await _repo.DeleteAsync(id);
        return NoContent();
    }
}