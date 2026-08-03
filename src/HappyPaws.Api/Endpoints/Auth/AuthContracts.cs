using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Auth;

/// <summary>Email submitted to start the sign-up flow.</summary>
/// <param name="Email">The address to verify before account creation.</param>
public record SignupSendCodeRequest(string Email);

/// <summary>OTP submitted to confirm email ownership during sign-up.</summary>
/// <param name="Email">The address the OTP was sent to.</param>
/// <param name="Code">The 6-digit code from the verification email.</param>
public record SignupVerifyCodeRequest(string Email, string Code);

/// <summary>A short-lived token returned after the sign-up OTP is verified.</summary>
/// <param name="SignupToken">Pass this to the signup/complete endpoint. It expires in 10 minutes.</param>
public record SignupVerifyCodeResponse(string SignupToken);

/// <summary>Data submitted to complete account creation after email verification.</summary>
/// <param name="SignupToken">The token returned by the signup/verify-code endpoint.</param>
/// <param name="Name">The user's display name.</param>
/// <param name="Password">The user's plain-text password. Stored as a PBKDF2 hash.</param>
/// <param name="Role">The initial platform role. Defaults to Adopter if not supplied.</param>
public record SignupCompleteRequest(string SignupToken, string Name, string Password, Role Role = Role.Adopter);

/// <summary>Credentials submitted when logging in.</summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's plain-text password.</param>
public record LoginRequest(string Email, string Password);

/// <summary>A refresh token submitted to rotate the token pair.</summary>
/// <param name="RefreshToken">The existing refresh token to rotate.</param>
public record RefreshRequest(string RefreshToken);

/// <summary>A refresh token submitted for revocation.</summary>
/// <param name="RefreshToken">The refresh token to revoke.</param>
public record RevokeRequest(string RefreshToken);

/// <summary>The email address to send an OTP code to.</summary>
/// <param name="Email">The destination email address.</param>
public record OtpRequest(string Email);

/// <summary>An OTP code submitted for verification.</summary>
/// <param name="Email">The email address the OTP was sent to.</param>
/// <param name="Code">The 6-digit code from the email.</param>
public record OtpVerifyRequest(string Email, string Code);

/// <summary>Tokens returned after a successful authentication or OTP verification.</summary>
/// <param name="AccessToken">Short-lived JWT for authorizing API requests.</param>
/// <param name="RefreshToken">Long-lived token used to rotate the access token.</param>
/// <param name="ExpiresAt">UTC timestamp when the access token expires.</param>
public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

/// <summary>An email address submitted to initiate password reset.</summary>
/// <param name="Email">The registered email address.</param>
public record ForgotPasswordRequest(string Email);

/// <summary>An OTP code submitted to verify the reset request.</summary>
/// <param name="Email">The registered email address the OTP was sent to.</param>
/// <param name="Code">The 6-digit code from the reset email.</param>
public record VerifyResetCodeRequest(string Email, string Code);

/// <summary>A short-lived reset token returned after OTP verification succeeds.</summary>
/// <param name="ResetToken">Pass this token to the reset-password endpoint. It expires in 10 minutes.</param>
public record VerifyResetCodeResponse(string ResetToken);

/// <summary>Data submitted to complete the password reset.</summary>
/// <param name="Email">The registered email address.</param>
/// <param name="ResetToken">The token returned by the verify-reset-code endpoint.</param>
/// <param name="NewPassword">The new plain-text password. Stored as a PBKDF2 hash.</param>
public record ResetPasswordRequest(string Email, string ResetToken, string NewPassword);
