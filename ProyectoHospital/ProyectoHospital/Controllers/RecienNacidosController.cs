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
            var nacimientos = await _repositorio.ListarTodosAsync();
            return View(nacimientos);
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
            // Evitar que un POST manipulado introduzca el valor genérico
            // "Otro", conservado únicamente para expedientes históricos.
            if (modelo.Genero == "Otro")
            {
                ModelState.AddModelError(nameof(modelo.Genero),
                    "Para nuevos nacimientos seleccione Masculino, Femenino o Indeterminado.");
            }

            // Esta ruta es para altas recientes. Un nacimiento de años atrás debe
            // tramitarse mediante un flujo histórico autorizado, no como RN actual.
            // Ventana provisional de QA: confirmar con el PO antes de producción.
            if (modelo.FechaHoraNacimiento.HasValue &&
                modelo.FechaHoraNacimiento.Value < DateTime.Now.AddDays(-30))
            {
                ModelState.AddModelError(nameof(modelo.FechaHoraNacimiento),
                    "El registro ordinario admite nacimientos de los últimos 30 días. " +
                    "Para un nacimiento anterior se requiere un procedimiento histórico.");
            }

            if (!ModelState.IsValid)
                return await MostrarFormularioAsync(modelo);

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),
                    out int usuarioId) || usuarioId <= 0)
                return Forbid();

            try
            {
                // Nunca confiar exclusivamente en el filtro del selector HTML.
                // Validación provisional: Género=Femenino en la ficha actual.
                // Una futura relación clínica de gestación evitará inferirlo
                // únicamente del género administrativo del expediente.
                if (!await _repositorio.EsMadreSeleccionableAsync(
                        modelo.MadrePacienteId, modelo.FechaHoraNacimiento!.Value))
                {
                    ModelState.AddModelError(nameof(modelo.MadrePacienteId),
                        "Seleccione un expediente habilitado como madre. " +
                        "Debe estar activo y tener al menos 10 años a la fecha del parto " +
                        "(regla provisional; los casos excepcionales requieren revisión clínica).");
                    return await MostrarFormularioAsync(modelo);
                }

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

        // Completar tipo sanguíneo del bebé solamente con resultado confirmado.
        [HttpGet]
        [Authorize(Roles = "Administrador,Medico,Médico")]
        public async Task<IActionResult> CompletarTipoSangre(int id)
        {
            if (id <= 0)
                return BadRequest();

            var nombre = await _repositorio.ObtenerNombrePendienteTipificacionAsync(id);
            if (nombre is null)
                return NotFound();

            ViewData["NombreBebe"] = nombre;
            return View(new CompletarTipoSangreRecienNacidoViewModel
            {
                PacienteId = id
            });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Medico,Médico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletarTipoSangre(
            CompletarTipoSangreRecienNacidoViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                ViewData["NombreBebe"] =
                    await _repositorio.ObtenerNombrePendienteTipificacionAsync(modelo.PacienteId);
                return View(modelo);
            }

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),
                    out int usuarioId) || usuarioId <= 0)
                return Forbid();

            try
            {
                await _repositorio.CompletarTipoSangreAsync(modelo, usuarioId);
                TempData["MensajeExito"] =
                    $"Tipificación registrada para el paciente #{modelo.PacienteId}, " +
                    "con referencia clínica y auditoría.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number == 51981)
            {
                ModelState.AddModelError(string.Empty,
                    "El paciente no tiene tipificación pendiente o el nacimiento está inactivo.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL al completar tipo sanguíneo.");
                ModelState.AddModelError(string.Empty,
                    "No fue posible completar la tipificación. Verifique la compatibilidad SQL.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al completar tipo sanguíneo.");
                ModelState.AddModelError(string.Empty,
                    "No fue posible completar la operación.");
            }

            ViewData["NombreBebe"] =
                await _repositorio.ObtenerNombrePendienteTipificacionAsync(modelo.PacienteId)
                ?? "Paciente";
            return View(modelo);
        }

        // Edición de datos administrativos del bebé y detalles del nacimiento.
        // No permite cambiar madre/médico, identificación ni tipo sanguíneo.
        [HttpGet]
        [Authorize(Roles = "Administrador,Medico,Médico")]
        public async Task<IActionResult> Editar(int id)
        {
            if (id <= 0)
                return BadRequest();

            var modelo = await _repositorio.ObtenerParaEditarAsync(id);
            return modelo is null ? NotFound() : View(modelo);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Medico,Médico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, EditarRecienNacidoViewModel modelo)
        {
            if (id <= 0 || modelo.NacimientoId != id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(modelo);

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),
                    out int usuarioId) || usuarioId <= 0)
                return Forbid();

            try
            {
                await _repositorio.EditarAsync(modelo, usuarioId);
                TempData["MensajeExito"] =
                    $"Nacimiento #{id} y datos administrativos del bebé actualizados correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number == 51951)
            {
                ModelState.AddModelError(string.Empty,
                    "El nacimiento ya no está disponible para edición. Actualice el listado.");
            }
            catch (SqlException ex) when (ex.Number == 51952)
            {
                ModelState.AddModelError(nameof(modelo.FechaHoraNacimiento),
                    "La fecha y hora del nacimiento no pueden ser futuras.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL al editar nacimiento {NacimientoId}.", id);
                ModelState.AddModelError(string.Empty,
                    "No se pudo guardar la edición. Revise los datos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar nacimiento {NacimientoId}.", id);
                ModelState.AddModelError(string.Empty, "No fue posible completar la edición.");
            }

            return View(modelo);
        }

        // La anulación es exclusiva del administrador, exige justificación,
        // mantiene el expediente del paciente y queda en la bitácora.
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Anular(int id)
        {
            if (id <= 0)
                return BadRequest();

            var nacimientos = await _repositorio.ListarTodosAsync();
            var nacimiento = nacimientos.FirstOrDefault(n => n.NacimientoId == id);
            if (nacimiento is null || !nacimiento.NacimientoActivo)
                return NotFound();

            ViewData["NombreBebe"] = nacimiento.NombreCompleto;
            ViewData["PacienteId"] = nacimiento.PacienteId;
            return View(new AnularNacimientoViewModel { NacimientoId = id });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Anular(int id, AnularNacimientoViewModel modelo)
        {
            if (id <= 0 || modelo.NacimientoId != id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(modelo);

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),
                    out int usuarioId) || usuarioId <= 0)
                return Forbid();

            try
            {
                await _repositorio.AnularAsync(id, modelo.Motivo, usuarioId);
                TempData["MensajeExito"] =
                    $"Nacimiento #{id} anulado. El expediente del paciente permanece conservado.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number == 51953)
            {
                ModelState.AddModelError(string.Empty,
                    "El nacimiento ya fue anulado o no existe.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL al anular nacimiento {NacimientoId}.", id);
                ModelState.AddModelError(string.Empty,
                    "No fue posible anular el nacimiento.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al anular nacimiento {NacimientoId}.", id);
                ModelState.AddModelError(string.Empty,
                    "No fue posible completar la operación.");
            }

            return View(modelo);
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
