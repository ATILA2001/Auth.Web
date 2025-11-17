using Auth.Web.Domain.Entities;

namespace Auth.Web.Services.Abstractions;

public interface ILoginService
{
    Task<LoginResult> LoginAsync(string userNameOrEmail, string password, CancellationToken ct = default);
}

public sealed record LoginResult(bool Success, string? Error, string? Redirect);
