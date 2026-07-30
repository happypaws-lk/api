using Microsoft.AspNetCore.Http.Features;

namespace HappyPaws.Api.Filters;

public class RequestSizeLimitFilter : IEndpointFilter
{
    private readonly long _maxBytes;

    public RequestSizeLimitFilter(long maxBytes)
    {
        _maxBytes = maxBytes;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;

        if (request.ContentLength.HasValue && request.ContentLength > _maxBytes)
        {
            return TypedResults.Problem(
                detail: $"Request size exceeds the maximum allowed size of {_maxBytes} bytes.",
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Payload Too Large"
            );
        }

        var maxRequestBodySizeFeature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (maxRequestBodySizeFeature != null && !maxRequestBodySizeFeature.IsReadOnly)
        {
            maxRequestBodySizeFeature.MaxRequestBodySize = _maxBytes;
        }

        return await next(context);
    }
}
