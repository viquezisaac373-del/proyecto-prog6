using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.Enums;
using ProyectoHospital.Models.ViewModels;
using System.Data;

namespace ProyectoHospital.Repositories
{
    public class BitacoraRepository
    {
        private const string FiltroFecha = @"
                (@Desde IS NULL OR b.FechaHora >= @Desde)
                AND (@Hasta IS NULL OR b.FechaHora < DATEADD(DAY, 1, @Hasta))";

        private const string SqlAccesos = @"
            SELECT TOP (@Maximo)
                v.AccesoId AS Id,
                DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME(0)), v.Hora),
                        CAST(v.Fecha AS DATETIME2(0))) AS FechaHora,
                v.Accion,
                TRY_CONVERT(INT, v.UsuarioId) AS UsuarioId,
                v.NombreUsuario AS Usuario,
                v.Resultado,
                v.Mensaje AS Descripcion,
                v.Ip,
                CAST(NULL AS NVARCHAR(MAX)) AS ValoresAnteriores,
                CAST(NULL AS NVARCHAR(MAX)) AS ValoresNuevos,
                CAST(NULL AS NVARCHAR(100)) AS Tabla,
                CAST(NULL AS NVARCHAR(100)) AS RegistroAfectado,
                CAST(NULL AS NVARCHAR(100)) AS Objeto,
                CAST(NULL AS INT) AS NumeroError,
                CAST(NULL AS INT) AS Severidad,
                CAST(NULL AS INT) AS Linea,
                CAST(NULL AS INT) AS PacienteId,
                CAST(NULL AS NVARCHAR(240)) AS Paciente,
                CAST(NULL AS INT) AS CitaId
            FROM dbo.vw_BitacoraAccesos v
            WHERE (@Desde IS NULL OR v.Fecha >= @Desde)
                AND (@Hasta IS NULL OR v.Fecha <= @Hasta)
                AND (@Usuario IS NULL OR v.NombreUsuario LIKE @Usuario ESCAPE N'\')
                AND (@Accion IS NULL OR v.Accion = @Accion)
            ORDER BY v.AccesoId DESC;";

        private const string SqlAuditoria = @"
            SELECT TOP (@Maximo)
                b.AuditoriaId AS Id,
                b.FechaHora,
                b.Accion,
                b.UsuarioId,
                u.Usuario,
                N'Exitoso' AS Resultado,
                CONCAT(b.Accion, N' en ', ISNULL(b.Tabla_Modificada, N'tabla no indicada'),
                       N', registro ', ISNULL(b.RegistroId, N'no indicado')) AS Descripcion,
                CAST(NULL AS NVARCHAR(45)) AS Ip,
                b.VAnteriores AS ValoresAnteriores,
                b.VNuevos AS ValoresNuevos,
                b.Tabla_Modificada AS Tabla,
                b.RegistroId AS RegistroAfectado,
                b.Objeto,
                CAST(NULL AS INT) AS NumeroError,
                CAST(NULL AS INT) AS Severidad,
                CAST(NULL AS INT) AS Linea,
                CAST(NULL AS INT) AS PacienteId,
                CAST(NULL AS NVARCHAR(240)) AS Paciente,
                CAST(NULL AS INT) AS CitaId
            FROM dbo.BitacoraAuditoria b
            LEFT JOIN dbo.Usuarios u ON u.UsuarioId = b.UsuarioId
            WHERE" + FiltroFecha + @"
                AND (@Usuario IS NULL OR u.Usuario LIKE @Usuario ESCAPE N'\')
                AND (@Accion IS NULL OR b.Accion = @Accion)
            ORDER BY b.FechaHora DESC, b.AuditoriaId DESC;";

        private const string SqlErrores = @"
            SELECT TOP (@Maximo)
                b.ErrorId AS Id,
                b.FechaHora,
                b.Accion,
                b.UsuarioId,
                u.Usuario,
                b.Resultado,
                b.Mensaje AS Descripcion,
                CAST(NULL AS NVARCHAR(45)) AS Ip,
                CAST(NULL AS NVARCHAR(MAX)) AS ValoresAnteriores,
                CAST(NULL AS NVARCHAR(MAX)) AS ValoresNuevos,
                CAST(NULL AS NVARCHAR(100)) AS Tabla,
                CAST(NULL AS NVARCHAR(100)) AS RegistroAfectado,
                b.Objeto,
                b.NumeroError,
                b.Severidad,
                b.Linea,
                CAST(NULL AS INT) AS PacienteId,
                CAST(NULL AS NVARCHAR(240)) AS Paciente,
                CAST(NULL AS INT) AS CitaId
            FROM dbo.BitacoraErrores b
            LEFT JOIN dbo.Usuarios u ON u.UsuarioId = b.UsuarioId
            WHERE" + FiltroFecha + @"
                AND (@Usuario IS NULL OR u.Usuario LIKE @Usuario ESCAPE N'\')
                AND (@Accion IS NULL OR b.Accion = @Accion)
            ORDER BY b.FechaHora DESC, b.ErrorId DESC;";

        private const string SqlNotificaciones = @"
            SELECT TOP (@Maximo)
                b.NotificacionId AS Id,
                b.FechaHora,
                N'Notificación' AS Accion,
                CAST(NULL AS INT) AS UsuarioId,
                CAST(NULL AS NVARCHAR(50)) AS Usuario,
                b.EstadoEnvio AS Resultado,
                b.Mensaje AS Descripcion,
                CAST(NULL AS NVARCHAR(45)) AS Ip,
                CAST(NULL AS NVARCHAR(MAX)) AS ValoresAnteriores,
                CAST(NULL AS NVARCHAR(MAX)) AS ValoresNuevos,
                CAST(NULL AS NVARCHAR(100)) AS Tabla,
                CAST(NULL AS NVARCHAR(100)) AS RegistroAfectado,
                CAST(NULL AS NVARCHAR(100)) AS Objeto,
                CAST(NULL AS INT) AS NumeroError,
                CAST(NULL AS INT) AS Severidad,
                CAST(NULL AS INT) AS Linea,
                b.PacienteId,
                CASE WHEN p.PacienteId IS NULL THEN NULL
                     ELSE CONCAT(p.Nombre, N' ', p.Apellidos) END AS Paciente,
                b.CitaId
            FROM dbo.BitacoraNotificaciones b
            LEFT JOIN dbo.Pacientes p ON p.PacienteId = b.PacienteId
            WHERE" + FiltroFecha + @"
                AND (@Usuario IS NULL OR CONCAT(p.Nombre, N' ', p.Apellidos) LIKE @Usuario ESCAPE N'\')
                AND (@Accion IS NULL OR b.EstadoEnvio = @Accion)
            ORDER BY b.FechaHora DESC, b.NotificacionId DESC;";

        private readonly string _connectionString;

        public BitacoraRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("HospitalDB")
                ?? throw new InvalidOperationException(
                    "No se configuró la conexión HospitalDB.");
        }

        public async Task<List<RegistroBitacora>> ConsultarAsync(TipoBitacora tipo, FiltroBitacora filtro)
        {
            var registros = new List<RegistroBitacora>();

            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            await using var comando = new SqlCommand(ObtenerSql(tipo), conexion);
            comando.Parameters.Add("@Maximo", SqlDbType.Int).Value = Math.Clamp(filtro.Maximo, 1, 1000);
            comando.Parameters.Add("@Desde", SqlDbType.Date).Value = (object?)filtro.Desde?.Date ?? DBNull.Value;
            comando.Parameters.Add("@Hasta", SqlDbType.Date).Value = (object?)filtro.Hasta?.Date ?? DBNull.Value;
            comando.Parameters.Add("@Usuario", SqlDbType.NVarChar, 260).Value = (object?)PatronBusqueda(filtro.Usuario) ?? DBNull.Value;
            comando.Parameters.Add("@Accion", SqlDbType.NVarChar, 100).Value = (object?)Limpiar(filtro.Accion) ?? DBNull.Value;

            await using var lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                registros.Add(new RegistroBitacora
                {
                    Id = lector.GetInt64(0),
                    FechaHora = lector.GetDateTime(1),
                    Accion = lector.GetString(2),
                    UsuarioId = Entero(lector, 3),
                    Usuario = Texto(lector, 4),
                    Resultado = lector.GetString(5),
                    Descripcion = Texto(lector, 6) ?? string.Empty,
                    Ip = Texto(lector, 7),
                    ValoresAnteriores = Texto(lector, 8),
                    ValoresNuevos = Texto(lector, 9),
                    Tabla = Texto(lector, 10),
                    RegistroAfectado = Texto(lector, 11),
                    Objeto = Texto(lector, 12),
                    NumeroError = Entero(lector, 13),
                    Severidad = Entero(lector, 14),
                    Linea = Entero(lector, 15),
                    PacienteId = Entero(lector, 16),
                    Paciente = Texto(lector, 17),
                    CitaId = Entero(lector, 18)
                });
            }

            return registros;
        }

        public async Task<List<string>> ObtenerAccionesAsync(TipoBitacora tipo)
        {
            string sql = tipo switch
            {
                TipoBitacora.Accesos => "SELECT DISTINCT Accion FROM dbo.vw_BitacoraAccesos ORDER BY Accion;",
                TipoBitacora.Auditoria => "SELECT DISTINCT Accion FROM dbo.BitacoraAuditoria ORDER BY Accion;",
                TipoBitacora.Errores => "SELECT DISTINCT Accion FROM dbo.BitacoraErrores ORDER BY Accion;",
                TipoBitacora.Notificaciones => "SELECT DISTINCT EstadoEnvio FROM dbo.BitacoraNotificaciones ORDER BY EstadoEnvio;",
                _ => throw new ArgumentOutOfRangeException(nameof(tipo))
            };

            var acciones = new List<string>();

            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            await using var comando = new SqlCommand(sql, conexion);
            await using var lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                acciones.Add(lector.GetString(0));
            }

            return acciones;
        }

        private static string ObtenerSql(TipoBitacora tipo) => tipo switch
        {
            TipoBitacora.Accesos => SqlAccesos,
            TipoBitacora.Auditoria => SqlAuditoria,
            TipoBitacora.Errores => SqlErrores,
            TipoBitacora.Notificaciones => SqlNotificaciones,
            _ => throw new ArgumentOutOfRangeException(nameof(tipo))
        };

        private static string? Texto(SqlDataReader lector, int indice) =>
            lector.IsDBNull(indice) ? null : lector.GetString(indice);

        private static int? Entero(SqlDataReader lector, int indice) =>
            lector.IsDBNull(indice) ? null : lector.GetInt32(indice);

        private static string? Limpiar(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        private static string? PatronBusqueda(string? valor)
        {
            string? texto = Limpiar(valor);

            if (texto is null)
                return null;

            string escapado = texto
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_")
                .Replace("[", "\\[");

            return $"%{escapado}%";
        }
    }
}
