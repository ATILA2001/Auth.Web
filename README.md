# Auth.Web — Sistema de Autenticación y Administración de Aplicaciones

## 1) Propósito

`Auth.Web` es el servicio central de autenticación SSO interno y panel de administración para aplicaciones de la organización. Sus responsabilidades principales son:

- Autenticación contra Active Directory (AD).
- Emisión de tokens JWT con claims (roles, áreas, aplicaciones y versión de permisos).
- Panel de administración para gestionar roles, áreas, clientes y rutas (Blazor Server UI).
- Enrutamiento por cliente: reglas que determinan la URL de retorno por `ClientId` + `Area`.

La UI está implementada con Blazor Server y utiliza componentes propios bajo `Components/`. El backend expone endpoints de conexión (por ejemplo `/connect/login`) y APIs de permisos.

## 2) Estructura del proyecto

A continuación se lista la estructura real del proyecto y una breve descripción de lo que va dentro de cada carpeta. Mantener esta convención ayuda a preservar separación de responsabilidades y escalabilidad.

```
Application/
├─ Abstractions/
│  ├─ IActiveDirectoryAuthService.cs
│  ├─ IClientService.cs
│  ├─ IJwtTokenService.cs
│  ├─ IPermissionService.cs
│  ├─ IRouteQueryService.cs
│  └─ IRoutingService.cs
├─ Admin/
│  ├─ Abstractions/
│  └─ Dtos/
├─ Auth/
├─ Dtos/
├─ Permissions/
└─ Users/

Domain/
├─ Dtos/
└─ Entities/

Infrastructure/
├─ Admin/
├─ Auth/
├─ Clients/
├─ Permissions/
├─ Routing/
└─ Users/

Components/
├─ Account/
│  ├─ Pages/
│  └─ Shared/
├─ Admin/
├─ Layout/
└─ Pages/

Controllers/
├─ ConnectController.cs
└─ PermissionsController.cs

Configuration/

Data/
Migrations/
Tests/
wwwroot/
```

### Qué contiene cada carpeta

- `Application/` — Orquestación y contratos. Contiene interfaces y servicios de alto nivel que implementan los casos de uso: flujos de autenticación (`AuthFlowService`, `AuthClaimsModel`), servicios administrativos (Dtos bajo `Application/Admin/Dtos`), abstracciones para AD, JWT, permisos, clientes y routing. Aquí no debe haber dependencias directas a EF o AD concretos.

- `Application/Abstractions/` — Interfaces públicas que usa la capa superior: `IActiveDirectoryAuthService`, `IClientService`, `IJwtTokenService`, `IPermissionService`, `IRoutingService`, `IRouteQueryService`.

- `Application/Admin/` — DTOs y contratos específicos del panel de administración. Ejemplos: `ApplicationClientAdminDto.cs`, `AreaAdminDto.cs`, `AreaRouteAdminDto.cs`.

- `Application/Auth/` — Modelos del flujo de autenticación como `AuthClaimsModel.cs` y `AuthFlowService` que orquesta validación AD y emisión de token.

- `Domain/` — Entidades de negocio y DTOs que representan el modelo persistido. Contiene entidades como `ApplicationUser`, `ApplicationClient`, `Area`, `AreaRoute`, `ActionPermission`.

- `Infrastructure/` — Implementaciones concretas de repositorios y servicios: autenticación AD (`Infrastructure/Auth/AdAuthService.cs`), servicios admin (`Infrastructure/Admin/AreaAdminService.cs`, `Infrastructure/Admin/ClientAdminService.cs`), `Clients/ClientService.cs` y `Routing`/`Permissions` concretos. También contiene `AuthDbContext` en `Data/` y migraciones EF Core en `Migrations/`.

- `Components/` — Toda la UI Blazor Server. Contiene páginas y componentes reutilizables:
  - `Components/Account/Pages/` y `Components/Account/Shared/` (`AccountLayout.razor`, `AccessDenied.razor`).
  - `Components/Admin/` (por ejemplo `Admin.razor`, `AreasAdmin.razor`, `ClientsAdmin.razor`).
  - `Components/Layout/` y `Components/Pages/` (páginas públicas, `Error.razor`).

- `Controllers/` — Endpoints HTTP/REST y de conexión:
  - `ConnectController.cs` — punto de entrada para login y portal-login.
  - `PermissionsController.cs` — API de permisos consumida por clientes.

- `Configuration/` — Clases de opciones (por ejemplo `AdOptions.cs`) usadas para binder `IOptions<T>` en `Program.cs`.

- `Data/` — `AuthDbContext.cs` y migraciones EF Core usadas para persistencia en SQL Server.

- `Migrations/` — Migraciones EF Core (ej.: `InitialCreate`, `AddAreaRoutes`).

- `Tests/` — Pruebas unitarias y de integración para servicios clave: `Tests/AuthFlowServiceTests.cs`, `Tests/ConnectControllerTests.cs`, `Tests/Admin/*`.

## 3) Flujos clave

- Flujo de login (resumen):
  1. El cliente redirige al formulario de login o invoca `/connect/login`.
  2. `AuthFlowService` valida credenciales con `IActiveDirectoryAuthService` (concrete: `Infrastructure/Auth/AdAuthService.cs`).
  3. Si la validación es exitosa, se construye un `AuthClaimsModel` con roles, áreas y aplicaciones del usuario.
  4. `IJwtTokenService` genera el JWT que incluye `sub`, `email`, `name`, `role`, `area`, `app` y `perms_ver`.
  5. Si es una sesión administrativa, se autentica con cookie y se redirige al panel `/admin`.
  6. Para clientes externos, se valida `ReturnUrl` con `IClientService.IsReturnUrlAllowed` y se redirige con `?token=...`.

