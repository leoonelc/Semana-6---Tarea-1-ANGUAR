using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

const string frontendOrigin = "http://localhost:4200";
const string authCookieName = "login_sesion";
const string xsrfCookieName = "XSRF-TOKEN";

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy.WithOrigins(frontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = authCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "login_xsrf_cookie";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "X-XSRF-TOKEN";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AngularApp");
app.UseAuthentication();
app.UseAuthorization();

var users = new Dictionary<string, DemoUser>(StringComparer.OrdinalIgnoreCase)
{
    ["maria"] = new("maria", "Maria Camila", "123456", ["Usuario"]),
    ["admin"] = new("admin", "Administrador", "admin123", ["Administrador", "Usuario"]),
    ["leonel"] = new("leonel", "Leonel Castillo", "leonel123", ["Usuario"])
};

app.MapGet("/", () => Results.Ok(new { app = "LoginSesion.Api", status = "ok" }));

app.MapGet("/api/auth/csrf", (HttpContext httpContext, IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(httpContext);
    httpContext.Response.Cookies.Append(xsrfCookieName, tokens.RequestToken ?? "", new CookieOptions
    {
        HttpOnly = false,
        SameSite = SameSiteMode.Lax,
        Secure = httpContext.Request.IsHttps,
        Path = "/"
    });

    return Results.Ok(new { tokenHeader = "X-XSRF-TOKEN", tokenCookie = xsrfCookieName, ready = !string.IsNullOrWhiteSpace(tokens.RequestToken) });
});

app.MapPost("/api/auth/login", async (HttpContext httpContext, IAntiforgery antiforgery, LoginRequest request) =>
{
    if (!await IsValidCsrf(httpContext, antiforgery))
    {
        return Results.BadRequest(new ApiMessage("Token XSRF invalido o ausente."));
    }

    if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new ApiMessage("Debe escribir usuario y contrasena."));
    }

    if (!users.TryGetValue(request.Usuario.Trim(), out var user) || user.Password != request.Password)
    {
        return Results.Unauthorized();
    }

    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Usuario),
        new(ClaimTypes.Name, user.Nombre)
    };
    claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    var properties = new AuthenticationProperties
    {
        IsPersistent = false,
        IssuedUtc = DateTimeOffset.UtcNow,
        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(20)
    };

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);

    return Results.Ok(new SessionResponse(true, user.Usuario, user.Nombre, DateTimeOffset.UtcNow.AddMinutes(20)));
});

app.MapGet("/api/auth/session", (ClaimsPrincipal user) =>
{
    if (user.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var usuario = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    var nombre = user.Identity.Name ?? usuario;
    return Results.Ok(new SessionResponse(true, usuario, nombre, null));
});

app.MapPost("/api/auth/logout", async (HttpContext httpContext, IAntiforgery antiforgery) =>
{
    if (!await IsValidCsrf(httpContext, antiforgery))
    {
        return Results.BadRequest(new ApiMessage("Token XSRF invalido o ausente."));
    }

    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new ApiMessage("Sesion cerrada correctamente."));
});

app.MapGet("/api/perfil", (ClaimsPrincipal user) =>
{
    if (user.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        nombre = user.Identity.Name,
        usuario = user.FindFirstValue(ClaimTypes.NameIdentifier),
        roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray(),
        mensaje = "Esta informacion solo se entrega con una sesion valida en el backend."
    });
});

app.Run();

static async Task<bool> IsValidCsrf(HttpContext httpContext, IAntiforgery antiforgery)
{
    try
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        return true;
    }
    catch (AntiforgeryValidationException)
    {
        return false;
    }
}

record LoginRequest(string Usuario, string Password);
record SessionResponse(bool Authenticated, string Usuario, string Nombre, DateTimeOffset? ExpiresAt);
record ApiMessage(string Message);
record DemoUser(string Usuario, string Nombre, string Password, string[] Roles);
