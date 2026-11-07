var builder = WebApplication.CreateBuilder(args);

// Configuración de MVC.
builder.Services.AddControllersWithViews();

// HU09: Registrar el repositorio de pacientes.
// Debe realizarse ANTES de builder.Build() para
// permitir su uso mediante inyección de dependencias.
builder.Services.AddScoped<
    ProyectoHospital.Repositories.PacienteRepository>();

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

app.UseAuthorization();

// Configuración de las rutas MVC.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();