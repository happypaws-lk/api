using System.Security.Claims;
using System.Security.Cryptography;
using HappyPaws.Api.Extensions;
using HappyPaws.Api.Filters;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Api.Endpoints.Auth;

public class AuthEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapPost("/register", RegisterAsync)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .RequireRateLimiting("RegisterLimiter")
            .WithName("Register")
            .WithSummary("Register a new user account")
            .WithDescription("Creates an account and returns an access token and refresh token. The role field sets the user's initial platform role (Adopter, Foster, Transporter, or Veterinarian). Returns 409 if the email is already taken.")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapPost("/login", LoginAsync)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .RequireRateLimiting("AuthLimiter")
            .WithName("Login")
            .WithSummary("Authenticate with email and password")
            .WithDescription("Verifies the email and password. Returns 403 if the account is suspended.")
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();

        group.MapPost("/refresh", RefreshAsync)
            .AddEndpointFilter<ValidationFilter<RefreshRequest>>()
            .RequireRateLimiting("AuthLimiter")
            .WithName("RefreshToken")
            .WithSummary("Rotate refresh token and get new access token")
            .WithDescription("Rotates the refresh token. If the supplied token was already revoked, every active token for that user is immediately revoked to contain potential token theft.")
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        group.MapPost("/revoke", RevokeAsync)
            .RequireAuthorization()
            .WithName("RevokeToken")
            .WithSummary("Revoke a refresh token")
            .WithDescription("Marks the supplied refresh token as revoked. Call this on sign-out.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/otp/send", SendOtpAsync)
            .RequireAuthorization()
            .RequireRateLimiting("OtpLimiter")
            .AddEndpointFilter<ValidationFilter<OtpRequest>>()
            .WithName("SendOtp")
            .WithSummary("Send OTP code via email for elevated access")
            .WithDescription("Sends a 6-digit OTP to the specified email. Only available to Admin and Veterinarian roles. The code expires in 5 minutes.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();

        group.MapPost("/otp/verify", VerifyOtpAsync)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<OtpVerifyRequest>>()
            .WithName("VerifyOtp")
            .WithSummary("Verify OTP code and get elevated access token")
            .WithDescription("Validates the OTP code and returns a fresh access and refresh token pair on success.")
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();
    }

    private static async Task<Results<Created<AuthResponse>, Conflict<string>>> RegisterAsync(
        RegisterRequest request,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        CancellationToken ct)
    {
        var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (emailExists)
            return TypedResults.Conflict("A user with this email already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = string.Empty
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Role = request.Role,
            AssignedAt = DateTimeOffset.UtcNow
        };

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, [request.Role.ToString()], false);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        db.Users.Add(user);
        db.UserRoles.Add(userRole);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        var response = new AuthResponse(accessToken, refreshTokenValue, DateTimeOffset.UtcNow.AddMinutes(15));
        return TypedResults.Created($"/api/v1/users/{user.Id}", response);
    }

    private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null)
            return TypedResults.Unauthorized();

        if (user.IsSuspended)
            return TypedResults.Problem(
                detail: "Account is suspended",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden");

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return TypedResults.Unauthorized();

        var roles = user.Roles.Select(r => r.Role.ToString()).ToList();
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roles, user.IsVerified);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new AuthResponse(accessToken, refreshTokenValue, DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>> RefreshAsync(
        RefreshRequest request,
        HappyPawsDbContext db,
        ITokenService tokenService,
        CancellationToken ct)
    {
        var existingToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.Roles)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, ct);

        if (existingToken is null)
            return TypedResults.Unauthorized();

        if (existingToken.RevokedAt is not null)
        {
            await db.RefreshTokens
                .Where(rt => rt.UserId == existingToken.UserId && rt.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, DateTimeOffset.UtcNow), ct);

            return TypedResults.Unauthorized();
        }

        if (existingToken.ExpiresAt < DateTimeOffset.UtcNow)
            return TypedResults.Unauthorized();

        existingToken.RevokedAt = DateTimeOffset.UtcNow;

        var user = existingToken.User;
        var roles = user.Roles.Select(r => r.Role.ToString()).ToList();
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roles, user.IsVerified);
        var newRefreshTokenValue = tokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        db.RefreshTokens.Add(newRefreshToken);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new AuthResponse(accessToken, newRefreshTokenValue, DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> RevokeAsync(
        RevokeRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId, ct);

        if (token is null)
            return TypedResults.Unauthorized();

        token.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, ProblemHttpResult>> SendOtpAsync(
        OtpRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        IEmailSender emailSender,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var roles = principal.GetRoles().ToList();

        if (!roles.Contains("Admin") && !roles.Contains("Veterinarian"))
            return TypedResults.Problem(
                detail: "OTP is only available for Admin and Veterinarian roles",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden");

        var code = GenerateOtpCode();
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null)
            return TypedResults.Unauthorized();

        var hashedCode = passwordHasher.HashPassword(user, code);

        var otpCode = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = hashedCode,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        db.OtpCodes.Add(otpCode);
        await db.SaveChangesAsync(ct);

        await emailSender.SendOtpAsync(request.Email, code, ct);

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>> VerifyOtpAsync(
        OtpVerifyRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var user = await db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return TypedResults.Unauthorized();

        var otpCodes = await db.OtpCodes
            .Where(o => o.UserId == userId && !o.IsUsed && o.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        foreach (var otp in otpCodes)
        {
            var verifyResult = passwordHasher.VerifyHashedPassword(user, otp.Code, request.Code);
            if (verifyResult != PasswordVerificationResult.Failed)
            {
                otp.IsUsed = true;
                await db.SaveChangesAsync(ct);

                var roles = user.Roles.Select(r => r.Role.ToString()).ToList();
                var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, roles, user.IsVerified);
                var refreshTokenValue = tokenService.GenerateRefreshToken();

                var refreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = refreshTokenValue,
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
                };

                db.RefreshTokens.Add(refreshToken);
                await db.SaveChangesAsync(ct);

                return TypedResults.Ok(new AuthResponse(accessToken, refreshTokenValue, DateTimeOffset.UtcNow.AddMinutes(15)));
            }
        }

        return TypedResults.Unauthorized();
    }

    private static string GenerateOtpCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }
}
