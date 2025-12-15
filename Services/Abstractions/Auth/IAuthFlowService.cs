using Auth.Web.Contracts.Auth;
using Auth.Web.Services.Abstractions.Auth.Models;

namespace Auth.Web.Services.Abstractions.Auth;

public interface IAuthFlowService
{
    Task<LoginResult> LoginAsync(LoginRequestDto request);
}
