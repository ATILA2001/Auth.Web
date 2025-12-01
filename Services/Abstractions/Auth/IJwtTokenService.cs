using Auth.Web.Application.Auth;

namespace Auth.Web.Services.Abstractions.Auth;

public interface IJwtTokenService
{
    string CreateToken(AuthClaimsModel model, string audience);
}
