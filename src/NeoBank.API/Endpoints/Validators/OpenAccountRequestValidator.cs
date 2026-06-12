using FluentValidation;
using NeoBank.API.Endpoints;

namespace NeoBank.API.Endpoints.Validators;

public class OpenAccountRequestValidator : AbstractValidator<OpenAccountRequest>
{
    public OpenAccountRequestValidator()
    {
        RuleFor(x => x.Currency).IsInEnum();
    }
}