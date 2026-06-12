using System.Security.Claims;
using NeoBank.API.Endpoints.Validators;
using NeoBank.API.Filters;
using NeoBank.Application.Commands.CreateAccount;
using NeoBank.Application.Queries.GetAccountById;
using NeoBank.Application.Queries.GetAccounts;
using NeoBank.Domain.Enums;

namespace NeoBank.API.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/accounts").RequireAuthorization();

        group.MapPost("/", OpenAccount)
            .AddEndpointFilter<ValidationFilter<OpenAccountRequest>>();

        group.MapGet("/", GetAccounts);
        group.MapGet("/{id:guid}", GetAccountById);

        return app;
    }

    private static async Task<IResult> OpenAccount(
        OpenAccountRequest req,
        HttpContext ctx,
        CreateAccountHandler handler,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.BadRequest(Error("Auth.MissingUserId", "X-User-Id header is required"));

        var result = await handler.HandleAsync(new CreateAccountCommand(userId.Value, req.Currency), ct);

        return result.IsSuccess
            ? Results.Created($"/api/v1/accounts/{result.Value.Id}", result.Value)
            : Results.Conflict(Error(result.Error, "Account already exists for this currency"));
    }

    private static async Task<IResult> GetAccounts(
        HttpContext ctx,
        GetAccountsHandler handler,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.BadRequest(Error("Auth.MissingUserId", "X-User-Id header is required"));

        var accounts = await handler.HandleAsync(new GetAccountsQuery(userId.Value), ct);
        return Results.Ok(accounts);
    }

    private static async Task<IResult> GetAccountById(
        Guid id,
        HttpContext ctx,
        GetAccountByIdHandler handler,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.BadRequest(Error("Auth.MissingUserId", "X-User-Id header is required"));

        var result = await handler.HandleAsync(new GetAccountByIdQuery(id, userId.Value), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error switch
            {
                "Account.NotFound"     => Results.NotFound(Error(result.Error, "Account not found")),
                "Account.AccessDenied" => Results.Json(Error(result.Error, "Access denied"), statusCode: 403),
                _                      => Results.StatusCode(500)
            };
    }

    private static Guid? GetUserId(HttpContext ctx)
    {
        var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? ctx.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static object Error(string code, string message) => new { error = code, message };
}

public record OpenAccountRequest(Currency Currency);