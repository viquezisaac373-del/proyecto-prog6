using System;

namespace ProyectoHospital.Models.Validation
{
    /// <summary>
    /// Valida únicamente el formato de la identificación, nunca su autenticidad.
    /// Componente previo a la migración de TipoDocumento en dbo.Pacientes.
    /// NO usar en el formulario hasta que el campo esté persistido en SQL.
    /// </summary>
    public static class DocumentoPacienteValidator
    {
        public const string CedulaNacional = "CEDULA_NACIONAL";
        public const string Dimex = "DIMEX";
        public const string Tim = "TIM";
        public const string Pasaporte = "PASAPORTE";
        public const string Otro = "OTRO";

        /// <returns>Null si la sintaxis es admisible; en otro caso, un mensaje.</returns>
        public static string? Validar(string? tipo, string? identificacion)
        {
            if (string.IsNullOrWhiteSpace(tipo))
                return "Seleccione un tipo de documento.";

            if (string.IsNullOrWhiteSpace(identificacion))
                return "Indique el número de identificación.";

            string numero = identificacion.Trim();

            if (numero.Length > 25 || numero.Length == 0 ||
                numero.IndexOfAny(new[] { '\r', '\n', '\t' }) >= 0)
                return "La identificación debe contener de 1 a 25 caracteres, sin saltos de línea.";

            return tipo switch
            {
                CedulaNacional => ValidarNumerico(numero, 9, "La cédula nacional"),
                Dimex => ValidarNumerico(numero, 12, "El DIMEX"),
                // El formato de TIM y de pasaporte varía por documento/origen.
                // No inventar una longitud fija no confirmada. La migración SQL
                // deberá permitir los formatos que apruebe el Product Owner.
                Tim or Pasaporte or Otro => null,
                _ => "Tipo de documento no reconocido."
            };
        }

        private static string? ValidarNumerico(
            string numero, int longitud, string nombre)
        {
            if (numero.Length != longitud)
                return $"{nombre} debe tener exactamente {longitud} dígitos.";

            bool todosCeros = true;
            foreach (char digito in numero)
            {
                if (digito < '0' || digito > '9')
                    return $"{nombre} debe contener únicamente dígitos del 0 al 9.";
                if (digito != '0')
                    todosCeros = false;
            }

            return todosCeros
                ? $"{nombre} no puede contener solamente ceros."
                : null;
        }
    }
}
