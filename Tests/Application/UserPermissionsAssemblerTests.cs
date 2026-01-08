using Auth.Web.Application.Permissions;
using Auth.Web.Application.Permissions.Dtos;
using Auth.Web.Data.Entities;
using Xunit;

namespace Auth.Web.Tests.Application;

public class UserPermissionsAssemblerTests
{
    [Fact]
    public void BuildClaims_Creates_Claims_With_All_Properties()
    {
        var assembler = new UserPermissionsAssembler();
        var user = new ApplicationUser
        {
            Id = "user123",
            Email = "test@example.com",
            UserName = "testuser",
            Nombre = "Test User Display"
        };
        var roles = new List<string> { "Admin", "User" };
        var permissions = new UserPermissionsDto
        {
            Areas = new List<int> { 1, 2, 3 },
            Version = 5
        };
        var apps = new List<string> { "App1", "App2" };

        var result = assembler.BuildClaims(user, roles, permissions, apps);

        Assert.Equal("user123", result.UserId);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User Display", result.DisplayName);
        Assert.Equal(2, result.Roles.Count);
        Assert.Contains("Admin", result.Roles);
        Assert.Contains("User", result.Roles);
        Assert.Equal(3, result.Areas.Count);
        Assert.Contains("1", result.Areas);
        Assert.Contains("2", result.Areas);
        Assert.Contains("3", result.Areas);
        Assert.Equal(2, result.Apps.Count);
        Assert.Contains("App1", result.Apps);
        Assert.Contains("App2", result.Apps);
        Assert.Equal(5, result.PermissionsVersion);
    }

    [Fact]
    public void BuildClaims_Converts_Areas_To_Strings()
    {
        var assembler = new UserPermissionsAssembler();
        var user = new ApplicationUser { Id = "u1", Email = "e@e.com", UserName = "u" };
        var permissions = new UserPermissionsDto
        {
            Areas = new List<int> { 100, 200, 300 },
            Version = 1
        };

        var result = assembler.BuildClaims(user, Array.Empty<string>(), permissions, Array.Empty<string>());

        Assert.All(result.Areas, area => Assert.True(int.TryParse(area, out _)));
        Assert.Contains("100", result.Areas);
        Assert.Contains("200", result.Areas);
        Assert.Contains("300", result.Areas);
    }

    [Fact]
    public void BuildClaims_Uses_DisplayName_When_Nombre_Present()
    {
        var assembler = new UserPermissionsAssembler();
        var user = new ApplicationUser
        {
            Id = "u1",
            Email = "e@e.com",
            UserName = "username",
            Nombre = "Display Name"
        };
        var permissions = new UserPermissionsDto { Areas = new List<int>(), Version = 1 };

        var result = assembler.BuildClaims(user, Array.Empty<string>(), permissions, Array.Empty<string>());

        Assert.Equal("Display Name", result.DisplayName);
    }

    [Fact]
    public void BuildClaims_Uses_UserName_When_Nombre_Null()
    {
        var assembler = new UserPermissionsAssembler();
        var user = new ApplicationUser
        {
            Id = "u1",
            Email = "e@e.com",
            UserName = "fallbackname",
            Nombre = null
        };
        var permissions = new UserPermissionsDto { Areas = new List<int>(), Version = 1 };

        var result = assembler.BuildClaims(user, Array.Empty<string>(), permissions, Array.Empty<string>());

        Assert.Equal("fallbackname", result.DisplayName);
    }

    [Fact]
    public void BuildClaims_Handles_Empty_Collections()
    {
        var assembler = new UserPermissionsAssembler();
        var user = new ApplicationUser { Id = "u1", Email = "e@e.com", UserName = "u" };
        var permissions = new UserPermissionsDto { Areas = new List<int>(), Version = 0 };

        var result = assembler.BuildClaims(user, Array.Empty<string>(), permissions, Array.Empty<string>());

        Assert.Empty(result.Roles);
        Assert.Empty(result.Areas);
        Assert.Empty(result.Apps);
        Assert.Equal(0, result.PermissionsVersion);
    }

    [Fact]
    public void BuildClaims_Preserves_PermissionsVersion()
    {
        var assembler = new UserPermissionsAssembler();
        var user = new ApplicationUser { Id = "u1", Email = "e@e.com", UserName = "u" };
        var permissions = new UserPermissionsDto { Areas = new List<int>(), Version = 42 };

        var result = assembler.BuildClaims(user, Array.Empty<string>(), permissions, Array.Empty<string>());

        Assert.Equal(42, result.PermissionsVersion);
    }
}
