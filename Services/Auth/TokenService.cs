using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Auth.Web.Configuration;
using Auth.Web.Domain.Dtos;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Web.Services.Auth;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public Task<string> CreateAsync(ApplicationUser user, IEnumerable<string> roles, UserPermissionsDto permissions, string audience)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var roleList = roles.ToList();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Name, user.Nombre ?? user.UserName ?? string.Empty),
            new("roles", string.Join(',', roleList)),
            new("area", string.Join(',', permissions.Areas)),
            new("perms_ver", permissions.Version.ToString())
        };

        claims.AddRange(roleList.Select(role => new Claim(ClaimTypes.Role, role)));

        var expires = now.AddMinutes(_options.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        var serialized = handler.WriteToken(token);
        return Task.FromResult(serialized);
    }
}
