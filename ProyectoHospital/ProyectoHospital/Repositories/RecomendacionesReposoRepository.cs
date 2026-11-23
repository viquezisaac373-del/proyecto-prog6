using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.ViewModels;
using System.Data;

namespace ProyectoHospital.Repositories;

/// <summary>
/// Recomendaciones médicas internas de reposo. No generan incapacidades
/// oficiales, códigos CCSS, subsidios ni homologaciones. Solo el profesional
/// con una cuenta activa asociada a un empleado médico puede registrar.
/// </summary>
public sealed class RecomendacionesReposoRepository
{
    private readonly string _cadena;

    public RecomendacionesReposoRepository(IConfiguration configuration)
    {
        _cadena = configuration.GetConnectionString("HospitalDB")
            ?? throw new InvalidOperationException("Falta la cadena HospitalDB.");
    }

    public async Task<int?> ObtenerMedicoPorUsuarioAsync(int usuarioId)
    {
        const string sql = @"
SELECT e.EmpleadoId
FROM dbo.Usuarios u
JOIN dbo.Empleados e ON e.EmpleadoId = u.EmpleadoId
WHERE u.UsuarioId = @UsuarioId AND u.Activo = 1
  AND e.Activo = 1 AND e.EsMedico = 1
  AND EXISTS (
    SELECT 1 FROM dbo.UsuarioRol ur
    JOIN dbo.Roles r ON r.RolId = ur.RolId
    WHERE ur.UsuarioId = u.UsuarioId AND r.Activo = 1
      AND r.Nombre_del_Rol IN (N'Medico', N'Médico')
  );";
        await using var cn = new SqlConnection(_cadena);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
        var valor = await cmd.ExecuteScalarAsync();
        return valor is null or DBNull ? null : Convert.ToInt32(valor);
    }

    public async Task<List<OpcionPacienteReposo>> ListarPacientesActivosAsync()
    {
        const string sql = @"
SELECT PacienteId, CONCAT(Nombre, N' ', Apellidos, N' (#', PacienteId, N')')
FROM dbo.Pacientes
WHERE Activo = 1
ORDER BY Apellidos, Nombre, PacienteId;";
        var resultado = new List<OpcionPacienteReposo>();
        await using var cn = new SqlConnection(_cadena);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            resultado.Add(new OpcionPacienteReposo
            {
                PacienteId = reader.GetInt32(0),
                Etiqueta = reader.GetString(1)
            });
        return resultado;
    }

    public async Task<int> CrearAsync(
        CrearRecomendacionReposoViewModel modelo, int usuarioId)
    {
        if (modelo.PacienteId <= 0 || !modelo.FechaInicio.HasValue ||
            !modelo.FechaFin.HasValue || modelo.FechaInicio.Value.Date > modelo.FechaFin.Value.Date)
            throw new InvalidOperationException("Indique un paciente y un rango de fechas válido.");

        string justificacion = (modelo.Justificacion ?? "").Trim();
        if (justificacion.Length is < 10 or > 500)
            throw new InvalidOperationException("Indique una justificación clínica entre 10 y 500 caracteres.");

        await using var cn = new SqlConnection(_cadena);
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            const string medicoSql = @"
SELECT e.EmpleadoId
FROM dbo.Usuarios u WITH (UPDLOCK, HOLDLOCK)
JOIN dbo.Empleados e WITH (UPDLOCK, HOLDLOCK) ON e.EmpleadoId = u.EmpleadoId
WHERE u.UsuarioId = @UsuarioId AND u.Activo = 1
  AND e.Activo = 1 AND e.EsMedico = 1
  AND EXISTS (
    SELECT 1 FROM dbo.UsuarioRol ur JOIN dbo.Roles r ON ur.RolId = r.RolId
    WHERE ur.UsuarioId = u.UsuarioId AND r.Activo = 1
      AND r.Nombre_del_Rol IN (N'Medico', N'Médico')
  );";
            int medicoId;
            using (var cmd = new SqlCommand(medicoSql, cn, tx))
            {
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
                var valor = await cmd.ExecuteScalarAsync();
                if (valor is null or DBNull)
                    throw new InvalidOperationException("La cuenta no está asociada a un médico activo.");
                medicoId = Convert.ToInt32(valor);
            }

            const string pacienteSql = @"
SELECT COUNT(1) FROM dbo.Pacientes WITH (UPDLOCK, HOLDLOCK)
WHERE PacienteId = @PacienteId AND Activo = 1;";
            using (var cmd = new SqlCommand(pacienteSql, cn, tx))
            {
                cmd.Parameters.Add("@PacienteId", SqlDbType.Int).Value = modelo.PacienteId;
                if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 1)
                    throw new InvalidOperationException("El paciente no está activo o no existe.");
            }

            const string duplicadoSql = @"
SELECT COUNT(1) FROM dbo.RecomendacionesReposo WITH (UPDLOCK, HOLDLOCK)
WHERE PacienteId = @PacienteId AND EmpleadoId = @EmpleadoId
  AND FechaInicio = @FechaInicio AND FechaFin = @FechaFin;";
            using (var cmd = new SqlCommand(duplicadoSql, cn, tx))
            {
                cmd.Parameters.Add("@PacienteId", SqlDbType.Int).Value = modelo.PacienteId;
                cmd.Parameters.Add("@EmpleadoId", SqlDbType.Int).Value = medicoId;
                cmd.Parameters.Add("@FechaInicio", SqlDbType.Date).Value = modelo.FechaInicio.Value.Date;
                cmd.Parameters.Add("@FechaFin", SqlDbType.Date).Value = modelo.FechaFin.Value.Date;
                if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 0)
                    throw new InvalidOperationException(
                        "Ya existe una recomendación de este médico para ese paciente y período.");
            }

            const string insertar = @"
