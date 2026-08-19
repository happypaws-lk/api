using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HappyPaws.Infrastructure.Services;

/// <summary>
/// Creates JWT access tokens and cryptographically random refresh tokens.
/// Reads issuer, audience, signing key, and expiry from <c>Jwt:*</c> configuration.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly SymmetricSecurityKey _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public TokenService(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured");

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        _issuer = configuration["Jwt:Issuer"] ?? "https://happypaws.lk";
        _audience = configuration["Jwt:Audience"] ?? "https://happypaws.lk";
        _expiryMinutes = configuration.GetValue<int>("Jwt:ExpiryMinutes", 15);
    }

    /// <summary>
    /// Builds and signs a JWT with sub, email, jti, roles, and an <c>is_verified</c> claim.
    /// </summary>
    public string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, bool isVerified)
    {
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("is_verified", isVerified.ToString())
        ];

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTimeOffset.UtcNow.AddMinutes(_expiryMinutes).UtcDateTime;

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Returns a 64-byte base64-encoded random string suitable for use as a refresh token.
    /// </summary>
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
