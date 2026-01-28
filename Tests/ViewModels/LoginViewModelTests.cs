using Auth.Web.Components.Account.Pages;
using Auth.Web.Application.Users.Registration;
using Auth.Web.Services.Abstractions.Users;
using Moq;
using Xunit;

namespace Auth.Web.Tests.ViewModels;

public class LoginViewModelTests
{
    private static Mock<IUserRegistrationService> CreateRegistrationServiceMock()
    {
        return new Mock<IUserRegistrationService>();
    }

    [Fact]
    public void LoadFromQuery_Parses_Error_Parameter()
    {
        var service = CreateRegistrationServiceMock();
        var vm = new LoginViewModel(service.Object);
        var uri = new Uri("https://localhost/Account/Login?error=InvalidCredentials");

        vm.LoadFromQuery(uri);

        Assert.Equal("InvalidCredentials", vm.ErrorMessage);
    }

    [Fact]
    public void LoadFromQuery_Parses_ReturnUrl_Parameter()
    {
        var service = CreateRegistrationServiceMock();
        var vm = new LoginViewModel(service.Object);
        var uri = new Uri("https://localhost/Account/Login?returnUrl=%2Fadmin");

        vm.LoadFromQuery(uri);

        Assert.Equal("/admin", vm.ReturnUrl);
    }

    [Fact]
    public void LoadFromQuery_Parses_ClientId_Parameter()
    {
        var service = CreateRegistrationServiceMock();
        var vm = new LoginViewModel(service.Object);
        var uri = new Uri("https://localhost/Account/Login?clientId=app123");

        vm.LoadFromQuery(uri);

        Assert.Equal("app123", vm.ClientId);
    }

    [Fact]
    public void LoadFromQuery_Parses_Multiple_Parameters()
    {
        var service = CreateRegistrationServiceMock();
        var vm = new LoginViewModel(service.Object);
        var uri = new Uri("https://localhost/Account/Login?error=AccessDenied&returnUrl=%2Fdashboard&clientId=client1");

        vm.LoadFromQuery(uri);

        Assert.Equal("AccessDenied", vm.ErrorMessage);
        Assert.Equal("/dashboard", vm.ReturnUrl);
        Assert.Equal("client1", vm.ClientId);
    }

    [Fact]
    public void LoadFromQuery_Clears_Previous_Values()
    {
        var service = CreateRegistrationServiceMock();
        var vm = new LoginViewModel(service.Object);
        
        var uri1 = new Uri("https://localhost/Account/Login?error=Error1&returnUrl=%2Fpage1");
        vm.LoadFromQuery(uri1);
        
        var uri2 = new Uri("https://localhost/Account/Login");
        vm.LoadFromQuery(uri2);

        Assert.Null(vm.ErrorMessage);
        Assert.Null(vm.ReturnUrl);
        Assert.Null(vm.ClientId);
    }

    [Fact]
    public void LoadFromQuery_Handles_Empty_Query_String()
    {
        var service = CreateRegistrationServiceMock();
        var vm = new LoginViewModel(service.Object);
        var uri = new Uri("https://localhost/Account/Login");

        vm.LoadFromQuery(uri);

        Assert.Null(vm.ErrorMessage);
        Assert.Null(vm.ReturnUrl);
        Assert.Null(vm.ClientId);
    }

    [Fact]
    public async Task RegisterUserAsync_Sets_SuccessMessage_On_Success()
    {
        var service = CreateRegistrationServiceMock();
        service.Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(new RegisterUserResult
            {
                Type = RegisterUserResultType.Success,
                Message = "Usuario registrado exitosamente"
            });

        var vm = new LoginViewModel(service.Object);
        vm.Register.FullName = "John Doe";
        vm.Register.Email = "john@example.com";

        await vm.RegisterUserAsync();

        Assert.Equal("Usuario registrado exitosamente", vm.SuccessMessage);
        Assert.Null(vm.RegisterMessage);
        Assert.Equal("john@example.com", vm.LoginUser);
    }

