using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using HappyPaws.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace HappyPaws.Tests.Unit;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456!",
                ["Jwt:Issuer"] = "https://test.happypaws.lk",
                ["Jwt:Audience"] = "https://test.happypaws.lk",
                ["Jwt:ExpiryMinutes"] = "15"
            })
            .Build();

        _tokenService = new TokenService(configuration);
    }

    [Fact]
    public void GenerateAccessToken_ContainsExpectedClaims()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var roles = new[] { "Adopter", "Foster" };

        var token = _tokenService.GenerateAccessToken(userId, email, roles, true);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
        jwt.Claims.Should().Contain(c => c.Type == "is_verified" && c.Value == "True");
        jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
            .Should().BeEquivalentTo(roles);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresInConfiguredMinutes()
    {
        var token = _tokenService.GenerateAccessToken(Guid.NewGuid(), "test@example.com", ["Adopter"], false);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var expectedExpiry = DateTime.UtcNow.AddMinutes(15);
        jwt.ValidTo.Should().BeCloseTo(expectedExpiry, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueBase64()
    {
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
        token1.Should().NotBe(token2);

        var bytes = Convert.FromBase64String(token1);
        bytes.Should().HaveCount(64);
    }
}
