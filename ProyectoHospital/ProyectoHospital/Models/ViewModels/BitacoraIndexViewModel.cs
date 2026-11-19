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
        public List<string> Acciones { get; set; } = new();
        public int Total { get; set; }

        public int TotalPaginas =>
            Total == 0 ? 1 : (int)Math.Ceiling(Total / (double)Filtro.TamanoPagina);

        public int PrimerRegistro =>
            Total == 0 ? 0 : (Filtro.Pagina - 1) * Filtro.TamanoPagina + 1;

        public int UltimoRegistro =>
            Math.Min(Filtro.Pagina * Filtro.TamanoPagina, Total);

        public IEnumerable<int?> PaginasVisibles()
        {
            int actual = Filtro.Pagina;
            int ultima = TotalPaginas;
            var paginas = new SortedSet<int> { 1, ultima };

            for (int i = actual - 2; i <= actual + 2; i++)
            {
                if (i >= 1 && i <= ultima)
                    paginas.Add(i);
            }

            int? anterior = null;
            foreach (int p in paginas)
            {
                if (anterior.HasValue && p - anterior.Value > 1)
                    yield return null;

                yield return p;
                anterior = p;
            }
        }

        public Dictionary<string, string> RutaPagina(int pagina)
        {
            var ruta = RutaFiltros();
            ruta["orden"] = Filtro.Orden ?? "fecha";
            ruta["dir"] = Filtro.Descendente ? "desc" : "asc";
            ruta["pagina"] = pagina.ToString();
            return ruta;
        }
        public string? Error { get; set; }
        public string? AvisoFiltro { get; set; }

        public bool HayFiltros =>
            Filtro.Desde.HasValue || Filtro.Hasta.HasValue
            || !string.IsNullOrWhiteSpace(Filtro.Usuario)
            || !string.IsNullOrWhiteSpace(Filtro.Accion);

        public string EtiquetaUsuario => Tipo == TipoBitacora.Notificaciones ? "Paciente" : "Usuario";

        public string EtiquetaAccion => Tipo switch
        {
            TipoBitacora.Errores => "Operación",
            TipoBitacora.Notificaciones => "Estado de envío",
            _ => "Acción"
        };

        public Dictionary<string, string> RutaOrden(string columna)
        {
            bool mismaColumna = string.Equals(Filtro.Orden, columna, StringComparison.OrdinalIgnoreCase);
            bool descendente = mismaColumna ? !Filtro.Descendente : columna == "fecha";

            var ruta = RutaFiltros();
            ruta["orden"] = columna;
            ruta["dir"] = descendente ? "desc" : "asc";
            return ruta;
        }

        public Dictionary<string, string> RutaFiltros()
        {
            var ruta = new Dictionary<string, string> { ["tipo"] = Tipo.ToString() };

            if (Filtro.Desde.HasValue)
                ruta["desde"] = Filtro.Desde.Value.ToString("yyyy-MM-dd");
            if (Filtro.Hasta.HasValue)
                ruta["hasta"] = Filtro.Hasta.Value.ToString("yyyy-MM-dd");
            if (!string.IsNullOrWhiteSpace(Filtro.Usuario))
                ruta["usuario"] = Filtro.Usuario;
            if (!string.IsNullOrWhiteSpace(Filtro.Accion))
                ruta["accion"] = Filtro.Accion;

            return ruta;
        }

        public string Flecha(string columna)
        {
            if (!string.Equals(Filtro.Orden, columna, StringComparison.OrdinalIgnoreCase))
                return "↕";

            return Filtro.Descendente ? "▼" : "▲";
        }

        public bool EsColumnaOrdenada(string columna) =>
            string.Equals(Filtro.Orden, columna, StringComparison.OrdinalIgnoreCase);

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
