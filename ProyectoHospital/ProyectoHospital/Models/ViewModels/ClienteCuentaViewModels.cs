using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models.ViewModels;

public sealed class RegistroClienteViewModel
{
    [Required(ErrorMessage = "Seleccione el tipo de documento.")]
    [RegularExpression("Cedula|DIMEX", ErrorMessage = "Tipo de documento inválido.")]
    [Display(Name = "Tipo de identificación")]
    public string TipoIdentificacion { get; set; } = "Cedula";

    [Required(ErrorMessage = "Indique su identificación.")]
    [StringLength(12)]
    [RegularExpression("^[0-9]+$", ErrorMessage = "La identificación debe contener solamente números.")]
    [Display(Name = "Número de identificación")]
    public string NumeroIdentificacion { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(130)]
    public string Apellidos { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[0-9]{8,20}$", ErrorMessage = "Indique de 8 a 20 dígitos.")]
    public string Telefono { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[A-Za-z0-9._-]{4,50}$", ErrorMessage = "Utilice 4 a 50 caracteres: letras, números, punto, guion o guion bajo.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "La contraseña debe tener al menos 12 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Clave { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Clave), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmacionClave { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar el tratamiento de datos indicado.")]
    public bool AceptaTratamiento { get; set; }
}

public sealed class ClienteDisponible
{
    public int ClienteId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
}

public sealed class VincularClienteViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un cliente.")]
    public int ClienteId { get; set; }

    [Required]
    [RegularExpression("^[A-Za-z0-9._-]{4,50}$", ErrorMessage = "Utilice 4 a 50 caracteres válidos.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 12)]
    [DataType(DataType.Password)]
    public string Clave { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Clave), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmacionClave { get; set; } = string.Empty;

    public List<ClienteDisponible> Clientes { get; set; } = new();
}

public sealed class ClienteCuentaResumen
{
    public int ClienteId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
}

public sealed class ProductoCatalogo
{
    public int InventarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal PrecioVenta { get; set; }
}
