using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoHospital.Models;
using ProyectoHospital.Models.ViewModels;
using System.Diagnostics;

namespace ProyectoHospital.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // HU06: Servicios del inicio y destino de navegación.
            var modelo = new HomeViewModel();

            modelo.Servicios.AddRange(new[]
            {
                new ServicioHospitalario
                {
                    Id = 1,
                    Nombre = "Citas médicas",
                    Descripcion = "Consulte la sección de citas del sistema hospitalario.",
                    TipoServicio = TipoServicioHospitalario.Citas
                },
                new ServicioHospitalario
                {
                    Id = 2,
                    Nombre = "Pacientes",
                    Descripcion = "Consulte, registre, edite y desactive pacientes.",
                    TipoServicio = TipoServicioHospitalario.Pacientes
                },
                new ServicioHospitalario
                {
                    Id = 3,
                    Nombre = "Medicamentos",
                    Descripcion = "Consulte la sección de medicamentos del sistema hospitalario.",
                    TipoServicio = TipoServicioHospitalario.Medicamentos
                }
            });

            return View(modelo);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
