using Microsoft.AspNetCore.Mvc;

namespace ProyectoHospital.Controllers
{
    // HU06: navegación a la sección de citas.
    // El mantenimiento de citas se integrará en su propia historia.
    public class CitasController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
