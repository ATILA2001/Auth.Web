using Auth.Web.Components.Admin.Permissions;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Admin;
using Moq;
using Xunit;

namespace Auth.Web.Tests.ViewModels;

public class PermissionsViewModelTests
{
    private readonly Mock<IAdminAreaPagePermissionService> _permissionServiceMock;
    private readonly Mock<IAdminAreaService> _areaServiceMock;
    private readonly Mock<IAdminPageService> _pageServiceMock;
    private readonly Mock<IAdminActionPermissionService> _actionServiceMock;

    public PermissionsViewModelTests()
    {
        _permissionServiceMock = new Mock<IAdminAreaPagePermissionService>();
        _areaServiceMock = new Mock<IAdminAreaService>();
        _pageServiceMock = new Mock<IAdminPageService>();
        _actionServiceMock = new Mock<IAdminActionPermissionService>();
    }

    private PermissionsViewModel CreateViewModel() =>
        new(_permissionServiceMock.Object, _areaServiceMock.Object, _pageServiceMock.Object, _actionServiceMock.Object);

    private static List<AreaAdminDto> DefaultAreas() =>
    [
        new() { Id = 1, Name = "Redeterminaciones" },
        new() { Id = 2, Name = "Presupuesto" }
    ];

    private static List<PageAdminDto> DefaultPages() =>
    [
        new() { Id = 1, Name = "Dashboard", Url = "/dashboard", ClientName = "SAI" },
        new() { Id = 2, Name = "Settings", Url = "/settings", ClientName = "SAI" }
    ];

    private static List<ActionPermissionAdminDto> DefaultActions() =>
    [
        new() { Id = 1, Name = "Ver", UsageCount = 5 },
        new() { Id = 2, Name = "Editar", UsageCount = 3 }
    ];

    private void SetupMasterData(
        List<AreaAdminDto>? areas = null,
        List<PageAdminDto>? pages = null,
        List<ActionPermissionAdminDto>? actions = null)
    {
        _areaServiceMock.Setup(s => s.GetAreasAsync(default)).ReturnsAsync(areas ?? DefaultAreas());
        _pageServiceMock.Setup(s => s.GetPagesAsync(default)).ReturnsAsync(pages ?? DefaultPages());
        _actionServiceMock.Setup(s => s.GetActionsAsync(default)).ReturnsAsync(actions ?? DefaultActions());
    }

    #region Constructor

