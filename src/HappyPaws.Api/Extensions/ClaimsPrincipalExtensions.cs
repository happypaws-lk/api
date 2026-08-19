using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HappyPaws.Api.Extensions;

/// <summary>
/// Convenience methods for reading standard claims from a <see cref="ClaimsPrincipal"/>.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the user ID from the "sub" or NameIdentifier claim. Throws if the claim is missing.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found");

        return Guid.Parse(sub);
    }

    /// <summary>
    /// Reads the email from the "email" or Email claim. Throws if the claim is missing.
    /// </summary>
    public static string GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? principal.FindFirstValue(ClaimTypes.Email)
            ?? throw new InvalidOperationException("Email claim not found");
    }

    /// <summary>
    /// Returns all role claim values for the principal.
    /// </summary>
    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }
}
