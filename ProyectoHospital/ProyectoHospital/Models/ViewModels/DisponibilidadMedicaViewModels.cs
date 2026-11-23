using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models.ViewModels;

public sealed class MedicoHorarioOpcion
{
    public int EmpleadoId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Especialidad { get; set; }
    public short DuracionCitaMin { get; set; }
}

public sealed class BloqueDisponibilidadMedica
{
    public int BloqueId { get; set; }
    public int EmpleadoId { get; set; }
    public string Medico { get; set; } = "";
    public string? Especialidad { get; set; }
    public int DiaSemana { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public short DuracionCitaMin { get; set; }
    public int Minutos { get; set; }

    // Capacidad teórica: no descuenta citas reservadas, feriados ni ausencias.
    public int CuposTeoricos => DuracionCitaMin > 0 ? Minutos / DuracionCitaMin : 0;
    public string DiaTexto => DiaSemana switch
    {
        1 => "Lunes", 2 => "Martes", 3 => "Miércoles",
        4 => "Jueves", 5 => "Viernes", 6 => "Sábado",
        7 => "Domingo", _ => "Desconocido"
    };
}

public sealed class CrearBloqueDisponibilidadViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un médico activo.")]
    [Display(Name = "Médico")]
    public int EmpleadoId { get; set; }

    [Range(1, 7, ErrorMessage = "Seleccione un día de la semana.")]
    [Display(Name = "Día")]
    public int DiaSemana { get; set; }

    [Required(ErrorMessage = "Indique la hora de inicio.")]
    [DataType(DataType.Time)]
    [Display(Name = "Desde")]
    public TimeSpan? HoraInicio { get; set; }

    [Required(ErrorMessage = "Indique la hora de finalización.")]
    [DataType(DataType.Time)]
    [Display(Name = "Hasta")]
    public TimeSpan? HoraFin { get; set; }
}

public sealed class DisponibilidadMedicaIndexViewModel
{
    public List<MedicoHorarioOpcion> Medicos { get; set; } = new();
    public List<BloqueDisponibilidadMedica> Bloques { get; set; } = new();
    public CrearBloqueDisponibilidadViewModel Nuevo { get; set; } = new();
    public int? MedicoFiltro { get; set; }
    public string? EspecialidadFiltro { get; set; }
    public IEnumerable<string> Especialidades => Medicos.Select(m => m.Especialidad)
        .Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e!)
        .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(e => e);
    public int TotalMinutos => Bloques.Sum(b => b.Minutos);
    public int TotalCuposTeoricos => Bloques.Sum(b => b.CuposTeoricos);
}
