using FluentValidation;

namespace HappyPaws.Api.Filters;

public class ValidationFilter<TRequest> : IEndpointFilter
{
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
