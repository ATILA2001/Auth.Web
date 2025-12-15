using System.Threading;
using System.Threading.Tasks;

namespace Auth.Web.Services.Abstractions.Auth;

public interface IAdminSignInService
{
    Task SignInAsync(string userId, CancellationToken cancellationToken = default);
}
