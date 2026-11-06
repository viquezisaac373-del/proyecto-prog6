using Microsoft.AspNetCore.Mvc;
using ProyectoHospital.Repositories;

namespace ProyectoHospital.Controllers
{
    // Controlador del mantenimiento de pacientes.
    public class PacientesController : Controller
    {
        private readonly PacienteRepository _pacientes;

        // ASP.NET Core proporciona el repositorio automáticamente.
        public PacientesController(PacienteRepository pacientes)
        {
            _pacientes = pacientes;
        }

        // GET: /Pacientes
        // Obtiene los registros y los envía a la vista.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pacientes = await _pacientes.ListarAsync();

            return View(pacientes);
        }
    }
}
