using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.Entities;
using System.Data;

namespace ProyectoHospital.Repositories
{
    public class UsuarioRepository
    {
        public const int MaximoIntentos = 5;
        public const int MinutosBloqueo = 15;

        private readonly string _connectionString;

        public UsuarioRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("HospitalDB")
                ?? throw new InvalidOperationException(
                    "No se configuró la conexión HospitalDB.");
        }

        public async Task<UsuarioRecord?> ObtenerPorUsuarioAsync(string usuario)
        {
            const string sql = @"
                SELECT UsuarioId, Usuario, PasswordHash, PasswordSalt, Activo,
                       CAST(CASE WHEN BloqueadoHasta > SYSDATETIME() THEN 1 ELSE 0 END AS BIT) AS Bloqueado
                FROM dbo.Usuarios
                WHERE Usuario = @Usuario;";

            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            await using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.Add("@Usuario", SqlDbType.NVarChar, 50).Value = usuario;

            await using var lector = await comando.ExecuteReaderAsync();

            if (!await lector.ReadAsync())
                return null;

            return new UsuarioRecord
            {
                UsuarioId = lector.GetInt32(0),
                NombreUsuario = lector.GetString(1),
                PasswordHash = (byte[])lector["PasswordHash"],
                PasswordSalt = (byte[])lector["PasswordSalt"],
                Activo = lector.GetBoolean(4),
                Bloqueado = lector.GetBoolean(5)
            };
        }

        public async Task<List<string>> ObtenerRolesAsync(int usuarioId)
        {
            var roles = new List<string>();

            const string sql = @"
                SELECT r.Nombre_del_Rol
                FROM dbo.UsuarioRol ur
                JOIN dbo.Roles r ON r.RolId = ur.RolId
                WHERE ur.UsuarioId = @UsuarioId AND r.Activo = 1
                ORDER BY r.Nombre_del_Rol;";

            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            await using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;

            await using var lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                roles.Add(lector.GetString(0));
            }

            return roles;
        }

        public async Task<bool> RegistrarIntentoFallidoAsync(int usuarioId)
        {
            const string sql = @"
                UPDATE dbo.Usuarios
                SET BloqueadoHasta = CASE WHEN IntentosFallidos + 1 >= @Maximo
                                          THEN DATEADD(MINUTE, @Minutos, SYSDATETIME())
                                          ELSE BloqueadoHasta END,
                    IntentosFallidos = CASE WHEN IntentosFallidos + 1 >= @Maximo
                                            THEN 0
                                            ELSE IntentosFallidos + 1 END,
                    UpdatedAt = SYSUTCDATETIME()
                OUTPUT CAST(CASE WHEN inserted.BloqueadoHasta > SYSDATETIME() THEN 1 ELSE 0 END AS BIT)
                WHERE UsuarioId = @UsuarioId;";

            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            await using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            comando.Parameters.Add("@Maximo", SqlDbType.Int).Value = MaximoIntentos;
            comando.Parameters.Add("@Minutos", SqlDbType.Int).Value = MinutosBloqueo;

            var resultado = await comando.ExecuteScalarAsync();

            return resultado is bool bloqueado && bloqueado;
        }

        public async Task ReiniciarIntentosAsync(int usuarioId)
        {
            const string sql = @"
                UPDATE dbo.Usuarios
                SET IntentosFallidos = 0,
                    BloqueadoHasta = NULL,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UsuarioId = @UsuarioId;";

            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            await using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;

            await comando.ExecuteNonQueryAsync();
        }

        public async Task RegistrarAccesoAsync(
            int? usuarioId, string nombreUsuario, string? ip,
            string accion, string resultado, string? mensaje)
        {
            const string sql = @"
                INSERT INTO dbo.BitacoraAccesos
                    (UsuarioId, NombreUsuario, Ip, Accion, Resultado, Mensaje)
                VALUES
                    (@UsuarioId, @NombreUsuario, @Ip, @Accion, @Resultado, @Mensaje);";

            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            await using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = (object?)usuarioId ?? DBNull.Value;
            comando.Parameters.Add("@NombreUsuario", SqlDbType.NVarChar, 50).Value = Recortar(nombreUsuario, 50);
            comando.Parameters.Add("@Ip", SqlDbType.NVarChar, 45).Value = (object?)Recortar(ip, 45) ?? DBNull.Value;
            comando.Parameters.Add("@Accion", SqlDbType.NVarChar, 100).Value = accion;
            comando.Parameters.Add("@Resultado", SqlDbType.NVarChar, 30).Value = resultado;
            comando.Parameters.Add("@Mensaje", SqlDbType.NVarChar, 500).Value = (object?)Recortar(mensaje, 500) ?? DBNull.Value;

            await comando.ExecuteNonQueryAsync();
        }

        public async Task RegistrarErrorAsync(int? usuarioId, string accion, Exception ex)
        {
            const string sql = @"
                INSERT INTO dbo.BitacoraErrores
                    (UsuarioId, Accion, Objeto, NumeroError, Severidad, Linea, Mensaje)
                VALUES
                    (@UsuarioId, @Accion, @Objeto, @Numero, @Severidad, @Linea, @Mensaje);";

            var sqlEx = ex as SqlException;

            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            await using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = (object?)usuarioId ?? DBNull.Value;
            comando.Parameters.Add("@Accion", SqlDbType.NVarChar, 100).Value = accion;
            comando.Parameters.Add("@Objeto", SqlDbType.NVarChar, 100).Value = "AccountController";
            comando.Parameters.Add("@Numero", SqlDbType.Int).Value = (object?)sqlEx?.Number ?? DBNull.Value;
            comando.Parameters.Add("@Severidad", SqlDbType.Int).Value = (object?)sqlEx?.Class ?? DBNull.Value;
            comando.Parameters.Add("@Linea", SqlDbType.Int).Value = (object?)sqlEx?.LineNumber ?? DBNull.Value;
            comando.Parameters.Add("@Mensaje", SqlDbType.NVarChar, 2000).Value = Recortar(ex.Message, 2000) ?? string.Empty;

            await comando.ExecuteNonQueryAsync();
        }

        private static string? Recortar(string? valor, int maximo)
        {
            if (valor == null)
                return null;

            return valor.Length <= maximo ? valor : valor.Substring(0, maximo);
        }
    }
}
