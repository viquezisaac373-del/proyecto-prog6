using CoreWCF;

namespace Hospinomenal.WcfService.Services;

[ServiceContract(Name = "IHorariosMedicosService", Namespace = "urn:hospinomenal:horarios")]
public interface IHorariosMedicosService
{
    [OperationContract]
    Task<string> ConsultarHorarios(int medicoId);
}