    [Fact]
    public void Constructor_Throws_When_PermissionService_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PermissionsViewModel(null!, _areaServiceMock.Object, _pageServiceMock.Object, _actionServiceMock.Object));
    }

    [Fact]
    public void Constructor_Throws_When_AreaService_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PermissionsViewModel(_permissionServiceMock.Object, null!, _pageServiceMock.Object, _actionServiceMock.Object));
    }

    [Fact]
    public void Constructor_Throws_When_PageService_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PermissionsViewModel(_permissionServiceMock.Object, _areaServiceMock.Object, null!, _actionServiceMock.Object));
    }

    [Fact]
    public void Constructor_Throws_When_ActionService_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PermissionsViewModel(_permissionServiceMock.Object, _areaServiceMock.Object, _pageServiceMock.Object, null!));
    }

    #endregion

    #region LoadAsync

    [Fact]
    public async Task LoadAsync_Loads_Areas_Pages_Actions()
    {
        SetupMasterData();
        var vm = CreateViewModel();

        await vm.LoadAsync();

        Assert.Equal(2, vm.Areas.Count);
        Assert.Equal(2, vm.Pages.Count);
        Assert.Equal(2, vm.Actions.Count);
    }

    [Fact]
    public async Task LoadAsync_Returns_Empty_Lists_When_No_Data()
    {
        SetupMasterData(
            areas: [],
            pages: [],
            actions: []);
        var vm = CreateViewModel();

        await vm.LoadAsync();

        Assert.Empty(vm.Areas);
        Assert.Empty(vm.Pages);
        Assert.Empty(vm.Actions);
    }

    [Fact]
    public async Task LoadAsync_MatrixRows_Empty_Before_LoadMatrixForArea_Called()
    {
        SetupMasterData();
        var vm = CreateViewModel();

        await vm.LoadAsync();

        Assert.Empty(vm.MatrixRows);
    }

    #endregion

    #region LoadMatrixForAreaAsync

    [Fact]
    public async Task LoadMatrixForAreaAsync_Builds_One_Row_Per_Page()
    {
        SetupMasterData();
        _permissionServiceMock
            .Setup(s => s.GetPermissionsByAreaAsync(1, default))
            .ReturnsAsync(new List<AreaPagePermissionAdminDto>());
        var vm = CreateViewModel();
        await vm.LoadAsync();

        await vm.LoadMatrixForAreaAsync(1);

        Assert.Equal(2, vm.MatrixRows.Count);
    }

    [Fact]
    public async Task LoadMatrixForAreaAsync_Marks_Assigned_Action_As_Enabled()
    {
        SetupMasterData();
        _permissionServiceMock
            .Setup(s => s.GetPermissionsByAreaAsync(1, default))
            .ReturnsAsync(new List<AreaPagePermissionAdminDto>
            {
                new() { Id = 10, AreaId = 1, PageId = 1, ActionPermissionId = 1 }
            });
        var vm = CreateViewModel();
        await vm.LoadAsync();

        await vm.LoadMatrixForAreaAsync(1);

        var row = vm.MatrixRows.Single(r => r.PageId == 1);
        Assert.True(row.IsEnabled(1));   // Ver — asignado
        Assert.False(row.IsEnabled(2));  // Editar — no asignado
    }

    [Fact]
    public async Task LoadMatrixForAreaAsync_Stores_PermissionId_For_Deletion()
    {
        SetupMasterData();
        _permissionServiceMock
            .Setup(s => s.GetPermissionsByAreaAsync(1, default))
            .ReturnsAsync(new List<AreaPagePermissionAdminDto>
            {
                new() { Id = 42, AreaId = 1, PageId = 2, ActionPermissionId = 2 }
            });
        var vm = CreateViewModel();
        await vm.LoadAsync();

        await vm.LoadMatrixForAreaAsync(1);

        var row = vm.MatrixRows.Single(r => r.PageId == 2);
        Assert.Equal(42, row.GetPermissionId(2));
        Assert.Null(row.GetPermissionId(1));
    }

    [Fact]
    public async Task LoadMatrixForAreaAsync_Produces_Empty_Matrix_When_No_Pages()
    {
        SetupMasterData(pages: []);
        _permissionServiceMock
            .Setup(s => s.GetPermissionsByAreaAsync(1, default))
            .ReturnsAsync(new List<AreaPagePermissionAdminDto>());
        var vm = CreateViewModel();
        await vm.LoadAsync();

        await vm.LoadMatrixForAreaAsync(1);

        Assert.Empty(vm.MatrixRows);
    }

    [Fact]
    public async Task LoadMatrixForAreaAsync_All_Actions_Disabled_When_No_Permissions()
    {
        SetupMasterData();
        _permissionServiceMock
            .Setup(s => s.GetPermissionsByAreaAsync(1, default))
            .ReturnsAsync(new List<AreaPagePermissionAdminDto>());
        var vm = CreateViewModel();
        await vm.LoadAsync();

        await vm.LoadMatrixForAreaAsync(1);

        foreach (var row in vm.MatrixRows)
        foreach (var action in vm.Actions)
        {
            Assert.False(row.IsEnabled(action.Id));
            Assert.Null(row.GetPermissionId(action.Id));
        }
    }

    [Fact]
    public async Task LoadMatrixForAreaAsync_Replacing_Permissions_On_Second_Call()
    {
        SetupMasterData();
        _permissionServiceMock
            .Setup(s => s.GetPermissionsByAreaAsync(1, default))
            .ReturnsAsync(new List<AreaPagePermissionAdminDto>
            {
                new() { Id = 5, AreaId = 1, PageId = 1, ActionPermissionId = 1 }
            });
        var vm = CreateViewModel();
        await vm.LoadAsync();
        await vm.LoadMatrixForAreaAsync(1);
        Assert.True(vm.MatrixRows.Single(r => r.PageId == 1).IsEnabled(1));

        _permissionServiceMock
            .Setup(s => s.GetPermissionsByAreaAsync(1, default))
            .ReturnsAsync(new List<AreaPagePermissionAdminDto>());
        await vm.LoadMatrixForAreaAsync(1);

        Assert.False(vm.MatrixRows.Single(r => r.PageId == 1).IsEnabled(1));
    }

    [Fact]
    public async Task LoadMatrixForAreaAsync_ActionMap_Contains_All_Actions_As_Keys()
    {
        SetupMasterData();
        _permissionServiceMock
            .Setup(s => s.GetPermissionsByAreaAsync(1, default))
            .ReturnsAsync(new List<AreaPagePermissionAdminDto>());
        var vm = CreateViewModel();
        await vm.LoadAsync();

        await vm.LoadMatrixForAreaAsync(1);

        foreach (var row in vm.MatrixRows)
        foreach (var action in vm.Actions)
            Assert.True(row.ActionMap.ContainsKey(action.Id));
    }

    #endregion

    #region PermissionMatrixRow

    [Fact]
    public void PermissionMatrixRow_IsEnabled_Returns_False_For_Unknown_ActionId()
    {
        var row = new PermissionMatrixRow();
        Assert.False(row.IsEnabled(999));
    }

    [Fact]
    public void PermissionMatrixRow_IsEnabled_Returns_False_When_Id_Is_Null()
    {
        var row = new PermissionMatrixRow();
        row.ActionMap[1] = null;
        Assert.False(row.IsEnabled(1));
    }

    [Fact]
    public void PermissionMatrixRow_IsEnabled_Returns_True_When_Id_Is_Set()
    {
        var row = new PermissionMatrixRow();
        row.ActionMap[1] = 42;
        Assert.True(row.IsEnabled(1));
    }

    [Fact]
    public void PermissionMatrixRow_GetPermissionId_Returns_Null_For_Unknown_ActionId()
    {
        var row = new PermissionMatrixRow();
        Assert.Null(row.GetPermissionId(999));
    }

    [Fact]
    public void PermissionMatrixRow_GetPermissionId_Returns_Stored_Id()
    {
        var row = new PermissionMatrixRow();
        row.ActionMap[3] = 77;
        Assert.Equal(77, row.GetPermissionId(3));
    }

    #endregion
}