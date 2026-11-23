using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.ViewModels;
using ProyectoHospital.Repositories;
using System.Security.Claims;

namespace ProyectoHospital.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class ProveedoresController : Controller
{
    private readonly ProveedoresRepository _repo;
    private readonly ILogger<ProveedoresController> _logger;

    public ProveedoresController(ProveedoresRepository repo, ILogger<ProveedoresController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index() => View(await ConstruirVistaAsync());

    [HttpGet]
    public async Task<IActionResult> Reporte(string? estado = null)
    {
        var proveedores = await _repo.ListarProveedoresAsync();
        var estadoValido = estado is "activos" or "inactivos" ? estado : "todos";
        ViewData["EstadoFiltro"] = estadoValido;
        ViewData["TotalActivos"] = proveedores.Count(p => p.Activo);
        ViewData["TotalInactivos"] = proveedores.Count(p => !p.Activo);
        var filtrados = estadoValido switch
        {
            "activos" => proveedores.Where(p => p.Activo).ToList(),
            "inactivos" => proveedores.Where(p => !p.Activo).ToList(),
            _ => proveedores
        };
        return View(filtrados);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearProveedor(
        [Bind(Prefix = "NuevoProveedor")] AltaProveedorViewModel form)
    {
        if (!ModelState.IsValid)
            return View("Index", await ConstruirVistaAsync(nuevoProveedor: form));
        if (!TryActor(out int actor)) return Forbid();

        try
        {
            int id = await _repo.CrearProveedorAsync(form, actor);
            TempData["Exito"] = $"Proveedor #{id} registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Fallo SQL al registrar proveedor");
            ModelState.AddModelError(string.Empty, "No se pudo registrar el proveedor.");
        }

        return View("Index", await ConstruirVistaAsync(nuevoProveedor: form));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearProducto(
        [Bind(Prefix = "NuevoProducto")] AltaProductoInventarioViewModel form)
    {
        if (!ModelState.IsValid)
            return View("Index", await ConstruirVistaAsync(nuevoProducto: form));
        if (!TryActor(out int actor)) return Forbid();

        try
        {
            int id = await _repo.CrearProductoAsync(form, actor);
            TempData["Exito"] = $"Producto interno #{id} agregado al inventario para abastecimientos.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Fallo SQL al agregar producto");
            ModelState.AddModelError(string.Empty, "No se pudo registrar el producto.");
        }

        return View("Index", await ConstruirVistaAsync(nuevoProducto: form));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VincularProducto(
        [Bind(Prefix = "NuevoVinculo")] VincularProductoProveedorViewModel form)
    {
        if (!ModelState.IsValid)
            return View("Index", await ConstruirVistaAsync(nuevoVinculo: form));
        if (!TryActor(out int actor)) return Forbid();

        try
        {
            await _repo.VincularProductoAsync(form, actor);
            TempData["Exito"] = "Producto asociado al proveedor.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Fallo SQL al asociar producto");
            ModelState.AddModelError(string.Empty, "No se pudo asociar el producto.");
        }

        return View("Index", await ConstruirVistaAsync(nuevoVinculo: form));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarAbastecimiento(
        [Bind(Prefix = "NuevoAbastecimiento")] RegistrarAbastecimientoViewModel form)
    {
        if (form.FechaRecepcion.HasValue && form.FechaRecepcion.Value.Date > DateTime.Today)
            ModelState.AddModelError("NuevoAbastecimiento.FechaRecepcion",
                "La fecha de recepción no puede ser futura.");
        if (!ModelState.IsValid)
            return View("Index", await ConstruirVistaAsync(nuevoAbastecimiento: form));
        if (!TryActor(out int actor)) return Forbid();

        try
        {
            int id = await _repo.RegistrarAbastecimientoAsync(form, actor);
            TempData["Exito"] = $"Abastecimiento #{id} registrado y vinculado a movimientos de inventario.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Fallo SQL al registrar abastecimiento");
            ModelState.AddModelError(string.Empty, "No se pudo registrar el abastecimiento.");
        }

        return View("Index", await ConstruirVistaAsync(nuevoAbastecimiento: form));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int proveedorId, bool activar)
    {
        if (!TryActor(out int actor)) return Forbid();

        try
        {
            await _repo.CambiarEstadoAsync(proveedorId, activar, actor);
            TempData["Exito"] = activar ? "Proveedor reactivado." : "Proveedor desactivado.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo al cambiar estado de proveedor #{ProveedorId}", proveedorId);
            TempData["Error"] = "No se pudo cambiar el estado del proveedor.";
        }

        return RedirectToAction(nameof(Index));
    }

    private bool TryActor(out int usuarioId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId)
        && usuarioId > 0;

    private async Task<ProveedoresIndexViewModel> ConstruirVistaAsync(
        AltaProveedorViewModel? nuevoProveedor = null,
        AltaProductoInventarioViewModel? nuevoProducto = null,
        VincularProductoProveedorViewModel? nuevoVinculo = null,
        RegistrarAbastecimientoViewModel? nuevoAbastecimiento = null)
    {
        return new ProveedoresIndexViewModel
        {
            NuevoProveedor = nuevoProveedor ?? new(),
            NuevoProducto = nuevoProducto ?? new(),
            NuevoVinculo = nuevoVinculo ?? new(),
            NuevoAbastecimiento = nuevoAbastecimiento ?? new(),
            Proveedores = await _repo.ListarProveedoresAsync(),
            Productos = await _repo.ListarProductosAsync(),
            Vinculos = await _repo.ListarVinculosAsync(),
            Abastecimientos = await _repo.ListarAbastecimientosAsync()
        };
    }
}
