using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HappyPaws.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found");

        return Guid.Parse(sub);
    }

    public static string GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? principal.FindFirstValue(ClaimTypes.Email)
            ?? throw new InvalidOperationException("Email claim not found");
    }

    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }
}
