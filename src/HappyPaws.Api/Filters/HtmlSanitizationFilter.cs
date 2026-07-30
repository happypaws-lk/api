using Ganss.Xss;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace HappyPaws.Api.Filters;

public class HtmlSanitizationFilter<T> : IEndpointFilter where T : class
{
    private static readonly HtmlSanitizer _sanitizer = new();

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.FirstOrDefault(a => a is T) as T;

        if (argument != null)
        {
            SanitizeProperties(argument);
        }

        return await next(context);
    }

    private void SanitizeProperties(object obj)
    {
        if (obj == null) return;

        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.PropertyType == typeof(string));

        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj) as string;
            if (!string.IsNullOrEmpty(value))
            {
                var sanitized = _sanitizer.Sanitize(value);
                prop.SetValue(obj, sanitized);
            }
        }
    }
}
