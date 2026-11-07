namespace ProyectoHospital.Models.Entities
{
    public class UsuarioRecord
    {
        public int UsuarioId { get; set; }

        public string NombreUsuario { get; set; } = string.Empty;

        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        public bool Activo { get; set; }

        public bool Bloqueado { get; set; }
    }
}
