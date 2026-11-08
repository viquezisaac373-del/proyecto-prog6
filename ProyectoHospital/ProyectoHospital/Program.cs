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