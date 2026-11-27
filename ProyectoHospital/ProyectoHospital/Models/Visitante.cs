using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models;

public class Visitante
{
    public int VisitanteId { get; set; }

    // =============================================
    // VALIDACIÓN DE IDENTIFICACIÓN
    // =============================================

    [Required(ErrorMessage =
        "La identificación es obligatoria.")]

    [StringLength(9, MinimumLength = 9,
        ErrorMessage =
        "La identificación debe contener exactamente 9 dígitos.")]

    [RegularExpression(@"^[1-9][0-9]{8}$",
        ErrorMessage =
        "Ingrese una cédula costarricense válida de 9 dígitos.")]

    [Display(Name = "Número de identificación")]
    public string NumeroIdentificacion { get; set; }
        = string.Empty;


    // =============================================
    // VALIDACIÓN DEL NOMBRE
    // =============================================

    [Required(ErrorMessage =
        "El nombre es obligatorio.")]

    [StringLength(100, ErrorMessage =
        "El nombre no puede superar los 100 caracteres.")]

    [RegularExpression(@"^\p{L}+(?: +\p{L}+)*$",
        ErrorMessage =
        "El nombre solo puede contener letras y espacios.")]

    public string Nombre { get; set; }
        = string.Empty;


    // =============================================
    // VALIDACIÓN DE APELLIDOS
    // =============================================

    [Required(ErrorMessage =
        "Los apellidos son obligatorios.")]

    [StringLength(200, ErrorMessage =
        "Los apellidos no pueden superar los 200 caracteres.")]

    [RegularExpression(@"^\p{L}+(?: +\p{L}+)*$",
        ErrorMessage =
        "Los apellidos solo pueden contener letras y espacios.")]

    public string Apellidos { get; set; }
        = string.Empty;


    // =============================================
    // VALIDACIÓN DEL TELÉFONO
    // =============================================

    [Required(ErrorMessage =
        "El teléfono es obligatorio.")]

    [StringLength(20, ErrorMessage =
        "El teléfono no puede superar los 20 caracteres.")]

    [RegularExpression(
    @"^[0-9]+$",
    ErrorMessage =
    "Ingrese un número de teléfono válido.")]

    [Display(Name = "Teléfono")]
    public string Telefono { get; set; }
        = string.Empty;
}