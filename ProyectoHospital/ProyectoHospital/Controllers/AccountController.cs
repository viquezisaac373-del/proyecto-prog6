using Microsoft.AspNetCore.Mvc;
using ProyectoHospital.Models.ViewModels;

namespace ProyectoHospital.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            ViewData["Info"] = "Formulario válido. La validación de credenciales se implementa en la siguiente tarea.";
            model.Password = string.Empty;
            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
