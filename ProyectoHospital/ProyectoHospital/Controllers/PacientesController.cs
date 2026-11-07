using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.ViewModels;
using ProyectoHospital.Repositories;

namespace ProyectoHospital.Controllers
{
    // HU09: Controlador del mantenimiento de pacientes.
    public class PacientesController : Controller
    {
        private readonly PacienteRepository _pacientes;
        private readonly ILogger<PacientesController> _logger;

        // Los servicios son proporcionados por ASP.NET Core.
        public PacientesController(
            PacienteRepository pacientes,
            ILogger<PacientesController> logger)
        {
            _pacientes = pacientes;
            _logger = logger;
        }

        // GET: /Pacientes
        // Consulta los pacientes registrados y muestra el listado.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pacientes = await _pacientes.ListarAsync();

            return View(pacientes);
        }

        // GET: /Pacientes/Create
        // Muestra el formulario de registro.
        [HttpGet]
        public IActionResult Create()
        {
            return View(new PacienteFormViewModel());
        }

        // POST: /Pacientes/Create
        // Recibe los datos del formulario y registra al paciente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PacienteFormViewModel modelo)
        {
            // Validar los datos recibidos antes de enviarlos a SQL Server.
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            try
            {
                // Ejecutar usp_RegistrarPaciente mediante el repositorio.
                int pacienteId = await _pacientes.RegistrarAsync(modelo);

                // Mostrar confirmación después del registro.
                TempData["MensajeExito"] =
                    $"Paciente registrado correctamente. ID: {pacienteId}.";

                // Regresar al listado para comprobar el resultado.
                return RedirectToAction(nameof(Index));
            }
            // HU09 - T03
            // SQL Server utiliza los errores 2601 y 2627
            // cuando se intenta registrar un valor que debe ser único.
            catch (SqlException ex)
                when (ex.Number == 2601 || ex.Number == 2627)
            {
                // Registrar el evento sin mostrar detalles técnicos al usuario.
                _logger.LogWarning(
                    "Se intentó registrar una identificación duplicada.");

                // Mostrar el mensaje junto al campo correspondiente.
                ModelState.AddModelError(
                    nameof(modelo.NumeroIdentificacion),
                    "Ya existe un paciente registrado con esta identificación.");

                // Conservar los datos del formulario para su corrección.
                return View(modelo);
            }
            catch (SqlException ex)
            {
                // Registrar el error para diagnóstico sin exponer
                // detalles internos de SQL Server al usuario.
                _logger.LogError(
                    ex,
                    "Error SQL al registrar un paciente.");

                ModelState.AddModelError(
                    string.Empty,
                    "No fue posible registrar al paciente. " +
                    "Verifique los datos o contacte al administrador.");

                return View(modelo);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error inesperado en el registro de pacientes.");

                ModelState.AddModelError(
                    string.Empty,
                    "Ocurrió un error al procesar el registro.");

                return View(modelo);
            }
        }
    }
}