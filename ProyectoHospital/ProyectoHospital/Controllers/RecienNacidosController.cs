using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.ViewModels;
using ProyectoHospital.Repositories;

namespace ProyectoHospital.Controllers
{
    // Solo personal autorizado. Las denominaciones deben coincidir con dbo.Roles.
    [Authorize(Roles = "Administrador,Medico,Médico,Secretaria")]
    public class RecienNacidosController : Controller
    {
        private readonly RecienNacidoRepository _repositorio;
        private readonly ILogger<RecienNacidosController> _logger;

        public RecienNacidosController(
            RecienNacidoRepository repositorio,
            ILogger<RecienNacidosController> logger)
        {
            _repositorio = repositorio;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pendientes = await _repositorio.ListarPendientesAsync();
            return View(pendientes);
        }

        [HttpGet]
        public async Task<IActionResult> Registrar()
        {
            await CargarSelectoresAsync();
            return View(new RecienNacidoFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RecienNacidoFormViewModel modelo)
        {
            if (!ModelState.IsValid)
                return await MostrarFormularioAsync(modelo);

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),
                    out int usuarioId) || usuarioId <= 0)
                return Forbid();

            try
            {
                var (pacienteId, nacimientoId) =
                    await _repositorio.RegistrarAsync(modelo, usuarioId);

                TempData["MensajeExito"] =
                    $"Nacimiento registrado. Paciente #{pacienteId}; nacimiento #{nacimientoId}. " +
                    "La identificación oficial queda pendiente.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number == 51030)
            {
                ModelState.AddModelError(nameof(modelo.Nombre),
                    "El nombre y los apellidos del bebé son obligatorios.");
            }
            catch (SqlException ex) when (ex.Number == 51032)
            {
                ModelState.AddModelError(nameof(modelo.FechaHoraNacimiento),
                    "La fecha y hora del nacimiento no son válidas.");
            }
            catch (SqlException ex) when (ex.Number == 51033)
            {
                ModelState.AddModelError(nameof(modelo.MadrePacienteId),
                    "La madre ya no está disponible o no está activa.");
            }
            catch (SqlException ex) when (ex.Number == 51034)
            {
                ModelState.AddModelError(nameof(modelo.EmpleadoId),
                    "El médico ya no está disponible o no está activo.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL al registrar un nacimiento.");
                ModelState.AddModelError(string.Empty,
                    "No se pudo registrar el nacimiento. Verifique los datos o contacte al administrador.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al registrar un nacimiento.");
                ModelState.AddModelError(string.Empty,
                    "No fue posible completar el registro.");
            }

            return await MostrarFormularioAsync(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> AsignarIdentificacion(int id)
        {
            if (id <= 0)
                return BadRequest();

            var pendiente = await _repositorio.ObtenerPendienteAsync(id);
            if (pendiente is null)
                return NotFound();

            ViewData["NombreBebe"] = pendiente.NombreCompleto;
            return View(new IdentificacionRecienNacidoViewModel
            {
                PacienteId = id
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarIdentificacion(
            IdentificacionRecienNacidoViewModel modelo)
        {
            if (!ModelState.IsValid)
                return await MostrarIdentificacionAsync(modelo);

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),
                    out int usuarioId) || usuarioId <= 0)
                return Forbid();

            try
            {
                await _repositorio.AsignarIdentificacionAsync(modelo, usuarioId);
                TempData["MensajeExito"] =
                    $"Identificación asignada al expediente #{modelo.PacienteId}, sin crear otro paciente.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                ModelState.AddModelError(nameof(modelo.NumeroIdentificacion),
                    "La identificación ya pertenece a otro paciente.");
            }
            catch (SqlException ex) when (ex.Number == 51110)
            {
                ModelState.AddModelError(nameof(modelo.NumeroIdentificacion),
                    "La identificación debe contener únicamente dígitos.");
            }
            catch (SqlException ex) when (ex.Number == 51111)
            {
                ModelState.AddModelError(string.Empty,
                    "El paciente no es un recién nacido pendiente de identificación.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL al asignar identificación al paciente {PacienteId}.",
                    modelo.PacienteId);
                ModelState.AddModelError(string.Empty,
                    "No fue posible asignar la identificación.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar identificación al paciente {PacienteId}.",
                    modelo.PacienteId);
                ModelState.AddModelError(string.Empty, "No fue posible completar la operación.");
            }

            return await MostrarIdentificacionAsync(modelo);
        }

        private async Task<IActionResult> MostrarFormularioAsync(RecienNacidoFormViewModel modelo)
        {
            await CargarSelectoresAsync();
            return View("Registrar", modelo);
        }

        private async Task<IActionResult> MostrarIdentificacionAsync(
            IdentificacionRecienNacidoViewModel modelo)
        {
            var pendiente = await _repositorio.ObtenerPendienteAsync(modelo.PacienteId);
            ViewData["NombreBebe"] = pendiente?.NombreCompleto ?? "Paciente";
            return View("AsignarIdentificacion", modelo);
        }

        private async Task CargarSelectoresAsync()
        {
            var madres = await _repositorio.ListarMadresAsync();
            var medicos = await _repositorio.ListarMedicosAsync();

            ViewBag.Madres = madres.Select(m => new SelectListItem(
                m.Etiqueta, m.Id.ToString())).ToList();
            ViewBag.Medicos = medicos.Select(m => new SelectListItem(
                m.Etiqueta, m.Id.ToString())).ToList();
            ViewBag.SinMedicos = medicos.Count == 0;
        }
    }
}
