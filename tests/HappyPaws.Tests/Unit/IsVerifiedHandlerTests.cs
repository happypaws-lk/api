using System.Security.Claims;
using FluentAssertions;
using HappyPaws.Api.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace HappyPaws.Tests.Unit;

public class IsVerifiedHandlerTests
{
    private readonly IsVerifiedAuthorizationHandler _handler = new();
    private readonly IsVerifiedRequirement _requirement = new();

    [Fact]
    public async Task HandleAsync_ClaimIsTrue_Succeeds()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("is_verified", "True")], "test"));

        var context = new AuthorizationHandlerContext([_requirement], user, null);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ClaimIsFalse_DoesNotSucceed()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("is_verified", "False")], "test"));

        var context = new AuthorizationHandlerContext([_requirement], user, null);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ClaimMissing_DoesNotSucceed()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([], "test"));

        var context = new AuthorizationHandlerContext([_requirement], user, null);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
