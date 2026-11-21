using System.Data;
using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.ViewModels;

namespace ProyectoHospital.Repositories
{
    public sealed record OpcionRegistro(int Id, string Etiqueta);
    public sealed record RecienNacidoPendiente(
        int PacienteId, int NacimientoId, string NombreCompleto,
        DateTime FechaHoraNacimiento, string NombreMadre);

    public sealed record RecienNacidoResumen(
        int PacienteId, int NacimientoId, string NombreCompleto,
        string NombreMadre, DateTime FechaHoraNacimiento,
        string? NumeroIdentificacion, bool NacimientoActivo, bool PacienteActivo);

    /// <summary>
    /// Acceso a datos del flujo de recién nacidos.
    /// Las altas y asignaciones de identificación usan procedimientos SQL existentes.
    /// </summary>
    public class RecienNacidoRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<RecienNacidoRepository> _logger;

        public RecienNacidoRepository(
            IConfiguration configuration,
            ILogger<RecienNacidoRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("HospitalDB")
                ?? throw new InvalidOperationException("No se configuró la conexión HospitalDB.");
            _logger = logger;
        }

        public async Task<List<OpcionRegistro>> ListarMadresAsync()
        {
            const string sql = @"
                SELECT PacienteId,
                       CONCAT(Nombre, N' ', Apellidos, N' (',
                              COALESCE(Numero_Identificacion, N'Sin identificación'), N')')
                FROM dbo.Pacientes
                WHERE Activo = 1
                ORDER BY Apellidos, Nombre, PacienteId;";

            var opciones = new List<OpcionRegistro>();
            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();
            await using var comando = new SqlCommand(sql, conexion);
            await using var lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
                opciones.Add(new OpcionRegistro(lector.GetInt32(0), lector.GetString(1)));

            return opciones;
        }

        public async Task<List<OpcionRegistro>> ListarMedicosAsync()
        {
            const string sql = @"
                SELECT EmpleadoId,
                       CONCAT(Nombre, N' ', Apellidos,
                              CASE WHEN NULLIF(Especialidad, N'') IS NOT NULL
                                   THEN CONCAT(N' - ', Especialidad) ELSE N'' END)
                FROM dbo.Empleados
                WHERE Activo = 1 AND EsMedico = 1
                ORDER BY Apellidos, Nombre, EmpleadoId;";

            var opciones = new List<OpcionRegistro>();
            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();
            await using var comando = new SqlCommand(sql, conexion);
            await using var lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
                opciones.Add(new OpcionRegistro(lector.GetInt32(0), lector.GetString(1)));

            return opciones;
        }

        /// <summary>
        /// Incluye nacimientos activos y anulados, con o sin identificación.
        /// Anular un nacimiento no elimina su expediente ni lo oculta del historial.
        /// </summary>
        public async Task<List<RecienNacidoResumen>> ListarTodosAsync()
        {
            const string sql = @"
                SELECT b.PacienteId, n.NacimientoId,
                       CONCAT(b.Nombre, N' ', b.Apellidos),
                       CONCAT(m.Nombre, N' ', m.Apellidos),
                       n.Fecha_Hora_Nacimiento,
                       b.Numero_Identificacion,
                       n.Activo,
                       b.Activo
                FROM dbo.Nacimientos AS n
                INNER JOIN dbo.Pacientes AS b ON b.PacienteId = n.BebePacienteId
                INNER JOIN dbo.Pacientes AS m ON m.PacienteId = n.MadrePacienteId
                ORDER BY n.Fecha_Hora_Nacimiento DESC, n.NacimientoId DESC;";

            var nacimientos = new List<RecienNacidoResumen>();
            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();
            await using var comando = new SqlCommand(sql, conexion);
            await using var lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
                nacimientos.Add(new RecienNacidoResumen(
                    lector.GetInt32(0),
                    lector.GetInt32(1),
                    lector.GetString(2),
                    lector.GetString(3),
                    lector.GetDateTime(4),
                    lector.IsDBNull(5) ? null : lector.GetString(5),
                    lector.GetBoolean(6),
                    lector.GetBoolean(7)));

            return nacimientos;
        }

