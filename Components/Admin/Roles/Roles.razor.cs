using Microsoft.AspNetCore.Components;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Admin.Dtos;
using Radzen;
using Radzen.Blazor;
using System.ComponentModel.DataAnnotations;

namespace Auth.Web.Components.Admin.Roles;

public partial class Roles : ComponentBase
{
    [Inject] private IAdminRoleService RoleService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private RolesViewModel _vm = null!;
    private RadzenDataGrid<RoleAdminDto> grid = null!;

    private List<RoleAdminDto> roles => _vm.Roles;
    private RoleAdminDto editModel => _vm.EditModel;
    private string editName
    {
        get => _vm.EditName;
        set => _vm.EditName = value;
    }

    private readonly Dictionary<RoleAdminDto, string> _nameBuffer = new();

    private string GetNameBuffer(RoleAdminDto role)
    {
        if (!_nameBuffer.TryGetValue(role, out var value))
        {
            value = role.Name;
            _nameBuffer[role] = value;
        }
        return value;
    }

    private void SetNameBuffer(RoleAdminDto role, string value)
    {
        _nameBuffer[role] = value;
    }

    protected override void OnInitialized()
    {
        _vm = new RolesViewModel(RoleService);
    }

    protected override async Task OnInitializedAsync()
    {
        await _vm.LoadAsync();
    }

    private async Task BeginCreate()
    {
        _vm.BeginCreate();
        var newRole = new RoleAdminDto { Id = string.Empty, Name = string.Empty, UserCount = 0 };
        roles.Insert(0, newRole);
        await grid.InsertRow(newRole);
    }

    private async Task OnRowCreate(RoleAdminDto role)
    {
        _vm.BeginCreate();
        editName = GetNameBuffer(role);
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
        }

        if (result.Outcome == RolesVmOutcome.ValidationError)
        {
            roles.Remove(role);
            await grid.Reload();
        }
    }

    private async Task EditRow(RoleAdminDto role)
    {
        await grid.EditRow(role);
    }

    private async Task OnRowUpdate(RoleAdminDto role)
    {
        _vm.BeginEdit(role);
        editName = GetNameBuffer(role);
        var result = await _vm.SaveAsync();
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
        }
    }

    private async Task DeleteRole(string roleId)
    {
        var confirm = await DialogService.Confirm("¿Eliminar el rol?", "Confirmar", new ConfirmOptions { OkButtonText = "Eliminar", CancelButtonText = "Cancelar", Icon = "warning" });
        if (confirm != true)
        {
            return;
        }

        var result = await _vm.DeleteAsync(roleId);
        NotifyUser(result);

        if (result.RequiresReload)
        {
            await _vm.LoadAsync();
            await grid.Reload();
        }
    }

    private void CancelEditRow(RoleAdminDto role)
    {
        grid.CancelEditRow(role);
        _nameBuffer.Remove(role);
        if (string.IsNullOrWhiteSpace(role.Id))
        {
            roles.Remove(role);
        }
    }

    private void ClearFilters()
    {
        grid.Reset(true);
    }

    private void NotifyUser(RolesVmResult result)
    {
        var severity = result.Outcome switch
        {
            RolesVmOutcome.Success => NotificationSeverity.Success,
            RolesVmOutcome.ValidationError => NotificationSeverity.Warning,
            RolesVmOutcome.Error => NotificationSeverity.Error,
            _ => NotificationSeverity.Info
        };

        NotificationService.Notify(severity, result.Title, result.Message);
    }
}

public sealed class RoleFormModel
{
    [Required(ErrorMessage = "Nombre requerido")]
    public string Name { get; set; } = string.Empty;
}
