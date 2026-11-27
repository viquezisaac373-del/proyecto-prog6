using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models;

public class RegistroVisita
{
    public int RegistroVisitaId { get; set; }

    public int VisitanteId { get; set; }

    [Display(Name = "Paciente que visita")]
    public int? PacienteId { get; set; }

    [Required(ErrorMessage = "Ingrese el motivo de la visita.")]
    [StringLength(300)]
    [Display(Name = "Motivo de visita")]
    public string Motivo_Visita { get; set; } = string.Empty;

    [Display(Name = "Fecha y hora de entrada")]
    public DateTime FechaHoraEntrada { get; set; }

    [Display(Name = "Fecha y hora de salida")]
    public DateTime? FechaHoraSalida { get; set; }

    public int? UsuarioEntradaId { get; set; }

    public int? UsuarioSalidaId { get; set; }

    // Estado calculado de la visita.
    public string Estado => FechaHoraSalida == null
        ? "En visita"
        : "Finalizada";
}