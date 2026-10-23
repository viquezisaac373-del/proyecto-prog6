using HospitalWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace HospitalWeb.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            //1. Se crea el ViewModel que viajará a la vista
            HomeViewModel modelo = new HomeViewModel();

            //2. Se agregan los servicios (por ahora fijos; luego vendrán de la base de datos)
            modelo.Servicios.Add(new ServicioHospitalario
            {
                Id = 1,
                Nombre = "Citas médicas",
                Descripcion = "Permite consultar y programar citas."
            });

            // Pacientes y Medicamentos se agregan de manera similar
            modelo.Servicios.Add(new ServicioHospitalario
            {
                Id = 2,
                Nombre = "Pacientes",
                Descripcion = "Permite consultar la información de los pacientes."
            });

            modelo.Servicios.Add(new ServicioHospitalario
            {
                Id = 3,
                Nombre = "Medicamentos",
                Descripcion = "Permite consultar medicamentos e indicaciones."
            });
            //3. View (modelo) buscA Views/Home/Index.cshtml y le entrega el modelo
            return View(modelo);
        }
    }
}