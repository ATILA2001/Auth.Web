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
Services/
├─ Abstractions/
│  ├─ Auth/
│  ├─ Admin/
│  ├─ Clients/
│  ├─ Permissions/
│  ├─ Routing/
│  └─ Users/
├─ Implementations/
│  ├─ Auth/
│  ├─ Admin/
│  ├─ Clients/
│  ├─ Permissions/
│  ├─ Routing/
│  └─ Users/

Repository/
├─ Abstractions/
│  ├─ Auth/
│  ├─ Admin/
│  ├─ Clients/
│  ├─ Permissions/
│  ├─ Routing/
│  └─ Users/
├─ Implementations/
│  ├─ Auth/
│  ├─ Admin/
│  ├─ Clients/
│  ├─ Permissions/
│  ├─ Routing/
│  └─ Users/

Data/
├─ Entities/
│  ├─ ApplicationUser.cs
│  ├─ ApplicationClient.cs
│  ├─ Area.cs
│  ├─ AreaRoute.cs
│  └─ ActionPermission.cs
├─ Migrations/
│  ├─ InitialCreate.cs
│  └─ ...
├─ AuthDbContext.cs
└─ Seed.cs

Security/
├─ Jwt/
│  ├─ JwtTokenService.cs
│  ├─ JwtOptions.cs
└─ Auth/
   └─ AdAuthService.cs

Components/
├─ Account/
│  ├─ Pages/
│  └─ Shared/
├─ Admin/
│  ├─ Areas/
│  │  ├─ Area.razor
│  │  ├─ Area.razor.cs
│  │  └─ AreaViewModel.cs
│  ├─ Clients/
│  │  ├─ Clients.razor
│  │  ├─ Clients.razor.cs
│  │  └─ ClientsViewModel.cs
│  └─ ...
├─ Layout/
└─ Pages/

Controllers/
├─ ConnectController.cs
└─ PermissionsController.cs

Configuration/
├─ AdOptions.cs
└─ FeatureOptions.cs

