namespace Auth.Web.Services.Abstractions.Auth.Models;

public sealed class LoginResult
{
    public string RedirectUrl { get; init; } = "/Account/Login";
    public bool SignInAdmin { get; init; }
    public string? AdminUserId { get; init; }
    public bool ShowAppPicker { get; init; }
    public IReadOnlyList<AppPickerOption> AvailableApps { get; init; } = [];
}
