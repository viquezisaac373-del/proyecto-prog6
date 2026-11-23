using System.ComponentModel.DataAnnotations;

namespace ProyectoHospital.Models.ViewModels;

public sealed class AltaProveedorViewModel
{
    [Required, StringLength(100, MinimumLength = 3)]
    [Display(Name = "Razón social")]
    public string RazonSocial { get; set; } = "";
    [Required, RegularExpression(@"^[A-Za-z0-9-]{4,30}$")]
    [Display(Name = "Identificación del proveedor")]
    public string Identificacion { get; set; } = "";
    [Required, RegularExpression(@"^[0-9]{8,20}$")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = "";
    [EmailAddress, StringLength(150)]
    [Display(Name = "Correo electrónico")]
    public string? Correo { get; set; }
    [Required, StringLength(250, MinimumLength = 5)]
    public string Direccion { get; set; } = "";
    [Required, RegularExpression("Medicamentos|Equipos|Instrumentos|Mixto")]
    public string Categoria { get; set; } = "Medicamentos";
}

public sealed class AltaProductoInventarioViewModel
{
    [Required, StringLength(200, MinimumLength = 3)]
    [Display(Name = "Nombre del producto")]
    public string Nombre { get; set; } = "";
    [Required, RegularExpression(@"^[A-Za-z0-9_-]{3,50}$")]
    [Display(Name = "Código interno")]
    public string Codigo { get; set; } = "";
    [Required, RegularExpression("Medicamentos|Equipos|Instrumentos")]
    public string Categoria { get; set; } = "Medicamentos";
    [StringLength(300)]
    public string? Descripcion { get; set; }
}

public sealed class VincularProductoProveedorViewModel
{
    [Range(1, int.MaxValue)]
    public int ProveedorId { get; set; }
    [Range(1, int.MaxValue)]
    public int InventarioId { get; set; }
}

public sealed class RegistrarAbastecimientoViewModel
{
    [Range(1, int.MaxValue)]
    public int ProveedorId { get; set; }
    [Range(1, int.MaxValue)]
    public int InventarioId { get; set; }
    [Range(1, 1000000)]
    public int Cantidad { get; set; }
    [Range(typeof(decimal), "0", "9999999999999.99")]
    public decimal CostoUnitario { get; set; }
    [Required, DataType(DataType.Date)]
    public DateTime? FechaRecepcion { get; set; } = DateTime.Today;
    [Required, StringLength(60, MinimumLength = 3)]
    public string Referencia { get; set; } = "";
    [StringLength(250)]
    public string? Observacion { get; set; }
}

public sealed class ProveedorResumen
{
    public int ProveedorId { get; set; }
    public string RazonSocial { get; set; } = "";
    public string Identificacion { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string? Correo { get; set; }
    public string Direccion { get; set; } = "";
    public string Categoria { get; set; } = "";
    public bool Activo { get; set; }
    public int ProductosAsociados { get; set; }
    public int EntregasRegistradas { get; set; }
}

public sealed class ProductoInventarioOpcion
{
    public int InventarioId { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Categoria { get; set; }
}

public sealed class ProductoProveedorResumen
{
    public int ProveedorId { get; set; }
    public string Proveedor { get; set; } = "";
    public int InventarioId { get; set; }
    public string Producto { get; set; } = "";
    public string Codigo { get; set; } = "";
}

public sealed class AbastecimientoResumen
{
    public int AbastecimientoId { get; set; }
    public int ProveedorId { get; set; }
    public string Proveedor { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Referencia { get; set; } = "";
    public DateTime FechaRecepcion { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Total => Cantidad * CostoUnitario;
}

public sealed class ProveedoresIndexViewModel
{
    public AltaProveedorViewModel NuevoProveedor { get; set; } = new();
    public AltaProductoInventarioViewModel NuevoProducto { get; set; } = new();
    public VincularProductoProveedorViewModel NuevoVinculo { get; set; } = new();
    public RegistrarAbastecimientoViewModel NuevoAbastecimiento { get; set; } = new();
    public List<ProveedorResumen> Proveedores { get; set; } = new();
    public List<ProductoInventarioOpcion> Productos { get; set; } = new();
    public List<ProductoProveedorResumen> Vinculos { get; set; } = new();
    public List<AbastecimientoResumen> Abastecimientos { get; set; } = new();
    public int TotalActivos => Proveedores.Count(x => x.Activo);
    public int TotalInactivos => Proveedores.Count(x => !x.Activo);
}
