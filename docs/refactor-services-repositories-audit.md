# Auth.Web Services/Repositories Audit (Blazor Server, .NET 8)

Scope
- Audited Admin, Auth, and Users modules.
- Ensured Controllers and Components depend on `Auth.Web.Services.Abstractions.*`.
- Verified Admin Services no longer access EF Core directly and now use Repositories.

Reviewed files
- Services/Implementations/Admin: `AreaAdminService.cs`, `ClientAdminService.cs`, `RoutingAdminService.cs`, `RoleAdminService.cs`, `UserAdminService.cs`
- Repositories/Abstractions/Admin: `IAreaAdminRepository.cs`, `IClientAdminRepository.cs`, `IRoutingAdminRepository.cs`, `IRoleAdminRepository.cs`, `IUserAdminRepository.cs`
- Repositories/Implementations/Admin: `AreaAdminRepository.cs`, `ClientAdminRepository.cs`, `RoutingAdminRepository.cs`, `RoleAdminRepository.cs`, `UserAdminRepository.cs`
- Controllers: `ConnectController.cs`, `PermissionsController.cs`
- Components/Admin: `AdminLayout.razor`, `AreasAdmin.razor.cs`, `ClientsAdmin.razor.cs`, `RoutesAdmin.razor.cs`, `RolesAdmin.razor.cs`, `UsersAdmin.razor.cs`
- Components/Account: `Login.razor.cs`
- Services/Implementations/Auth: `AuthFlowService.cs`
- Program.cs

Findings & corrections
1) Admin Services used `AuthDbContext` via `IServiceScopeFactory`.
   - Refactored to repositories:
     - `AreaAdminService` ? `IAreaAdminRepository`
     - `ClientAdminService` ? `IClientAdminRepository`
     - `RoutingAdminService` ? `IRoutingAdminRepository` (+ validation with `IClientService` and client resolution using `IClientAdminRepository`)
     - `RoleAdminService` ? `IRoleAdminRepository` for reads; mutations remain on `RoleManager`
     - `UserAdminService` ? `IUserAdminRepository` for reads and area updates; role ops remain on `UserManager`
   - Removed direct usage of `AuthDbContext` and `IServiceScopeFactory` from these services.

2) Created Admin Repositories (EF Core on `AuthDbContext`):
   - `AreaAdminRepository`, `ClientAdminRepository`, `RoutingAdminRepository`, `RoleAdminRepository`, `UserAdminRepository`
   - Methods mapped 1:1 to previous EF Core operations in services.

3) DI registrations (Scoped) added in `Program.cs`:
   - `IAreaAdminRepository`, `IClientAdminRepository`, `IRoutingAdminRepository`, `IRoleAdminRepository`, `IUserAdminRepository`.

4) UI Admin dependency cleanup:
   - All Admin components/layouts now inject `Auth.Web.Services.Abstractions.Admin.*`.
   - Fixed `AdminLayout.razor` injection of `IAdminClientService` to use `Services.Abstractions.Admin`.

5) Controllers:
   - `ConnectController` uses `Auth.Web.Services.Abstractions.Auth.IAuthFlowService`.
   - `PermissionsController` uses `Auth.Web.Services.Abstractions.Permissions.IPermissionService`.

6) Account/Login UI:
   - `Login.razor.cs` injects `Auth.Web.Services.Abstractions.Users.IUserRegistrationService`.

7) Auth orchestrator:
   - `AuthFlowService.cs` depends on `Services.Abstractions` interfaces for Auth/Users/Permissions/Routing/Clients.
   - No direct `AuthDbContext` usage.
   - Note: Uses DTOs from `Auth.Web.Application.Dtos` and `UserPermissionsAssembler` from `Application.Permissions` by design (DTO layer retained).

Namespace cleanup
- Removed legacy `Auth.Web.Application.Admin.Abstractions` usings from Admin UI components.
- Services/Implementations/Admin no longer reference `AuthDbContext` or `Microsoft.EntityFrameworkCore` directly.

Pending / TODO
- Legacy namespaces (`Auth.Web.Application.*`, `Auth.Web.Infrastructure.*`) still exist in codebase for not-yet-migrated modules.
  - Example: `AuthFlowService` references DTOs in `Application.*` intentionally; UI does not depend on them.
- Future migration candidates:
  - Move any remaining EF Core access in non-Admin services to appropriate repositories when those modules are addressed.
- Consider adding unit tests for the new repositories.

Build & behavior
- Full build successful.
- No changes to public service contracts, DTOs, or controller signatures.
- Observable behavior preserved for login, admin UI, and auth flows.
