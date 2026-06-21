# Semana-6---Tarea-1-ANGUAR

Proyecto academico con autenticacion end-to-end entre Angular y ASP.NET Core. El frontend almacena solo el nombre visible del usuario en `localStorage`; la sesion real se valida en el backend mediante cookie HttpOnly.

## Tecnologias

- Angular 21
- ASP.NET Core Web API (.NET 10)
- Cookie Authentication
- Antiforgery/XSRF token
- CORS con credenciales

## Estructura

```text
login-sesion-angular-dotnet/
├── LoginSesion.Api/       # Backend .NET
├── LoginSesion.Angular/   # Frontend Angular
├── LoginSesion.slnx
└── README.md
```

## Usuarios demo

| Usuario | Contrasena |
| --- | --- |
| `maria` | `123456` |
| `admin` | `admin123` |

## Ejecutar backend

```bash
cd LoginSesion.Api
dotnet run --launch-profile http
```

API local:

```text
http://localhost:5222
```

## Ejecutar frontend

En otra terminal:

```bash
cd LoginSesion.Angular
npm install
npm start
```

Aplicacion local:

```text
http://localhost:4200
```

## Flujo implementado

1. Angular solicita `/api/auth/csrf` para recibir cookie `XSRF-TOKEN`.
2. Angular envia login a `/api/auth/login` con `withCredentials` y header `X-XSRF-TOKEN`.
3. .NET valida credenciales y crea cookie `login_sesion` HttpOnly.
4. Angular guarda el nombre del usuario en `localStorage` con la clave `usuarioNombre`.
5. El guard de Angular protege `/dashboard` consultando `/api/auth/session`.
6. El logout llama `/api/auth/logout`, elimina la cookie del backend y limpia `localStorage`.

## Seguridad aplicada

- La cookie de sesion es `HttpOnly`.
- `SameSite=Lax` para ambiente local.
- `SecurePolicy=SameAsRequest`; en produccion debe cambiarse a `Always` y ejecutarse sobre HTTPS.
- CORS permite solo `http://localhost:4200` y usa `AllowCredentials`.
- Las rutas protegidas devuelven `401` si no hay sesion valida.
- El cliente no guarda contrasenas ni tokens de sesion en `localStorage`.
- El interceptor Angular agrega `withCredentials` y el header XSRF cuando existe cookie `XSRF-TOKEN`.

## Endpoints principales

| Metodo | Ruta | Uso |
| --- | --- | --- |
| `GET` | `/api/auth/csrf` | Emite token XSRF |
| `POST` | `/api/auth/login` | Inicia sesion |
| `GET` | `/api/auth/session` | Valida sesion actual |
| `GET` | `/api/perfil` | Ruta protegida |
| `POST` | `/api/auth/logout` | Cierra sesion |

## Pruebas manuales

El archivo `LoginSesion.Api/LoginSesion.Api.http` incluye una coleccion para probar:

- healthcheck
- login correcto
- login incorrecto `401`
- validacion de sesion
- ruta protegida
- logout

Tambien se puede validar desde el navegador:

1. Entrar a `http://localhost:4200`.
2. Iniciar sesion con `maria / 123456`.
3. Confirmar que carga `/dashboard`.
4. Revisar DevTools > Application > Local Storage: debe existir `usuarioNombre`.
5. Cerrar sesion y confirmar que vuelve a `/login`.

## Comandos de verificacion

```bash
dotnet build LoginSesion.slnx
cd LoginSesion.Angular
npm run build
```
