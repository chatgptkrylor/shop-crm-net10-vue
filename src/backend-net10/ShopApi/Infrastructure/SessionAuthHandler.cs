using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopApi.Repository;

namespace ShopApi.Infrastructure;

public class SessionAuthOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "Session";
    public int TimeoutMinutes { get; set; } = 20;
}

public class SessionAuthHandler : AuthenticationHandler<SessionAuthOptions>
{
    private readonly ISessionRepository _sessionRepo;

    public SessionAuthHandler(
        IOptionsMonitor<SessionAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISessionRepository sessionRepo)
        : base(options, logger, encoder)
    {
        _sessionRepo = sessionRepo;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var sessionId = Request.Cookies[AuthExtensions.CookieName];
        if (string.IsNullOrEmpty(sessionId))
            return AuthenticateResult.NoResult();

        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null)
            return AuthenticateResult.Fail("Session expired or invalid");

        var newExpiry = DateTime.UtcNow.AddMinutes(Options.TimeoutMinutes);
        await _sessionRepo.ExtendAsync(session.Id, newExpiry);

        var claims = new[]
        {
            new Claim("userId", session.UserId.ToString()),
            new Claim("username", session.Username),
            new Claim("role", session.Role),
        };
        var identity = new ClaimsIdentity(claims, SessionAuthOptions.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SessionAuthOptions.SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}
