using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.ViewModels;
using System.Data;

namespace ProyectoHospital.Repositories;

/// <summary>
/// Disponibilidad semanal recurrente. Cada alta/desactivación se guarda con
/// auditoría en una transacción y un candado por médico/día para evitar carreras.
/// No reemplaza la gestión de citas, permisos clínicos ni feriados.
/// </summary>
public sealed class DisponibilidadMedicaRepository
{
    private readonly string _connectionString;

    public DisponibilidadMedicaRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("HospitalDB")
            ?? throw new InvalidOperationException("Falta configurar HospitalDB.");
    }

    public async Task<List<MedicoHorarioOpcion>> ListarMedicosAsync()
    {
        const string sql = @"
SELECT EmpleadoId, CONCAT(Nombre, N' ', Apellidos), Especialidad, DuracionCitaMin
FROM dbo.Empleados
WHERE EsMedico = 1 AND Activo = 1 AND DuracionCitaMin > 0
ORDER BY Nombre, Apellidos, EmpleadoId;";

        var medicos = new List<MedicoHorarioOpcion>();
        await using var cn = new SqlConnection(_connectionString);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        await using var lector = await cmd.ExecuteReaderAsync();

        while (await lector.ReadAsync())
            medicos.Add(new MedicoHorarioOpcion
            {
                EmpleadoId = lector.GetInt32(0),
                Nombre = lector.GetString(1),
                Especialidad = lector.IsDBNull(2) ? null : lector.GetString(2),
                DuracionCitaMin = lector.GetInt16(3)
            });
        return medicos;
    }

    public async Task<List<BloqueDisponibilidadMedica>> ListarBloquesAsync(int? medicoId, string? especialidad)
    {
        const string sql = @"
SELECT d.BloqueId, d.EmpleadoId,
       CONCAT(e.Nombre, N' ', e.Apellidos) AS Medico,
       e.Especialidad, d.DiaSemana, d.HoraInicio, d.HoraFin,
       e.DuracionCitaMin, DATEDIFF(MINUTE, d.HoraInicio, d.HoraFin) AS Minutos
FROM dbo.DisponibilidadMedicaSemanal d
JOIN dbo.Empleados e ON e.EmpleadoId = d.EmpleadoId
WHERE d.Activo = 1 AND (@MedicoId IS NULL OR d.EmpleadoId = @MedicoId)
  AND (@Especialidad IS NULL OR e.Especialidad = @Especialidad)
ORDER BY e.Nombre, e.Apellidos, d.DiaSemana, d.HoraInicio;";

        var bloques = new List<BloqueDisponibilidadMedica>();
        await using var cn = new SqlConnection(_connectionString);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add("@MedicoId", SqlDbType.Int).Value = (object?)medicoId ?? DBNull.Value;
        cmd.Parameters.Add("@Especialidad", SqlDbType.NVarChar, 100).Value =
            string.IsNullOrWhiteSpace(especialidad) ? DBNull.Value : especialidad.Trim();
        await using var lector = await cmd.ExecuteReaderAsync();

        while (await lector.ReadAsync())
            bloques.Add(new BloqueDisponibilidadMedica
            {
                BloqueId = lector.GetInt32(0),
                EmpleadoId = lector.GetInt32(1),
                Medico = lector.GetString(2),
                Especialidad = lector.IsDBNull(3) ? null : lector.GetString(3),
                DiaSemana = lector.GetInt32(4),
                HoraInicio = lector.GetTimeSpan(5),
                HoraFin = lector.GetTimeSpan(6),
                DuracionCitaMin = lector.GetInt16(7),
                Minutos = lector.GetInt32(8)
            });

        return bloques;
    }

    public async Task<int> CrearAsync(CrearBloqueDisponibilidadViewModel form, int actorId)
    {
        if (form.EmpleadoId <= 0 || form.DiaSemana is < 1 or > 7 ||
            !form.HoraInicio.HasValue || !form.HoraFin.HasValue ||
            form.HoraInicio.Value < TimeSpan.Zero ||
            form.HoraFin.Value >= TimeSpan.FromDays(1) ||
            form.HoraInicio.Value >= form.HoraFin.Value)
            throw new InvalidOperationException("El bloque semanal indicado no es válido.");

        await using var cn = new SqlConnection(_connectionString);
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            await BloquearMedicoDiaAsync(cn, tx, form.EmpleadoId, form.DiaSemana);

            const string sqlMedico = @"
SELECT COUNT(1) FROM dbo.Empleados WITH (UPDLOCK, HOLDLOCK)
WHERE EmpleadoId = @EmpleadoId AND Activo = 1 AND EsMedico = 1 AND DuracionCitaMin > 0;";
            using (var cmd = new SqlCommand(sqlMedico, cn, tx))
            {
                cmd.Parameters.Add("@EmpleadoId", SqlDbType.Int).Value = form.EmpleadoId;
                if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 1)
                    throw new InvalidOperationException("Seleccione un médico activo con duración de cita configurada.");
            }

            const string sqlSolape = @"
