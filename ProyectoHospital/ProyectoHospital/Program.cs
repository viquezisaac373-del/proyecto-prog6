using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ProyectoHospital.Models.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Configuración de MVC.
builder.Services.AddControllersWithViews();

// HU09: Registrar el repositorio de pacientes.
// Debe realizarse ANTES de builder.Build() para
// permitir su uso mediante inyección de dependencias.
builder.Services.AddScoped<
    ProyectoHospital.Repositories.PacienteRepository>();

builder.Services.AddScoped<
    ProyectoHospital.Repositories.UsuarioRepository>();

builder.Services.AddScoped<
    ProyectoHospital.Repositories.UsuarioGestionRepository>();

builder.Services.AddScoped<
    ProyectoHospital.Repositories.ClienteCuentaRepository>();

builder.Services.AddScoped<
    ProyectoHospital.Repositories.DisponibilidadMedicaRepository>();

builder.Services.AddScoped<
    ProyectoHospital.Repositories.RecomendacionesReposoRepository>();

builder.Services.AddScoped<
    ProyectoHospital.Repositories.ProveedoresRepository>();

builder.Services.AddScoped<
    ProyectoHospital.Repositories.BitacoraRepository>();

// Alta, consulta y gestión de nacimientos.
builder.Services.AddScoped<
    ProyectoHospital.Repositories.RecienNacidoRepository>();

// PasswordHasher de ASP.NET Core Identity (PBKDF2, versión Identity V3).
// Se mantiene el mismo formato verificable al actualizar su costo.
builder.Services.Configure<PasswordHasherOptions>(options =>
{
    options.IterationCount = 210_000;
});

builder.Services.AddScoped<IPasswordHasher<UsuarioRecord>, PasswordHasher<UsuarioRecord>>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        // El contenido de una cookie no se actualiza automáticamente al
        // desactivar cuentas o modificar roles. Revisar la BD en cada solicitud
        // autenticada para revocar sesiones existentes inmediatamente.
        options.Events.OnValidatePrincipal = async context =>
        {
            bool sesionValida = false;

            if (int.TryParse(
                    context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),
                    out int usuarioId) && usuarioId > 0)
            {
                try
                {
                    var usuarios = context.HttpContext.RequestServices
                        .GetRequiredService<ProyectoHospital.Repositories.UsuarioRepository>();
                    var (activo, rolesActuales) = await usuarios.ObtenerEstadoSesionAsync(usuarioId);

                    var rolesCookie = context.Principal!
                        .FindAll(ClaimTypes.Role)
                        .Select(claim => claim.Value)
                        .ToHashSet(StringComparer.Ordinal);

                    // También revocar cookies con permisos que ya no correspondan.
                    sesionValida = activo && rolesActuales.Count > 0 &&
                        rolesCookie.SetEquals(rolesActuales);
                }
                catch (Exception ex)
                {
                    // En un error de BD, negar acceso antes que conservar
                    // una sesión cuya autorización no se puede comprobar.
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("Hospinomenal.ValidacionSesion");
                    logger.LogError(ex, "No fue posible revalidar la sesión del usuario {UsuarioId}.", usuarioId);
                }
            }

            if (!sesionValida)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Construir la aplicación después de registrar los servicios.
var app = builder.Build();

// Configuración del entorno.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// Configuración de las rutas MVC.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();