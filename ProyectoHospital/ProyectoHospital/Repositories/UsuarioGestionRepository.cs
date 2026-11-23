using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.ViewModels;
using System.Data;

namespace ProyectoHospital.Repositories;

/// <summary>
/// Mantenimiento de usuarios y empleados. Las altas se realizan de forma atómica.
/// Nunca almacena contraseñas en texto plano ni las incorpora a la auditoría.
/// </summary>
public sealed class UsuarioGestionRepository
{
    private readonly string _connectionString;

    public UsuarioGestionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HospitalDB")
            ?? throw new InvalidOperationException("No se configuró HospitalDB.");
    }

    public async Task<List<UsuarioResumen>> ListarUsuariosAsync()
    {
        const string sql = @"
SELECT u.UsuarioId, u.Usuario, u.Activo, u.EmpleadoId,
       CASE WHEN e.EmpleadoId IS NULL THEN NULL
            ELSE CONCAT(e.Nombre, N' ', e.Apellidos) END AS Empleado,
       e.Puesto,
       COALESCE(STRING_AGG(r.Nombre_del_Rol, N', '), N'') AS Roles
FROM dbo.Usuarios AS u
LEFT JOIN dbo.Empleados AS e ON e.EmpleadoId = u.EmpleadoId
LEFT JOIN dbo.UsuarioRol AS ur ON ur.UsuarioId = u.UsuarioId
LEFT JOIN dbo.Roles AS r ON r.RolId = ur.RolId
GROUP BY u.UsuarioId, u.Usuario, u.Activo, u.EmpleadoId,
         e.EmpleadoId, e.Nombre, e.Apellidos, e.Puesto
ORDER BY u.Usuario;";

        var lista = new List<UsuarioResumen>();
        await using var conexion = new SqlConnection(_connectionString);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand(sql, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            lista.Add(new UsuarioResumen
            {
                UsuarioId = lector.GetInt32(0),
                Usuario = lector.GetString(1),
                Activo = lector.GetBoolean(2),
                EmpleadoId = lector.IsDBNull(3) ? null : lector.GetInt32(3),
                Empleado = lector.IsDBNull(4) ? null : lector.GetString(4),
                Puesto = lector.IsDBNull(5) ? null : lector.GetString(5),
                Roles = lector.GetString(6)
            });
        }
        return lista;
    }

    public async Task<List<RolUsuarioOpcion>> ListarRolesAsync()
    {
        const string sql = @"
SELECT RolId, Nombre_del_Rol
FROM dbo.Roles WHERE Activo = 1
ORDER BY Nombre_del_Rol;";

        var lista = new List<RolUsuarioOpcion>();
        await using var conexion = new SqlConnection(_connectionString);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand(sql, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            lista.Add(new RolUsuarioOpcion { RolId = lector.GetInt32(0), Nombre = lector.GetString(1) });
        return lista;
    }

    public async Task<RolUsuarioOpcion?> ObtenerRolAsync(int rolId)
    {
        const string sql = @"
SELECT RolId, Nombre_del_Rol
FROM dbo.Roles WHERE RolId = @RolId AND Activo = 1;";

        await using var conexion = new SqlConnection(_connectionString);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand(sql, conexion);
        comando.Parameters.Add("@RolId", SqlDbType.Int).Value = rolId;
        await using var lector = await comando.ExecuteReaderAsync();
        if (!await lector.ReadAsync()) return null;
        return new RolUsuarioOpcion { RolId = lector.GetInt32(0), Nombre = lector.GetString(1) };
    }

    public async Task<List<EmpleadoSinCuenta>> ListarEmpleadosSinCuentaAsync()
    {
        const string sql = @"
SELECT e.EmpleadoId, CONCAT(e.Nombre, N' ', e.Apellidos), e.Puesto
FROM dbo.Empleados AS e
WHERE e.Activo = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.Usuarios AS u WHERE u.EmpleadoId = e.EmpleadoId)
ORDER BY e.Nombre, e.Apellidos;";

        var lista = new List<EmpleadoSinCuenta>();
        await using var conexion = new SqlConnection(_connectionString);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand(sql, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            lista.Add(new EmpleadoSinCuenta
            {
                EmpleadoId = lector.GetInt32(0),
                NombreCompleto = lector.GetString(1),
                Puesto = lector.GetString(2)
            });
        return lista;
    }

    public async Task<int> CrearConEmpleadoAsync(
        CrearUsuarioViewModel modelo, string passwordHashIdentity, bool esMedico, int operadorId)
    {
        await using var conexion = new SqlConnection(_connectionString);
        await conexion.OpenAsync();
        using var transaccion = conexion.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            string rol = await ValidarRolYUsuarioAsync(
                conexion, transaccion, modelo.RolId, modelo.NombreUsuario);
            if (EsRolMedico(rol) && !esMedico)
                throw new InvalidOperationException("El rol Médico requiere un empleado médico.");

            const string sqlEmpleado = @"
INSERT INTO dbo.Empleados
    (Numero_Identificacion, Nombre, Apellidos, Telefono, Correo,
     Puesto, Especialidad, EsMedico, HoraEntrada, HoraSalida, DuracionCitaMin)
OUTPUT INSERTED.EmpleadoId
VALUES
    (@Identificacion, @Nombre, @Apellidos, @Telefono, @Correo,
     @Puesto, @Especialidad, @EsMedico, @HoraEntrada, @HoraSalida, @Duracion);";

            int empleadoId;
            using (var comando = new SqlCommand(sqlEmpleado, conexion, transaccion))
            {
                comando.Parameters.Add("@Identificacion", SqlDbType.NVarChar, 30).Value =
                    modelo.NumeroIdentificacion.Trim();
                comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = modelo.Nombre.Trim();
                comando.Parameters.Add("@Apellidos", SqlDbType.NVarChar, 150).Value = modelo.Apellidos.Trim();
                comando.Parameters.Add("@Telefono", SqlDbType.NVarChar, 20).Value = modelo.Telefono.Trim();
                comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 150).Value =
                    (object?)modelo.Correo?.Trim() ?? DBNull.Value;
                comando.Parameters.Add("@Puesto", SqlDbType.NVarChar, 80).Value = modelo.Puesto.Trim();
                comando.Parameters.Add("@Especialidad", SqlDbType.NVarChar, 100).Value =
                    esMedico ? (object?)modelo.Especialidad?.Trim() ?? DBNull.Value : DBNull.Value;
                comando.Parameters.Add("@EsMedico", SqlDbType.Bit).Value = esMedico;
                comando.Parameters.Add("@HoraEntrada", SqlDbType.Time).Value =
                    esMedico ? (object?)modelo.HoraEntrada ?? DBNull.Value : DBNull.Value;
                comando.Parameters.Add("@HoraSalida", SqlDbType.Time).Value =
                    esMedico ? (object?)modelo.HoraSalida ?? DBNull.Value : DBNull.Value;
                comando.Parameters.Add("@Duracion", SqlDbType.SmallInt).Value =
                    esMedico ? (object?)modelo.DuracionCitaMin ?? DBNull.Value : DBNull.Value;
                empleadoId = Convert.ToInt32(await comando.ExecuteScalarAsync());
            }

            int usuarioId = await InsertarCuentaAsync(
                conexion, transaccion, modelo.NombreUsuario, passwordHashIdentity, empleadoId, modelo.RolId);

            await RegistrarAuditoriaAsync(conexion, transaccion, operadorId, "INSERT",
                usuarioId, $"Cuenta {modelo.NombreUsuario.Trim()}; EmpleadoId={empleadoId}; Rol={rol}; nuevo empleado");
            transaccion.Commit();
            return usuarioId;
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    public async Task<int> VincularAsync(
        VincularUsuarioViewModel modelo, string passwordHashIdentity, int operadorId)
    {
        await using var conexion = new SqlConnection(_connectionString);
        await conexion.OpenAsync();
        using var transaccion = conexion.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            string rol = await ValidarRolYUsuarioAsync(
                conexion, transaccion, modelo.RolId, modelo.NombreUsuario);

            const string sqlEmpleado = @"
SELECT EsMedico
FROM dbo.Empleados WITH (UPDLOCK, HOLDLOCK)
WHERE EmpleadoId = @EmpleadoId AND Activo = 1;";

            bool esMedico;
            using (var comando = new SqlCommand(sqlEmpleado, conexion, transaccion))
            {
                comando.Parameters.Add("@EmpleadoId", SqlDbType.Int).Value = modelo.EmpleadoId;
                var resultado = await comando.ExecuteScalarAsync();
                if (resultado is null || resultado == DBNull.Value)
                    throw new InvalidOperationException("No se encontró un empleado activo.");
                esMedico = (bool)resultado;
            }

            if (EsRolMedico(rol) && !esMedico)
                throw new InvalidOperationException("El rol Médico requiere un empleado marcado como médico.");

            const string sqlVinculacion = @"
SELECT COUNT(1) FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK)
WHERE EmpleadoId = @EmpleadoId;";
            using (var comando = new SqlCommand(sqlVinculacion, conexion, transaccion))
            {
                comando.Parameters.Add("@EmpleadoId", SqlDbType.Int).Value = modelo.EmpleadoId;
                if (Convert.ToInt32(await comando.ExecuteScalarAsync()) > 0)
                    throw new InvalidOperationException("Este empleado ya tiene una cuenta vinculada.");
            }

            int usuarioId = await InsertarCuentaAsync(
                conexion, transaccion, modelo.NombreUsuario, passwordHashIdentity,
                modelo.EmpleadoId, modelo.RolId);

            await RegistrarAuditoriaAsync(conexion, transaccion, operadorId, "INSERT",
                usuarioId, $"Cuenta {modelo.NombreUsuario.Trim()}; EmpleadoId={modelo.EmpleadoId}; Rol={rol}; empleado existente");
            transaccion.Commit();
            return usuarioId;
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    public async Task CambiarEstadoAsync(int usuarioId, bool activar, int operadorId)
    {
        await using var conexion = new SqlConnection(_connectionString);
        await conexion.OpenAsync();
        using var transaccion = conexion.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            const string sqlActual = @"
SELECT Activo FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK)
WHERE UsuarioId = @UsuarioId;";
            bool estadoAnterior;
            using (var comando = new SqlCommand(sqlActual, conexion, transaccion))
            {
                comando.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
                var resultado = await comando.ExecuteScalarAsync();
                if (resultado is null || resultado == DBNull.Value)
                    throw new InvalidOperationException("La cuenta solicitada no existe.");
                estadoAnterior = (bool)resultado;
            }

            if (!activar && usuarioId == operadorId)
                throw new InvalidOperationException("No puede desactivar su propia cuenta.");

            if (!activar)
            {
                const string sqlAdministrador = @"
SELECT COUNT(1)
FROM dbo.UsuarioRol ur
JOIN dbo.Roles r ON r.RolId = ur.RolId
WHERE ur.UsuarioId = @UsuarioId AND r.Nombre_del_Rol = N'Administrador' AND r.Activo = 1;";
                using var comando = new SqlCommand(sqlAdministrador, conexion, transaccion);
                comando.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
                if (Convert.ToInt32(await comando.ExecuteScalarAsync()) > 0)
                {
                    const string sqlAdministradores = @"
SELECT COUNT(DISTINCT u.UsuarioId)
FROM dbo.Usuarios u
JOIN dbo.UsuarioRol ur ON ur.UsuarioId = u.UsuarioId
JOIN dbo.Roles r ON r.RolId = ur.RolId
WHERE u.Activo = 1 AND r.Activo = 1 AND r.Nombre_del_Rol = N'Administrador';";
                    using var contador = new SqlCommand(sqlAdministradores, conexion, transaccion);
                    if (Convert.ToInt32(await contador.ExecuteScalarAsync()) <= 1)
                        throw new InvalidOperationException("Debe conservarse al menos un administrador activo.");
                }
            }

            if (estadoAnterior != activar)
            {
                const string sqlEstado = @"
UPDATE dbo.Usuarios SET Activo = @Activo, UpdatedAt = SYSUTCDATETIME()
WHERE UsuarioId = @UsuarioId;";
                using (var comando = new SqlCommand(sqlEstado, conexion, transaccion))
                {
                    comando.Parameters.Add("@Activo", SqlDbType.Bit).Value = activar;
                    comando.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
                    await comando.ExecuteNonQueryAsync();
                }

                await RegistrarAuditoriaAsync(conexion, transaccion, operadorId, "UPDATE",
                    usuarioId, $"Estado de cuenta: {(estadoAnterior ? "Activo" : "Inactivo")} -> {(activar ? "Activo" : "Inactivo")}");
            }

            transaccion.Commit();
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    private static async Task<string> ValidarRolYUsuarioAsync(
        SqlConnection conexion, SqlTransaction tx, int rolId, string usuario)
    {
        const string sqlRol = @"
SELECT Nombre_del_Rol FROM dbo.Roles WITH (HOLDLOCK)
WHERE RolId = @RolId AND Activo = 1;";
        string rol;
        using (var comando = new SqlCommand(sqlRol, conexion, tx))
        {
            comando.Parameters.Add("@RolId", SqlDbType.Int).Value = rolId;
            rol = (string?)await comando.ExecuteScalarAsync()
                ?? throw new InvalidOperationException("El rol seleccionado no está disponible.");
        }

        const string sqlUsuario = @"
SELECT COUNT(1) FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK)
WHERE Usuario = @Usuario;";
        using (var comando = new SqlCommand(sqlUsuario, conexion, tx))
        {
            comando.Parameters.Add("@Usuario", SqlDbType.NVarChar, 50).Value = usuario.Trim();
            if (Convert.ToInt32(await comando.ExecuteScalarAsync()) > 0)
                throw new InvalidOperationException("El nombre de usuario ya está registrado.");
        }
        return rol;
    }

    private static bool EsRolMedico(string rol) =>
        string.Equals(rol, "Medico", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(rol, "Médico", StringComparison.OrdinalIgnoreCase);

    private static async Task<int> InsertarCuentaAsync(
        SqlConnection conexion, SqlTransaction tx, string usuario,
        string passwordHashIdentity, int empleadoId, int rolId)
    {
        const string sqlCuenta = @"
INSERT INTO dbo.Usuarios
    (Usuario, PasswordHash, PasswordSalt, PasswordHashIdentity, EmpleadoId, Activo)
OUTPUT INSERTED.UsuarioId
VALUES (@Usuario, NULL, NULL, @PasswordHashIdentity, @EmpleadoId, 1);";

        int usuarioId;
        using (var comando = new SqlCommand(sqlCuenta, conexion, tx))
        {
            comando.Parameters.Add("@Usuario", SqlDbType.NVarChar, 50).Value = usuario.Trim();
            comando.Parameters.Add("@PasswordHashIdentity", SqlDbType.NVarChar, 256).Value = passwordHashIdentity;
            comando.Parameters.Add("@EmpleadoId", SqlDbType.Int).Value = empleadoId;
            usuarioId = Convert.ToInt32(await comando.ExecuteScalarAsync());
        }

        const string sqlRol = @"
INSERT INTO dbo.UsuarioRol (UsuarioId, RolId)
VALUES (@UsuarioId, @RolId);";
        using (var comando = new SqlCommand(sqlRol, conexion, tx))
        {
            comando.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            comando.Parameters.Add("@RolId", SqlDbType.Int).Value = rolId;
            await comando.ExecuteNonQueryAsync();
        }
        return usuarioId;
    }

    private static async Task RegistrarAuditoriaAsync(
        SqlConnection conexion, SqlTransaction tx, int operadorId, string accion,
        int usuarioId, string detalle)
    {
        const string sql = @"
INSERT INTO dbo.BitacoraAuditoria
    (UsuarioId, Accion, Tabla_Modificada, RegistroId, VNuevos, Objeto)
VALUES (@Operador, @Accion, N'Usuarios', @RegistroId, @Detalle, N'UsuariosController');";
        using var comando = new SqlCommand(sql, conexion, tx);
        comando.Parameters.Add("@Operador", SqlDbType.Int).Value = operadorId;
        comando.Parameters.Add("@Accion", SqlDbType.NVarChar, 100).Value = accion;
        comando.Parameters.Add("@RegistroId", SqlDbType.NVarChar, 100).Value = usuarioId.ToString();
        comando.Parameters.Add("@Detalle", SqlDbType.NVarChar, -1).Value = detalle;
        await comando.ExecuteNonQueryAsync();
    }
}
