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
            // Servicios informativos hasta que se implementen los módulos.
            var modelo = new HomeViewModel();
            modelo.Servicios.AddRange(new[]
            {
                new ServicioHospitalario { Id = 1, Nombre = "Citas médicas", Descripcion = "Permite consultar y programar citas." },
                new ServicioHospitalario { Id = 2, Nombre = "Pacientes", Descripcion = "Permite consultar la información de los pacientes." },
                new ServicioHospitalario { Id = 3, Nombre = "Medicamentos", Descripcion = "Permite consultar medicamentos e indicaciones." }
            });
            return View(modelo);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
