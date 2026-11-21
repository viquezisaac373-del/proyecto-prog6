using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models.ViewModels
{
    // Reutiliza las validaciones del alta y conserva los identificadores.
    // Madre y médico se muestran como referencia; no se reasignan en la edición.
    public class EditarRecienNacidoViewModel : RecienNacidoFormViewModel
    {
        [Range(1, int.MaxValue)]
        public int NacimientoId { get; set; }

        [Range(1, int.MaxValue)]
        public int PacienteId { get; set; }
    }
}
