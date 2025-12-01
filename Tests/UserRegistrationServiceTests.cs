using System.Threading;
using System.Threading.Tasks;
using Auth.Web.Application.Dtos;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Implementations.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Auth.Web.Tests
{
    public class UserRegistrationServiceTests
    {
        private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        private static Mock<IUserEmailStore<ApplicationUser>> CreateUserStoreMock()
        {
            return new Mock<IUserEmailStore<ApplicationUser>>();
        }

        [Fact]
        public async Task RegisterUserAsync_Returns_ValidationError_When_Missing_Fields()
        {
            var ad = new Mock<Auth.Web.Services.Abstractions.Auth.IActiveDirectoryAuthService>();
            var um = CreateUserManagerMock();
            var store = CreateUserStoreMock();
            var svc = new UserRegistrationService(ad.Object, um.Object, store.Object, new Mock<ILogger<UserRegistrationService>>().Object);

            var res = await svc.RegisterUserAsync(new RegisterUserRequest { Email = "", FullName = "" });
            Assert.Equal("Complete los campos requeridos.", res.Message);
        }

        [Fact]
        public async Task RegisterUserAsync_Returns_AlreadyExists_When_Email_Exists()
        {
            var ad = new Mock<Auth.Web.Services.Abstractions.Auth.IActiveDirectoryAuthService>();
            var um = CreateUserManagerMock();
            var store = CreateUserStoreMock();

            um.Setup(x => x.FindByEmailAsync("a@b.com")).ReturnsAsync(new ApplicationUser { Id = "u1", Email = "a@b.com" });
            var svc = new UserRegistrationService(ad.Object, um.Object, store.Object, new Mock<ILogger<UserRegistrationService>>().Object);

            var res = await svc.RegisterUserAsync(new RegisterUserRequest { Email = "a@b.com", FullName = "Name" });
            Assert.Equal("El correo ya está registrado.", res.Message);
        }

        [Fact]
        public async Task RegisterUserAsync_Returns_NotInActiveDirectory_When_NotInAd()
        {
            var ad = new Mock<Auth.Web.Services.Abstractions.Auth.IActiveDirectoryAuthService>();
            ad.Setup(x => x.ExistsByEmailAsync("c@d.com")).ReturnsAsync(false);
            var um = CreateUserManagerMock();
            var store = CreateUserStoreMock();

            var svc = new UserRegistrationService(ad.Object, um.Object, store.Object, new Mock<ILogger<UserRegistrationService>>().Object);
            var res = await svc.RegisterUserAsync(new RegisterUserRequest { Email = "c@d.com", FullName = "Name" });
            Assert.Equal("El correo no pertenece al dominio (AD) o no existe en el directorio.", res.Message);
        }

        [Fact]
        public async Task RegisterUserAsync_Creates_User_When_Valid_And_InAD()
        {
            var ad = new Mock<Auth.Web.Services.Abstractions.Auth.IActiveDirectoryAuthService>();
            ad.Setup(x => x.ExistsByEmailAsync("e@d.com")).ReturnsAsync(true);
            var um = CreateUserManagerMock();
            var store = CreateUserStoreMock();

            um.SetupGet(x => x.SupportsUserEmail).Returns(true);
            um.Setup(x => x.FindByEmailAsync("e@d.com")).ReturnsAsync((ApplicationUser?)null);
            um.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            // Setup user store methods used by the service
            store.Setup(s => s.SetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            store.Setup(s => s.SetEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var svc = new UserRegistrationService(ad.Object, um.Object, store.Object, new Mock<ILogger<UserRegistrationService>>().Object);
            var res = await svc.RegisterUserAsync(new RegisterUserRequest { Email = "e@d.com", FullName = "Valid Name" });
            Assert.Equal("Cuenta creada correctamente. Inicie sesión.", res.Message);
        }
    }
}
