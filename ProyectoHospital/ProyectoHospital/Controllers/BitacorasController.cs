using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoHospital.Models.Enums;
using ProyectoHospital.Models.ViewModels;
using ProyectoHospital.Repositories;

namespace ProyectoHospital.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class BitacorasController : Controller
    {
        private const int RegistrosPorPagina = 50;

        private readonly BitacoraRepository _bitacoras;
        private readonly ILogger<BitacorasController> _logger;

        public BitacorasController(BitacoraRepository bitacoras, ILogger<BitacorasController> logger)
        {
            _bitacoras = bitacoras;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            TipoBitacora tipo = TipoBitacora.Accesos,
            DateTime? desde = null,
            DateTime? hasta = null,
            string? usuario = null,
            string? accion = null,
            string? orden = null,
            string? dir = null,
            int pagina = 1)
        {
            if (!Enum.IsDefined(tipo))
                tipo = TipoBitacora.Accesos;

            var modelo = new BitacoraIndexViewModel
            {
                Tipo = tipo,
                Filtro = new FiltroBitacora
                {
                    Desde = desde?.Date,
                    Hasta = hasta?.Date,
                    Usuario = usuario?.Trim(),
                    Accion = accion?.Trim(),
                    Pagina = Math.Max(pagina, 1),
                    TamanoPagina = RegistrosPorPagina,
                    Orden = BitacoraRepository.EsOrdenValido(orden) ? orden!.ToLowerInvariant() : "fecha",
                    Descendente = !string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase)
                }
            };

            try
            {
                modelo.Acciones = await _bitacoras.ObtenerAccionesAsync(tipo);

                if (modelo.Filtro.Desde > modelo.Filtro.Hasta)
                {
                    modelo.AvisoFiltro = "La fecha inicial no puede ser posterior a la fecha final.";
                    return View(modelo);
                }

                if (modelo.Filtro.Usuario?.Length > 100)
                {
                    modelo.AvisoFiltro = "El texto de búsqueda no puede superar 100 caracteres.";
                    return View(modelo);
                }

                var resultado = await _bitacoras.ConsultarAsync(tipo, modelo.Filtro);

                if (resultado.Registros.Count == 0 && modelo.Filtro.Pagina > 1)
                {
                    modelo.Filtro.Pagina = 1;
                    resultado = await _bitacoras.ConsultarAsync(tipo, modelo.Filtro);
                }

                modelo.Registros = resultado.Registros;
                modelo.Total = resultado.Total;
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
