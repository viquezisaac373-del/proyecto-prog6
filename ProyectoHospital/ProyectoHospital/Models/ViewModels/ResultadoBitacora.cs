namespace ProyectoHospital.Models.ViewModels
{
    public class ResultadoBitacora
    {
        public List<RegistroBitacora> Registros { get; set; } = new();
        public int Total { get; set; }
    }
}
