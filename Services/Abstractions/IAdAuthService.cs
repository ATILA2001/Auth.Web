namespace Auth.Web.Services.Abstractions;

public interface IAdAuthService
{
    Task<bool> ValidateAsync(string userName, string password);
}
