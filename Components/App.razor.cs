using Microsoft.AspNetCore.Components;
using static Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Auth.Web.Components;

public partial class App : ComponentBase
{
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    private IComponentRenderMode? RenderModeForPage =>
        HttpContext.Request.Path.StartsWithSegments("/Account")
            ? null
            : InteractiveServer;
}
