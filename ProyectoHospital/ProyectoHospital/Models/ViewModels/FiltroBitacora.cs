namespace ProyectoHospital.Models.ViewModels
{
    public class FiltroBitacora
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string? Usuario { get; set; }
        public string? Accion { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 50;
        public string? Orden { get; set; }
        public bool Descendente { get; set; } = true;
    }
}