Tests/
wwwroot/
```

### Qué contiene cada carpeta

- `Services/` — Orquestación y contratos del dominio de aplicación (excepto JWT). Contiene interfaces en `Abstractions/` y servicios de alto nivel en `Implementations/` que implementan los casos de uso (autenticación, permisos, clientes, routing, administración). No deben tener lógica de persistencia directa; acceden a `Repository/` cuando sea necesario.

- `Services/Abstractions/` — Interfaces públicas usadas por la capa superior para dominios de negocio: `IActiveDirectoryAuthService` (si se expone vía Services), `IClientService`, `IPermissionService`, `IRoutingService`, `IRouteQueryService`, `IUserService`, etc.

- `Services/Implementations/` — Implementaciones concretas de servicios de negocio: autenticación de sesión administrativa (`Auth/`), administración (`Admin/`), clientes (`Clients/`), permisos (`Permissions/`), routing (`Routing/`), usuarios (`Users/`).

- `Repository/` — Persistencia y acceso a datos, con la misma división que `Services`: `Abstractions/` para interfaces de repositorios y contratos de acceso a datos, y `Implementations/` para repositorios concretos y adaptadores.

- `Data/Entities/` — Entidades de negocio y DTOs persistidos (POCOs): `ApplicationUser`, `ApplicationClient`, `Area`, `AreaRoute`, `ActionPermission`.

- `Data/Migrations/` — Migraciones EF Core.

- `Data/AuthDbContext.cs` y `Data/Seed.cs` — Contexto de EF Core y seeding inicial.

- `Security/` — Todo lo relacionado a autenticación externa y JWT: opciones y servicios para emisión/validación de tokens en `Jwt/`, y proveedores de autenticación como AD en `Auth/`.

- `Components/` — Toda la UI Blazor Server. Los ViewModels de cada página/componente residen junto al componente.
  - Ejemplo: `Components/Admin/Areas/Area.razor`, `Area.razor.cs`, `AreaViewModel.cs`.
  - `Components/Account/Pages/` y `Components/Account/Shared/` (`AccountLayout.razor`, `AccessDenied.razor`).
  - `Components/Admin/` (por ejemplo `Areas`, `Clients`).
  - `Components/Layout/` y `Components/Pages/` (páginas públicas, `Error.razor`).

- `Controllers/` — Endpoints HTTP/REST y de conexión:
  - `ConnectController.cs` — punto de entrada para login y portal-login.
  - `PermissionsController.cs` — API de permisos consumida por clientes.

- `Configuration/` — Clases de opciones para configuración de la app (`AdOptions.cs`, `FeatureOptions.cs`) usadas para binder `IOptions<T>` en `Program.cs`.

- `Tests/` — Pruebas unitarias e integración para servicios clave y controladores.

## 3) Flujos clave

- Flujo de login (resumen):
  1. El cliente redirige al formulario de login o invoca `/connect/login`.
  2. Servicio de autenticación AD valida credenciales contra Active Directory (concreto en `Security/Auth/AdAuthService.cs`).
  3. Si la validación es exitosa, se construyen claims del usuario (roles, áreas y aplicaciones) vía servicios en `Services/Implementations/*`.
  4. Servicio JWT en `Security/Jwt` genera el token que incluye `sub`, `email`, `name`, `role`, `area`, `app` y `perms_ver`.
  5. Si es una sesión administrativa, se autentica con cookie y se redirige al panel `/admin`.
  6. Para clientes externos, se valida `ReturnUrl` con `IClientService.IsReturnUrlAllowed` y se redirige con `?token=...`.

- Administración (panel Blazor): CRUD de `ApplicationClient`, `Area`, `AreaRoute` y asignación de roles/areas a usuarios. Las páginas de administración usan Radzen Blazor components y están protegidas con `[Authorize(Roles = "Admin")]`.

## 4) Convenciones y buenas prácticas (Reglas del equipo)

Convenciones de nombres
- Namespaces: `Auth.Web.Services`, `Auth.Web.Repository`, `Auth.Web.Security`, `Auth.Web.Components`, `Auth.Web.Data`, `Auth.Web.Configuration`.
- Interfaces: prefijo `I` y sufijo descriptivo (`IClientService`, `IPermissionService`).
- DTOs: sufijo `Dto` o `AdminDto` según propósito.
- Blazor components y ViewModels: `PascalCase.razor`, `PascalCase.razor.cs` y `PascalCaseViewModel.cs`.

Cómo trabajar día a día
- Crear ramas `feature/<descripcion>`, `bugfix/<descripcion>` desde `dev`.
- Mantener `dev` como rama activa y `main` para releases estables.
- Hacer PRs pequeñas y atómicas, referenciando issues.

Qué va en cada capa
- UI (Components): únicamente UI, validación superficial y llamadas a servicios de `Services`.
- Services: orquestación, casos de uso e interfaces; sin dependencias directas al proveedor de datos.
- Repository: EF Core, repositorios y acceso a datos, dividido en `Abstractions` e `Implementations`.
- Data/Entities: entidades y reglas de negocio (POCOs).
- Security: autenticación, JWT y claves.
- Configuration: opciones de configuración vinculadas a `IOptions<T>`.

Buenas prácticas
- No realizar llamadas a `Repository` desde Components directamente: inyectar servicios de `Services`.
- Validar `ReturnUrl` siempre con `IClientService`.
- Mantener `Jwt:SigningKey` fuera del código fuente (usar User Secrets o variables de entorno).
- Tests unitarios para servicios de autenticación, JWT y administración.

Reglas de carpetas para componentes
- Un componente por archivo `.razor` y opcional `*.razor.cs` para code-behind, más su `*ViewModel.cs`.
- Components agrupados por dominio: `Account/`, `Admin/`, `Layout/`, `Pages/.

## 5) Inyección de dependencias y lifetimes

Recomendación de lifetimes para servicios registrados en `Program.cs`:
- `Singleton`:
  - Configuración (`IOptions<T>`), proveedores de configuración inmutables.
- `Scoped` (por petición / circuito Blazor Server):
  - `AuthDbContext` (EF Core), repositorios que usan DbContext.
  - Servicios de `Services` que agrupan operaciones sobre la base de datos.
- `Transient`:
  - Servicios ligeros y sin estado que se crean por uso.

Ejemplos reales en el proyecto:
- `Security/Auth/AdAuthService` — servicio de autenticación contra AD (normalmente `Scoped` o `Transient`).
- `Services/Implementations/Clients/ClientService` — implementa `IClientService` y normalmente es `Scoped`.
- `Services/Implementations/*Admin*Service` — `Scoped`.

## 6) Configuración y requisitos

Requisitos mínimos
- .NET 8 SDK
- SQL Server (o compatibilidad con cadena de conexión SQL Server)

Configuración principal (appsettings)
- `ConnectionStrings:DefaultConnection` — cadena de conexión a SQL Server.
- `ActiveDirectory` — opciones para conexión AD (ver `Configuration/AdOptions.cs`).
- `Jwt` — `Issuer`, `Audience`, `SigningKey` (mín. 32 chars), `TokenLifetimeMinutes`.
- `Features` — opciones de toggles/feature flags (ver `Configuration/FeatureOptions.cs`).

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
  - Servicios de autenticación y orquestación de login.
  - `ConnectController` (endpoints de conexión).
  - Servicios administrativos (`Tests/Admin/*`).

Cómo ejecutar
- `dotnet test` en la raíz del repositorio ejecuta todas las pruebas.

Cobertura objetivo
- Validación AD y manejo de errores.
- Generación y contenido de JWT (servicios bajo `Security/Jwt`).
- Reglas de `IClientService.IsReturnUrlAllowed`.
- Operaciones CRUD del panel admin (Services + Repository).

## 8) Branching model

- `main`: versiones de producción estables y tags de release.
- `dev`: integración continua de features completados y aprobados.
- `feature/*`: ramas de desarrollo por característica.
- `hotfix/*`: correcciones críticas a `main`.

Política de PR
- PR hacia `dev` con descripción, pasos para test y referencia a issue.
- Al menos una revisión de código aprobada antes de merge.

## 9) Notas operacionales y seguridad

- Migraciones EF Core en `Data/Migrations/` y `AuthDbContext` en `Data/AuthDbContext.cs`.
- Revisar `docs/` para diagramas y flujos adicionales: `docs/auth-admin-overview.md`, `docs/auth-login-flow.md`.
- Consideraciones futuras: agregar `jti` y revocación de tokens, firma asimétrica (RSA) si hay múltiples emisores, y caching distribuido para permissions/versioning (`perms_ver`).

---

Mantener este README como referencia de alto nivel. Para detalles de implementación y flujos concretos consulte el código en `Services/`, `Repository/`, `Security/`, `Data/` y las guías en `docs/`.
