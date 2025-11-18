using Microsoft.AspNetCore.Components;

namespace Auth.Web.Components.Account.Shared;

public partial class AccountLayout : LayoutComponentBase
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (HttpContext is null)
        {
            NavigationManager.Refresh(forceReload: true);
        }
    }
}
