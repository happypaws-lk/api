using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
        group.MapPost("/signup/send-code", SendSignupCodeAsync)
            .AddEndpointFilter<ValidationFilter<SignupSendCodeRequest>>()
            .RequireRateLimiting("SignupLimiter")
            .WithName("SignupSendCode")
            .WithSummary("Start sign-up by sending a verification code")
            .WithDescription("Sends a 6-digit OTP to the supplied email. Returns 409 if the address already has a completed account. Always returns 200 otherwise.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapPost("/signup/verify-code", VerifySignupCodeAsync)
            .AddEndpointFilter<ValidationFilter<SignupVerifyCodeRequest>>()
            .RequireRateLimiting("SignupLimiter")
            .WithName("SignupVerifyCode")
            .WithSummary("Verify the sign-up OTP and get a signup token")
            .WithDescription("Validates the 6-digit OTP. On success returns a short-lived signup token (valid for 10 minutes) to use with the signup/complete endpoint.")
            .Produces<SignupVerifyCodeResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        group.MapPost("/signup/complete", CompleteSignupAsync)
            .AddEndpointFilter<ValidationFilter<SignupCompleteRequest>>()
            .RequireRateLimiting("RegisterLimiter")
            .WithName("SignupComplete")
            .WithSummary("Complete sign-up with name and password")
            .WithDescription("Creates the account using the verified signup token. The token is single-use and expires in 10 minutes. Returns an access token and refresh token on success.")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
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

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .AddEndpointFilter<ValidationFilter<ForgotPasswordRequest>>()
            .RequireRateLimiting("ForgotPasswordLimiter")
            .WithName("ForgotPassword")
            .WithSummary("Send a password reset OTP to an email address")
            .WithDescription("Sends a 6-digit reset code to the supplied email. Always returns 200 OK regardless of whether the email exists, to prevent email enumeration.")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapPost("/verify-reset-code", VerifyResetCodeAsync)
            .AddEndpointFilter<ValidationFilter<VerifyResetCodeRequest>>()
            .RequireRateLimiting("ForgotPasswordLimiter")
            .WithName("VerifyResetCode")
            .WithSummary("Verify the OTP code from the reset email")
            .WithDescription("Validates the 6-digit reset OTP. On success returns a short-lived reset token (valid for 10 minutes) to use with the reset-password endpoint. The OTP is not consumed until the password is changed.")
            .Produces<VerifyResetCodeResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        group.MapPost("/reset-password", ResetPasswordAsync)
            .AddEndpointFilter<ValidationFilter<ResetPasswordRequest>>()
            .RequireRateLimiting("ForgotPasswordLimiter")
            .WithName("ResetPassword")
            .WithSummary("Set a new password using the verified reset token")
            .WithDescription("Resets the user's password. Invalidates all active refresh tokens (signs out every device). The reset token is single-use and expires in 10 minutes.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        group.MapPost("/change-password", ChangePasswordAsync)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<ChangePasswordAuthRequest>>()
            .WithName("AuthChangePassword")
            .WithSummary("Change the authenticated user's password")
            .WithDescription("Verifies the current password and replaces it with the new one. Returns 400 if the current password is incorrect.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();
    }

    private static async Task<Results<Ok, Conflict<string>>> SendSignupCodeAsync(
        SignupSendCodeRequest request,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        IEmailSender emailSender,
        CancellationToken ct)
    {
        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        // A completed account has a non-empty password hash. Reject it outright.
        if (existingUser is not null && !string.IsNullOrEmpty(existingUser.PasswordHash))
            return TypedResults.Conflict("A user with this email already exists");

        // Reuse the pending placeholder if one already exists from a previous send-code call.
        var pendingUser = existingUser;
        if (pendingUser is null)
        {
            pendingUser = new User
            {
                Id = Guid.NewGuid(),
                Name = string.Empty,
                Email = request.Email,
                PasswordHash = string.Empty
            };
            db.Users.Add(pendingUser);
        }

        var code = GenerateOtpCode();
        var hashedCode = passwordHasher.HashPassword(pendingUser, code);

        db.OtpCodes.Add(new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = pendingUser.Id,
            Code = hashedCode,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        });

        await db.SaveChangesAsync(ct);

        await emailSender.SendSignupOtpAsync(request.Email, code, ct);

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<SignupVerifyCodeResponse>, UnauthorizedHttpResult>> VerifySignupCodeAsync(
        SignupVerifyCodeRequest request,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        CancellationToken ct)
    {
        // Only a pending user (no password set yet) can be verified through this flow.
        var pendingUser = await db.Users.FirstOrDefaultAsync(
            u => u.Email == request.Email && u.PasswordHash == string.Empty, ct);

        if (pendingUser is null)
            return TypedResults.Unauthorized();

        var otpCodes = await db.OtpCodes
            .Where(o => o.UserId == pendingUser.Id && !o.IsUsed && o.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        var matched = otpCodes.Any(otp =>
            passwordHasher.VerifyHashedPassword(pendingUser, otp.Code, request.Code) != PasswordVerificationResult.Failed);

        if (!matched)
            return TypedResults.Unauthorized();

        // Generate a one-time signup token, hashed before storage so a DB leak cannot
        // be replayed to complete registrations without going through the OTP step.
        var signupToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signupToken)));

        db.OtpCodes.Add(new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = pendingUser.Id,
            Code = hashedToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        });

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new SignupVerifyCodeResponse(signupToken));
    }

    private static async Task<Results<Created<AuthResponse>, UnauthorizedHttpResult>> CompleteSignupAsync(
        SignupCompleteRequest request,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        CancellationToken ct)
    {
        var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.SignupToken)));

        var tokenRecord = await db.OtpCodes
            .FirstOrDefaultAsync(o =>
                !o.IsUsed &&
                o.ExpiresAt > DateTimeOffset.UtcNow &&
                o.Code == hashedToken, ct);

        if (tokenRecord is null)
            return TypedResults.Unauthorized();

        var user = await db.Users.FindAsync([tokenRecord.UserId], ct);

        // Guard: only complete a pending account, not an existing one.
        if (user is null || !string.IsNullOrEmpty(user.PasswordHash))
            return TypedResults.Unauthorized();

        tokenRecord.IsUsed = true;
        user.Name = request.Name;
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        var role = request.Role;

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, [role.ToString()], false);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        db.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Role = role,
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

        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
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

    private static async Task<Ok> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        IEmailSender emailSender,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        // Always return 200 OK regardless of whether the address is registered,
        // pending signup, or unknown. Never leak that information to callers.
        // The random delay prevents timing-based email enumeration.
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            await Task.Delay(RandomNumberGenerator.GetInt32(50, 150), ct);
            return TypedResults.Ok();
        }

        var code = GenerateOtpCode();
        var hashedCode = passwordHasher.HashPassword(user, code);

        var otpCode = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = hashedCode,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
        };

        db.OtpCodes.Add(otpCode);
        await db.SaveChangesAsync(ct);

        await emailSender.SendPasswordResetOtpAsync(user.Email, code, ct);

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<VerifyResetCodeResponse>, UnauthorizedHttpResult>> VerifyResetCodeAsync(
        VerifyResetCodeRequest request,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        CancellationToken ct)
    {
        // Only let completed accounts (non-empty password hash) go through the reset flow.
        // Pending signup placeholders are excluded to prevent the forgot-password flow
        // from interfering with an in-progress signup on the same email address.
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
            return TypedResults.Unauthorized();

        var otpCodes = await db.OtpCodes
            .Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        var matched = otpCodes.Any(otp =>
            passwordHasher.VerifyHashedPassword(user, otp.Code, request.Code) != PasswordVerificationResult.Failed);

        if (!matched)
            return TypedResults.Unauthorized();

        // Generate a one-time reset token. We hash it with SHA-256 before storing so
        // a database leak cannot be used to reset accounts directly.
        var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resetToken)));

        var tokenRecord = new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = hashedToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        db.OtpCodes.Add(tokenRecord);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new VerifyResetCodeResponse(resetToken));
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult>> ResetPasswordAsync(
        ResetPasswordRequest request,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
            return TypedResults.Unauthorized();

        var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.ResetToken)));

        var tokenRecord = await db.OtpCodes
            .FirstOrDefaultAsync(o =>
                o.UserId == user.Id &&
                !o.IsUsed &&
                o.ExpiresAt > DateTimeOffset.UtcNow &&
                o.Code == hashedToken, ct);

        if (tokenRecord is null)
            return TypedResults.Unauthorized();

        tokenRecord.IsUsed = true;

        // Invalidate any remaining numeric OTP codes for this user so they cannot
        // be replayed to generate another reset token after the password is changed.
        var remainingOtps = await db.OtpCodes
            .Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTimeOffset.UtcNow && o.Code != hashedToken)
            .ToListAsync(ct);

        foreach (var otp in remainingOtps)
            otp.IsUsed = true;

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);

        // Revoke every active session. A password reset is a security event —
        // anyone who had access to the old password must re-authenticate.
        await db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, DateTimeOffset.UtcNow), ct);

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, BadRequest<string>>> ChangePasswordAsync(
        ChangePasswordAuthRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);

        var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verifyResult == PasswordVerificationResult.Failed)
            return TypedResults.BadRequest("Current password is incorrect");

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
