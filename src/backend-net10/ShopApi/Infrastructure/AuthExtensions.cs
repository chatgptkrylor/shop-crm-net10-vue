using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ShopApi.Infrastructure;

public static class AuthExtensions
{
    public const string CookieName = "shopcrm_token";

    public static IServiceCollection AddShopApiAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var timeoutMinutes = int.TryParse(configuration["Session:TimeoutMinutes"], out var t) ? t : 20;

        services.AddAuthentication(SessionAuthOptions.SchemeName)
            .AddScheme<SessionAuthOptions, SessionAuthHandler>(
                SessionAuthOptions.SchemeName,
                options => options.TimeoutMinutes = timeoutMinutes);

        services.AddAuthorization();
        return services;
    }

    public static void SetAuthCookie(this HttpResponse response, string token, bool isHttps, int timeoutMinutes = 20)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = isHttps,
            Expires = DateTimeOffset.UtcNow.AddMinutes(timeoutMinutes),
            Path = "/",
        };
        response.Cookies.Append(CookieName, token, cookieOptions);
    }

    public static void ClearAuthCookie(this HttpResponse response)
    {
        response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });
    }
}
