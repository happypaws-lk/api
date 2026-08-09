using HappyPaws.Api.Endpoints.Auth;
using HappyPaws.Api.Filters;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Api.Endpoints.Setup;

public class SetupEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/status", GetStatusAsync)
            .WithName("GetSetupStatus")
            .WithSummary("Check if first-time setup is complete")
            .WithDescription("Returns whether an admin account already exists. Clients can use this to decide whether to show the setup screen on first launch.")
            .Produces<SetupStatusResponse>();

        group.MapPost("/complete", CompleteSetupAsync)
            .AddEndpointFilter<ValidationFilter<SetupCompleteRequest>>()
            .RequireRateLimiting("RegisterLimiter")
            .WithName("CompleteSetup")
            .WithSummary("Create the first admin account")
            .WithDescription("Creates the initial admin account and returns a token pair. Returns 409 if an admin already exists or if the email is already taken. This endpoint is permanently locked once setup completes.")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();
    }

    private static async Task<Ok<SetupStatusResponse>> GetStatusAsync(
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var isComplete = await db.UserRoles.AnyAsync(r => r.Role == Role.Admin, ct);
        return TypedResults.Ok(new SetupStatusResponse(isComplete));
    }

    private static async Task<Results<Created<AuthResponse>, Conflict<string>>> CompleteSetupAsync(
        SetupCompleteRequest request,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        CancellationToken ct)
    {
        // One-time lock: once an admin exists this endpoint is permanently closed.
        var adminExists = await db.UserRoles.AnyAsync(r => r.Role == Role.Admin, ct);
        if (adminExists)
            return TypedResults.Conflict("Setup is already complete. An admin account already exists.");

        var emailTaken = await db.Users.AnyAsync(
            u => u.Email == request.Email && !string.IsNullOrEmpty(u.PasswordHash), ct);
        if (emailTaken)
            return TypedResults.Conflict("An account with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            IsVerified = true,
            PasswordHash = string.Empty
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, [Role.Admin.ToString()], true);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        db.Users.Add(user);

        db.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Role = Role.Admin,
            AssignedAt = DateTimeOffset.UtcNow
        });

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        });

        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/v1/users/{user.Id}",
            new AuthResponse(accessToken, refreshTokenValue, DateTimeOffset.UtcNow.AddMinutes(15)));
    }
}
