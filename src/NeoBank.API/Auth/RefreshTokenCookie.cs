using NeoBank.API.Configuration;

namespace NeoBank.API.Auth;

public static class RefreshTokenCookie
{
    public static void Write(HttpResponse response, string token, DateTimeOffset expiresAt, RefreshCookieSettings settings)
        => response.Cookies.Append(settings.Name, token, BuildOptions(settings, expiresAt));

    public static string? Read(HttpRequest request, RefreshCookieSettings settings)
        => request.Cookies.TryGetValue(settings.Name, out var value) ? value : null;

    // Attributes must match the ones used on write, otherwise the browser keeps the cookie.
    public static void Delete(HttpResponse response, RefreshCookieSettings settings)
        => response.Cookies.Delete(settings.Name, BuildOptions(settings, expiresAt: null));

    private static CookieOptions BuildOptions(RefreshCookieSettings settings, DateTimeOffset? expiresAt)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = settings.Secure,
            SameSite = ParseSameSite(settings.SameSite),
            Path = settings.Path,
        };

        if (expiresAt is not null)
            options.Expires = expiresAt;

        return options;
    }

    private static SameSiteMode ParseSameSite(string value) => value.ToLowerInvariant() switch
    {
        "none" => SameSiteMode.None,
        "lax" => SameSiteMode.Lax,
        _ => SameSiteMode.Strict,
    };
}