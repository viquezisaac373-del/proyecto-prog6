using Microsoft.AspNetCore.Mvc;

namespace ProyectoHospital.Controllers
{
    // HU06: navegación a la sección de medicamentos.
    // El mantenimiento de medicamentos todavía no está implementado.
    public class MedicamentosController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
