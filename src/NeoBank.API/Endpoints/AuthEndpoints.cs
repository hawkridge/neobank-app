using System.Security.Claims;
using NeoBank.API.Endpoints.Validators;
using NeoBank.API.Filters;
using NeoBank.Application.Commands.Login;
using NeoBank.Application.Commands.Logout;
using NeoBank.Application.Commands.Refresh;
using NeoBank.Application.Commands.Register;

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
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new LoginCommand(req.Email, req.Password), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Unauthorized();
    }
    
    private static async Task<IResult> Refresh(
        RefreshRequest req,
        RefreshHandler handler,
        CancellationToken ct)
    {   
        var result = await handler.HandleAsync(new RefreshCommand(req.RefreshToken), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
    }

    private static async Task<IResult> Logout(
        LogoutRequest req,
        HttpContext ctx,
        LogoutHandler handler,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.Unauthorized();

        // Idempotent by design — always 204, whether or not the token existed.
        await handler.HandleAsync(new LogoutCommand(userId.Value, req.RefreshToken), ct);
        return Results.NoContent();
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
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
