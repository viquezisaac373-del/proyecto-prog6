using System.ServiceModel;

namespace ProyectoHospital.Services;

/// <summary>Contrato espejo del servicio SOAP. Nombres y namespace deben coincidir con CoreWCF.</summary>
[ServiceContract(Name = "IHorariosMedicosService", Namespace = "urn:hospinomenal:horarios")]
public interface IHorariosMedicosSoap
{
    [OperationContract]
    Task<string> ConsultarHorarios(int medicoId);
}

/// <summary>
/// Cliente SOAP de uso local. No habilitar HTTP hacia servidores remotos.
/// El login MVC y la base de datos de la aplicación NO se modifican.
/// </summary>
public sealed class HorariosMedicosWcfClient
{
    private readonly IConfiguration _config;

    public HorariosMedicosWcfClient(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> ConsultarAsync(int medicoId)
    {
        if (medicoId <= 0)
            throw new ArgumentOutOfRangeException(nameof(medicoId));

        string url = _config["Wcf:HorariosEndpoint"]
            ?? throw new InvalidOperationException("Falta configurar Wcf:HorariosEndpoint.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("El endpoint SOAP configurado no es una URL HTTP(S) válida.");

        if (uri.Scheme == Uri.UriSchemeHttp && !uri.IsLoopback)
            throw new InvalidOperationException("No se admite HTTP sin cifrar fuera del equipo local.");

        var seguridad = uri.Scheme == Uri.UriSchemeHttps
            ? BasicHttpSecurityMode.Transport
            : BasicHttpSecurityMode.None;

        var binding = new BasicHttpBinding(seguridad)
        {
            OpenTimeout = TimeSpan.FromSeconds(5),
            SendTimeout = TimeSpan.FromSeconds(10),
            ReceiveTimeout = TimeSpan.FromSeconds(10),
            MaxReceivedMessageSize = 32 * 1024
        };

        var factory = new ChannelFactory<IHorariosMedicosSoap>(binding, new EndpointAddress(uri));
        IHorariosMedicosSoap? proxy = null;

        try
        {
            proxy = factory.CreateChannel();
            return await proxy.ConsultarHorarios(medicoId);
        }
        finally
        {
            // Liberar recursos incluso cuando la llamada falla por timeout.
            (proxy as IClientChannel)?.Abort();
            factory.Abort();
        }
    }
}
