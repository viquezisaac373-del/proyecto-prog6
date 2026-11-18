using Microsoft.Data.SqlClient;
using System.Data;

namespace ProyectoHospital.Repositories
{
    /// <summary>
    /// Gestiona la identidad del usuario en una conexión SQL.
    /// Permite que los triggers de auditoría identifiquen
    /// al operador responsable de las modificaciones.
    /// </summary>
    internal static class SqlAuditContext
    {
        /// <summary>
        /// Establece el UsuarioId autenticado en SESSION_CONTEXT.
        /// La conexión debe estar abierta.
        /// </summary>
        public static async Task EstablecerUsuarioAsync(
            SqlConnection conexion,
            int usuarioId)
        {
            if (usuarioId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(usuarioId),
                    "El identificador del usuario debe ser válido.");
            }

            if (conexion.State != ConnectionState.Open)
            {
                throw new InvalidOperationException(
                    "La conexión SQL debe estar abierta.");
            }

            await using var comando = conexion.CreateCommand();

            comando.CommandText = @"
                EXEC sys.sp_set_session_context
                    @key = N'UsuarioId',
                    @value = @UsuarioId;";

            comando.Parameters.Add(
                "@UsuarioId",
                SqlDbType.Int).Value = usuarioId;

            await comando.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Limpia el identificador almacenado en SESSION_CONTEXT.
        /// Debe ejecutarse al finalizar la operación.
        /// </summary>
        public static async Task LimpiarUsuarioAsync(
            SqlConnection conexion)
        {
            if (conexion.State != ConnectionState.Open)
            {
                return;
            }

            await using var comando = conexion.CreateCommand();

            comando.CommandText = @"
                EXEC sys.sp_set_session_context
                    @key = N'UsuarioId',
                    @value = NULL;";

            await comando.ExecuteNonQueryAsync();
        }
    }
}