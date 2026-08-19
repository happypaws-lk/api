using System.Reflection;
using HappyPaws.Api.Endpoints;

namespace HappyPaws.Api.Extensions;

/// <summary>
/// Extension methods for registering endpoint groups via assembly scanning.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Scans the executing assembly for all <see cref="IEndpointGroup"/> implementations and registers each one
    /// under <c>/api/v1/{prefix}</c>, where the prefix is derived from the class name.
    /// </summary>
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var endpointGroupTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IEndpointGroup)));

        foreach (var type in endpointGroupTypes)
        {
            var instance = (IEndpointGroup)Activator.CreateInstance(type)!;
            
            // By convention, we can derive the base route from the class name
            // e.g., "AuthEndpoints" -> "auth", "UserEndpoints" -> "users"
            var routePrefix = type.Name.Replace("Endpoints", string.Empty).ToLowerInvariant();
            
            var group = app.MapGroup($"/api/v1/{routePrefix}")
                .WithTags(routePrefix);

            instance.Map(group);
        }

        return app;
    }
}
