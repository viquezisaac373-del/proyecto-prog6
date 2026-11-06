using Microsoft.AspNetCore.Mvc;

namespace ProyectoHospital.Controllers
{
    // Controlador del módulo de gestión de pacientes.
    // Recibe las solicitudes del usuario y determina
    // qué vista debe presentar la aplicación.
    public class PacientesController : Controller
    {
        // GET: /Pacientes
        // Muestra la pantalla principal del mantenimiento.
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
