
using System.ComponentModel.DataAnnotations;
using PhoneNumbers;
using System.Text.RegularExpressions;

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
            @"^(?:[0-9]{8}|\+[1-9][0-9]{7,14})$",
            ErrorMessage = "Ingrese 8 dígitos nacionales o un número internacional válido.")]
        public string NumeroTelefono { get; set; } = "";

        // CORREO ELECTRÓNICO
        // Comprueba la estructura básica del correo.
        // Permite dominios como .com, .co, .cr, entre otros.
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [StringLength(200, ErrorMessage = "El correo no puede superar 200 caracteres.")]
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
            @"^(?:[0-9]{8}|\+[1-9][0-9]{7,14})$",
            ErrorMessage = "Ingrese un teléfono nacional o internacional válido.")]
        public string TelefonoEmergencia { get; set; } = "";

        // VALIDACIONES ADICIONALES
        public IEnumerable<ValidationResult> Validate(
     ValidationContext validationContext)
        {
            // VALIDACIÓN DE FECHA DE NACIMIENTO
            if (FechaNacimiento.HasValue)
            {
                DateTime fechaNacimiento = FechaNacimiento.Value.Date;
                DateTime fechaActual = DateTime.Today;

                if (fechaNacimiento > fechaActual)
                {
                    yield return new ValidationResult(
                        "La fecha de nacimiento no puede ser futura.",
                        new[] { nameof(FechaNacimiento) });
                }

                // Límite provisional pendiente de aprobación del PO.
                DateTime fechaMinima = fechaActual.AddYears(-125);

                if (fechaNacimiento < fechaMinima)
                {
                    yield return new ValidationResult(
                        "La fecha de nacimiento supera el límite de antigüedad permitido.",
                        new[] { nameof(FechaNacimiento) });
                }
            }

            // VALIDACIÓN DE TELÉFONOS
            var utilidadTelefonica = PhoneNumberUtil.GetInstance();

            var telefonos = new[]
            {
        (Valor: NumeroTelefono, Campo: nameof(NumeroTelefono)),
        (Valor: TelefonoEmergencia, Campo: nameof(TelefonoEmergencia))
    };

            foreach (var telefono in telefonos)
            {
                // Required valida los campos vacíos.
                if (string.IsNullOrWhiteSpace(telefono.Valor))
                {
                    continue;
                }

                bool esValido = false;

                try
                {
                    // CR se usa para interpretar números nacionales.
                    // Si comienza con +, se reconoce el prefijo internacional.
                    var numero = utilidadTelefonica.Parse(
                        telefono.Valor, "CR");

                    esValido = utilidadTelefonica.IsValidNumber(numero);
                }
                catch (NumberParseException)
                {
                    esValido = false;
                }

                if (!esValido)
                {
                    yield return new ValidationResult(
                        "Ingrese un número telefónico válido para el país correspondiente.",
                        new[] { telefono.Campo });
                }
            }
            // VALIDACIÓN DE NOMBRES Y CONTACTO DE EMERGENCIA
            var camposNombre = new[]
            {
                (Valor: Nombre, Campo: nameof(Nombre)),
                (Valor: Apellidos, Campo: nameof(Apellidos)),
                (Valor: ContactoEmergencia, Campo: nameof(ContactoEmergencia))
            };

            foreach (var dato in camposNombre)
            {
                // Required comprueba los campos vacíos.
                if (string.IsNullOrWhiteSpace(dato.Valor))
                {
                    continue;
                }

                // Exigir al menos una letra.
                // Admitir letras Unicode, tildes, espacios,
                // apóstrofos, guiones y puntos.
                bool formatoValido = Regex.IsMatch(
                    dato.Valor,
                    @"^(?=.*\p{L})[\p{L}\p{M} .'\u2019-]+$"
                );

                if (!formatoValido)
                {
                    yield return new ValidationResult(
                        "Ingrese un nombre válido. Se permiten letras, espacios, apóstrofos, guiones y puntos.",
                        new[] { dato.Campo }
                    );
                }
            }
            // VALIDACIÓN DE DIRECCIÓN
            if (!string.IsNullOrWhiteSpace(Direccion))
            {
                string direccion = Direccion.Trim();

                // Exigir una longitud mínima y al menos una letra.
                // Evitar valores como "0", "123456" o "A".
                if (direccion.Length < 3 ||
                    !Regex.IsMatch(direccion, @"\p{L}"))
                {
                    yield return new ValidationResult(
                        "Ingrese una dirección descriptiva que incluya letras.",
                        new[] { nameof(Direccion) }
                    );
                }
            }
        }
    }
}
