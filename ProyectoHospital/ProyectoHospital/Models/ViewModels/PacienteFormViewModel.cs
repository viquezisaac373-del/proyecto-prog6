
using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models.ViewModels
{
    // HU09 - T03
    // Representa los datos del formulario de registro.
    // Incluye las validaciones antes de enviar los datos a SQL Server.
    public class PacienteFormViewModel : IValidatableObject
    {
        // IDENTIFICACIÓN
        // Regla provisional: entre 9 y 25 dígitos.
        // Rechaza identificaciones formadas únicamente por ceros.
        // Los documentos extranjeros requieren una revisión posterior.
        [Required(ErrorMessage = "La identificación es obligatoria.")]
        [StringLength(25)]
        [RegularExpression(
            @"^(?!0+$)[0-9]{9,25}$",
            ErrorMessage = "Ingrese entre 9 y 25 dígitos. No se permiten únicamente ceros.")]
        public string NumeroIdentificacion { get; set; } = "";

        // NOMBRE
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
        public string Nombre { get; set; } = "";

        // APELLIDOS
        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(130, ErrorMessage = "Los apellidos no pueden superar 130 caracteres.")]
        public string Apellidos { get; set; } = "";

        // FECHA DE NACIMIENTO
        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }

        // GÉNERO
        [Required(ErrorMessage = "Seleccione un género.")]
        [RegularExpression(
            @"^(Masculino|Femenino|Otro)$",
            ErrorMessage = "Seleccione una opción válida.")]
        public string Genero { get; set; } = "";

        // TIPO DE SANGRE
        [Required(ErrorMessage = "Seleccione un tipo de sangre.")]
        [RegularExpression(
            @"^(A\+|A-|B\+|B-|AB\+|AB-|O\+|O-|ND)$",
            ErrorMessage = "Seleccione un tipo de sangre válido.")]
        public string TipoSangre { get; set; } = "";

        // TELÉFONO
        // Acepta ocho dígitos nacionales o formato internacional
        // con +, por ejemplo: +506 85102283.
        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [StringLength(20)]
        [RegularExpression(
            @"^(?:[2-9][0-9]{7}|\+[1-9][0-9]{7,14}|\+[1-9][0-9]{0,2} [0-9]{7,12})$",
            ErrorMessage = "Ingrese 8 dígitos nacionales o un número internacional, por ejemplo +506 85102283.")]
        public string NumeroTelefono { get; set; } = "";

        // CORREO ELECTRÓNICO
        // Comprueba un formato básico y una terminación de dominio
        // de al menos dos letras, sin exigir que termine en .com.
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [StringLength(200)]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        [RegularExpression(
            @"^[^@\s]+@(?:[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z]{2,}$",
            ErrorMessage = "Ingrese un dominio de correo completo.")]
        public string Correo { get; set; } = "";

        // DIRECCIÓN
        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(250, ErrorMessage = "La dirección no puede superar 250 caracteres.")]
        public string Direccion { get; set; } = "";

        // CONTACTO DE EMERGENCIA
        [Required(ErrorMessage = "El contacto de emergencia es obligatorio.")]
        [StringLength(100)]
        public string ContactoEmergencia { get; set; } = "";

        // TELÉFONO DE EMERGENCIA
        // Aplicamos las mismas reglas que al teléfono principal.
        [Required(ErrorMessage = "El teléfono de emergencia es obligatorio.")]
        [StringLength(20)]
        [RegularExpression(
            @"^(?:[2-9][0-9]{7}|\+[1-9][0-9]{7,14}|\+[1-9][0-9]{0,2} [0-9]{7,12})$",
            ErrorMessage = "Ingrese un teléfono nacional válido o uno con código internacional.")]
        public string TelefonoEmergencia { get; set; } = "";

        // VALIDACIONES ADICIONALES
        // IValidatableObject permite comprobar reglas que
        // no dependen únicamente de un atributo.
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            // Impedir fechas de nacimiento posteriores al día actual.
            if (FechaNacimiento.HasValue &&
                FechaNacimiento.Value.Date > DateTime.Today)
            {
                yield return new ValidationResult(
                    "La fecha de nacimiento no puede ser futura.",
                    new[] { nameof(FechaNacimiento) });
            }
        }
    }
}
