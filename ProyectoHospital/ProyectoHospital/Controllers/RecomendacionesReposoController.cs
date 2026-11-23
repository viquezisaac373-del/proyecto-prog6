using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoHospital.Models.ViewModels;
using ProyectoHospital.Repositories;
using System.Security.Claims;

namespace ProyectoHospital.Controllers;

/// <summary>
/// Documento interno para registrar recomendación médica de reposo.
/// NO constituye una incapacidad oficial de CCSS ni INS.
/// </summary>
[Authorize(Roles = "Medico,Médico")]
public sealed class RecomendacionesReposoController : Controller
{
    private readonly RecomendacionesReposoRepository _repo;
    private readonly ILogger<RecomendacionesReposoController> _logger;

    public RecomendacionesReposoController(
        RecomendacionesReposoRepository repo,
        ILogger<RecomendacionesReposoController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var medicoId = await ObtenerMedicoActualAsync();
        if (medicoId is null) return Forbid();
        return View(await ConstruirModeloAsync(medicoId.Value, new CrearRecomendacionReposoViewModel
        {
            FechaInicio = DateTime.Today,
            FechaFin = DateTime.Today
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        [Bind(Prefix = "Nuevo")] CrearRecomendacionReposoViewModel modelo)
    {
        var medicoId = await ObtenerMedicoActualAsync();
        if (medicoId is null) return Forbid();

        if (modelo.FechaInicio.HasValue && modelo.FechaFin.HasValue &&
            modelo.FechaFin.Value.Date < modelo.FechaInicio.Value.Date)
            ModelState.AddModelError("Nuevo.FechaFin", "La fecha final debe ser igual o posterior a la inicial.");

        if (!ModelState.IsValid)
            return View("Index", await ConstruirModeloAsync(medicoId.Value, modelo));

        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            int id = await _repo.CrearAsync(modelo, usuarioId);
            TempData["Exito"] = "Recomendación interna registrada. No es una incapacidad oficial de la CCSS.";
            return RedirectToAction(nameof(Comprobante), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo al registrar recomendación médica de reposo.");
            ModelState.AddModelError(string.Empty, "No se pudo registrar la recomendación.");
        }

        return View("Index", await ConstruirModeloAsync(medicoId.Value, modelo));
    }

    [HttpGet]
    public async Task<IActionResult> Comprobante(int id)
    {
        var medicoId = await ObtenerMedicoActualAsync();
        if (medicoId is null) return Forbid();

        var registro = await _repo.ObtenerPropiaAsync(id, medicoId.Value);
        if (registro is null) return NotFound();
        return View(registro);
    }

    private async Task<int?> ObtenerMedicoActualAsync()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int usuarioId)
            || usuarioId <= 0) return null;

        return await _repo.ObtenerMedicoPorUsuarioAsync(usuarioId);
    }

    private async Task<RecomendacionesReposoIndexViewModel> ConstruirModeloAsync(
        int medicoId, CrearRecomendacionReposoViewModel nuevo) => new()
    {
        MedicoEmpleadoId = medicoId,
        Nuevo = nuevo,
        Pacientes = await _repo.ListarPacientesActivosAsync(),
        Registros = await _repo.ListarPropiasAsync(medicoId)
    };
}
