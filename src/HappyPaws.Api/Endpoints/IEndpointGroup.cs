namespace HappyPaws.Api.Endpoints;

/// <summary>
/// Implemented by every feature's endpoint class. <see cref="EndpointExtensions.MapEndpoints"/> discovers and
/// registers all implementations automatically — no manual wiring in Program.cs required.
/// </summary>
public interface IEndpointGroup
{
    /// <summary>
    /// Registers the feature's routes on the supplied route group.
    /// </summary>
    void Map(RouteGroupBuilder group);
}