    [Fact]
    public async Task RegisterUserAsync_Sets_RegisterMessage_On_AlreadyExists()
    {
        var service = CreateRegistrationServiceMock();
        service.Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(new RegisterUserResult
            {
                Type = RegisterUserResultType.AlreadyExists,
                Message = "El usuario ya existe"
            });

        var vm = new LoginViewModel(service.Object);
        vm.Register.FullName = "Jane Doe";
        vm.Register.Email = "jane@example.com";

        await vm.RegisterUserAsync();

        Assert.Equal("El usuario ya existe", vm.RegisterMessage);
        Assert.Null(vm.SuccessMessage);
    }

    [Fact]
    public async Task RegisterUserAsync_Sets_RegisterMessage_On_NotInActiveDirectory()
    {
        var service = CreateRegistrationServiceMock();
        service.Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(new RegisterUserResult
            {
                Type = RegisterUserResultType.NotInActiveDirectory,
                Message = "Usuario no encontrado en AD"
            });

        var vm = new LoginViewModel(service.Object);
        vm.Register.FullName = "Bob Smith";
        vm.Register.Email = "bob@external.com";

        await vm.RegisterUserAsync();

        Assert.Equal("Usuario no encontrado en AD", vm.RegisterMessage);
        Assert.Null(vm.SuccessMessage);
    }

    [Fact]
    public async Task RegisterUserAsync_Sets_RegisterMessage_On_ValidationError()
    {
        var service = CreateRegistrationServiceMock();
        service.Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(new RegisterUserResult
            {
                Type = RegisterUserResultType.ValidationError,
                Message = "Datos inválidos"
            });

        var vm = new LoginViewModel(service.Object);
        vm.Register.FullName = "";
        vm.Register.Email = "";

        await vm.RegisterUserAsync();

        Assert.Equal("Datos inválidos", vm.RegisterMessage);
        Assert.Null(vm.SuccessMessage);
    }

    [Fact]
    public async Task RegisterUserAsync_Trims_Input_Values()
    {
        var service = CreateRegistrationServiceMock();
        RegisterUserRequest? capturedRequest = null;
        
        service.Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>(), default))
            .Callback<RegisterUserRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new RegisterUserResult { Type = RegisterUserResultType.Success, Message = "OK" });

        var vm = new LoginViewModel(service.Object);
        vm.Register.FullName = "  Alice Wonder  ";
        vm.Register.Email = "  alice@example.com  ";

        await vm.RegisterUserAsync();

        Assert.NotNull(capturedRequest);
        Assert.Equal("Alice Wonder", capturedRequest.FullName);
        Assert.Equal("alice@example.com", capturedRequest.Email);
    }

    [Fact]
    public async Task RegisterUserAsync_Clears_Previous_Messages()
    {
        var service = CreateRegistrationServiceMock();
        service.Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(new RegisterUserResult { Type = RegisterUserResultType.Success, Message = "OK" });

        var vm = new LoginViewModel(service.Object);
        vm.Register.FullName = "Test User";
        vm.Register.Email = "test@example.com";

        await vm.RegisterUserAsync();

        Assert.Null(vm.RegisterMessage);
    }

    [Fact]
    public void RegisterInputModel_Has_Required_Validation_For_FullName()
    {
        var model = new LoginViewModel.RegisterInputModel();
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(model);

        model.Email = "valid@email.com";
        model.FullName = "";

        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            model, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("FullName"));
    }

    [Fact]
    public void RegisterInputModel_Has_Email_Validation()
    {
        var model = new LoginViewModel.RegisterInputModel();
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(model);

        model.FullName = "Test User";
        model.Email = "invalid-email";

        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            model, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Email"));
    }

    [Fact]
    public void Constructor_Throws_When_Service_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new LoginViewModel(null!));
    }

    [Fact]
    public void LoadFromQuery_Throws_When_Uri_Is_Null()
    {
        var service = CreateRegistrationServiceMock();
        var vm = new LoginViewModel(service.Object);

        Assert.Throws<ArgumentNullException>(() => vm.LoadFromQuery(null!));
    }
}
