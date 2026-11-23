using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.Entities;
using ProyectoHospital.Models.ViewModels;
using ProyectoHospital.Repositories;
using System.Security.Claims;

namespace ProyectoHospital.Controllers;

[Authorize(Roles = "Cliente")]
public sealed class CuentaClienteController : Controller
{
    private readonly ClienteCuentaRepository _clientes;
    private readonly IPasswordHasher<UsuarioRecord> _hasher;
    private readonly ILogger<CuentaClienteController> _logger;

    public CuentaClienteController(
        ClienteCuentaRepository clientes,
        IPasswordHasher<UsuarioRecord> hasher,
        ILogger<CuentaClienteController> logger)
    {
        _clientes = clientes;
        _hasher = hasher;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Registrar()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new RegistroClienteViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistroClienteViewModel modelo)
    {
        if (User.Identity?.IsAuthenticated == true)
            return BadRequest("Cierre la sesión actual antes de registrar una nueva cuenta.");

        string identificacion = modelo.NumeroIdentificacion?.Trim() ?? string.Empty;
        if (modelo.TipoIdentificacion == "Cedula" &&
            (identificacion.Length != 9 || identificacion[0] == '0'))
            ModelState.AddModelError(nameof(modelo.NumeroIdentificacion),
                "La cédula física costarricense debe tener 9 dígitos y no iniciar con cero.");

        if (modelo.TipoIdentificacion == "DIMEX" &&
            (identificacion.Length is not (11 or 12) || identificacion[0] == '0'))
            ModelState.AddModelError(nameof(modelo.NumeroIdentificacion),
                "El DIMEX debe tener 11 o 12 dígitos y no iniciar con cero.");

        if (!modelo.AceptaTratamiento)
            ModelState.AddModelError(nameof(modelo.AceptaTratamiento),
                "Debe aceptar el tratamiento de datos indicado.");

        if (!ModelState.IsValid)
            return View(modelo);

        try
        {
            string usuario = modelo.NombreUsuario.Trim();
            string hash = _hasher.HashPassword(
                new UsuarioRecord { NombreUsuario = usuario }, modelo.Clave);
            await _clientes.RegistrarAsync(modelo, hash);
            TempData["Exito"] = "Cuenta de cliente creada. Puede iniciar sesión.";
            return RedirectToAction("Login", "Account");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            ModelState.AddModelError(string.Empty, "Ya existe un usuario o cliente con esos datos.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible registrar al cliente.");
            ModelState.AddModelError(string.Empty, "No se pudo completar el registro. Intente nuevamente.");
        }

        modelo.Clave = string.Empty;
        modelo.ConfirmacionClave = string.Empty;
        ModelState.Remove(nameof(modelo.Clave));
        ModelState.Remove(nameof(modelo.ConfirmacionClave));
        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> MiCuenta()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) || id <= 0)
            return Forbid();

        var cuenta = await _clientes.ObtenerMiCuentaAsync(id);
        if (cuenta is null) return NotFound("No se encontró un cliente activo asociado a esta cuenta.");
        return View(cuenta);
    }

    [HttpGet]
    public async Task<IActionResult> Catalogo()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) || id <= 0)
            return Forbid();

        // Exigir vínculo vigente para no mostrar funciones de cliente a cuentas mal configuradas.
        if (await _clientes.ObtenerMiCuentaAsync(id) is null)
            return NotFound("No existe un cliente activo asociado a esta cuenta.");

        return View(await _clientes.ListarCatalogoAsync());
    }
}
