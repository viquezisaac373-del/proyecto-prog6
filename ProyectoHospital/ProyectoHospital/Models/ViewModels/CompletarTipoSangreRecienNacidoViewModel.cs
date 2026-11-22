using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models.ViewModels
{
    /// <summary>
    /// Completar un tipo sanguíneo previamente pendiente (ND).
    /// Solo debe utilizarse con un resultado de laboratorio confirmado.
    /// </summary>
    public class CompletarTipoSangreRecienNacidoViewModel
    {
        [Range(1, int.MaxValue)]
        public int PacienteId { get; set; }

        [Required(ErrorMessage = "Seleccione el grupo confirmado.")]
        [RegularExpression(@"^(A\+|A-|B\+|B-|AB\+|AB-|O\+|O-|RHN)$",
            ErrorMessage = "Seleccione un grupo sanguíneo confirmado válido.")]
        public string TipoSangre { get; set; } = "";

        [Required(ErrorMessage = "Indique una referencia de la evidencia clínica.")]
        [StringLength(200, MinimumLength = 3,
            ErrorMessage = "Indique una referencia de 3 a 200 caracteres.")]
        public string ReferenciaLaboratorio { get; set; } = "";
    }
}
