using Microsoft.AspNetCore.Mvc;
using ProyectoHospital.Models.Enums;
using ProyectoHospital.Models.ViewModels;
using ProyectoHospital.Repositories;

namespace ProyectoHospital.Controllers
{
    public class BitacorasController : Controller
    {
        private readonly BitacoraRepository _bitacoras;
        private readonly ILogger<BitacorasController> _logger;

        public BitacorasController(BitacoraRepository bitacoras, ILogger<BitacorasController> logger)
        {
            _bitacoras = bitacoras;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(TipoBitacora tipo = TipoBitacora.Accesos)
        {
            if (!Enum.IsDefined(tipo))
                tipo = TipoBitacora.Accesos;

            var modelo = new BitacoraIndexViewModel { Tipo = tipo };

            try
            {
                modelo.Registros = await _bitacoras.ConsultarAsync(tipo, modelo.Filtro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar la bitácora {Tipo}.", tipo);
                modelo.Error = "No fue posible consultar la bitácora. Intente de nuevo o contacte al administrador.";
            }

            return View(modelo);
        }
    }
}
