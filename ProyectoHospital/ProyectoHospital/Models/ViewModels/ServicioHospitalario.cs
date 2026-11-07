namespace ProyectoHospital.Models.ViewModels;

// Representa una tarjeta de servicio en la página principal.
public class ServicioHospitalario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public TipoServicioHospitalario TipoServicio { get; set; }
}
