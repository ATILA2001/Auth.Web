using Microsoft.AspNetCore.Components;

namespace Auth.Web.Components.Account.Pages.Manage;

public partial class PersonalData : ComponentBase
{
    [Inject] private IdentityUserAccessor UserAccessor { get; set; } = default!;
    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _ = await UserAccessor.GetRequiredUserAsync(HttpContext);
    }
}
