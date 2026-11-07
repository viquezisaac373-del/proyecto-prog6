
using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models.ViewModels
{
    // HU09 - T03 y T04
    // Modelo utilizado para registrar y editar pacientes.
    // Centraliza las validaciones del formulario MVC.
    public class PacienteFormViewModel : IValidatableObject
    {
        // IDENTIFICADOR DEL PACIENTE
        // 0 = Registro nuevo.
        // Mayor que 0 = Edición de un paciente existente.
        // Es un número entero, por lo que NO lleva StringLength.
        public int PacienteId { get; set; }

        // IDENTIFICACIÓN
        // Entre 9 y 25 dígitos, sin aceptar solamente ceros.
        // Regla provisional para documentos numéricos.
        [Required(ErrorMessage = "La identificación es obligatoria.")]
        [StringLength(25)]
        [RegularExpression(
            @"^(?!0+$)[0-9]{9,25}$",
            ErrorMessage = "Ingrese entre 9 y 25 dígitos. No se permiten únicamente ceros.")]
        public string NumeroIdentificacion { get; set; } = "";

        // NOMBRE
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(
            100,
            ErrorMessage = "El nombre no puede superar 100 caracteres.")]
        public string Nombre { get; set; } = "";

        // APELLIDOS
        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(
            130,
            ErrorMessage = "Los apellidos no pueden superar 130 caracteres.")]
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
        // ND significa No determinado.
        [Required(ErrorMessage = "Seleccione un tipo de sangre.")]
        [RegularExpression(
            @"^(A\+|A-|B\+|B-|AB\+|AB-|O\+|O-|ND)$",
            ErrorMessage = "Seleccione un tipo de sangre válido.")]
        public string TipoSangre { get; set; } = "";

        // TELÉFONO PRINCIPAL
        // Admite ocho dígitos nacionales o formato internacional.
        // Ejemplos: 85102283 y +50685102283.
        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [StringLength(20)]
        [RegularExpression(
            @"^(?:[2-9][0-9]{7}|\+[1-9][0-9]{7,14}|\+[1-9][0-9]{0,2} [0-9]{7,12})$",
            ErrorMessage = "Ingrese 8 dígitos nacionales o un número internacional válido.")]
        public string NumeroTelefono { get; set; } = "";

        // CORREO ELECTRÓNICO
        // Comprueba la estructura básica del correo.
        // Permite dominios como .com, .co, .cr, entre otros.
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [StringLength(200)]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        [RegularExpression(
            @"^[^@\s]+@(?:[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z]{2,}$",
            ErrorMessage = "Ingrese un dominio de correo completo.")]
        public string Correo { get; set; } = "";

        // DIRECCIÓN
        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(
            250,
            ErrorMessage = "La dirección no puede superar 250 caracteres.")]
        public string Direccion { get; set; } = "";

        // CONTACTO DE EMERGENCIA
        [Required(ErrorMessage = "El contacto de emergencia es obligatorio.")]
        [StringLength(
            100,
            ErrorMessage = "El contacto no puede superar 100 caracteres.")]
        public string ContactoEmergencia { get; set; } = "";

        // TELÉFONO DE EMERGENCIA
        // Utiliza las mismas reglas que el teléfono principal.
        [Required(ErrorMessage = "El teléfono de emergencia es obligatorio.")]
        [StringLength(20)]
        [RegularExpression(
            @"^(?:[2-9][0-9]{7}|\+[1-9][0-9]{7,14}|\+[1-9][0-9]{0,2} [0-9]{7,12})$",
            ErrorMessage = "Ingrese un teléfono nacional o internacional válido.")]
        public string TelefonoEmergencia { get; set; } = "";

        // VALIDACIONES ADICIONALES
        // IValidatableObject permite aplicar reglas que
        // requieren evaluar el valor de una propiedad.
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            // La fecha de nacimiento no puede ser posterior a hoy.
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
