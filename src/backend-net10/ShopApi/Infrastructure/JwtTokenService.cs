using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ShopApi.Infrastructure;

public interface IJwtTokenService
{
    string GenerateToken(int userId, string username, string role);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key not configured");
        _issuer = configuration["Jwt:Issuer"] ?? "ShopApi";
        _audience = configuration["Jwt:Audience"] ?? "ShopApi";
        _expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) ? m : 20;
    }

    static JwtTokenService()
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    }

    public string GenerateToken(int userId, string username, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("userId", userId.ToString()),
            new Claim("username", username),
            new Claim("role", role),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}