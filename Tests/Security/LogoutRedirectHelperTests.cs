using Auth.Web.Security;
using Xunit;

namespace Auth.Web.Tests.Security;

public class LogoutRedirectHelperTests
{
    [Fact]
    public void Resolve_Returns_Default_When_Null()
    {
        var result = LogoutRedirectHelper.Resolve(null);
        Assert.Equal("/Account/Login", result);
    }

    [Fact]
    public void Resolve_Returns_Default_When_Empty()
    {
        var result = LogoutRedirectHelper.Resolve("");
        Assert.Equal("/Account/Login", result);
    }

    [Fact]
    public void Resolve_Returns_Default_When_Whitespace()
    {
        var result = LogoutRedirectHelper.Resolve("   ");
        Assert.Equal("/Account/Login", result);
    }

    [Fact]
    public void Resolve_Returns_Url_When_Valid_Local_Slash()
    {
        var result = LogoutRedirectHelper.Resolve("/admin");
        Assert.Equal("/admin", result);
    }

    [Fact]
    public void Resolve_Returns_Url_When_Valid_Tilde_Slash()
    {
        var result = LogoutRedirectHelper.Resolve("~/Account/Login");
        Assert.Equal("~/Account/Login", result);
    }

    [Fact]
    public void Resolve_Returns_Default_When_Double_Slash()
    {
        var result = LogoutRedirectHelper.Resolve("//evil.com");
        Assert.Equal("/Account/Login", result);
    }

    [Fact]
    public void Resolve_Returns_Default_When_Backslash_After_Slash()
    {
        var result = LogoutRedirectHelper.Resolve("/\\evil.com");
        Assert.Equal("/Account/Login", result);
    }

    [Fact]
    public void Resolve_Returns_Default_When_Contains_Carriage_Return()
    {
        var result = LogoutRedirectHelper.Resolve("/admin\r");
        Assert.Equal("/Account/Login", result);
    }

    [Fact]
    public void Resolve_Returns_Default_When_Contains_Newline()
    {
        var result = LogoutRedirectHelper.Resolve("/admin\n");
        Assert.Equal("/Account/Login", result);
    }

    [Fact]
    public void Resolve_Returns_Default_When_Contains_Null_Character()
    {
        var result = LogoutRedirectHelper.Resolve("/admin\0");
        Assert.Equal("/Account/Login", result);
    }

    [Fact]
    public void Resolve_Returns_Default_When_Absolute_Url()
    {
        var result = LogoutRedirectHelper.Resolve("https://evil.com");
        Assert.Equal("/Account/Login", result);
    }

    [Fact]
    public void Resolve_Returns_Default_When_Protocol_Relative()
    {
        var result = LogoutRedirectHelper.Resolve("//evil.com/path");
        Assert.Equal("/Account/Login", result);
    }

    [Fact]
    public void Resolve_Returns_Url_When_Valid_Path_With_Query()
    {
        var result = LogoutRedirectHelper.Resolve("/admin?returnUrl=/home");
        Assert.Equal("/admin?returnUrl=/home", result);
    }

    [Fact]
    public void Resolve_Returns_Default_When_Tilde_Without_Slash()
    {
        var result = LogoutRedirectHelper.Resolve("~");
        Assert.Equal("/Account/Login", result);
    }

    [Fact]
    public void Resolve_Uses_Custom_Validator_When_Provided()
    {
        var result = LogoutRedirectHelper.Resolve("/admin", url => false);
        Assert.Equal("/Account/Login", result);
    }

    [Fact]
    public void Resolve_Uses_Custom_Validator_Returns_Url_When_True()
    {
        var result = LogoutRedirectHelper.Resolve("/admin", url => true);
        Assert.Equal("/admin", result);
    }
}
