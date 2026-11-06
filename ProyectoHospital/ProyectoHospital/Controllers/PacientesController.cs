using Microsoft.AspNetCore.Mvc;
public class PacientesController : Controller
{
    public IActionResult Pacientes()
    {
        return View();
    }
}