namespace Auth.Web.Application.Dtos;

public sealed class LoginRequestDto
{
    public string UserNameOrEmail { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string? ReturnUrl { get; init; }
    public string? ClientId { get; init; }
}

public enum LoginOutcomeType
{
    SuccessAdmin,
    SuccessExternalApp,
    Failure
}

public sealed class LoginOutcome
{
    public LoginOutcomeType Type { get; init; }
    public string? RedirectUrl { get; init; }
    public string? ErrorMessage { get; init; }

    public static LoginOutcome Failure(string error)
        => new() { Type = LoginOutcomeType.Failure, ErrorMessage = error };

    public static LoginOutcome Admin(string redirectUrl = "/admin")
        => new() { Type = LoginOutcomeType.SuccessAdmin, RedirectUrl = redirectUrl };

    public static LoginOutcome ExternalApp(string redirectUrl)
        => new() { Type = LoginOutcomeType.SuccessExternalApp, RedirectUrl = redirectUrl };
}

public sealed class RegisterUserRequest
{
    public string FullName { get; init; } = default!;
    public string Email { get; init; } = default!;
}

public enum RegisterUserResultType
{
    Success,
    AlreadyExists,
    NotInActiveDirectory,
    ValidationError
}

public sealed class RegisterUserResult
{
    public RegisterUserResultType Type { get; init; }
    public string Message { get; init; } = default!;

    public static RegisterUserResult Success(string message)
        => new() { Type = RegisterUserResultType.Success, Message = message };

    public static RegisterUserResult AlreadyExists(string message)
        => new() { Type = RegisterUserResultType.AlreadyExists, Message = message };

    public static RegisterUserResult NotInActiveDirectory(string message)
        => new() { Type = RegisterUserResultType.NotInActiveDirectory, Message = message };

    public static RegisterUserResult ValidationError(string message)
        => new() { Type = RegisterUserResultType.ValidationError, Message = message };
}
