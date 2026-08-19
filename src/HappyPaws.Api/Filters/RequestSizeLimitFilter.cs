using Microsoft.AspNetCore.Http.Features;

namespace HappyPaws.Api.Filters;

/// <summary>
/// Endpoint filter that rejects requests whose Content-Length exceeds a configured byte limit.
/// Also sets the server-side body size cap via <see cref="IHttpMaxRequestBodySizeFeature"/>.
/// </summary>
public class RequestSizeLimitFilter : IEndpointFilter
{
    private readonly long _maxBytes;

    /// <summary>
    /// Creates a new filter with the given byte limit.
    /// </summary>
    public RequestSizeLimitFilter(long maxBytes)
    {
        _maxBytes = maxBytes;
    }

    /// <summary>
    /// Returns 413 if the declared Content-Length exceeds <c>maxBytes</c>; otherwise forwards to the next filter.
    /// </summary>
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
