using Microsoft.AspNetCore.Components;

namespace Auth.Web.Components.Account.Pages;

public partial class AccessDenied : ComponentBase
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
}
