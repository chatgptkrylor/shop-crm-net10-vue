using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApi.Infrastructure;
using ShopApi.Models;
using ShopApi.Repository;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepo;
    private readonly int _timeoutMinutes;

    public AccountController(
        IUserRepository userRepository,
        ISessionRepository sessionRepo,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _sessionRepo = sessionRepo;
        _timeoutMinutes = int.TryParse(configuration["Session:TimeoutMinutes"], out var t) ? t : 20;
    }

    [HttpPost("login")]
    [ServiceFilter(typeof(XRequestedWithFilter))]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Status = 401,
                Detail = "Invalid username or password",
            });
        }

        var token = Guid.NewGuid().ToString("N");
        var session = new Session
        {
            Id = token,
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_timeoutMinutes),
        };
        await _sessionRepo.CreateAsync(session);
        Response.SetAuthCookie(token, Request.IsHttps, _timeoutMinutes);

        return Ok(new LoginResponse { Username = user.Username, Role = user.Role });
    }

    [HttpPost("logout")]
    [Authorize]
    [ServiceFilter(typeof(XRequestedWithFilter))]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies[AuthExtensions.CookieName];
        if (token != null)
            await _sessionRepo.DeleteAsync(token);
        Response.ClearAuthCookie();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst("userId")?.Value;
        var username = User.FindFirst("username")?.Value;
        var role = User.FindFirst("role")?.Value;

        if (userId == null || username == null || role == null)
            return Unauthorized();

        return Ok(new UserDto
        {
            UserId = int.Parse(userId),
            Username = username,
            Role = role,
        });
    }
}
