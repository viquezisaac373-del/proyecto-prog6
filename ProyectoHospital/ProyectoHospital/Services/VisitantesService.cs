using Microsoft.Data.SqlClient;
using System.Data;
using ProyectoHospital.Models;

namespace ProyectoHospital.Services;

public class VisitantesService
{
    private readonly string _connectionString;

    public VisitantesService(IConfiguration configuration)
    {
        _connectionString = configuration
            .GetConnectionString("HospitalDB")
            ?? throw new InvalidOperationException(
                "No se configuró la conexión HospitalDB.");
    }

    // =============================================
    // 1. REGISTRAR ENTRADA
    // =============================================

    public async Task RegistrarEntradaAsync(
        Visitante visitante,
        RegistroVisita visita,
        int usuarioId)
    {
        using var conexion = new SqlConnection(_connectionString);

        using var comando = new SqlCommand(
            "dbo.usp_RegistrarEntradaVisitante", conexion);

        comando.CommandType = CommandType.StoredProcedure;

        comando.Parameters.Add(
            "@NumeroIdentificacion", SqlDbType.NVarChar, 25)
            .Value = visitante.NumeroIdentificacion.Trim();

        comando.Parameters.Add(
            "@Nombre", SqlDbType.NVarChar, 100)
            .Value = visitante.Nombre.Trim();

        comando.Parameters.Add(
            "@Apellidos", SqlDbType.NVarChar, 200)
            .Value = visitante.Apellidos.Trim();

        comando.Parameters.Add(
            "@Telefono", SqlDbType.NVarChar, 20)
            .Value = visitante.Telefono.Trim();

        comando.Parameters.Add(
            "@Motivo_Visita", SqlDbType.NVarChar, 300)
            .Value = visita.Motivo_Visita.Trim();

        comando.Parameters.Add("@PacienteId", SqlDbType.Int)
            .Value = (object?)visita.PacienteId ?? DBNull.Value;

        comando.Parameters.Add("@UsuarioEntradaId", SqlDbType.Int)
            .Value = usuarioId;

        await conexion.OpenAsync();
        await comando.ExecuteNonQueryAsync();
    }

    // =============================================
    // 2. REGISTRAR SALIDA
    // =============================================

    public async Task RegistrarSalidaAsync(
        int registroVisitaId,
        int usuarioId)
    {
        using var conexion = new SqlConnection(_connectionString);

        using var comando = new SqlCommand(
            "dbo.usp_RegistrarSalidaVisitante", conexion);

        comando.CommandType = CommandType.StoredProcedure;

        comando.Parameters.Add("@RegistroVisitaId", SqlDbType.Int)
            .Value = registroVisitaId;

        comando.Parameters.Add("@UsuarioSalidaId", SqlDbType.Int)
            .Value = usuarioId;

        await conexion.OpenAsync();
        await comando.ExecuteNonQueryAsync();
    }

    // =============================================
    // 3. CONSULTAR VISITANTES
    // =============================================

    public async Task<DataTable> ConsultarVisitantesAsync(
        string? buscar = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? estado = null)
    {
        using var conexion = new SqlConnection(_connectionString);

        using var comando = new SqlCommand(
            "dbo.usp_ConsultarVisitantes", conexion);

        comando.CommandType = CommandType.StoredProcedure;

        comando.Parameters.Add("@Buscar", SqlDbType.NVarChar, 200)
            .Value = string.IsNullOrWhiteSpace(buscar)
                ? DBNull.Value : buscar.Trim();

        comando.Parameters.Add("@FechaDesde", SqlDbType.Date)
            .Value = (object?)fechaDesde ?? DBNull.Value;

        comando.Parameters.Add("@FechaHasta", SqlDbType.Date)
            .Value = (object?)fechaHasta ?? DBNull.Value;

        comando.Parameters.Add("@Estado", SqlDbType.NVarChar, 20)
            .Value = string.IsNullOrWhiteSpace(estado)
                ? DBNull.Value : estado;

        await conexion.OpenAsync();

        using var lector = await comando.ExecuteReaderAsync();

        var tabla = new DataTable();

        tabla.Load(lector);

        return tabla;
    }

    // =============================================
    // 4. BUSCAR VISITANTE POR IDENTIFICACIÓN
    // =============================================

    public async Task<Visitante?> BuscarVisitanteAsync(
        string identificacion)
    {
        using var conexion = new SqlConnection(_connectionString);

        const string consulta = @"
            SELECT
                VisitanteId,
                NumeroIdentificacion,
                Nombre,
                Apellidos,
                Telefono
            FROM dbo.Visitantes
            WHERE NumeroIdentificacion = @Identificacion;";

        using var comando = new SqlCommand(consulta, conexion);

        comando.Parameters.Add(
            "@Identificacion", SqlDbType.NVarChar, 25)
            .Value = identificacion.Trim();

        await conexion.OpenAsync();

        using var lector = await comando.ExecuteReaderAsync();

        if (!await lector.ReadAsync())
            return null;

        return new Visitante
        {
            VisitanteId = lector.GetInt32(0),
            NumeroIdentificacion = lector.GetString(1),
            Nombre = lector.GetString(2),
            Apellidos = lector.GetString(3),
            Telefono = lector.GetString(4)
        };
    }

    // =============================================
    // LISTAR PACIENTES ACTIVOS PARA VISITANTES
    // =============================================

    public async Task<DataTable> ListarPacientesActivosAsync()
    {
        using var conexion =
            new SqlConnection(_connectionString);

        const string consulta = @"
        SELECT
            PacienteId,
            Numero_Identificacion,
            Nombre,
            Apellidos
        FROM dbo.Pacientes
        WHERE Activo = 1
        ORDER BY Nombre, Apellidos, PacienteId;";

        using var comando =
            new SqlCommand(consulta, conexion);

        await conexion.OpenAsync();

        using var lector =
            await comando.ExecuteReaderAsync();

        var tabla = new DataTable();

        tabla.Load(lector);

        return tabla;
    }

    // =============================================
    // 6. VERIFICAR SI EL VISITANTE TIENE VISITA ACTIVA
    // =============================================

    public async Task<bool> TieneVisitaActivaAsync(
        int visitanteId)
    {
        using var conexion =
            new SqlConnection(_connectionString);

        const string consulta = @"
        SELECT CASE
            WHEN EXISTS (
                SELECT 1
                FROM dbo.RegistroVisitas
                WHERE VisitanteId = @VisitanteId
                  AND FechaHoraSalida IS NULL
            )
            THEN 1
            ELSE 0
        END;";

        using var comando =
            new SqlCommand(consulta, conexion);

        comando.Parameters.Add(
            "@VisitanteId", SqlDbType.Int)
            .Value = visitanteId;

        await conexion.OpenAsync();

        var resultado =
            await comando.ExecuteScalarAsync();

        return Convert.ToInt32(resultado) == 1;
    }
}