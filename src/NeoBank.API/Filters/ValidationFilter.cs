using FluentValidation;

namespace NeoBank.API.Filters;

public class ValidationFilter<T>(IValidator<T>? validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (validator is null)
            return await next(context);

        var target = context.Arguments.OfType<T>().FirstOrDefault();
        if (target is null)
            return await next(context);

        var result = await validator.ValidateAsync(target, context.HttpContext.RequestAborted);

        if (result.IsValid)
            return await next(context);

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }
}