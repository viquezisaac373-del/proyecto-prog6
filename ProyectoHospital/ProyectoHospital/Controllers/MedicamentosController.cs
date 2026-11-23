using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoHospital.Controllers
{
    // HU06: navegación a la sección de medicamentos.
    // El mantenimiento de medicamentos todavía no está implementado.
    [Authorize(Roles = "Administrador,Medico,Médico,Secretaria")]
    public class MedicamentosController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
