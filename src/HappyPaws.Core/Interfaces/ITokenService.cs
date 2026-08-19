namespace HappyPaws.Core.Interfaces;

/// <summary>
/// Creates access tokens and refresh tokens for authenticated users.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Builds a signed JWT carrying the user's ID, email, roles, and verification status.
    /// </summary>
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, bool isVerified);

    /// <summary>
    /// Returns a cryptographically random opaque string for use as a refresh token.
    /// </summary>
    string GenerateRefreshToken();
}
