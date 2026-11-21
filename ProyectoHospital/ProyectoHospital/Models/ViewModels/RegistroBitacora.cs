namespace ProyectoHospital.Models.ViewModels
{
    public class RegistroBitacora
    {
        public long Id { get; set; }
        public DateTime FechaHora { get; set; }
        public string Accion { get; set; } = string.Empty;
        public int? UsuarioId { get; set; }
        public string? Usuario { get; set; }
        public string Resultado { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string? Ip { get; set; }
        public string? ValoresAnteriores { get; set; }
        public string? ValoresNuevos { get; set; }
        public string? Tabla { get; set; }
        public string? RegistroAfectado { get; set; }
        public string? Objeto { get; set; }
        public int? NumeroError { get; set; }
        public int? Severidad { get; set; }
        public int? Linea { get; set; }
        public int? PacienteId { get; set; }
        public string? Paciente { get; set; }
        public int? CitaId { get; set; }
    }
}
