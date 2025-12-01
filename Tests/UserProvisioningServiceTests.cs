using System;
using System.Threading.Tasks;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Implementations.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Auth.Web.Tests
{
    public class UserProvisioningServiceTests
    {
        private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        [Fact]
        public async Task EnsureUserAsync_CreatesUser_When_NotExists_And_Assigns_Role()
        {
            var um = CreateUserManagerMock();
            um.Setup(x => x.FindByNameAsync("newuser")).ReturnsAsync((ApplicationUser?)null);
            um.Setup(x => x.FindByEmailAsync("newuser")).ReturnsAsync((ApplicationUser?)null);

            var createdUser = new ApplicationUser { Id = "u-created", UserName = "newuser", Email = "newuser" };
            um.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success)
              .Callback<ApplicationUser>(u => { u.Id = createdUser.Id; });

            um.Setup(x => x.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Usuario")).ReturnsAsync(false);
            um.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Usuario")).ReturnsAsync(IdentityResult.Success);

            var logger = new Mock<ILogger<UserProvisioningService>>();
            var svc = new UserProvisioningService(um.Object, logger.Object);

            var user = await svc.EnsureUserAsync("newuser");
            Assert.NotNull(user);
            Assert.Equal(createdUser.Id, user.Id);
            um.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
            um.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Usuario"), Times.Once);
        }

        [Fact]
        public async Task EnsureUserAsync_Uses_ExistingUser_When_Found()
        {
            var um = CreateUserManagerMock();
            var existing = new ApplicationUser { Id = "uX", UserName = "exist", Email = "exist@corp" };
            um.Setup(x => x.FindByNameAsync("exist")).ReturnsAsync(existing);
            um.Setup(x => x.IsInRoleAsync(existing, "Usuario")).ReturnsAsync(true);

            var svc = new UserProvisioningService(um.Object, new Mock<ILogger<UserProvisioningService>>().Object);
            var res = await svc.EnsureUserAsync("exist");
            Assert.Equal(existing.Id, res.Id);
            um.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task EnsureUserAsync_Throws_When_Create_Fails()
        {
            var um = CreateUserManagerMock();
            um.Setup(x => x.FindByNameAsync("bad")).ReturnsAsync((ApplicationUser?)null);
            um.Setup(x => x.FindByEmailAsync("bad")).ReturnsAsync((ApplicationUser?)null);
            var fail = IdentityResult.Failed(new IdentityError { Description = "err" });
            um.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(fail);

            var svc = new UserProvisioningService(um.Object, new Mock<ILogger<UserProvisioningService>>().Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.EnsureUserAsync("bad"));
        }
    }
}