SELECT COUNT(1)
FROM dbo.DisponibilidadMedicaSemanal WITH (UPDLOCK, HOLDLOCK)
WHERE EmpleadoId = @EmpleadoId AND DiaSemana = @DiaSemana AND Activo = 1
  AND @HoraInicio < HoraFin AND HoraInicio < @HoraFin;";
            using (var cmd = new SqlCommand(sqlSolape, cn, tx))
            {
                AgregarParametrosBloque(cmd, form);
                if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 0)
                    throw new InvalidOperationException(
                        "El médico ya tiene un horario activo que se solapa con ese día y tramo.");
            }

            const string sqlInsert = @"
INSERT INTO dbo.DisponibilidadMedicaSemanal
    (EmpleadoId, DiaSemana, HoraInicio, HoraFin, Activo, UsuarioRegistroId)
OUTPUT INSERTED.BloqueId
VALUES (@EmpleadoId, @DiaSemana, @HoraInicio, @HoraFin, 1, @UsuarioId);";
            int bloqueId;
            using (var cmd = new SqlCommand(sqlInsert, cn, tx))
            {
                AgregarParametrosBloque(cmd, form);
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = actorId;
                bloqueId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            await AuditarAsync(cn, tx, actorId, "INSERT", bloqueId,
                $"EmpleadoId={form.EmpleadoId}; DiaSemana={form.DiaSemana}; " +
                $"Inicio={form.HoraInicio.Value.ToString(@"hh\:mm")}; Fin={form.HoraFin.Value.ToString(@"hh\:mm")}");
            tx.Commit();
            return bloqueId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task DesactivarAsync(int bloqueId, int actorId)
    {
        if (bloqueId <= 0)
            throw new InvalidOperationException("Bloque de disponibilidad inválido.");

        await using var cn = new SqlConnection(_connectionString);
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            int empleadoId, dia;
            TimeSpan inicio, fin;
            const string buscar = @"
SELECT EmpleadoId, DiaSemana, HoraInicio, HoraFin
FROM dbo.DisponibilidadMedicaSemanal WITH (UPDLOCK, HOLDLOCK)
WHERE BloqueId = @BloqueId AND Activo = 1;";

            using (var cmd = new SqlCommand(buscar, cn, tx))
            {
                cmd.Parameters.Add("@BloqueId", SqlDbType.Int).Value = bloqueId;
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new InvalidOperationException("El bloque ya no está activo o no existe.");
                empleadoId = reader.GetInt32(0);
                dia = reader.GetInt32(1);
                inicio = reader.GetTimeSpan(2);
                fin = reader.GetTimeSpan(3);
            }

            await BloquearMedicoDiaAsync(cn, tx, empleadoId, dia);

            // Bloquear bajas que dejarían citas no canceladas sin su turno habitual.
            // 1900-01-01 fue lunes. Evita depender de DATEFIRST del servidor.
            const string citaVigente = @"
SELECT COUNT(1)
FROM dbo.Citas WITH (UPDLOCK, HOLDLOCK)
WHERE EmpleadoId = @EmpleadoId
  AND Fecha_Hora >= SYSDATETIME()
  AND Estado IN (N'Programada', N'EnEspera', N'EnAtencion')
  AND (DATEDIFF(DAY, '19000101', CONVERT(date, Fecha_Hora)) % 7 + 1) = @DiaSemana
  AND CONVERT(time, Fecha_Hora) >= @Inicio
  AND CONVERT(time, Fecha_Hora) < @Fin;";
            using (var cmd = new SqlCommand(citaVigente, cn, tx))
            {
                cmd.Parameters.Add("@EmpleadoId", SqlDbType.Int).Value = empleadoId;
                cmd.Parameters.Add("@DiaSemana", SqlDbType.Int).Value = dia;
                cmd.Parameters.Add("@Inicio", SqlDbType.Time).Value = inicio;
                cmd.Parameters.Add("@Fin", SqlDbType.Time).Value = fin;
                if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0)
                    throw new InvalidOperationException(
                        "Existen citas futuras no canceladas en ese bloque. Reprográmelas antes de desactivarlo.");
            }

            const string actualizar = @"
UPDATE dbo.DisponibilidadMedicaSemanal SET Activo = 0, FechaActualizacion = SYSDATETIME()
WHERE BloqueId = @BloqueId AND Activo = 1;";
            using (var cmd = new SqlCommand(actualizar, cn, tx))
            {
                cmd.Parameters.Add("@BloqueId", SqlDbType.Int).Value = bloqueId;
                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException("No fue posible desactivar el bloque.");
            }

            await AuditarAsync(cn, tx, actorId, "UPDATE", bloqueId,
                $"Se desactivó horario: EmpleadoId={empleadoId}; DiaSemana={dia}; " +
                $"Inicio={inicio.ToString(@"hh\:mm")}; Fin={fin.ToString(@"hh\:mm")}");
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static void AgregarParametrosBloque(
        SqlCommand cmd, CrearBloqueDisponibilidadViewModel form)
    {
        cmd.Parameters.Add("@EmpleadoId", SqlDbType.Int).Value = form.EmpleadoId;
        cmd.Parameters.Add("@DiaSemana", SqlDbType.Int).Value = form.DiaSemana;
        cmd.Parameters.Add("@HoraInicio", SqlDbType.Time).Value = form.HoraInicio!.Value;
        cmd.Parameters.Add("@HoraFin", SqlDbType.Time).Value = form.HoraFin!.Value;
    }

    private static async Task BloquearMedicoDiaAsync(
        SqlConnection cn, SqlTransaction tx, int empleadoId, int dia)
    {
        const string sql = @"
DECLARE @resultado INT;
EXEC @resultado = sys.sp_getapplock
    @Resource = @Resource,
    @LockMode = N'Exclusive',
    @LockOwner = N'Transaction',
    @LockTimeout = 10000;
SELECT @resultado;";
        using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.Add("@Resource", SqlDbType.NVarChar, 255).Value =
            $"Hospinomenal.Disponibilidad.{empleadoId}.{dia}";
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) < 0)
            throw new InvalidOperationException("No fue posible bloquear el horario para verificar solapamientos.");
    }

    private static async Task AuditarAsync(
        SqlConnection cn, SqlTransaction tx, int usuarioId, string accion, int bloqueId, string detalle)
    {
        const string sql = @"
INSERT INTO dbo.BitacoraAuditoria
    (UsuarioId, Accion, Tabla_Modificada, RegistroId, VNuevos, Objeto)
VALUES (@UsuarioId, @Accion, N'DisponibilidadMedicaSemanal', @RegistroId,
        @Detalle, N'DisponibilidadMedicaController');";
        using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
        cmd.Parameters.Add("@Accion", SqlDbType.NVarChar, 100).Value = accion;
        cmd.Parameters.Add("@RegistroId", SqlDbType.NVarChar, 100).Value = bloqueId.ToString();
        cmd.Parameters.Add("@Detalle", SqlDbType.NVarChar, -1).Value = detalle;
        await cmd.ExecuteNonQueryAsync();
    }
}
