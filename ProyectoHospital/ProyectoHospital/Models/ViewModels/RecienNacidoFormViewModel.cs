using System.ComponentModel.DataAnnotations;
using PhoneNumbers;

namespace ProyectoHospital.Models.ViewModels
{
    // Registro especializado: la identificación del bebé se asigna posteriormente.
    // La madre y el médico se seleccionan de registros existentes.
    public class RecienNacidoFormViewModel : IValidatableObject
    {
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione a la madre del recién nacido.")]
        public int MadrePacienteId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Seleccione al médico responsable.")]
        public int EmpleadoId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(130)]
        public string Apellidos { get; set; } = "";

        [Required(ErrorMessage = "Seleccione el género.")]
        [RegularExpression(@"^(Masculino|Femenino|Otro)$",
            ErrorMessage = "Seleccione un género válido.")]
        public string Genero { get; set; } = "";

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^[0-9]{8,20}$",
            ErrorMessage = "Ingrese de 8 a 20 dígitos sin espacios.")]
        public string NumeroTelefono { get; set; } = "";

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(250)]
        public string Direccion { get; set; } = "";

        [Required(ErrorMessage = "El contacto de emergencia es obligatorio.")]
        [StringLength(100)]
        public string ContactoEmergencia { get; set; } = "";

        [Required(ErrorMessage = "El teléfono de emergencia es obligatorio.")]
        [RegularExpression(@"^[0-9]{8,20}$",
            ErrorMessage = "Ingrese de 8 a 20 dígitos sin espacios.")]
        public string TelefonoEmergencia { get; set; } = "";

        [Required(ErrorMessage = "La fecha y hora del nacimiento son obligatorias.")]
        [DataType(DataType.DateTime)]
        public DateTime? FechaHoraNacimiento { get; set; }

        [Required(ErrorMessage = "Seleccione el tipo de parto.")]
        [RegularExpression(@"^(Vaginal|Cesarea)$",
            ErrorMessage = "Seleccione un tipo de parto válido.")]
        public string TipoParto { get; set; } = "";

        [Range(1, 255, ErrorMessage = "Las semanas de gestación deben ser mayores que cero.")]
        public byte? SemanasGestacion { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El peso debe ser mayor que cero.")]
        public int? PesoEnGramos { get; set; }

        // Parsear los límites con cultura invariante evita FormatException
        // en equipos cuya configuración regional usa coma decimal.
        [Range(typeof(decimal), "0.01", "9999.99",
            ParseLimitsInInvariantCulture = true,
            ConvertValueInInvariantCulture = true,
            ErrorMessage = "La talla debe ser mayor que cero.")]
        public decimal? TallaCentimetros { get; set; }

        [Range(0, 10, ErrorMessage = "El Apgar debe estar entre 0 y 10.")]
        public byte? Apgar1Min { get; set; }

        [Range(0, 10, ErrorMessage = "El Apgar debe estar entre 0 y 10.")]
        public byte? Apgar5Min { get; set; }

        [StringLength(100)]
        public string? Respiracion { get; set; }

        [StringLength(150)]
        public string? EstadoSalud { get; set; }

        [StringLength(400)]
        public string? Malformaciones { get; set; }

        [StringLength(500)]
        public string? ObservacionesMedicas { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // El navegador envía únicamente dígitos con prefijo de país,
            // siguiendo las restricciones SQL existentes. Validar el país
            // también en servidor para impedir que se envíen números inventados.
            var utilidadTelefonica = PhoneNumberUtil.GetInstance();
            var telefonos = new[]
            {
                (Valor: NumeroTelefono, Campo: nameof(NumeroTelefono)),
                (Valor: TelefonoEmergencia, Campo: nameof(TelefonoEmergencia))
            };

            foreach (var telefono in telefonos)
            {
                if (string.IsNullOrWhiteSpace(telefono.Valor) ||
                    telefono.Valor.Length < 8 ||
                    telefono.Valor.Length > 20 ||
                    telefono.Valor.Any(c => c < '0' || c > '9'))
                    continue; // Required y RegularExpression cubren esos casos.

                // El código CR se presupone únicamente para 8 dígitos sin prefijo;
                // los valores internacionales deben incluir el indicativo.
                string numero = telefono.Valor.Length == 8
                    ? telefono.Valor
                    : "+" + telefono.Valor;
                bool valido;
                try
                {
                    var parseado = utilidadTelefonica.Parse(numero, "CR");
                    valido = utilidadTelefonica.IsValidNumber(parseado);
                }
                catch (NumberParseException)
                {
                    valido = false;
                }

                if (!valido)
                    yield return new ValidationResult(
                        "Ingrese un teléfono válido para el país seleccionado.",
                        new[] { telefono.Campo });
            }

            if (FechaHoraNacimiento.HasValue &&
                FechaHoraNacimiento.Value > DateTime.Now)
            {
                yield return new ValidationResult(
                    "La fecha y hora del nacimiento no pueden ser futuras.",
                    new[] { nameof(FechaHoraNacimiento) });
            }
        }
    }
}
