using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Auth;

/// <summary>Data submitted when registering a new account.</summary>
/// <param name="Name">The user's display name.</param>
/// <param name="Email">The user's email address. Must be unique across the platform.</param>
/// <param name="Password">The user's plain-text password. Stored as a bcrypt hash.</param>
/// <param name="Role">The initial platform role for the account.</param>
public record RegisterRequest(string Name, string Email, string Password, Role Role);

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
