using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models.ViewModels
{
    public class AnularNacimientoViewModel
    {
        [Range(1, int.MaxValue)]
        public int NacimientoId { get; set; }

        [Required(ErrorMessage = "Es obligatorio justificar la anulación.")]
        [StringLength(300, MinimumLength = 5,
            ErrorMessage = "La justificación debe contener entre 5 y 300 caracteres.")]
        public string Motivo { get; set; } = "";
    }
}
