namespace HospitalWeb.Models
{

    //ViewModel: agrupa los datos que la vista Index necesita.
    // Así el controlador le pasa un slo objeto a la vista.
    public class HomeViewModel
    {
        public List<ServicioHospitalario> Servicios { get; set; }
            = new List<ServicioHospitalario>();
    }
}