- Administración (panel Blazor): CRUD de `ApplicationClient`, `Area`, `AreaRoute` y asignación de roles/areas a usuarios. Las páginas de administración usan Radzen Blazor components y están protegidas con `[Authorize(Roles = "Admin")]`.

## 4) Convenciones y buenas prácticas (Reglas del equipo)

Convenciones de nombres
- Namespaces: `Auth.Web.Application`, `Auth.Web.Domain`, `Auth.Web.Infrastructure`, `Auth.Web.Components`.
- Interfaces: prefijo `I` y sufijo descriptivo (`IClientService`, `IJwtTokenService`).
- DTOs: sufijo `Dto` o `AdminDto` según propósito (p. ej. `AreaAdminDto`).
- Blazor components: `PascalCase.razor` y código asociado en `PascalCase.razor.cs` para code-behind.

Cómo trabajar día a día
- Crear ramas `feature/<descripcion>`, `bugfix/<descripcion>` desde `dev`.
- Mantener `dev` como rama activa y `main` para releases estables.
- Hacer PRs pequeñas y atómicas, referenciando issues.

Qué va en cada capa
- UI (Components): únicamente UI, validación superficial y llamadas a servicios de Application.
- Application: orquestación, casos de uso y interfaces. No dependencias a EF o AD concretos.
- Domain: entidades y reglas de negocio (POCOs).
- Infrastructure: dependencias concretas (EF Core, AD, JWT, proveedores externos) y adaptadores.

Buenas prácticas
- No realizar llamadas a infraestructura desde Components directamente: inyectar servicios de `Application`.
- Validar `ReturnUrl` siempre con `IClientService`.
- Mantener `Jwt:SigningKey` fuera del código fuente (usar User Secrets o variables de entorno).
- Tests unitarios para `AuthFlowService`, `JwtTokenService` y servicios admin.

Reglas de carpetas para componentes
- Un componente por archivo `.razor` y opcional `*.razor.cs` para lógica.
- Components agrupados por dominio: `Account/`, `Admin/`, `Layout/`, `Pages/`.

## 5) Inyección de dependencias y lifetimes

Recomendación de lifetimes para servicios registrados en `Program.cs`:
- `Singleton`:
  - Configuración (`IOptions<T>`), proveedores de configuración inmutables.
- `Scoped` (por petición / circuito Blazor Server):
  - `AuthDbContext` (EF Core), servicios que usan DbContext.
  - Servicios de Application que agrupan operaciones sobre la base de datos.
- `Transient`:
  - Servicios ligeros y sin estado que se crean por uso.

Ejemplos reales en el proyecto:
- `Infrastructure/Auth/AdAuthService` — implementa `IActiveDirectoryAuthService` (normalmente `Scoped` o `Transient` según la estrategia de conexión AD).
- `Infrastructure/Clients/ClientService` — implementa `IClientService` y normalmente es `Scoped`.
- `Infrastructure/*Admin*Service` — `Scoped`.

## 6) Configuración y requisitos

Requisitos mínimos
- .NET 8 SDK
- SQL Server (o compatibilidad con cadena de conexión SQL Server)

Configuración principal (appsettings)
- `ConnectionStrings:DefaultConnection` — cadena de conexión a SQL Server.
- `ActiveDirectory` — opciones para conexión AD (ver `Configuration/AdOptions.cs`).
- `Jwt` — `Issuer`, `Audience`, `SigningKey` (mín. 32 chars), `TokenLifetimeMinutes`.

Paquetes NuGet recomendados (presentes o esperados)
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.IdentityModel.Tokens`
- `Radzen.Blazor` (UI)
- `System.DirectoryServices.Protocols` o librería para comunicación con AD

Notas de seguridad
- Nunca commitear `Jwt:SigningKey` ni credenciales AD.
- Usar User Secrets en desarrollo y variables de entorno / secret store en producción.

## 7) Tests

Estructura de pruebas
- `Tests/` contiene unit tests para:
  - `AuthFlowServiceTests` (orquestación del login).
  - `ConnectControllerTests` (endpoints de conexión).
  - Servicios administrativos (`Tests/Admin/*`).

Cómo ejecutar
- `dotnet test` en la raíz del repositorio ejecuta todas las pruebas.

Cobertura objetivo
- Validación AD y manejo de errores.
- Generación y contenido de JWT (`JwtTokenService`).
- Reglas de `IClientService.IsReturnUrlAllowed`.
- Operaciones CRUD del panel admin (Services).

## 8) Branching model

- `main`: versiones de producción estables y tags de release.
- `dev`: integración continua de features completados y aprobados.
- `feature/*`: ramas de desarrollo por característica.
- `hotfix/*`: correcciones críticas a `main`.

Política de PR
- PR hacia `dev` con descripción, pasos para test y referencia a issue.
- Al menos una revisión de código aprobada antes de merge.

## 9) Notas operacionales y seguridad

- Migraciones EF Core en `Migrations/` y `AuthDbContext` en `Data/`.
- Revisar `docs/` para diagramas y flujos adicionales: `docs/auth-admin-overview.md`, `docs/auth-login-flow.md`.
- Consideraciones futuras: agregar `jti` y revocación de tokens, firma asimétrica (RSA) si hay múltiples emisores, y caching distribuido para permissions/versioning (`perms_ver`).

---

Mantener este README como referencia de alto nivel. Para detalles de implementación y flujos concretos consulte el código en `Application/`, `Infrastructure/` y las guías en `docs/`.
