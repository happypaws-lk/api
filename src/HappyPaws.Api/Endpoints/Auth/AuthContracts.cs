using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Auth;

public record RegisterRequest(string Name, string Email, string Password, Role Role);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record RevokeRequest(string RefreshToken);
public record OtpRequest(string Email);
public record OtpVerifyRequest(string Email, string Code);
public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
