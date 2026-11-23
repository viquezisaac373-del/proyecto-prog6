using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.Entities;
using ProyectoHospital.Models.ViewModels;
using ProyectoHospital.Repositories;
using System.Security.Claims;

namespace ProyectoHospital.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class UsuariosController : Controller
{
    private readonly UsuarioGestionRepository _repositorio;
    private readonly IPasswordHasher<UsuarioRecord> _hasher;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        UsuarioGestionRepository repositorio,
        IPasswordHasher<UsuarioRecord> hasher,
        ILogger<UsuariosController> logger)
    {
        _repositorio = repositorio;
        _hasher = hasher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var modelo = new UsuariosIndexViewModel
        {
            Usuarios = await _repositorio.ListarUsuariosAsync(),
            CantidadSinCuenta = (await _repositorio.ListarEmpleadosSinCuentaAsync()).Count
        };
        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        var modelo = new CrearUsuarioViewModel
        {
            Roles = await _repositorio.ListarRolesAsync()
        };
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearUsuarioViewModel modelo)
    {
        var rol = await _repositorio.ObtenerRolAsync(modelo.RolId);
        if (rol is null)
            ModelState.AddModelError(nameof(modelo.RolId), "Seleccione un rol activo.");

        bool esMedico = modelo.EsMedico ||
            string.Equals(rol?.Nombre, "Medico", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rol?.Nombre, "Médico", StringComparison.OrdinalIgnoreCase);

        if (esMedico)
        {
            if (string.IsNullOrWhiteSpace(modelo.Especialidad))
                ModelState.AddModelError(nameof(modelo.Especialidad), "Indique la especialidad médica.");
            if (!modelo.HoraEntrada.HasValue)
                ModelState.AddModelError(nameof(modelo.HoraEntrada), "Indique la hora de entrada.");
            if (!modelo.HoraSalida.HasValue)
                ModelState.AddModelError(nameof(modelo.HoraSalida), "Indique la hora de salida.");
            if (modelo.HoraEntrada.HasValue && modelo.HoraSalida.HasValue &&
                modelo.HoraSalida <= modelo.HoraEntrada)
                ModelState.AddModelError(nameof(modelo.HoraSalida), "La salida debe ser posterior a la entrada.");
            if (!modelo.DuracionCitaMin.HasValue || modelo.DuracionCitaMin <= 0)
                ModelState.AddModelError(nameof(modelo.DuracionCitaMin), "Indique la duración de cita.");
        }

        if (!ModelState.IsValid)
        {
            modelo.Roles = await _repositorio.ListarRolesAsync();
            return View(modelo);
        }

        if (!ObtenerUsuarioActual(out int operadorId))
            return Forbid();

        try
        {
            string nombreUsuario = modelo.NombreUsuario.Trim();
            string hash = _hasher.HashPassword(
                new UsuarioRecord { NombreUsuario = nombreUsuario }, modelo.Clave);

            await _repositorio.CrearConEmpleadoAsync(modelo, hash, esMedico, operadorId);
            TempData["Exito"] = "El empleado y su cuenta se registraron correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            ModelState.AddModelError(string.Empty, "El usuario o la identificación ya están registrados.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible crear el empleado y su usuario.");
            ModelState.AddModelError(string.Empty, "No fue posible registrar la cuenta. Revise el estado de la base de datos.");
        }

        modelo.Clave = string.Empty;
        modelo.ConfirmacionClave = string.Empty;
        ModelState.Remove(nameof(modelo.Clave));
        ModelState.Remove(nameof(modelo.ConfirmacionClave));
        modelo.Roles = await _repositorio.ListarRolesAsync();
        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Vincular()
    {
        var modelo = new VincularUsuarioViewModel
        {
            Empleados = await _repositorio.ListarEmpleadosSinCuentaAsync(),
            Roles = await _repositorio.ListarRolesAsync()
        };
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vincular(VincularUsuarioViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            await CompletarOpciones(modelo);
            return View(modelo);
        }

        if (!ObtenerUsuarioActual(out int operadorId))
            return Forbid();

        try
        {
            string nombreUsuario = modelo.NombreUsuario.Trim();
            string hash = _hasher.HashPassword(
                new UsuarioRecord { NombreUsuario = nombreUsuario }, modelo.Clave);
            await _repositorio.VincularAsync(modelo, hash, operadorId);
            TempData["Exito"] = "Se creó la cuenta para el empleado existente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            ModelState.AddModelError(string.Empty, "El nombre de usuario ya está registrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible vincular un empleado con una cuenta.");
            ModelState.AddModelError(string.Empty, "No fue posible vincular la cuenta.");
        }

        modelo.Clave = string.Empty;
        modelo.ConfirmacionClave = string.Empty;
        ModelState.Remove(nameof(modelo.Clave));
        ModelState.Remove(nameof(modelo.ConfirmacionClave));
        await CompletarOpciones(modelo);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int usuarioId, bool activar)
    {
        if (!ObtenerUsuarioActual(out int operadorId))
            return Forbid();

        if (usuarioId == operadorId && !activar)
        {
            TempData["Error"] = "No puede desactivar su propia cuenta.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _repositorio.CambiarEstadoAsync(usuarioId, activar, operadorId);
            TempData["Exito"] = activar ? "Cuenta activada." : "Cuenta desactivada.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al modificar el estado de un usuario.");
            TempData["Error"] = "No se pudo modificar el estado de la cuenta.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static bool EsUsuarioValido(string? id, out int usuarioId) =>
        int.TryParse(id, out usuarioId) && usuarioId > 0;

    private bool ObtenerUsuarioActual(out int usuarioId) =>
        EsUsuarioValido(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);

    private async Task CompletarOpciones(VincularUsuarioViewModel modelo)
    {
        modelo.Roles = await _repositorio.ListarRolesAsync();
        modelo.Empleados = await _repositorio.ListarEmpleadosSinCuentaAsync();
    }
}
