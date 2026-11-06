using Microsoft.AspNetCore.Mvc;
public class MedicamentosController : Controller
{
    public IActionResult Medicamentos()
    {
        return View();
    }
}