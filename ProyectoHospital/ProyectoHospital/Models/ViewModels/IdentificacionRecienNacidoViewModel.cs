using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models.ViewModels
{
    public class IdentificacionRecienNacidoViewModel
    {
        [Range(1, int.MaxValue)]
        public int PacienteId { get; set; }

        [Required(ErrorMessage = "La identificación oficial es obligatoria.")]
        [StringLength(9, MinimumLength = 9,
            ErrorMessage = "La cédula costarricense debe contener exactamente 9 dígitos.")]
        [RegularExpression(@"^(?!0{9}$)[0-9]{9}$",
            ErrorMessage = "La cédula debe contener exactamente 9 dígitos numéricos y no ser solo ceros.")]
        public string NumeroIdentificacion { get; set; } = "";
    }
}
