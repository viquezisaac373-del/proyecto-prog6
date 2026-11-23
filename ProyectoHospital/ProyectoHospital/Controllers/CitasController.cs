using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoHospital.Controllers
{
    // HU06: navegación a la sección de citas.
    // El mantenimiento de citas se integrará en su propia historia.
    [Authorize(Roles = "Administrador,Medico,Médico,Secretaria")]
    public class CitasController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
