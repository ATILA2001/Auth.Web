# Flujo de login con cookie compartida

## Resumen
- AuthFlowService valida AD, provisiona usuario local, carga roles y permisos.
- Admin: firma cookie y redirige a /admin.
- No admin: resuelve cliente/returnUrl, arma claims (roles/areas/apps/permisos) y firma cookie del esquema compartido; redirige sin `?token=`.
- Data Protection se guarda en la base de datos (tabla `DataProtectionKeys`) para que todas las apps (.NET 8/10/4.8) puedan leer la cookie.

## Claims en la cookie
- `sub` (NameIdentifier), `name`, `email`.
- `role` (roles), `area` (ids de area), `app` (client ids autorizados).
- `perms_version` y `perms_json` (paginas y acciones permitidas en JSON compacto).

## Configuracion requerida (variables de entorno o user secrets en dev)
- `ConnectionStrings__DefaultConnection`
- `SharedCookie__Name`
- `SharedCookie__Domain`
- `SharedCookie__ApplicationName`

## Consumo en aplicaciones cliente
- Configurar el mismo nombre/dominio de cookie y SameSite=None, Secure=Always.
- Usar el mismo key ring (UNC/SQL/Redis) y `ApplicationName` identico.
- Para OWIN 4.8: `UseCookieAuthentication` con `AspNetTicketDataFormat` usando el key ring compartido.
- Autorizar leyendo `role`, `area`, `app`, `perms_json` segun necesidades locales.

## Logout
- POST `/connect/logout` (con antiforgery) limpia el esquema compartido. Los clientes deben redirigir aqui para cerrar sesion central.
