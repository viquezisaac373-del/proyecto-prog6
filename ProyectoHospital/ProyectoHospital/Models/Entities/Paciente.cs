namespace ProyectoHospital.Models.Entities
{
    // Representa la información de un paciente
    // almacenada en la tabla dbo.Pacientes de SQL Server.
    public class Paciente
    {
        // Identificador único generado por la base de datos.
        public int PacienteId { get; set; }

        // Puede ser nulo para pacientes recién nacidos
        // que todavía no poseen identificación.
        public string? NumeroIdentificacion { get; set; }

        // Información personal del paciente.
        public string Nombre { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public DateTime FechaNacimiento { get; set; }

        public string Genero { get; set; } = string.Empty;

        public string TipoSangre { get; set; } = string.Empty;

        // Información de contacto.
        // Se utiliza string porque los teléfonos
        // son identificadores, no valores matemáticos.
        public string NumeroTelefono { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string Direccion { get; set; } = string.Empty;

        // Persona de contacto en caso de emergencia.
        public string ContactoEmergencia { get; set; } = string.Empty;

        public string TelefonoEmergencia { get; set; } = string.Empty;

        // Información administrativa.
        public DateTime FechaRegistro { get; set; }

        // Permite desactivar pacientes sin eliminarlos
        // físicamente de la base de datos.
        public bool Activo { get; set; }
    }
}
