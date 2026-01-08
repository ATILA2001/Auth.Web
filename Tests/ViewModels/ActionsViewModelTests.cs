using Auth.Web.Components.Admin.Actions;
using Auth.Web.Application.Admin.Dtos;
using Auth.Web.Services.Abstractions.Admin;
using Moq;
using Xunit;

namespace Auth.Web.Tests.ViewModels;

public class ActionsViewModelTests
{
    private static Mock<IAdminActionPermissionService> CreateServiceMock()
    {
        return new Mock<IAdminActionPermissionService>();
    }

    [Fact]
    public async Task LoadAsync_Loads_Actions_From_Service()
    {
        var serviceMock = CreateServiceMock();
        var actions = new List<ActionPermissionAdminDto>
        {
            new() { Id = 1, Name = "Read", UsageCount = 5 },
            new() { Id = 2, Name = "Write", UsageCount = 3 }
        };
        serviceMock.Setup(s => s.GetActionsAsync(default)).ReturnsAsync(actions);

        var vm = new ActionsViewModel(serviceMock.Object);
        await vm.LoadAsync();

        Assert.Equal(2, vm.Actions.Count);
        Assert.Contains(vm.Actions, a => a.Name == "Read");
        Assert.Contains(vm.Actions, a => a.Name == "Write");
    }

    [Fact]
    public void BeginCreate_Initializes_New_Action()
    {
        var serviceMock = CreateServiceMock();
        var vm = new ActionsViewModel(serviceMock.Object);

        vm.BeginCreate();

        Assert.Equal(0, vm.EditModel.Id);
        Assert.Empty(vm.EditName);
        Assert.Null(vm.ValidationError);
    }

    [Fact]
    public void BeginEdit_Sets_Edit_Model()
    {
        var serviceMock = CreateServiceMock();
        var vm = new ActionsViewModel(serviceMock.Object);
        var action = new ActionPermissionAdminDto { Id = 5, Name = "Delete", UsageCount = 2 };

        vm.BeginEdit(action);

        Assert.Equal(5, vm.EditModel.Id);
        Assert.Equal("Delete", vm.EditName);
        Assert.Null(vm.ValidationError);
    }

