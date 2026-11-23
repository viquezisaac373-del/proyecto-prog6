
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using PhoneNumbers;

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
        // Las ALTAS nuevas solo admiten cédula nacional (9 dígitos).
        // En edición conservamos las identificaciones históricas tal como están,
        // porque la edición ordinaria NO modifica este campo en SQL.
        [Required(ErrorMessage = "La identificación es obligatoria.")]
        [StringLength(25)]
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
            @"^(Masculino|Femenino|Otro|Indeterminado)$",
            ErrorMessage = "Seleccione una opción válida.")]
        public string Genero { get; set; } = "";

        // TIPO DE SANGRE
        // ND = No determinado (valor técnico provisional, NO es SQL NULL).
        // RHN = fenotipo Rh nulo, distinto de Rh negativo y de ND.
        // Debe utilizarse únicamente con tipificación de laboratorio confirmada.
        [Required(ErrorMessage = "Seleccione un tipo de sangre.")]
        [RegularExpression(
            @"^(A\+|A-|B\+|B-|AB\+|AB-|O\+|O-|ND|RHN)$",
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
            @"^[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z]{2,}$",
            ErrorMessage = "Ingrese un correo válido, sin puntos consecutivos y con dominio completo.")]
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
            if (PacienteId == 0 && !System.Text.RegularExpressions.Regex.IsMatch(
                    NumeroIdentificacion ?? "", @"^(?!0{9}$)[0-9]{9}$"))
            {
                yield return new ValidationResult(
                    "Para nuevos pacientes ingrese una cédula costarricense de exactamente 9 dígitos.",
                    new[] { nameof(NumeroIdentificacion) });
            }

            // HU12: la selección telefónica del navegador no sustituye
            // la validación del servidor. El formulario envía +<país><número>
            // y SQL solo retiene los dígitos después de validarlos.
            var utilidadTelefonica = PhoneNumberUtil.GetInstance();
            var telefonos = new[]
            {
                (Valor: NumeroTelefono, Campo: nameof(NumeroTelefono)),
                (Valor: TelefonoEmergencia, Campo: nameof(TelefonoEmergencia))
            };

            foreach (var telefono in telefonos)
            {
                if (string.IsNullOrWhiteSpace(telefono.Valor))
                    continue; // Required ya comprueba los campos vacíos.

                bool valido;
                try
                {
                    // En alta/edición el selector JS entrega E.164 con '+',
                    // pero admitimos también los 8 dígitos nacionales.
                    var analizado = utilidadTelefonica.Parse(telefono.Valor, "CR");
                    valido = utilidadTelefonica.IsValidNumber(analizado);
                }
                catch (NumberParseException)
                {
                    valido = false;
                }

                if (!valido)
                    yield return new ValidationResult(
                        "Ingrese un número telefónico válido para el país correspondiente.",
                        new[] { telefono.Campo });
            }

            // HU12: nombres de paciente y contacto sin números ni símbolos
            // ajenos a la escritura de nombres; admite tildes y Unicode.
            var camposNombre = new[]
            {
                (Valor: Nombre, Campo: nameof(Nombre)),
                (Valor: Apellidos, Campo: nameof(Apellidos)),
                (Valor: ContactoEmergencia, Campo: nameof(ContactoEmergencia))
            };

            foreach (var dato in camposNombre)
            {
                if (string.IsNullOrWhiteSpace(dato.Valor))
                    continue; // Required cubre los valores vacíos.

                if (!Regex.IsMatch(dato.Valor, @"^(?=.*\p{L})[\p{L}\p{M} .'\u2019-]+$"))
                    yield return new ValidationResult(
                        "Ingrese un nombre válido: letras, espacios, apóstrofos, guiones o puntos.",
                        new[] { dato.Campo });
            }

            // HU12: dirección descriptiva. No exige códigos postales ni
            // direcciones estructuradas que el esquema actual no soporta.
            if (!string.IsNullOrWhiteSpace(Direccion))
            {
                var direccion = Direccion.Trim();
                if (direccion.Length < 3 || !Regex.IsMatch(direccion, @"\p{L}"))
                    yield return new ValidationResult(
                        "Ingrese una dirección descriptiva que incluya letras.",
                        new[] { nameof(Direccion) });
            }

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
