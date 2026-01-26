namespace Auth.Web.Configuration;

public sealed class TestUsersOptions
{
    public List<TestUserOptions> Users { get; set; } = new();
}

public sealed class TestUserOptions
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Area { get; set; }
    public List<string> Roles { get; set; } = new();
}
