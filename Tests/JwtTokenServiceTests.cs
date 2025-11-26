using System.IdentityModel.Tokens.Jwt;
using Auth.Web.Application.Auth;
using Auth.Web.Application.Abstractions;
using Auth.Web.Configuration;
using Auth.Web.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Xunit;

namespace Auth.Web.Tests;

public class JwtTokenServiceTests
{
    private IJwtTokenService CreateService(string signingKey = "0123456789abcdef0123456789abcdef")
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            SigningKey = signingKey,
            TokenLifetimeMinutes = 8
        });
        return new JwtTokenService(options);
    }

    [Fact]
    public void CreateToken_EmitsStandardClaims()
    {
        var svc = CreateService();
        var model = new AuthClaimsModel
        {
            UserId = "user-1"
        };
        var token = svc.CreateToken(model, "test-aud");
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == "test-aud");
        Assert.Equal("user-1", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.NotNull(jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti));
        var exp = jwt.ValidTo;
        var deltaMinutes = (exp - DateTime.UtcNow).TotalMinutes;
        Assert.InRange(deltaMinutes, 7, 9); // alrededor de 8 min
    }

    [Fact]
    public void CreateToken_IncludesMultipleRolesAreasApps()
    {
        var svc = CreateService();
        var model = new AuthClaimsModel
        {
            UserId = "user-2",
            Roles = new [] { "R1", "R2" },
            Areas = new [] { "10", "20" },
            Apps = new [] { "app1", "app2" }
        };
        var token = svc.CreateToken(model, "aud-x");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var roles = jwt.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains("R1", roles);
        Assert.Contains("R2", roles);

        var areas = jwt.Claims.Where(c => c.Type == "area").Select(c => c.Value).ToList();
        Assert.Contains("10", areas);
        Assert.Contains("20", areas);

        var apps = jwt.Claims.Where(c => c.Type == "app").Select(c => c.Value).ToList();
        Assert.Contains("app1", apps);
        Assert.Contains("app2", apps);
    }

    [Fact]
    public void CreateToken_EmailAndNameOptional()
    {
        var svc = CreateService();
        var modelWithout = new AuthClaimsModel { UserId = "u1" };
        var tokenWithout = svc.CreateToken(modelWithout, "aud");
        var jwtWithout = new JwtSecurityTokenHandler().ReadJwtToken(tokenWithout);
        Assert.DoesNotContain(jwtWithout.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Email);
        Assert.DoesNotContain(jwtWithout.Claims, c => c.Type == "name");

        var modelWith = new AuthClaimsModel { UserId = "u2", Email = "user@example.com", DisplayName = "User Two" };
        var tokenWith = svc.CreateToken(modelWith, "aud");
        var jwtWith = new JwtSecurityTokenHandler().ReadJwtToken(tokenWith);
        Assert.Equal("user@example.com", jwtWith.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Email).Value);
        Assert.Equal("User Two", jwtWith.Claims.First(c => c.Type == "name").Value);
    }
}