    [Fact]
    public void ValidateOnly_Returns_Error_When_Name_Empty()
    {
        var serviceMock = CreateServiceMock();
        var vm = new ActionsViewModel(serviceMock.Object);
        vm.BeginCreate();

        var result = vm.ValidateOnly("");

        Assert.Equal(ActionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("vacío", result.Message);
        Assert.NotNull(vm.ValidationError);
    }

    [Fact]
    public void ValidateOnly_Returns_Error_When_Name_Whitespace()
    {
        var serviceMock = CreateServiceMock();
        var vm = new ActionsViewModel(serviceMock.Object);
        vm.BeginCreate();

        var result = vm.ValidateOnly("   ");

        Assert.Equal(ActionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("vacío", result.Message);
    }

    [Fact]
    public void ValidateOnly_Returns_Error_When_Duplicate_Name()
    {
        var serviceMock = CreateServiceMock();
        var vm = new ActionsViewModel(serviceMock.Object);
        vm.Actions.Add(new ActionPermissionAdminDto { Id = 1, Name = "Read", UsageCount = 0 });
        vm.BeginCreate();

        var result = vm.ValidateOnly("Read");

        Assert.Equal(ActionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("existe", result.Message);
        Assert.NotNull(vm.ValidationError);
    }

    [Fact]
    public void ValidateOnly_Returns_Error_When_Duplicate_Name_Case_Insensitive()
    {
        var serviceMock = CreateServiceMock();
        var vm = new ActionsViewModel(serviceMock.Object);
        vm.Actions.Add(new ActionPermissionAdminDto { Id = 1, Name = "Read", UsageCount = 0 });
        vm.BeginCreate();

        var result = vm.ValidateOnly("READ");

        Assert.Equal(ActionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("existe", result.Message);
    }

    [Fact]
    public void ValidateOnly_Returns_Success_When_Valid()
    {
        var serviceMock = CreateServiceMock();
        var vm = new ActionsViewModel(serviceMock.Object);
        vm.BeginCreate();

        var result = vm.ValidateOnly("NewAction");

        Assert.Equal(ActionsVmOutcome.Success, result.Outcome);
        Assert.Null(vm.ValidationError);
    }

    [Fact]
    public void ValidateOnly_Allows_Same_Name_For_Same_Action_On_Edit()
    {
        var serviceMock = CreateServiceMock();
        var vm = new ActionsViewModel(serviceMock.Object);
        vm.Actions.Add(new ActionPermissionAdminDto { Id = 1, Name = "Read", UsageCount = 0 });
        vm.BeginEdit(vm.Actions[0]);

        var result = vm.ValidateOnly("Read");

        Assert.Equal(ActionsVmOutcome.Success, result.Outcome);
        Assert.Null(vm.ValidationError);
    }

    [Fact]
    public async Task SaveAsync_Creates_Action_When_Id_Zero()
    {
        var serviceMock = CreateServiceMock();
        serviceMock.Setup(s => s.CreateActionAsync("NewAction", default)).ReturnsAsync(10);

        var vm = new ActionsViewModel(serviceMock.Object);
        vm.BeginCreate();
        vm.EditName = "NewAction";

        var result = await vm.SaveAsync();

        Assert.Equal(ActionsVmOutcome.Success, result.Outcome);
        Assert.Equal(10, result.CreatedId);
        Assert.False(result.RequiresReload);
        serviceMock.Verify(s => s.CreateActionAsync("NewAction", default), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_Updates_Action_When_Id_Exists()
    {
        var serviceMock = CreateServiceMock();
        serviceMock.Setup(s => s.UpdateActionAsync(5, "UpdatedName", default)).Returns(Task.CompletedTask);

        var vm = new ActionsViewModel(serviceMock.Object);
        var action = new ActionPermissionAdminDto { Id = 5, Name = "OldName", UsageCount = 0 };
        vm.BeginEdit(action);
        vm.EditName = "UpdatedName";

        var result = await vm.SaveAsync();

        Assert.Equal(ActionsVmOutcome.Success, result.Outcome);
        Assert.False(result.RequiresReload);
        serviceMock.Verify(s => s.UpdateActionAsync(5, "UpdatedName", default), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_Returns_ValidationError_When_Name_Empty()
    {
        var serviceMock = CreateServiceMock();
        var vm = new ActionsViewModel(serviceMock.Object);
        vm.BeginCreate();
        vm.EditName = "";

        var result = await vm.SaveAsync();

        Assert.Equal(ActionsVmOutcome.ValidationError, result.Outcome);
        serviceMock.Verify(s => s.CreateActionAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_Returns_ValidationError_When_CreateAsync_Returns_Zero()
    {
        var serviceMock = CreateServiceMock();
        serviceMock.Setup(s => s.CreateActionAsync(It.IsAny<string>(), default)).ReturnsAsync(0);

        var vm = new ActionsViewModel(serviceMock.Object);
        vm.BeginCreate();
        vm.EditName = "ValidName";

        var result = await vm.SaveAsync();

        Assert.Equal(ActionsVmOutcome.ValidationError, result.Outcome);
        Assert.NotNull(vm.ValidationError);
    }

    [Fact]
    public async Task SaveAsync_Returns_Error_When_Exception_Occurs()
    {
        var serviceMock = CreateServiceMock();
        serviceMock.Setup(s => s.CreateActionAsync(It.IsAny<string>(), default))
            .ThrowsAsync(new Exception("Database error"));

        var vm = new ActionsViewModel(serviceMock.Object);
        vm.BeginCreate();
        vm.EditName = "ValidName";

        var result = await vm.SaveAsync();

        Assert.Equal(ActionsVmOutcome.Error, result.Outcome);
        Assert.Contains("Database error", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_Calls_Service_And_Requires_Reload()
    {
        var serviceMock = CreateServiceMock();
        serviceMock.Setup(s => s.DeleteActionAsync(3, default)).Returns(Task.CompletedTask);

        var vm = new ActionsViewModel(serviceMock.Object);

        var result = await vm.DeleteAsync(3);

        Assert.Equal(ActionsVmOutcome.Success, result.Outcome);
        Assert.True(result.RequiresReload);
        serviceMock.Verify(s => s.DeleteActionAsync(3, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Returns_Error_When_Exception_Occurs()
    {
        var serviceMock = CreateServiceMock();
        serviceMock.Setup(s => s.DeleteActionAsync(It.IsAny<int>(), default))
            .ThrowsAsync(new Exception("Cannot delete"));

        var vm = new ActionsViewModel(serviceMock.Object);

        var result = await vm.DeleteAsync(5);

        Assert.Equal(ActionsVmOutcome.Error, result.Outcome);
        Assert.Contains("Cannot delete", result.Message);
    }

    [Fact]
    public void ValidateOnly_Trims_Name_Before_Validation()
    {
        var serviceMock = CreateServiceMock();
        var vm = new ActionsViewModel(serviceMock.Object);
        vm.Actions.Add(new ActionPermissionAdminDto { Id = 1, Name = "Read", UsageCount = 0 });
        vm.BeginCreate();

        var result = vm.ValidateOnly("  Read  ");

        Assert.Equal(ActionsVmOutcome.ValidationError, result.Outcome);
        Assert.Contains("existe", result.Message);
    }
}
