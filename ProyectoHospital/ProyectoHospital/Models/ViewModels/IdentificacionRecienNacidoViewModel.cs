using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models.ViewModels
{
    public class IdentificacionRecienNacidoViewModel
    {
        [Range(1, int.MaxValue)]
        public int PacienteId { get; set; }

        [Required(ErrorMessage = "La identificación oficial es obligatoria.")]
        [StringLength(25)]
        [RegularExpression(@"^(?!0+$)[0-9]{9,25}$",
            ErrorMessage = "Ingrese de 9 a 25 dígitos; no se permiten solamente ceros.")]
        public string NumeroIdentificacion { get; set; } = "";
    }
}
