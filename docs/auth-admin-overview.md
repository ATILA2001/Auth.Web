# Auth & Admin Module Overview

## 1. Login Flow
- Client submits credentials via `POST /connect/login`.
- `ConnectController` delegates to `AuthFlowService.LoginAsync`.
- Active Directory credentials validated (`IActiveDirectoryAuthService`).
- Outcome:
  - Admin user: Cookie sign-in with ASP.NET Identity, redirect to `/admin`.
  - Non-admin user: JWT issued (`IJwtTokenService`), redirect to external application with `?token=...`.

## 2. Token Contents (Non-admin)
JWT includes standard and domain claims:
- `sub`: User Id
- `email`: Email (if present)
- `name`: Display Name (if present)
- `role`: One or many role claims
- `area`: One or many area claims
- `app`: Client/application identifiers
- `perms_ver`: Permissions version (for cache invalidation)
- Standard: `iss`, `aud`, `exp`, `iat`, `jti`

## 3. Domain Model (Simplified)
- User ? Roles (IdentityRole)
- User ? Areas (UserAreas: many-to-many)
- ApplicationClient: Defines `ClientId`, `Audience`, and allowed return URLs.
- AreaRoute: (AreaId + ClientId + ReturnUrl + Priority + IsActive) decides default redirect target for non-admin users.

## 4. Admin Module Pages (All require `[Authorize(Roles = "Admin")]`)
| Page | Path | Service | Purpose |
|------|------|---------|---------|
| Users | /admin/users | `IAdminUserService` | View & edit user roles and areas (combined). |
| Areas | /admin/areas | `IAdminAreaService` | CRUD of areas. |
| Roles | /admin/roles | `IAdminRoleService` | CRUD of roles. |
| Clients | /admin/clients | `IAdminClientService` | CRUD of application clients & allowed return URLs. |
| Routes | /admin/routes | `IAdminRoutingService` | CRUD of area-based routing rules (validates ReturnUrl). |

## 5. Adding a New Client Application
1. Create client in `/admin/clients` with its `ClientId`, `Audience`, and allowed return URL(s).
2. Define routing rules in `/admin/routes` mapping areas to (client + returnUrl).
3. External app validates received JWT: check `iss`, `aud`, signature, and extract `sub`, `role`, `area`, `app` claims.

## 6. Updating Roles / Areas
- Use `/admin/users` to modify roles and area assignments.
- Internally calls `UpdateUserRolesAndAreasAsync` (atomic update).

## 7. Permissions Extension Points (Future)
- Potential entities: `Page`, `ActionPermission`, `RolePagePermission`.
- Extend token with granular permissions or version numbers.
- Add new admin page (e.g. `/admin/permissions`) backed by an `IAdminPermissionService`.

## 8. Security Notes
- All admin endpoints/pages require Admin role.
- Non-admin redirect based on AreaRoute and allowed return URLs; enforced by `ClientService.IsReturnUrlAllowed`.
- JWT signing key must be ?32 chars (HS256); enforced in `JwtTokenService` constructor.

## 9. Where to Change Things
| Task | Location |
|------|----------|
| Change token lifetime | `JwtOptions.TokenLifetimeMinutes` (appsettings) |
| Add a role | `/admin/roles` or seed logic |
| Add/edit areas | `/admin/areas` |
| Assign roles/areas to user | `/admin/users` |
| Add client app | `/admin/clients` |
| Add routing rule | `/admin/routes` |
| Adjust AD auth | `AdAuthService` & configuration `ActiveDirectory` section |

## 10. Deprecations
- Legacy `UserAreasAdmin` & old routing component retained temporarily with deprecation comment.
- Prefer unified editing in `/admin/users`.

## 11. High-level External App Validation
External applications should:
1. Accept `token` query param.
2. Validate signature (HS256) using shared secret.
3. Validate `iss`, `aud`, and expiration.
4. Map `sub` to user identity and apply roles/areas for authorization.

---
_Last updated: (CI closure iteration)_
