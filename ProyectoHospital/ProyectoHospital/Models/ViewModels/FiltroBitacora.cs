namespace ProyectoHospital.Models.ViewModels
{
    public class FiltroBitacora
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string? Usuario { get; set; }
        public string? Accion { get; set; }
        public int Maximo { get; set; } = 500;
    }
}
