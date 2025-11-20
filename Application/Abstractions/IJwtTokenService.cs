using Auth.Web.Application.Auth;

namespace Auth.Web.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateToken(AuthClaimsModel model, string audience);
}
