using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProyectoHospital.Controllers
{
    // Panel solo de módulos realmente integrados en esta rama.
    // Las rutas provisionales de citas y medicamentos no se anuncian
    // como disponibles para el usuario.
    [Authorize(Roles = "Administrador,Medico,Médico,Secretaria")]
    public class ModulosController : Controller
    {
        [HttpGet]
        public IActionResult Index() => View();
    }
}
