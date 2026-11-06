using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.Entities;

namespace ProyectoHospital.Repositories
{
    // Acceso a datos de pacientes para las pruebas locales.
    // Más adelante este componente se trasladará o adaptará
    // a la capa de servicio WCF/CoreWCF del proyecto.
    public class PacienteRepository
    {
        private readonly string _connectionString;

        public PacienteRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("HospitalDB")
                ?? throw new InvalidOperationException(
                    "No se configuró la conexión HospitalDB.");
        }

        // Consulta los pacientes registrados en SQL Server.
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
    }
}