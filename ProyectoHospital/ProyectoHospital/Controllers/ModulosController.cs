using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoHospital.Models.ViewModels;

namespace ProyectoHospital.Controllers
{
    [Authorize]
    public class ModulosController : Controller
    {
        public IActionResult Index()
        {
            var modelo = new HomeViewModel();
            modelo.Servicios.AddRange(new[]
            {
                new ServicioHospitalario
                {
                    Id = 1, Nombre = "Citas médicas",
                    Descripcion = "Sección de citas del sistema. Mantenimiento pendiente de integración.",
                    TipoServicio = TipoServicioHospitalario.Citas
                },
                new ServicioHospitalario
                {
                    Id = 2, Nombre = "Pacientes",
                    Descripcion = "Consultar, registrar, editar y desactivar pacientes.",
                    TipoServicio = TipoServicioHospitalario.Pacientes
                },
                new ServicioHospitalario
                {
                    Id = 3, Nombre = "Medicamentos",
                    Descripcion = "Sección de medicamentos. Mantenimiento pendiente de integración.",
                    TipoServicio = TipoServicioHospitalario.Medicamentos
                }
            });
            return View(modelo);
        }
    }
}
