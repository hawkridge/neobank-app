namespace NeoBank.Application.Queries.GetAccountById;

public record GetAccountByIdQuery(Guid AccountId, Guid UserId);