        public async Task<List<RecienNacidoPendiente>> ListarPendientesAsync()
        {
            const string sql = @"
                SELECT b.PacienteId, n.NacimientoId,
                       CONCAT(b.Nombre, N' ', b.Apellidos),
                       n.Fecha_Hora_Nacimiento,
                       CONCAT(m.Nombre, N' ', m.Apellidos)
                FROM dbo.Nacimientos AS n
                INNER JOIN dbo.Pacientes AS b ON b.PacienteId = n.BebePacienteId
                INNER JOIN dbo.Pacientes AS m ON m.PacienteId = n.MadrePacienteId
                WHERE n.Activo = 1 AND b.Activo = 1
                  AND b.Numero_Identificacion IS NULL
                ORDER BY n.Fecha_Hora_Nacimiento DESC, n.NacimientoId DESC;";

            var pendientes = new List<RecienNacidoPendiente>();
            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();
            await using var comando = new SqlCommand(sql, conexion);
            await using var lector = await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                pendientes.Add(new RecienNacidoPendiente(
                    lector.GetInt32(0),
                    lector.GetInt32(1),
                    lector.GetString(2),
                    lector.GetDateTime(3),
                    lector.GetString(4)));
            }

            return pendientes;
        }

        public async Task<RecienNacidoPendiente?> ObtenerPendienteAsync(int pacienteId)
        {
            var pendientes = await ListarPendientesAsync();
            return pendientes.FirstOrDefault(p => p.PacienteId == pacienteId);
        }

        public Task<(int PacienteId, int NacimientoId)> RegistrarAsync(
            RecienNacidoFormViewModel modelo, int usuarioId)
        {
            if (!modelo.FechaHoraNacimiento.HasValue)
                throw new ArgumentException("La fecha y hora del nacimiento son obligatorias.");

            return EjecutarConAuditoriaAsync(usuarioId, async conexion =>
            {
                await using var comando =
                    new SqlCommand("dbo.usp_RegistrarRecienNacido", conexion);
                comando.CommandType = CommandType.StoredProcedure;

                comando.Parameters.Add("@MadrePacienteId", SqlDbType.Int).Value =
                    modelo.MadrePacienteId;
                comando.Parameters.Add("@EmpleadoId", SqlDbType.Int).Value =
                    modelo.EmpleadoId;
                comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value =
                    modelo.Nombre.Trim();
                comando.Parameters.Add("@Apellidos", SqlDbType.NVarChar, 130).Value =
                    modelo.Apellidos.Trim();
                comando.Parameters.Add("@Genero", SqlDbType.NVarChar, 15).Value =
                    modelo.Genero.Trim();
                comando.Parameters.Add("@NumeroTelefono", SqlDbType.NVarChar, 20).Value =
                    modelo.NumeroTelefono.Trim();
                comando.Parameters.Add("@Direccion", SqlDbType.NVarChar, 250).Value =
                    modelo.Direccion.Trim();
                comando.Parameters.Add("@ContactoEmergencia", SqlDbType.NVarChar, 100).Value =
                    modelo.ContactoEmergencia.Trim();
                comando.Parameters.Add("@TelefonoEmergencia", SqlDbType.NVarChar, 20).Value =
                    modelo.TelefonoEmergencia.Trim();
                comando.Parameters.Add("@FechaHoraNacimiento", SqlDbType.DateTime2).Value =
                    modelo.FechaHoraNacimiento.Value;
                comando.Parameters.Add("@TipoParto", SqlDbType.NVarChar, 30).Value =
                    modelo.TipoParto;

                comando.Parameters.Add("@SemanasGestacion", SqlDbType.TinyInt).Value =
                    (object?)modelo.SemanasGestacion ?? DBNull.Value;
                comando.Parameters.Add("@PesoEnGramos", SqlDbType.Int).Value =
                    (object?)modelo.PesoEnGramos ?? DBNull.Value;
                var talla = comando.Parameters.Add("@TallaCentimetros", SqlDbType.Decimal);
                talla.Precision = 6;
                talla.Scale = 2;
                talla.Value = (object?)modelo.TallaCentimetros ?? DBNull.Value;
                comando.Parameters.Add("@Apgar1Min", SqlDbType.TinyInt).Value =
                    (object?)modelo.Apgar1Min ?? DBNull.Value;
                comando.Parameters.Add("@Apgar5Min", SqlDbType.TinyInt).Value =
                    (object?)modelo.Apgar5Min ?? DBNull.Value;
                comando.Parameters.Add("@Respiracion", SqlDbType.NVarChar, 100).Value =
                    TextoOpcional(modelo.Respiracion);
                comando.Parameters.Add("@EstadoSalud", SqlDbType.NVarChar, 150).Value =
                    TextoOpcional(modelo.EstadoSalud);
                comando.Parameters.Add("@Malformaciones", SqlDbType.NVarChar, 400).Value =
                    TextoOpcional(modelo.Malformaciones);
                comando.Parameters.Add("@ObservacionesMedicas", SqlDbType.NVarChar, 500).Value =
                    TextoOpcional(modelo.ObservacionesMedicas);

                var idBebe = comando.Parameters.Add("@BebePacienteId", SqlDbType.Int);
                idBebe.Direction = ParameterDirection.Output;
                var idNacimiento = comando.Parameters.Add("@NacimientoId", SqlDbType.Int);
                idNacimiento.Direction = ParameterDirection.Output;

                await comando.ExecuteNonQueryAsync();

                if (idBebe.Value is not int pacienteId ||
                    idNacimiento.Value is not int nacimientoId)
                {
                    throw new InvalidOperationException(
                        "La operación no devolvió los identificadores del nacimiento.");
                }

                return (pacienteId, nacimientoId);
            });
        }

        public Task<bool> AsignarIdentificacionAsync(
            IdentificacionRecienNacidoViewModel modelo, int usuarioId)
        {
            return EjecutarConAuditoriaAsync(usuarioId, async conexion =>
            {
                await using var comando =
                    new SqlCommand("dbo.usp_AsignarIdentificacionRecienNacido", conexion);
                comando.CommandType = CommandType.StoredProcedure;

                comando.Parameters.Add("@PacienteId", SqlDbType.Int).Value =
                    modelo.PacienteId;
                comando.Parameters.Add("@NumeroIdentificacion", SqlDbType.NVarChar, 25).Value =
                    modelo.NumeroIdentificacion.Trim();

                await comando.ExecuteNonQueryAsync();
                return true;
            });
        }

        /// <summary>
        /// Recupera los datos editables de un nacimiento activo y su expediente.
        /// Las relaciones madre/médico son informativas y no se modifican aquí.
        /// </summary>
        public async Task<EditarRecienNacidoViewModel?> ObtenerParaEditarAsync(int nacimientoId)
        {
            const string sql = @"
                SELECT n.NacimientoId, n.BebePacienteId, n.MadrePacienteId, n.EmpleadoId,
                       p.Nombre, p.Apellidos, p.Genero, p.Numero_Telefono,
                       p.Direccion, p.ContactoEmergencia, p.TelefonoEmergencia,
                       n.Fecha_Hora_Nacimiento, n.TipoParto, n.Semanas_Gestacion,
                       n.Peso_en_Gramos, n.Talla_Centimetros, n.Apgar1Min, n.Apgar5Min,
                       n.Respiracion, n.EstadoSalud, n.Mal_formaciones_Observadas,
                       n.Observaciones_Medicas
                FROM dbo.Nacimientos AS n
                INNER JOIN dbo.Pacientes AS p ON p.PacienteId = n.BebePacienteId
                WHERE n.NacimientoId = @NacimientoId
                  AND n.Activo = 1 AND p.Activo = 1;";

            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();
            await using var comando = new SqlCommand(sql, conexion);
            comando.Parameters.Add("@NacimientoId", SqlDbType.Int).Value = nacimientoId;
            await using var lector = await comando.ExecuteReaderAsync();

            if (!await lector.ReadAsync())
                return null;

            return new EditarRecienNacidoViewModel
            {
                NacimientoId = lector.GetInt32(0),
                PacienteId = lector.GetInt32(1),
                MadrePacienteId = lector.GetInt32(2),
                EmpleadoId = lector.GetInt32(3),
                Nombre = lector.GetString(4),
                Apellidos = lector.GetString(5),
                Genero = lector.GetString(6),
                NumeroTelefono = lector.GetString(7),
                Direccion = lector.GetString(8),
                ContactoEmergencia = lector.GetString(9),
                TelefonoEmergencia = lector.GetString(10),
                FechaHoraNacimiento = lector.GetDateTime(11),
                TipoParto = lector.GetString(12),
                SemanasGestacion = lector.IsDBNull(13) ? null : lector.GetByte(13),
                PesoEnGramos = lector.IsDBNull(14) ? null : lector.GetInt32(14),
                TallaCentimetros = lector.IsDBNull(15) ? null : lector.GetDecimal(15),
                Apgar1Min = lector.IsDBNull(16) ? null : lector.GetByte(16),
                Apgar5Min = lector.IsDBNull(17) ? null : lector.GetByte(17),
                Respiracion = lector.IsDBNull(18) ? null : lector.GetString(18),
                EstadoSalud = lector.IsDBNull(19) ? null : lector.GetString(19),
                Malformaciones = lector.IsDBNull(20) ? null : lector.GetString(20),
                ObservacionesMedicas = lector.IsDBNull(21) ? null : lector.GetString(21)
            };
        }

        /// <summary>
        /// Actualiza paciente y nacimiento en una única transacción. La identificación
        /// oficial, el tipo sanguíneo, la madre y el médico no se alteran.
        /// El trigger de Pacientes registra sus cambios; aquí se audita el nacimiento.
        /// </summary>
        public Task<bool> EditarAsync(EditarRecienNacidoViewModel modelo, int usuarioId)
        {
            const string sql = @"
                SET NOCOUNT ON;
                IF NOT EXISTS
                (
                    SELECT 1 FROM dbo.Nacimientos AS n WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN dbo.Pacientes AS p WITH (UPDLOCK, HOLDLOCK)
                        ON p.PacienteId = n.BebePacienteId
                    WHERE n.NacimientoId = @NacimientoId
                      AND n.BebePacienteId = @PacienteId
                      AND n.Activo = 1 AND p.Activo = 1
                )
                    THROW 51951, N'El nacimiento o el paciente ya no está activo.', 1;

                IF @FechaHoraNacimiento > SYSDATETIME()
                    THROW 51952, N'La fecha de nacimiento no puede ser futura.', 1;

                DECLARE @Antes NVARCHAR(MAX) =
                (
                    SELECT p.Nombre, p.Apellidos, p.Genero, p.Numero_Telefono,
                           p.Direccion, p.ContactoEmergencia, p.TelefonoEmergencia,
                           n.Fecha_Hora_Nacimiento, n.TipoParto, n.Semanas_Gestacion,
                           n.Peso_en_Gramos, n.Talla_Centimetros,
                           n.Apgar1Min, n.Apgar5Min, n.Respiracion, n.EstadoSalud,
                           n.Mal_formaciones_Observadas, n.Observaciones_Medicas
                    FROM dbo.Nacimientos AS n
                    INNER JOIN dbo.Pacientes AS p ON p.PacienteId = n.BebePacienteId
                    WHERE n.NacimientoId = @NacimientoId
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
                );

                UPDATE dbo.Pacientes
                SET Nombre = @Nombre, Apellidos = @Apellidos, Genero = @Genero,
                    Fecha_Nacimiento = CONVERT(DATE, @FechaHoraNacimiento),
                    Numero_Telefono = @NumeroTelefono, Direccion = @Direccion,
                    ContactoEmergencia = @ContactoEmergencia,
                    TelefonoEmergencia = @TelefonoEmergencia
                WHERE PacienteId = @PacienteId AND Activo = 1;

                IF @@ROWCOUNT <> 1
                    THROW 51951, N'No se pudo actualizar el expediente del bebé.', 1;

                UPDATE dbo.Nacimientos
                SET Fecha_Hora_Nacimiento = @FechaHoraNacimiento,
                    TipoParto = @TipoParto, Semanas_Gestacion = @SemanasGestacion,
                    Peso_en_Gramos = @PesoEnGramos,
                    Talla_Centimetros = @TallaCentimetros,
                    Apgar1Min = @Apgar1Min, Apgar5Min = @Apgar5Min,
                    Respiracion = @Respiracion, EstadoSalud = @EstadoSalud,
                    Mal_formaciones_Observadas = @Malformaciones,
                    Observaciones_Medicas = @ObservacionesMedicas
                WHERE NacimientoId = @NacimientoId AND Activo = 1;

                IF @@ROWCOUNT <> 1
                    THROW 51951, N'No se pudo actualizar el nacimiento.', 1;

                DECLARE @Despues NVARCHAR(MAX) =
                (
                    SELECT p.Nombre, p.Apellidos, p.Genero, p.Numero_Telefono,
                           p.Direccion, p.ContactoEmergencia, p.TelefonoEmergencia,
                           n.Fecha_Hora_Nacimiento, n.TipoParto, n.Semanas_Gestacion,
                           n.Peso_en_Gramos, n.Talla_Centimetros,
                           n.Apgar1Min, n.Apgar5Min, n.Respiracion, n.EstadoSalud,
                           n.Mal_formaciones_Observadas, n.Observaciones_Medicas
                    FROM dbo.Nacimientos AS n
                    INNER JOIN dbo.Pacientes AS p ON p.PacienteId = n.BebePacienteId
                    WHERE n.NacimientoId = @NacimientoId
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
                );

                IF @Antes <> @Despues
                    INSERT INTO dbo.BitacoraAuditoria
                        (UsuarioId, Accion, Tabla_Modificada, RegistroId,
                         VAnteriores, VNuevos, Objeto)
                    VALUES
                        (TRY_CONVERT(INT, SESSION_CONTEXT(N'UsuarioId')),
                         N'UPDATE', N'Nacimientos',
                         CONVERT(NVARCHAR(100), @NacimientoId),
                         @Antes, @Despues, N'RecienNacidoRepository');
            ";

            return EjecutarConAuditoriaAsync(usuarioId, async conexion =>
            {
                await using var transaccion =
                    (SqlTransaction)await conexion.BeginTransactionAsync();
                try
                {
                    await using var comando = new SqlCommand(sql, conexion, transaccion);
                    comando.Parameters.Add("@NacimientoId", SqlDbType.Int).Value = modelo.NacimientoId;
                    comando.Parameters.Add("@PacienteId", SqlDbType.Int).Value = modelo.PacienteId;
                    comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = modelo.Nombre.Trim();
                    comando.Parameters.Add("@Apellidos", SqlDbType.NVarChar, 130).Value = modelo.Apellidos.Trim();
                    comando.Parameters.Add("@Genero", SqlDbType.NVarChar, 15).Value = modelo.Genero.Trim();
                    comando.Parameters.Add("@NumeroTelefono", SqlDbType.NVarChar, 20).Value = modelo.NumeroTelefono.Trim();
                    comando.Parameters.Add("@Direccion", SqlDbType.NVarChar, 250).Value = modelo.Direccion.Trim();
                    comando.Parameters.Add("@ContactoEmergencia", SqlDbType.NVarChar, 100).Value = modelo.ContactoEmergencia.Trim();
                    comando.Parameters.Add("@TelefonoEmergencia", SqlDbType.NVarChar, 20).Value = modelo.TelefonoEmergencia.Trim();
                    comando.Parameters.Add("@FechaHoraNacimiento", SqlDbType.DateTime2).Value =
                        modelo.FechaHoraNacimiento!.Value;
                    comando.Parameters.Add("@TipoParto", SqlDbType.NVarChar, 30).Value = modelo.TipoParto;

                    comando.Parameters.Add("@SemanasGestacion", SqlDbType.TinyInt).Value =
                        (object?)modelo.SemanasGestacion ?? DBNull.Value;
                    comando.Parameters.Add("@PesoEnGramos", SqlDbType.Int).Value =
                        (object?)modelo.PesoEnGramos ?? DBNull.Value;
                    var talla = comando.Parameters.Add("@TallaCentimetros", SqlDbType.Decimal);
                    talla.Precision = 6;
                    talla.Scale = 2;
                    talla.Value = (object?)modelo.TallaCentimetros ?? DBNull.Value;
                    comando.Parameters.Add("@Apgar1Min", SqlDbType.TinyInt).Value =
                        (object?)modelo.Apgar1Min ?? DBNull.Value;
                    comando.Parameters.Add("@Apgar5Min", SqlDbType.TinyInt).Value =
                        (object?)modelo.Apgar5Min ?? DBNull.Value;
                    comando.Parameters.Add("@Respiracion", SqlDbType.NVarChar, 100).Value =
                        TextoOpcional(modelo.Respiracion);
                    comando.Parameters.Add("@EstadoSalud", SqlDbType.NVarChar, 150).Value =
                        TextoOpcional(modelo.EstadoSalud);
                    comando.Parameters.Add("@Malformaciones", SqlDbType.NVarChar, 400).Value =
                        TextoOpcional(modelo.Malformaciones);
                    comando.Parameters.Add("@ObservacionesMedicas", SqlDbType.NVarChar, 500).Value =
                        TextoOpcional(modelo.ObservacionesMedicas);

                    await comando.ExecuteNonQueryAsync();
                    await transaccion.CommitAsync();
                    return true;
                }
                catch
                {
                    try { await transaccion.RollbackAsync(); }
                    catch (Exception ex) { _logger.LogError(ex, "Error al revertir edición de nacimiento."); }
                    throw;
                }
            });
        }

        /// <summary>
        /// Anula solo el registro de nacimiento (Activo=0), nunca el expediente
        /// del paciente. La justificación queda en BitacoraAuditoria.
        /// </summary>
        public Task<bool> AnularAsync(int nacimientoId, string motivo, int usuarioId)
        {
            const string sql = @"
                SET NOCOUNT ON;
                DECLARE @Antes NVARCHAR(MAX) =
                (
                    SELECT NacimientoId, BebePacienteId, MadrePacienteId,
                           EmpleadoId, Activo
                    FROM dbo.Nacimientos WITH (UPDLOCK, HOLDLOCK)
                    WHERE NacimientoId = @NacimientoId AND Activo = 1
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
                );

                IF @Antes IS NULL
                    THROW 51953, N'El nacimiento no existe o ya fue anulado.', 1;

                UPDATE dbo.Nacimientos
                SET Activo = 0
                WHERE NacimientoId = @NacimientoId AND Activo = 1;

                IF @@ROWCOUNT <> 1
                    THROW 51953, N'No fue posible anular el nacimiento.', 1;

                DECLARE @Despues NVARCHAR(MAX) =
                (
                    SELECT NacimientoId, BebePacienteId, MadrePacienteId,
                           EmpleadoId, Activo, @Motivo AS MotivoAnulacion
                    FROM dbo.Nacimientos
                    WHERE NacimientoId = @NacimientoId
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
                );

                INSERT INTO dbo.BitacoraAuditoria
                    (UsuarioId, Accion, Tabla_Modificada, RegistroId,
                     VAnteriores, VNuevos, Objeto)
                VALUES
                    (TRY_CONVERT(INT, SESSION_CONTEXT(N'UsuarioId')),
                     N'DESACTIVAR', N'Nacimientos',
                     CONVERT(NVARCHAR(100), @NacimientoId),
                     @Antes, @Despues, N'RecienNacidoRepository');
            ";

            return EjecutarConAuditoriaAsync(usuarioId, async conexion =>
            {
                await using var transaccion =
                    (SqlTransaction)await conexion.BeginTransactionAsync();
                try
                {
                    await using var comando = new SqlCommand(sql, conexion, transaccion);
                    comando.Parameters.Add("@NacimientoId", SqlDbType.Int).Value = nacimientoId;
                    comando.Parameters.Add("@Motivo", SqlDbType.NVarChar, 300).Value = motivo.Trim();

                    await comando.ExecuteNonQueryAsync();
                    await transaccion.CommitAsync();
                    return true;
                }
                catch
                {
                    try { await transaccion.RollbackAsync(); }
                    catch (Exception ex) { _logger.LogError(ex, "Error al revertir anulación de nacimiento."); }
                    throw;
                }
            });
        }

        private async Task<T> EjecutarConAuditoriaAsync<T>(
            int usuarioId, Func<SqlConnection, Task<T>> operacion)
        {
            if (usuarioId <= 0)
                throw new ArgumentOutOfRangeException(nameof(usuarioId));

            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            try
            {
                // Contexto y procedimiento deben usar exactamente esta conexión.
                await EstablecerContextoAsync(conexion, usuarioId);
                return await operacion(conexion);
            }
            finally
            {
                try
                {
                    if (conexion.State != ConnectionState.Open)
                    {
                        SqlConnection.ClearPool(conexion);
                    }
                    else
                    {
                        await EstablecerContextoAsync(conexion, null);
                    }
                }
                catch (Exception exLimpieza)
                {
                    // La operación SQL podría haberse confirmado: no indicar
                    // falsamente al usuario que debe reintentarla.
                    SqlConnection.ClearPool(conexion);
                    _logger.LogError(exLimpieza,
                        "No fue posible limpiar SESSION_CONTEXT; se invalidó el grupo de conexiones.");
                }
            }
        }

        private static async Task EstablecerContextoAsync(
            SqlConnection conexion, int? usuarioId)
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = @"
                EXEC sys.sp_set_session_context
                    @key = N'UsuarioId',
                    @value = @Valor;";
            comando.Parameters.Add("@Valor", SqlDbType.Int).Value =
                (object?)usuarioId ?? DBNull.Value;
            await comando.ExecuteNonQueryAsync();
        }

        private static object TextoOpcional(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();
    }
}
