using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models.ViewModels;

public sealed class RolUsuarioOpcion
{
    public int RolId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed class EmpleadoSinCuenta
{
    public int EmpleadoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
}

public sealed class UsuarioResumen
{
    public int UsuarioId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int? EmpleadoId { get; set; }
    public string? Empleado { get; set; }
    public string? Cliente { get; set; }
    public string? Puesto { get; set; }
    public string Roles { get; set; } = string.Empty;
}

public sealed class UsuariosIndexViewModel
{
    public List<UsuarioResumen> Usuarios { get; set; } = new();
    public int CantidadSinCuenta { get; set; }
}

public sealed class CrearUsuarioViewModel
{
    [Required(ErrorMessage = "Indique el nombre de usuario.")]
    [RegularExpression(@"^[A-Za-z0-9._-]{4,50}$", ErrorMessage = "Use de 4 a 50 caracteres: letras, números, punto, guion o guion bajo.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese una contraseña.")]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "La contraseña debe contener entre 12 y 128 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Clave { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contraseña.")]
    [Compare(nameof(Clave), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmacionClave { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un rol.")]
    [Display(Name = "Rol")]
    public int RolId { get; set; }

    [Required(ErrorMessage = "Indique la identificación del empleado.")]
    [RegularExpression(@"^[0-9]{9,30}$", ErrorMessage = "Ingrese de 9 a 30 dígitos, sin guiones.")]
    [Display(Name = "Identificación")]
    public string NumeroIdentificacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique el nombre.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique los apellidos.")]
    [StringLength(150)]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique el teléfono.")]
    [RegularExpression(@"^[0-9]{8,20}$", ErrorMessage = "Ingrese de 8 a 20 dígitos, sin espacios ni signo más.")]
    public string Telefono { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo no tiene una estructura válida.")]
    [StringLength(150)]
    public string? Correo { get; set; }

    [Required(ErrorMessage = "Indique el puesto.")]
    [StringLength(80)]
    public string Puesto { get; set; } = string.Empty;

    [Display(Name = "¿Es médico?")]
    public bool EsMedico { get; set; }

    [StringLength(100)]
    public string? Especialidad { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Hora de entrada")]
    public TimeSpan? HoraEntrada { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Hora de salida")]
    public TimeSpan? HoraSalida { get; set; }

    [Range(1, 240, ErrorMessage = "La duración debe estar entre 1 y 240 minutos.")]
    [Display(Name = "Duración de cita (minutos)")]
    public short? DuracionCitaMin { get; set; }

    public List<RolUsuarioOpcion> Roles { get; set; } = new();
}

public sealed class VincularUsuarioViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un empleado.")]
    [Display(Name = "Empleado existente")]
    public int EmpleadoId { get; set; }

    [Required(ErrorMessage = "Indique el nombre de usuario.")]
    [RegularExpression(@"^[A-Za-z0-9._-]{4,50}$", ErrorMessage = "Use de 4 a 50 caracteres: letras, números, punto, guion o guion bajo.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese una contraseña.")]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "La contraseña debe contener entre 12 y 128 caracteres.")]
    [DataType(DataType.Password)]
    public string Clave { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contraseña.")]
    [Compare(nameof(Clave), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmacionClave { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un rol.")]
    public int RolId { get; set; }

    public List<RolUsuarioOpcion> Roles { get; set; } = new();
    public List<EmpleadoSinCuenta> Empleados { get; set; } = new();
}
