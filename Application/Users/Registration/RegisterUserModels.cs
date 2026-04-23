namespace Auth.Web.Application.Users.Registration;

public sealed class RegisterUserRequest
{
    public string Cuil { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
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
    public string Message { get; init; } = string.Empty;

    public static RegisterUserResult Success(string message)
        => new() { Type = RegisterUserResultType.Success, Message = message };

    public static RegisterUserResult AlreadyExists(string message)
        => new() { Type = RegisterUserResultType.AlreadyExists, Message = message };

    public static RegisterUserResult NotInActiveDirectory(string message)
        => new() { Type = RegisterUserResultType.NotInActiveDirectory, Message = message };

    public static RegisterUserResult ValidationError(string message)
        => new() { Type = RegisterUserResultType.ValidationError, Message = message };
}
