using Microsoft.AspNetCore.Authorization;

namespace HappyPaws.Api.Authorization;

public class IsVerifiedAuthorizationHandler : AuthorizationHandler<IsVerifiedRequirement>
{
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
