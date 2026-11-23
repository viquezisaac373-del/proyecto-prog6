using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models.ViewModels;

public sealed class OpcionPacienteReposo
{
    public int PacienteId { get; set; }
    public string Etiqueta { get; set; } = "";
}

public sealed class CrearRecomendacionReposoViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un paciente activo.")]
    [Display(Name = "Paciente")]
    public int PacienteId { get; set; }

    [Required(ErrorMessage = "Indique la fecha de inicio.")]
    [DataType(DataType.Date)]
    [Display(Name = "Inicio del reposo")]
    public DateTime? FechaInicio { get; set; }

    [Required(ErrorMessage = "Indique la fecha final.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fin del reposo")]
    public DateTime? FechaFin { get; set; }

    [Required(ErrorMessage = "Describa la razón clínica.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "La justificación debe tener entre 10 y 500 caracteres.")]
    [Display(Name = "Justificación clínica")]
    public string Justificacion { get; set; } = "";
}

public sealed class RecomendacionReposoResumen
{
    public int RecomendacionId { get; set; }
    public int PacienteId { get; set; }
    public string Paciente { get; set; } = "";
    public string? Identificacion { get; set; }
    public string Medico { get; set; } = "";
    public int EmpleadoId { get; set; }
    public string? Especialidad { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string Justificacion { get; set; } = "";
    public int DiasCalendario => (FechaFin - FechaInicio).Days + 1;
}

public sealed class RecomendacionesReposoIndexViewModel
{
    public int MedicoEmpleadoId { get; set; }
    public List<OpcionPacienteReposo> Pacientes { get; set; } = new();
    public List<RecomendacionReposoResumen> Registros { get; set; } = new();
    public CrearRecomendacionReposoViewModel Nuevo { get; set; } = new()
    {
        FechaInicio = DateTime.Today,
        FechaFin = DateTime.Today
    };
}
