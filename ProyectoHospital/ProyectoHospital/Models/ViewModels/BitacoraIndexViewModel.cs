using ProyectoHospital.Models.Enums;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ProyectoHospital.Models.ViewModels
{
    public class BitacoraIndexViewModel
    {
        private static readonly JsonSerializerOptions OpcionesJson = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public TipoBitacora Tipo { get; set; } = TipoBitacora.Accesos;
        public FiltroBitacora Filtro { get; set; } = new();
        public List<RegistroBitacora> Registros { get; set; } = new();
        public string? Error { get; set; }

        public static string NombreBitacora(TipoBitacora tipo) => tipo switch
        {
            TipoBitacora.Accesos => "Accesos",
            TipoBitacora.Auditoria => "Acciones",
            TipoBitacora.Errores => "Errores",
            TipoBitacora.Notificaciones => "Notificaciones",
            _ => tipo.ToString()
        };

        public static string FormatearDetalle(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return "Sin datos";

            try
            {
                using var documento = JsonDocument.Parse(valor);
                return JsonSerializer.Serialize(documento.RootElement, OpcionesJson);
            }
            catch (JsonException)
            {
                return valor;
            }
        }

        public static string ClaseResultado(string resultado) => resultado switch
        {
            "Exitoso" or "Enviada" => "bg-success",
            "Fallido" or "Fallida" => "bg-danger",
            "Bloqueado" => "bg-warning text-dark",
            "Pendiente" => "bg-secondary",
            _ => "bg-light text-dark border"
        };
    }
}
