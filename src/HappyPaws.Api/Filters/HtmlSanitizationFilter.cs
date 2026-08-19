using Ganss.Xss;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace HappyPaws.Api.Filters;

/// <summary>
/// Endpoint filter that strips HTML from every public string property on a request of type <typeparamref name="T"/>
/// before the handler runs. Uses <see cref="HtmlSanitizer"/> from Ganss.Xss.
/// </summary>
public class HtmlSanitizationFilter<T> : IEndpointFilter where T : class
{
    private static readonly HtmlSanitizer _sanitizer = new();

    /// <summary>
    /// Finds the first argument of type <typeparamref name="T"/> and sanitizes its string properties in place.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.FirstOrDefault(a => a is T) as T;

        if (argument != null)
        {
            SanitizeProperties(argument);
        }

        return await next(context);
    }

    /// <summary>
    /// Iterates all writable public string properties on <paramref name="obj"/> and replaces each value with its sanitized form.
    /// </summary>
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
