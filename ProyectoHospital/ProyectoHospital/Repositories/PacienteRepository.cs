
using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.Entities;
using ProyectoHospital.Models.ViewModels;
using System.Data;

namespace ProyectoHospital.Repositories
{
    // HU09 - Repositorio de pacientes.
    // Centraliza las operaciones de acceso a SQL Server.
    // En una etapa posterior deberá integrarse con WCF/CoreWCF.
    public class PacienteRepository
    {
        private readonly string _connectionString;

        // Obtener la conexión configurada en los secretos
        // de usuario o en la configuración de la aplicación.
        public PacienteRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("HospitalDB")
                ?? throw new InvalidOperationException(
                    "No se configuró la conexión HospitalDB.");
        }

        // HU09 - T02
        // Consultar los pacientes registrados en la base de datos.
        public async Task<List<Paciente>> ListarAsync()
        {
            var pacientes = new List<Paciente>();

            const string sql = @"
                SELECT
                    PacienteId,
                    Numero_Identificacion,
                    Nombre,
                    Apellidos,
                    Fecha_Nacimiento,
                    Numero_Telefono,
                    Activo
                FROM dbo.Pacientes
                ORDER BY PacienteId;";

            await using var conexion =
                new SqlConnection(_connectionString);

            await conexion.OpenAsync();

            await using var comando =
                new SqlCommand(sql, conexion);

            await using var lector =
                await comando.ExecuteReaderAsync();

            // Transformar cada fila de SQL Server
            // en un objeto del modelo Paciente.
            while (await lector.ReadAsync())
            {
                pacientes.Add(new Paciente
                {
                    PacienteId = lector.GetInt32(0),

                    NumeroIdentificacion =
                        lector.IsDBNull(1)
                            ? null
                            : lector.GetString(1),

                    Nombre = lector.GetString(2),
                    Apellidos = lector.GetString(3),
                    FechaNacimiento = lector.GetDateTime(4),
                    NumeroTelefono = lector.GetString(5),
                    Activo = lector.GetBoolean(6)
                });
            }

            return pacientes;
        }

        // HU09 - T03
        // Registrar un paciente utilizando el procedimiento
        // almacenado usp_RegistrarPaciente.
        // Devuelve el identificador generado por SQL Server.
        public async Task<int> RegistrarAsync(
            PacienteFormViewModel paciente)
        {
            // Protección adicional ante una fecha no proporcionada.
            if (!paciente.FechaNacimiento.HasValue)
            {
                throw new ArgumentException(
                    "La fecha de nacimiento es obligatoria.");
            }

            // El esquema SQL actual permite únicamente
            // dígitos en los campos telefónicos.
            // Por eso retiramos el signo + y los espacios,
            // pero conservamos los dígitos del código de país.
            string telefono = NormalizarTelefono(
                paciente.NumeroTelefono);

            string telefonoEmergencia = NormalizarTelefono(
                paciente.TelefonoEmergencia);

            // Abrir la conexión con SQL Server.
            await using var conexion =
                new SqlConnection(_connectionString);

            await conexion.OpenAsync();

            // Utilizar el procedimiento almacenado existente.
            await using var comando =
                new SqlCommand("dbo.usp_RegistrarPaciente", conexion);

            comando.CommandType = CommandType.StoredProcedure;

            // IDENTIFICACIÓN
            comando.Parameters.Add(
                "@NumeroIdentificacion", SqlDbType.NVarChar, 25)
                .Value = paciente.NumeroIdentificacion.Trim();

            // NOMBRE
            comando.Parameters.Add(
                "@Nombre", SqlDbType.NVarChar, 100)
                .Value = paciente.Nombre.Trim();

            // APELLIDOS
            comando.Parameters.Add(
                "@Apellidos", SqlDbType.NVarChar, 130)
                .Value = paciente.Apellidos.Trim();

            // FECHA DE NACIMIENTO
            comando.Parameters.Add(
                "@FechaNacimiento", SqlDbType.Date)
                .Value = paciente.FechaNacimiento.Value.Date;

            // GÉNERO
            comando.Parameters.Add(
                "@Genero", SqlDbType.NVarChar, 15)
                .Value = paciente.Genero.Trim();

            // TIPO DE SANGRE
            comando.Parameters.Add(
                "@TipoSangre", SqlDbType.NVarChar, 3)
                .Value = paciente.TipoSangre.Trim();

            // TELÉFONO PRINCIPAL
            comando.Parameters.Add(
                "@NumeroTelefono", SqlDbType.NVarChar, 20)
                .Value = telefono;

            // CORREO ELECTRÓNICO
            comando.Parameters.Add(
                "@Correo", SqlDbType.NVarChar, 200)
                .Value = paciente.Correo.Trim();

            // DIRECCIÓN
            comando.Parameters.Add(
                "@Direccion", SqlDbType.NVarChar, 250)
                .Value = paciente.Direccion.Trim();

            // CONTACTO DE EMERGENCIA
            comando.Parameters.Add(
                "@ContactoEmergencia", SqlDbType.NVarChar, 100)
                .Value = paciente.ContactoEmergencia.Trim();

            // TELÉFONO DE EMERGENCIA
            comando.Parameters.Add(
                "@TelefonoEmergencia", SqlDbType.NVarChar, 20)
                .Value = telefonoEmergencia;

            // PARÁMETRO DE SALIDA
            // SQL Server devolverá el ID del nuevo paciente.
            var parametroId = comando.Parameters.Add(
                "@PacienteId", SqlDbType.Int);

            parametroId.Direction = ParameterDirection.Output;

            // Ejecutar el procedimiento.
            await comando.ExecuteNonQueryAsync();

            // Comprobar que SQL Server devolvió un ID.
            if (parametroId.Value == null ||
                parametroId.Value == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "El procedimiento no devolvió el ID del paciente.");
            }

            return Convert.ToInt32(parametroId.Value);
        }

        // MÉTODO AUXILIAR
        // Convierte:
        // +506 85102283 -> 50685102283
        // 85102283      -> 85102283
        //
        // Se utiliza porque el esquema actual de DB4
        // no admite el símbolo + en las columnas telefónicas.
        private static string NormalizarTelefono(string telefono)
        {
            return telefono.Trim()
                .Replace("+", "")
                .Replace(" ", "");
        }
    }
}
