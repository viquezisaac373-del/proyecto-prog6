namespace ProyectoHospital.Models.ViewModels;

// Servicio visible en la portada; aún no depende de la base de datos.
public class ServicioHospitalario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
}
