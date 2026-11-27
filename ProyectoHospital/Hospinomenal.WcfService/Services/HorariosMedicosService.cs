using System.Data;
using CoreWCF;
using Microsoft.Data.SqlClient;

namespace Hospinomenal.WcfService.Services;

/// <summary>
/// Servicio SOAP de solo lectura para consultar BLOQUES SEMANALES.
/// No representa cupos libres de citas: las reservas no se descuentan aquí.
/// El host se publica solo en loopback para la demostración académica.
/// </summary>
public sealed class HorariosMedicosService : IHorariosMedicosService
{
    private static readonly string[] Dias =
        { "lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo" };

    private readonly string _connectionString;
    private readonly ILogger<HorariosMedicosService> _logger;

    public HorariosMedicosService(IConfiguration config, ILogger<HorariosMedicosService> logger)
    {
        _connectionString = config.GetConnectionString("HospitalDB")
            ?? throw new InvalidOperationException(
                "Configure ConnectionStrings:HospitalDB con secretos de usuario en el proyecto WCF.");
        _logger = logger;
    }

    public async Task<string> ConsultarHorarios(int medicoId)
    {
        if (medicoId <= 0)
            throw new FaultException("Indique un identificador de médico válido.");

        const string sql = @"
SELECT CONCAT(e.Nombre, N' ', e.Apellidos) AS Medico,
       d.DiaSemana, d.HoraInicio, d.HoraFin
FROM dbo.DisponibilidadMedicaSemanal AS d
INNER JOIN dbo.Empleados AS e ON e.EmpleadoId = d.EmpleadoId
WHERE d.EmpleadoId = @MedicoId
  AND e.Activo = 1 AND e.EsMedico = 1 AND d.Activo = 1
ORDER BY d.DiaSemana, d.HoraInicio;";

        try
        {
            var franjas = new List<string>();
            string? medico = null;
            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();
            await using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.Add("@MedicoId", SqlDbType.Int).Value = medicoId;
            await using var lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                medico ??= lector.GetString(0);
                int dia = lector.GetInt32(1);
                string nombreDia = dia is >= 1 and <= 7 ? Dias[dia - 1] : "día no definido";
                TimeSpan inicio = lector.GetTimeSpan(2);
                TimeSpan fin = lector.GetTimeSpan(3);
                franjas.Add($"{nombreDia}: {inicio.ToString(@"hh\\:mm")} a {fin.ToString(@"hh\\:mm")}");
            }

            if (franjas.Count == 0)
                return "No se encontraron bloques semanales activos para el médico solicitado.";

            return $"Médico: {medico}\n" +
                   "Disponibilidad semanal registrada (no equivale a citas libres):\n" +
                   string.Join("\n", franjas);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex,
                "Error de SQL Server al consultar disponibilidad WCF del médico {MedicoId}.", medicoId);
            throw new FaultException("No fue posible consultar los horarios en este momento.");
        }
    }
}