INSERT INTO dbo.RecomendacionesReposo
  (PacienteId, EmpleadoId, UsuarioRegistroId, FechaInicio, FechaFin, Justificacion)
OUTPUT INSERTED.RecomendacionId
VALUES (@PacienteId, @EmpleadoId, @UsuarioId, @Inicio, @Fin, @Justificacion);";
            int recomendacionId;
            using (var cmd = new SqlCommand(insertar, cn, tx))
            {
                cmd.Parameters.Add("@PacienteId", SqlDbType.Int).Value = modelo.PacienteId;
                cmd.Parameters.Add("@EmpleadoId", SqlDbType.Int).Value = medicoId;
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
                cmd.Parameters.Add("@Inicio", SqlDbType.Date).Value = modelo.FechaInicio.Value.Date;
                cmd.Parameters.Add("@Fin", SqlDbType.Date).Value = modelo.FechaFin.Value.Date;
                cmd.Parameters.Add("@Justificacion", SqlDbType.NVarChar, 500).Value = justificacion;
                recomendacionId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // No registrar diagnósticos o justificaciones clínicas en la bitácora general.
            const string auditoria = @"
INSERT INTO dbo.BitacoraAuditoria
  (UsuarioId, Accion, Tabla_Modificada, RegistroId, VNuevos, Objeto)
VALUES (@UsuarioId, N'INSERT', N'RecomendacionesReposo', @Registro,
        N'Recomendación interna registrada (sin detalles clínicos).',
        N'RecomendacionesReposoController');";
            using (var cmd = new SqlCommand(auditoria, cn, tx))
            {
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
                cmd.Parameters.Add("@Registro", SqlDbType.NVarChar, 100).Value = recomendacionId.ToString();
                await cmd.ExecuteNonQueryAsync();
            }

            tx.Commit();
            return recomendacionId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<List<RecomendacionReposoResumen>> ListarPropiasAsync(int medicoEmpleadoId)
    {
        const string sql = @"
SELECT r.RecomendacionId, r.PacienteId,
       CONCAT(p.Nombre, N' ', p.Apellidos), p.Numero_Identificacion,
       CONCAT(e.Nombre, N' ', e.Apellidos), r.EmpleadoId,
       e.Especialidad, r.FechaInicio, r.FechaFin, r.FechaRegistro, r.Justificacion
FROM dbo.RecomendacionesReposo r
JOIN dbo.Pacientes p ON p.PacienteId = r.PacienteId
JOIN dbo.Empleados e ON e.EmpleadoId = r.EmpleadoId
WHERE r.EmpleadoId = @MedicoId
ORDER BY r.FechaRegistro DESC, r.RecomendacionId DESC;";
        var resultado = new List<RecomendacionReposoResumen>();
        await using var cn = new SqlConnection(_cadena);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add("@MedicoId", SqlDbType.Int).Value = medicoEmpleadoId;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            resultado.Add(LeerRegistro(reader));
        return resultado;
    }

    public async Task<RecomendacionReposoResumen?> ObtenerPropiaAsync(int recomendacionId, int medicoEmpleadoId)
    {
        const string sql = @"
SELECT r.RecomendacionId, r.PacienteId,
       CONCAT(p.Nombre, N' ', p.Apellidos), p.Numero_Identificacion,
       CONCAT(e.Nombre, N' ', e.Apellidos), r.EmpleadoId,
       e.Especialidad, r.FechaInicio, r.FechaFin, r.FechaRegistro, r.Justificacion
FROM dbo.RecomendacionesReposo r
JOIN dbo.Pacientes p ON p.PacienteId = r.PacienteId
JOIN dbo.Empleados e ON e.EmpleadoId = r.EmpleadoId
WHERE r.RecomendacionId = @RecomendacionId AND r.EmpleadoId = @MedicoId;";
        await using var cn = new SqlConnection(_cadena);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add("@RecomendacionId", SqlDbType.Int).Value = recomendacionId;
        cmd.Parameters.Add("@MedicoId", SqlDbType.Int).Value = medicoEmpleadoId;
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? LeerRegistro(reader) : null;
    }

    private static RecomendacionReposoResumen LeerRegistro(SqlDataReader r) => new()
    {
        RecomendacionId = r.GetInt32(0),
        PacienteId = r.GetInt32(1),
        Paciente = r.GetString(2),
        Identificacion = r.IsDBNull(3) ? null : r.GetString(3),
        Medico = r.GetString(4),
        EmpleadoId = r.GetInt32(5),
        Especialidad = r.IsDBNull(6) ? null : r.GetString(6),
        FechaInicio = r.GetDateTime(7),
        FechaFin = r.GetDateTime(8),
        FechaRegistro = r.GetDateTime(9),
        Justificacion = r.GetString(10)
    };
}
