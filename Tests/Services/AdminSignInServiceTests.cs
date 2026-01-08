using Auth.Web.Services.Implementations.Auth;
using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Auth.Web.Tests.Services;

public class AdminSignInServiceTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock(
        Mock<UserManager<ApplicationUser>> userManager)
    {
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        
        return new Mock<SignInManager<ApplicationUser>>(
            userManager.Object,
            contextAccessor.Object,
            claimsPrincipalFactory.Object,
            null!, null!, null!, null!);
    }

    [Fact]
    public async Task SignInAsync_Calls_SignInManager_When_User_Exists()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock);

        var user = new ApplicationUser
        {
            Id = "admin123",
            UserName = "admin@company.com",
            Email = "admin@company.com"
        };

        userManagerMock.Setup(um => um.FindByIdAsync("admin123"))
            .ReturnsAsync(user);

        signInManagerMock.Setup(sim => sim.SignInAsync(user, false, null))
            .Returns(Task.CompletedTask);

        var service = new AdminSignInService(userManagerMock.Object, signInManagerMock.Object);

        await service.SignInAsync("admin123");

        signInManagerMock.Verify(sim => sim.SignInAsync(user, false, null), Times.Once);
    }

    [Fact]
    public async Task SignInAsync_Does_Not_Call_SignInManager_When_User_Not_Found()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock);

        userManagerMock.Setup(um => um.FindByIdAsync("nonexistent"))
            .ReturnsAsync((ApplicationUser?)null);

        var service = new AdminSignInService(userManagerMock.Object, signInManagerMock.Object);

        await service.SignInAsync("nonexistent");

        signInManagerMock.Verify(sim => sim.SignInAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<bool>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task SignInAsync_Does_Not_Call_SignInManager_When_UserId_Empty()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock);

        var service = new AdminSignInService(userManagerMock.Object, signInManagerMock.Object);

        await service.SignInAsync("");

        userManagerMock.Verify(um => um.FindByIdAsync(It.IsAny<string>()), Times.Never);
        signInManagerMock.Verify(sim => sim.SignInAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<bool>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task SignInAsync_Does_Not_Call_SignInManager_When_UserId_Whitespace()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock);

        var service = new AdminSignInService(userManagerMock.Object, signInManagerMock.Object);

        await service.SignInAsync("   ");

        userManagerMock.Verify(um => um.FindByIdAsync(It.IsAny<string>()), Times.Never);
        signInManagerMock.Verify(sim => sim.SignInAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<bool>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task SignInAsync_Signs_In_With_IsPersistent_False()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock);

        var user = new ApplicationUser { Id = "user1", UserName = "user", Email = "user@test.com" };

        userManagerMock.Setup(um => um.FindByIdAsync("user1"))
            .ReturnsAsync(user);

        bool? capturedIsPersistent = null;
        signInManagerMock.Setup(sim => sim.SignInAsync(user, It.IsAny<bool>(), null))
            .Callback<ApplicationUser, bool, string?>((u, persistent, scheme) => capturedIsPersistent = persistent)
            .Returns(Task.CompletedTask);

        var service = new AdminSignInService(userManagerMock.Object, signInManagerMock.Object);

        await service.SignInAsync("user1");

        Assert.NotNull(capturedIsPersistent);
        Assert.False(capturedIsPersistent.Value);
    }
}
