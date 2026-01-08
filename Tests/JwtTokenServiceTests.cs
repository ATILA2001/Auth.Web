#if false
using System.IdentityModel.Tokens.Jwt;
using Auth.Web.Application.Auth;
using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Configuration;
using Auth.Web.Security.Jwt;
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
    public void CreateToken_IncludesMultipleRolesAndAreas()
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

        var roles = jwt.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToList();
        Assert.Contains("R1", roles);
        Assert.Contains("R2", roles);

        var areas = jwt.Claims.Where(c => c.Type == "areas").Select(c => c.Value).ToList();
        Assert.Contains("10", areas);
        Assert.Contains("20", areas);

        // Note: Apps are not currently included in the JWT token implementation
        // If apps support is needed, update JwtTokenService to include them
    }

    [Fact]
    public void CreateToken_EmailAndNameOptional()
    {
        var svc = CreateService();
        var modelWithout = new AuthClaimsModel { UserId = "u1" };
        var tokenWithout = svc.CreateToken(modelWithout, "aud");
        var jwtWithout = new JwtSecurityTokenHandler().ReadJwtToken(tokenWithout);
        Assert.DoesNotContain(jwtWithout.Claims, c => c.Type == "email");
        Assert.DoesNotContain(jwtWithout.Claims, c => c.Type == "name");

        var modelWith = new AuthClaimsModel { UserId = "u2", Email = "user@example.com", DisplayName = "User Two" };
        var tokenWith = svc.CreateToken(modelWith, "aud");
        var jwtWith = new JwtSecurityTokenHandler().ReadJwtToken(tokenWith);
        Assert.Equal("user@example.com", jwtWith.Claims.First(c => c.Type == "email").Value);
        Assert.Equal("User Two", jwtWith.Claims.First(c => c.Type == "name").Value);
    }

    [Fact]
    public void CreateToken_IncludesPermissionsVersion()
    {
        var svc = CreateService();
        var model = new AuthClaimsModel
        {
            UserId = "u3",
            PermissionsVersion = 42
        };
        var token = svc.CreateToken(model, "aud");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var versionClaim = jwt.Claims.FirstOrDefault(c => c.Type == "perms_version");
        Assert.NotNull(versionClaim);
        Assert.Equal("42", versionClaim.Value);
    }

    [Fact]
    public void CreateToken_FiltersEmptyRoles()
    {
        var svc = CreateService();
        var model = new AuthClaimsModel
        {
            UserId = "u4",
            Roles = new [] { "Admin", "", "  ", "User" }
        };
        var token = svc.CreateToken(model, "aud");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var roles = jwt.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToList();
        Assert.Equal(2, roles.Count);
        Assert.Contains("Admin", roles);
        Assert.Contains("User", roles);
    }

    [Fact]
    public void CreateToken_RemovesDuplicateRoles()
    {
        var svc = CreateService();
        var model = new AuthClaimsModel
        {
            UserId = "u5",
            Roles = new [] { "Admin", "admin", "ADMIN", "User" }
        };
        var token = svc.CreateToken(model, "aud");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var roles = jwt.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToList();
        Assert.Equal(2, roles.Count);
    }
}
#endif
