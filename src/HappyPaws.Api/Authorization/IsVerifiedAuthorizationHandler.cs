using Microsoft.AspNetCore.Authorization;

namespace HappyPaws.Api.Authorization;

/// <summary>
/// Authorizes users who have a verified identity claim.
/// </summary>
public class IsVerifiedAuthorizationHandler : AuthorizationHandler<IsVerifiedRequirement>
{
    /// <summary>
    /// Checks the user's claims for an "is_verified" value of "True".
    /// Succeeds the requirement if the claim exists and matches.
    /// </summary>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IsVerifiedRequirement requirement)
    {
        var isVerifiedClaim = context.User.FindFirst("is_verified")?.Value;

        if (string.Equals(isVerifiedClaim, "True", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
