using FluentValidation;

namespace HappyPaws.Api.Filters;

/// <summary>
/// Endpoint filter that runs FluentValidation against the first argument of type <typeparamref name="TRequest"/>.
/// Returns a 422 ValidationProblem if validation fails; otherwise calls the next filter.
/// </summary>
public class ValidationFilter<TRequest> : IEndpointFilter
{
    /// <summary>
    /// Resolves the validator for <typeparamref name="TRequest"/> from DI and validates the incoming request.
    /// Skips validation if no validator is registered.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
        if (validator is not null)
        {
            var request = context.Arguments
                .OfType<TRequest>()
                .FirstOrDefault();

            if (request is not null)
            {
                var validationResult = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.ErrorMessage).ToArray()
                        );

                    return TypedResults.ValidationProblem(errors);
                }
            }
        }

        return await next(context);
    }
}
