namespace HospitalWeb.Models
{

    // Representa un servicio qu ofrece al hospital (cita, pacientes, medicamentos).
    //Es una clase simple de datos: solo guarda información, no tiene lógica.
    public class ServicioHospitalario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Descripcion { get; set; } = "";
}
}   