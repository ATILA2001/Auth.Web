using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auth.Web.Configuration;
using Auth.Web.Application.Auth;
using Auth.Web.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Web.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("JwtOptions.SigningKey must be at least 32 characters long for HS256.");
        }
    }

    public string CreateToken(AuthClaimsModel model, string audience)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.TokenLifetimeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, model.UserId),
            new(JwtRegisteredClaimNames.Iss, _options.Issuer),
            new(JwtRegisteredClaimNames.Aud, audience),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, model.Email));
        }

        if (!string.IsNullOrWhiteSpace(model.DisplayName))
        {
            claims.Add(new Claim("name", model.DisplayName));
        }

        foreach (var role in model.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var area in model.Areas)
        {
            claims.Add(new Claim("area", area));
        }

        foreach (var app in model.Apps)
        {
            claims.Add(new Claim("app", app));
        }

        claims.Add(new Claim("perms_ver", model.PermissionsVersion.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}