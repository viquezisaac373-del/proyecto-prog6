using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoHospital.Models.ViewModels;
using ProyectoHospital.Repositories;
using System.Security.Claims;

namespace ProyectoHospital.Controllers;

[Authorize(Roles = "Administrador,Medico,Médico,Secretaria")]
public sealed class DisponibilidadMedicaController : Controller
{
    private readonly DisponibilidadMedicaRepository _repository;
    private readonly ILogger<DisponibilidadMedicaController> _logger;

    public DisponibilidadMedicaController(
        DisponibilidadMedicaRepository repository,
        ILogger<DisponibilidadMedicaController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? medicoId = null, string? especialidad = null)
    {
        var vm = await ConstruirVistaAsync(new CrearBloqueDisponibilidadViewModel(), medicoId, especialidad);
        return View(vm);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        [Bind(Prefix = "Nuevo")] CrearBloqueDisponibilidadViewModel form)
    {
        if (form.HoraInicio.HasValue && form.HoraFin.HasValue &&
            form.HoraFin <= form.HoraInicio)
            ModelState.AddModelError("Nuevo.HoraFin",
                "La hora final debe ser posterior a la inicial. No se admiten turnos nocturnos en este formulario.");

        if (!ModelState.IsValid)
            return View("Index", await ConstruirVistaAsync(form, null, null));

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int actorId))
            return Forbid();

        try
        {
            await _repository.CrearAsync(form, actorId);
            TempData["Exito"] = "Disponibilidad semanal registrada correctamente.";
            return RedirectToAction(nameof(Index), new { medicoId = form.EmpleadoId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar disponibilidad semanal.");
            ModelState.AddModelError(string.Empty,
                "No fue posible guardar el horario. Verifique que se aplicó la migración de integración.");
        }
        return View("Index", await ConstruirVistaAsync(form, null, null));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int bloqueId, int? medicoId = null)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int actorId))
            return Forbid();

        try
        {
            await _repository.DesactivarAsync(bloqueId, actorId);
            TempData["Exito"] = "Bloque desactivado.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar bloque de disponibilidad.");
            TempData["Error"] = "No fue posible desactivar el bloque.";
        }
        return RedirectToAction(nameof(Index), new { medicoId });
    }

    private async Task<DisponibilidadMedicaIndexViewModel> ConstruirVistaAsync(
        CrearBloqueDisponibilidadViewModel form, int? medicoId, string? especialidad)
    {
        return new DisponibilidadMedicaIndexViewModel
        {
            Nuevo = form,
            Medicos = await _repository.ListarMedicosAsync(),
            Bloques = await _repository.ListarBloquesAsync(medicoId, especialidad),
            MedicoFiltro = medicoId,
            EspecialidadFiltro = especialidad
        };
    }
}
