namespace HappyPaws.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, bool isVerified);
    string GenerateRefreshToken();
}
