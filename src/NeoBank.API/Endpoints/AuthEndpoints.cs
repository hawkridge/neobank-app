using System.Security.Claims;
using Microsoft.Extensions.Options;
using NeoBank.API.Auth;
using NeoBank.API.Configuration;
using NeoBank.API.Endpoints.Validators;
using NeoBank.API.Filters;
using NeoBank.Application.Commands.Login;
using NeoBank.Application.Commands.Logout;
using NeoBank.Application.Commands.Refresh;
using NeoBank.Application.Commands.Register;
using NeoBank.Application.Configuration;

namespace NeoBank.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/register", Register)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .AllowAnonymous();

        group.MapPost("/login", Login)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .AllowAnonymous();
        
        group.MapPost("/refresh", Refresh)
            .AllowAnonymous();

        group.MapPost("/logout", Logout)
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> Register(
        RegisterRequest req,
        RegisterHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(
            new RegisterCommand(req.Email, req.Password, req.FirstName, req.LastName, req.PhoneNumber), ct);

        return result.IsSuccess
            ? Results.Created($"/api/v1/users/{result.Value.UserId}", result.Value)
            : Results.Conflict(Error(result.Error, "An account with this email already exists."));
    }

    private static async Task<IResult> Login(
        LoginRequest req,
        LoginHandler handler,
        HttpContext ctx,
        IOptions<RefreshCookieSettings> cookieOptions,
        IOptions<JwtSettings> jwtOptions,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new LoginCommand(req.Email, req.Password), ct);
        if (!result.IsSuccess)
            return Results.Unauthorized();

        IssueRefreshCookie(ctx, result.Value.RefreshToken, cookieOptions.Value, jwtOptions.Value);
        return Results.Ok(new AuthResponse(result.Value.AccessToken, result.Value.ExpiresIn));
    }

    private static async Task<IResult> Refresh(
        HttpContext ctx,
        RefreshHandler handler,
        IOptions<RefreshCookieSettings> cookieOptions,
        IOptions<JwtSettings> jwtOptions,
        CancellationToken ct)
    {
        var cookie = cookieOptions.Value;
        var presented = RefreshTokenCookie.Read(ctx.Request, cookie);
        if (string.IsNullOrEmpty(presented))
            return Results.Unauthorized();

        var result = await handler.HandleAsync(new RefreshCommand(presented), ct);
        if (!result.IsSuccess)
        {
            RefreshTokenCookie.Delete(ctx.Response, cookie);
            return Results.Unauthorized();
        }

        IssueRefreshCookie(ctx, result.Value.RefreshToken, cookie, jwtOptions.Value);
        return Results.Ok(new AuthResponse(result.Value.AccessToken, result.Value.ExpiresIn));
    }

    private static async Task<IResult> Logout(
        HttpContext ctx,
        LogoutHandler handler,
        IOptions<RefreshCookieSettings> cookieOptions,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.Unauthorized();

        var cookie = cookieOptions.Value;
        var presented = RefreshTokenCookie.Read(ctx.Request, cookie);

        // Idempotent by design — always 204, whether or not the token existed.
        if (!string.IsNullOrEmpty(presented))
            await handler.HandleAsync(new LogoutCommand(userId.Value, presented), ct);

        RefreshTokenCookie.Delete(ctx.Response, cookie);
        return Results.NoContent();
    }

    private static void IssueRefreshCookie(HttpContext ctx, string token, RefreshCookieSettings cookie, JwtSettings jwt)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(jwt.RefreshTokenExpiryDays);
        RefreshTokenCookie.Write(ctx.Response, token, expiresAt, cookie);
    }

    private static Guid? GetUserId(HttpContext ctx)
    {
        var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? ctx.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static object Error(string code, string message) => new { error = code, message };
}

public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string PhoneNumber);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, int ExpiresIn);
