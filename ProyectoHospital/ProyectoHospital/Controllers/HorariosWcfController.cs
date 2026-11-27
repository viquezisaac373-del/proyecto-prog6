using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoHospital.Models.ViewModels;
using ProyectoHospital.Services;

namespace ProyectoHospital.Controllers;

[Authorize(Roles = "Administrador,Medico,Médico,Secretaria")]
public sealed class HorariosWcfController : Controller
{
    private readonly HorariosMedicosWcfClient _client;
    private readonly ILogger<HorariosWcfController> _logger;

    public HorariosWcfController(
        HorariosMedicosWcfClient client, ILogger<HorariosWcfController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? medicoId)
    {
        var model = new ConsultaHorarioWcfViewModel { MedicoId = medicoId };

        if (!medicoId.HasValue)
            return View(model);

        if (medicoId.Value <= 0)
        {
            model.Error = "Ingrese un identificador numérico de médico mayor que cero.";
            return View(model);
        }

        try
        {
            model.Resultado = await _client.ConsultarAsync(medicoId.Value);
        }
        catch (Exception ex) when (
            ex is System.ServiceModel.CommunicationException or
                  TimeoutException or InvalidOperationException)
        {
            _logger.LogError(ex, "Fallo en la consulta SOAP de horarios del médico {MedicoId}.", medicoId);
            model.Error = "No fue posible consultar el servicio de horarios. Compruebe que el servicio WCF esté ejecutándose.";
        }

        return View(model);
    }
}
