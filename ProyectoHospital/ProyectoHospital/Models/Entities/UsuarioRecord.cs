namespace ProyectoHospital.Models.Entities
{
    public class UsuarioRecord
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;

        // HU08: formato recomendado por ASP.NET Core Identity.
        public string? PasswordHashIdentity { get; set; }

        // Compatibilidad transitoria con registros previos SHA-512 + salt.
        // Al iniciar sesión correctamente, se migran automáticamente.
        public byte[]? PasswordHash { get; set; }
        public byte[]? PasswordSalt { get; set; }

        public bool Activo { get; set; }
        public bool Bloqueado { get; set; }
    }
}
