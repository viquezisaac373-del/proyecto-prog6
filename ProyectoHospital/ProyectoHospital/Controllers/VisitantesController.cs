using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Text.RegularExpressions;
using ProyectoHospital.Models;
using ProyectoHospital.Services;
using System.Data;

namespace ProyectoHospital.Controllers;

[Authorize(Roles = "Administrador")]
public class VisitantesController : Controller
{
    private readonly VisitantesService _visitantes;
    private readonly ILogger<VisitantesController> _logger;

    public VisitantesController(
        VisitantesService visitantes,
        ILogger<VisitantesController> logger)
    {
        _visitantes = visitantes;
        _logger = logger;
    }

    // =============================================
    // 1. CONSULTAR VISITANTES
    // =============================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? buscar,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        string? estado)
    {
        ViewBag.Buscar = buscar;
        ViewBag.FechaDesde = fechaDesde;
        ViewBag.FechaHasta = fechaHasta;
        ViewBag.Estado = estado;

        try
        {
            var registros =
                await _visitantes.ConsultarVisitantesAsync(
                    buscar,
                    fechaDesde,
                    fechaHasta,
                    estado);

            return View(registros);
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex, "Error al consultar visitantes.");

            TempData["Error"] =
                "No se pudieron consultar las visitas.";

            return View(new DataTable());
        }
    }

    // =============================================
    // 2. MOSTRAR FORMULARIO DE REGISTRO
    // =============================================

    [HttpGet]
    public async Task<IActionResult> Registrar()
    {
        await CargarPacientesAsync();

        return View();
    }

    // =============================================
    // 3. REGISTRAR ENTRADA
    // =============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarEntrada(
        Visitante visitante,
        RegistroVisita visita)
    {
        if (!ObtenerUsuarioId(out int usuarioId))
            return Forbid();

        if (!ModelState.IsValid)
        {
            await CargarPacientesAsync();

            return View("Registrar");
        }

        try
        {
            // Buscar si la persona ya está registrada.

            var visitanteExistente =
                await _visitantes.BuscarVisitanteAsync(
                    visitante.NumeroIdentificacion);

            // =============================================
            // VALIDAR DATOS PERSONALES DEL VISITANTE
            // =============================================

            if (visitanteExistente != null)
            {
                bool nombreDiferente = !string.Equals(
                    visitanteExistente.Nombre.Trim(),
                    visitante.Nombre.Trim(),
                    StringComparison.OrdinalIgnoreCase);

                bool apellidosDiferentes = !string.Equals(
                    visitanteExistente.Apellidos.Trim(),
                    visitante.Apellidos.Trim(),
                    StringComparison.OrdinalIgnoreCase);

                string telefonoRegistrado =
                    visitanteExistente.Telefono.Trim();

                string telefonoIngresado =
                    visitante.Telefono.Trim();

                // Normalizar teléfonos nacionales anteriores.

                if (telefonoRegistrado.Length == 8)
                    telefonoRegistrado = "506" + telefonoRegistrado;

                if (telefonoIngresado.Length == 8)
                    telefonoIngresado = "506" + telefonoIngresado;

                bool telefonoDiferente =
                    telefonoRegistrado != telefonoIngresado;

                // Rechazar datos diferentes a los originales.

                if (nombreDiferente ||
                    apellidosDiferentes ||
                    telefonoDiferente)
                {
                    ModelState.AddModelError(
                        "visitante.NumeroIdentificacion",
                        "Esta identificación ya está registrada con otros " +
                        "datos personales. Presione Buscar para recuperar " +
                        "la información correcta.");

                    await CargarPacientesAsync();

                    return View("Registrar");
                }
            }

            // Comprobar si tiene una visita activa.

            if (visitanteExistente != null &&
                await _visitantes.TieneVisitaActivaAsync(
                    visitanteExistente.VisitanteId))
            {
                ModelState.AddModelError(
                    "visitante.NumeroIdentificacion",
                    "El visitante ya tiene una visita activa. " +
                    "Debe registrar su salida antes de ingresar nuevamente.");

                await CargarPacientesAsync();

                return View("Registrar");
            }

            // Registrar la entrada utilizando
            // el procedimiento almacenado existente.

            await _visitantes.RegistrarEntradaAsync(
                visitante,
                visita,
                usuarioId);

            TempData["Exito"] =
                "Entrada registrada correctamente.";

            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex, "Error al registrar entrada de visitante.");

            // Mostrar mensajes comprensibles para el usuario.

            switch (ex.Number)
            {
                // Campos obligatorios incompletos.

                case 50001:

                    ModelState.AddModelError(
                        string.Empty,
                        "Complete los campos obligatorios.");

                    break;

                // Paciente inexistente.

                case 50002:

                    ModelState.AddModelError(
                        "visita.PacienteId",
                        "El paciente seleccionado no existe.");

                    break;

                // Visitante con una visita activa.

                case 50003:

                    ModelState.AddModelError(
                        "visitante.NumeroIdentificacion",
                        "El visitante ya tiene una visita activa. " +
                        "Debe registrar su salida primero.");

                    break;

                // NUEVO: Datos personales diferentes
                // rechazados desde SQL Server.

                case 50006:

                    ModelState.AddModelError(
                        "visitante.NumeroIdentificacion",
                        "Esta identificación ya está registrada con otros " +
                        "datos personales. Presione Buscar para recuperar " +
                        "la información correcta.");

                    break;

                // Protección del índice único de visitas activas.

                case 2601 when ex.Message.Contains(
                    "UX_RegistroVisitas_VisitaActiva"):

                    ModelState.AddModelError(
                        "visitante.NumeroIdentificacion",
                        "El visitante ya tiene una visita activa.");

                    break;

                // Otros errores no contemplados.

                default:

                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo registrar la entrada. " +
                        "Intente nuevamente o contacte al administrador.");

                    break;
            }

            // Conservar los datos ingresados y la lista
            // de pacientes cuando exista un error.

            await CargarPacientesAsync();

            return View("Registrar");
        }
    }

    // =============================================
    // 4. REGISTRAR SALIDA
    // =============================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarSalida(
        int registroVisitaId)
    {
        if (!ObtenerUsuarioId(out int usuarioId))
            return Forbid();

        try
        {
            await _visitantes.RegistrarSalidaAsync(
                registroVisitaId,
                usuarioId);

            TempData["Exito"] =
                "Salida registrada correctamente.";
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex, "Error al registrar salida de visitante.");

            TempData["Error"] = ex.Number switch
            {
                50004 => "El registro de visita no existe.",

                50005 => "La salida ya fue registrada.",

                _ => "No se pudo registrar la salida."
            };
        }

        return RedirectToAction(nameof(Index));
    }

    // =============================================
    // 5. BUSCAR VISITANTE POR IDENTIFICACIÓN
    // =============================================

    [HttpGet]
    [ResponseCache(
        NoStore = true,
        Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> BuscarVisitante(
        string identificacion)
    {
        // Validar que la identificación contenga
        // exactamente nueve dígitos.

        if (string.IsNullOrWhiteSpace(identificacion) ||
            !Regex.IsMatch(
                identificacion, @"\A[1-9][0-9]{8}\z"))
        {
            return BadRequest(new
            {
                mensaje =
                    "Ingrese una cédula válida de 9 dígitos."
            });
        }

        try
        {
            var visitante =
                await _visitantes.BuscarVisitanteAsync(
                    identificacion);

            // La persona todavía no está registrada.

            if (visitante == null)
            {
                return Json(new
                {
                    encontrado = false,
                    visitaActiva = false
                });
            }

            // Consultar si tiene una visita activa.

            bool visitaActiva =
                await _visitantes.TieneVisitaActivaAsync(
                    visitante.VisitanteId);

            // Devolver sus datos personales
            // y el estado de su visita.

            return Json(new
            {
                encontrado = true,

                nombre = visitante.Nombre,

                apellidos = visitante.Apellidos,

                telefono = visitante.Telefono,

                visitaActiva = visitaActiva,

                mensaje = visitaActiva
                    ? "El visitante ya tiene una visita activa. " +
                      "Debe registrar su salida primero."
                    : "Visitante registrado. Puede realizar una nueva entrada."
            });
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex, "Error al buscar visitante.");

            return StatusCode(500, new
            {
                mensaje =
                    "No se pudo consultar el visitante."
            });
        }
    }

    // =============================================
    // 6. OBTENER USUARIO AUTENTICADO
    // =============================================

    private bool ObtenerUsuarioId(out int usuarioId)
    {
        var identificador =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(
            identificador, out usuarioId)
            && usuarioId > 0;
    }

    // =============================================
    // 7. CARGAR LISTA DE PACIENTES ACTIVOS
    // =============================================

    private async Task CargarPacientesAsync()
    {
        var tabla = await _visitantes
            .ListarPacientesActivosAsync();

        var pacientes = new List<SelectListItem>();

        foreach (DataRow fila in tabla.Rows)
        {
            string identificacion =
                fila["Numero_Identificacion"] == DBNull.Value
                    ? "Sin identificación"
                    : fila["Numero_Identificacion"].ToString()!;

            pacientes.Add(new SelectListItem
            {
                Value = fila["PacienteId"].ToString(),

                Text = $"{fila["Nombre"]} " +
                       $"{fila["Apellidos"]} " +
                       $"({identificacion})"
            });
        }

        ViewBag.Pacientes = pacientes;
    }
}