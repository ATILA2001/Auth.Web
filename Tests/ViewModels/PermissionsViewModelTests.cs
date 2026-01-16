using Auth.Web.Components.Admin.Permissions;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Admin;
using Moq;
using Xunit;

namespace Auth.Web.Tests.ViewModels;

public class PermissionsViewModelTests
{
    private readonly Mock<IAdminRolePagePermissionService> _permissionServiceMock;
    private readonly Mock<IAdminRoleService> _roleServiceMock;
    private readonly Mock<IAdminPageService> _pageServiceMock;
    private readonly Mock<IAdminActionPermissionService> _actionServiceMock;

    public PermissionsViewModelTests()
    {
        _permissionServiceMock = new Mock<IAdminRolePagePermissionService>();
        _roleServiceMock = new Mock<IAdminRoleService>();
        _pageServiceMock = new Mock<IAdminPageService>();
        _actionServiceMock = new Mock<IAdminActionPermissionService>();
    }

    private PermissionsViewModel CreateViewModel() =>
        new(_permissionServiceMock.Object, _roleServiceMock.Object, _pageServiceMock.Object, _actionServiceMock.Object);

    private void SetupDefaultData()
    {
        var roles = new List<RoleAdminDto>
        {
            new() { Id = "role1", Name = "Admin", UserCount = 5 },
            new() { Id = "role2", Name = "User", UserCount = 10 }
        };
        var pages = new List<PageAdminDto>
        {
            new() { Id = 1, Name = "Dashboard", Url = "/dashboard", PermissionCount = 2 },
            new() { Id = 2, Name = "Settings", Url = "/settings", PermissionCount = 1 }
        };
        var actions = new List<ActionPermissionAdminDto>
        {
            new() { Id = 1, Name = "Read", UsageCount = 5 },
            new() { Id = 2, Name = "Write", UsageCount = 3 }
        };
        var permissions = new List<RolePagePermissionAdminDto>
        {
            new() { Id = 1, RoleId = "role1", RoleName = "Admin", PageId = 1, PageName = "Dashboard", ActionPermissionId = 1, ActionName = "Read" }
        };

        _roleServiceMock.Setup(s => s.GetRolesAsync(default)).ReturnsAsync(roles);
        _pageServiceMock.Setup(s => s.GetPagesAsync(default)).ReturnsAsync(pages);
        _actionServiceMock.Setup(s => s.GetActionsAsync(default)).ReturnsAsync(actions);
        _permissionServiceMock.Setup(s => s.GetPermissionsAsync(default)).ReturnsAsync(permissions);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Throws_When_PermissionService_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PermissionsViewModel(null!, _roleServiceMock.Object, _pageServiceMock.Object, _actionServiceMock.Object));
    }

    [Fact]
    public void Constructor_Throws_When_RoleService_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PermissionsViewModel(_permissionServiceMock.Object, null!, _pageServiceMock.Object, _actionServiceMock.Object));
    }

    [Fact]
    public void Constructor_Throws_When_PageService_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PermissionsViewModel(_permissionServiceMock.Object, _roleServiceMock.Object, null!, _actionServiceMock.Object));
    }

    [Fact]
    public void Constructor_Throws_When_ActionService_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PermissionsViewModel(_permissionServiceMock.Object, _roleServiceMock.Object, _pageServiceMock.Object, null!));
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_Loads_All_Data_From_Services()
    {
        SetupDefaultData();
        var vm = CreateViewModel();

        await vm.LoadAsync();

        Assert.Equal(2, vm.Roles.Count);
        Assert.Equal(2, vm.Pages.Count);
        Assert.Equal(2, vm.Actions.Count);
        Assert.Single(vm.Permissions);
    }

    [Fact]
    public async Task LoadAsync_Returns_Empty_Lists_When_No_Data()
    {
        _roleServiceMock.Setup(s => s.GetRolesAsync(default)).ReturnsAsync(new List<RoleAdminDto>());
        _pageServiceMock.Setup(s => s.GetPagesAsync(default)).ReturnsAsync(new List<PageAdminDto>());
        _actionServiceMock.Setup(s => s.GetActionsAsync(default)).ReturnsAsync(new List<ActionPermissionAdminDto>());
        _permissionServiceMock.Setup(s => s.GetPermissionsAsync(default)).ReturnsAsync(new List<RolePagePermissionAdminDto>());

        var vm = CreateViewModel();
        await vm.LoadAsync();

        Assert.Empty(vm.Roles);
        Assert.Empty(vm.Pages);
        Assert.Empty(vm.Actions);
        Assert.Empty(vm.Permissions);
    }

    #endregion

    #region BeginCreate Tests

    [Fact]
    public async Task BeginCreate_Sets_Default_Values_From_First_Items()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.BeginCreate();

        Assert.Equal(0, vm.EditModel.Id);
        Assert.Equal("role1", vm.SelectedRoleId);
        Assert.Equal(1, vm.SelectedPageId);
        Assert.Equal(1, vm.SelectedActionId);
        Assert.Null(vm.ValidationError);
    }

    [Fact]
    public void BeginCreate_Sets_Empty_Values_When_No_Data()
    {
        var vm = CreateViewModel();

        vm.BeginCreate();

        Assert.Equal(0, vm.EditModel.Id);
        Assert.Equal(string.Empty, vm.SelectedRoleId);
        Assert.Equal(0, vm.SelectedPageId);
        Assert.Equal(0, vm.SelectedActionId);
    }

    #endregion

    #region BeginEdit Tests

    [Fact]
    public void BeginEdit_Throws_When_Dto_Is_Null()
    {
        var vm = CreateViewModel();

        Assert.Throws<ArgumentNullException>(() => vm.BeginEdit(null!));
    }

    [Fact]
    public void BeginEdit_Sets_Edit_Model_From_Dto()
    {
        var vm = CreateViewModel();
        var dto = new RolePagePermissionAdminDto
        {
            Id = 5,
            RoleId = "role2",
            PageId = 2,
            ActionPermissionId = 2
        };

        vm.BeginEdit(dto);

        Assert.Equal(5, vm.EditModel.Id);
        Assert.Equal("role2", vm.SelectedRoleId);
        Assert.Equal(2, vm.SelectedPageId);
        Assert.Equal(2, vm.SelectedActionId);
        Assert.Null(vm.ValidationError);
    }

    #endregion

    #region ValidateOnly Tests

    [Fact]
    public async Task ValidateOnly_Fails_When_RoleId_Is_Empty()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        var result = vm.ValidateOnly("", 1, 1);

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("rol", vm.ValidationError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateOnly_Fails_When_PageId_Is_Zero()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        var result = vm.ValidateOnly("role1", 0, 1);

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("página", vm.ValidationError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateOnly_Fails_When_ActionId_Is_Zero()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        var result = vm.ValidateOnly("role1", 1, 0);

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("acción", vm.ValidationError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateOnly_Fails_When_Role_Not_Exists()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        var result = vm.ValidateOnly("nonexistent", 1, 1);

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("rol seleccionado no existe", vm.ValidationError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateOnly_Fails_When_Page_Not_Exists()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        var result = vm.ValidateOnly("role1", 999, 1);

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("página seleccionada no existe", vm.ValidationError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateOnly_Fails_When_Action_Not_Exists()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        var result = vm.ValidateOnly("role1", 1, 999);

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("acción seleccionada no existe", vm.ValidationError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateOnly_Fails_When_Duplicate_Permission_Exists()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        // Trying to create same permission that already exists (role1, page1, action1)
        var result = vm.ValidateOnly("role1", 1, 1);

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("Ya existe", vm.ValidationError);
    }

    [Fact]
    public async Task ValidateOnly_Allows_Same_Combination_When_Editing_Same_Permission()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        // Editing permission ID=1 with same values should be allowed
        var result = vm.ValidateOnly("role1", 1, 1, currentPermissionId: 1);

        Assert.Equal(PermissionsVmOutcome.Success, result.Outcome);
    }

    [Fact]
    public async Task ValidateOnly_Succeeds_With_Valid_Unique_Data()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        // New unique combination
        var result = vm.ValidateOnly("role2", 2, 2);

        Assert.Equal(PermissionsVmOutcome.Success, result.Outcome);
        Assert.Null(vm.ValidationError);
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_Returns_ValidationError_When_Invalid()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.SelectedRoleId = "";
        vm.SelectedPageId = 1;
        vm.SelectedActionId = 1;

        var result = await vm.SaveAsync();

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
    }

    [Fact]
    public async Task SaveAsync_Creates_Permission_When_Valid()
    {
        SetupDefaultData();
        _permissionServiceMock
            .Setup(s => s.CreatePermissionAsync("role2", 2, 2, default))
            .ReturnsAsync(10);

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.SelectedRoleId = "role2";
        vm.SelectedPageId = 2;
        vm.SelectedActionId = 2;

        var result = await vm.SaveAsync();

        Assert.Equal(PermissionsVmOutcome.Success, result.Outcome);
        Assert.Equal(10, result.CreatedId);
        Assert.False(result.RequiresReload);
    }

    [Fact]
    public async Task SaveAsync_Returns_ValidationFailed_When_Service_Returns_Zero()
    {
        SetupDefaultData();
        _permissionServiceMock
            .Setup(s => s.CreatePermissionAsync("role2", 2, 2, default))
            .ReturnsAsync(0);

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.SelectedRoleId = "role2";
        vm.SelectedPageId = 2;
        vm.SelectedActionId = 2;

        var result = await vm.SaveAsync();

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("No se pudo crear", vm.ValidationError);
    }

    [Fact]
    public async Task SaveAsync_Returns_Error_When_Service_Throws()
    {
        SetupDefaultData();
        _permissionServiceMock
            .Setup(s => s.CreatePermissionAsync("role2", 2, 2, default))
            .ThrowsAsync(new Exception("Database error"));

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.SelectedRoleId = "role2";
        vm.SelectedPageId = 2;
        vm.SelectedActionId = 2;

        var result = await vm.SaveAsync();

        Assert.Equal(PermissionsVmOutcome.Error, result.Outcome);
        Assert.Contains("Database error", result.Message);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_Returns_ValidationError_When_Invalid()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.BeginEdit(new RolePagePermissionAdminDto { Id = 1, RoleId = "role1", PageId = 1, ActionPermissionId = 1 });
        vm.SelectedRoleId = "";

        var result = await vm.UpdateAsync();

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
    }

    [Fact]
    public async Task UpdateAsync_Returns_ValidationFailed_When_No_Changes()
    {
        SetupDefaultData();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.BeginEdit(new RolePagePermissionAdminDto { Id = 1, RoleId = "role1", PageId = 1, ActionPermissionId = 1 });
        // No changes made - same values

        var result = await vm.UpdateAsync();

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("No se realizaron cambios", vm.ValidationError);
    }

    [Fact]
    public async Task UpdateAsync_Updates_Permission_When_Changed()
    {
        SetupDefaultData();
        _permissionServiceMock
            .Setup(s => s.UpdatePermissionAsync(1, "role2", 2, 2, default))
            .Returns(Task.CompletedTask);

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.BeginEdit(new RolePagePermissionAdminDto { Id = 1, RoleId = "role1", PageId = 1, ActionPermissionId = 1 });
        vm.SelectedRoleId = "role2";
        vm.SelectedPageId = 2;
        vm.SelectedActionId = 2;

        var result = await vm.UpdateAsync();

        Assert.Equal(PermissionsVmOutcome.Success, result.Outcome);
        Assert.False(result.RequiresReload);
        _permissionServiceMock.Verify(s => s.UpdatePermissionAsync(1, "role2", 2, 2, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Returns_ValidationFailed_When_Duplicate_Exception()
    {
        SetupDefaultData();
        _permissionServiceMock
            .Setup(s => s.UpdatePermissionAsync(1, "role2", 2, 2, default))
            .ThrowsAsync(new InvalidOperationException("Ya existe un permiso"));

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.BeginEdit(new RolePagePermissionAdminDto { Id = 1, RoleId = "role1", PageId = 1, ActionPermissionId = 1 });
        vm.SelectedRoleId = "role2";
        vm.SelectedPageId = 2;
        vm.SelectedActionId = 2;

        var result = await vm.UpdateAsync();

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Error_When_Service_Throws_Other_Exception()
    {
        SetupDefaultData();
        _permissionServiceMock
            .Setup(s => s.UpdatePermissionAsync(1, "role2", 2, 2, default))
            .ThrowsAsync(new Exception("Unexpected error"));

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.BeginEdit(new RolePagePermissionAdminDto { Id = 1, RoleId = "role1", PageId = 1, ActionPermissionId = 1 });
        vm.SelectedRoleId = "role2";
        vm.SelectedPageId = 2;
        vm.SelectedActionId = 2;

        var result = await vm.UpdateAsync();

        Assert.Equal(PermissionsVmOutcome.Error, result.Outcome);
        Assert.Contains("Unexpected error", result.Message);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_Deletes_Permission_Successfully()
    {
        _permissionServiceMock
            .Setup(s => s.DeletePermissionAsync(5, default))
            .Returns(Task.CompletedTask);

        var vm = CreateViewModel();

        var result = await vm.DeleteAsync(5);

        Assert.Equal(PermissionsVmOutcome.Success, result.Outcome);
        Assert.True(result.RequiresReload);
        _permissionServiceMock.Verify(s => s.DeletePermissionAsync(5, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Returns_Error_When_Service_Throws()
    {
        _permissionServiceMock
            .Setup(s => s.DeletePermissionAsync(5, default))
            .ThrowsAsync(new Exception("Delete failed"));

        var vm = CreateViewModel();

        var result = await vm.DeleteAsync(5);

        Assert.Equal(PermissionsVmOutcome.Error, result.Outcome);
        Assert.Contains("Delete failed", result.Message);
    }

    #endregion

    #region PermissionsVmResult Factory Tests

    [Fact]
    public void PermissionsVmResult_Success_Sets_Correct_Properties()
    {
        var result = PermissionsVmResult.Success("Title", "Message", requiresReload: true);

        Assert.Equal(PermissionsVmOutcome.Success, result.Outcome);
        Assert.Equal("Title", result.Title);
        Assert.Equal("Message", result.Message);
        Assert.True(result.RequiresReload);
        Assert.Null(result.CreatedId);
    }

    [Fact]
    public void PermissionsVmResult_CreateSuccess_Sets_CreatedId()
    {
        var result = PermissionsVmResult.CreateSuccess("Title", "Message", 42);

        Assert.Equal(PermissionsVmOutcome.Success, result.Outcome);
        Assert.Equal(42, result.CreatedId);
        Assert.False(result.RequiresReload);
    }

    [Fact]
    public void PermissionsVmResult_ValidationFailed_Sets_Correct_Outcome()
    {
        var result = PermissionsVmResult.ValidationFailed("Title", "Error");

        Assert.Equal(PermissionsVmOutcome.ValidationError, result.Outcome);
        Assert.False(result.RequiresReload);
    }

    [Fact]
    public void PermissionsVmResult_Failed_Sets_Error_Outcome()
    {
        var result = PermissionsVmResult.Failed("Title", "Error");

        Assert.Equal(PermissionsVmOutcome.Error, result.Outcome);
        Assert.False(result.RequiresReload);
    }

    #endregion
}
