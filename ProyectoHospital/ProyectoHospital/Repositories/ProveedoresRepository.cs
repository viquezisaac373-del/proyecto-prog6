using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.ViewModels;
using System.Data;

namespace ProyectoHospital.Repositories;

public sealed class ProveedoresRepository
{
    private readonly string _conexion;

    public ProveedoresRepository(IConfiguration configuration)
    {
        _conexion = configuration.GetConnectionString("HospitalDB")
            ?? throw new InvalidOperationException("La cadena de conexión HospitalDB no está configurada.");
    }

    public async Task<List<ProveedorResumen>> ListarProveedoresAsync()
    {
        const string sql = @"
SELECT p.ProveedorId, p.RazonSocial, p.Identificacion, p.Telefono,
       p.Correo, p.Direccion, p.Categoria, p.Activo,
       (SELECT COUNT(*) FROM dbo.ProveedorProductos x WHERE x.ProveedorId = p.ProveedorId),
       (SELECT COUNT(*) FROM dbo.AbastecimientosHospitalarios a WHERE a.ProveedorId = p.ProveedorId)
FROM dbo.ProveedoresHospitalarios p
ORDER BY p.RazonSocial, p.ProveedorId;";
        var resultado = new List<ProveedorResumen>();
        await using var cn = new SqlConnection(_conexion);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            resultado.Add(new ProveedorResumen
            {
                ProveedorId = r.GetInt32(0), RazonSocial = r.GetString(1),
                Identificacion = r.GetString(2), Telefono = r.GetString(3),
                Correo = r.IsDBNull(4) ? null : r.GetString(4),
                Direccion = r.GetString(5), Categoria = r.GetString(6),
                Activo = r.GetBoolean(7), ProductosAsociados = r.GetInt32(8),
                EntregasRegistradas = r.GetInt32(9)
            });
        return resultado;
    }

    public async Task<List<ProductoInventarioOpcion>> ListarProductosAsync()
    {
        const string sql = @"
SELECT InventarioId, Nombre_del_Producto, Codigo, Categoria
FROM dbo.Inventario WHERE Activo = 1
ORDER BY Nombre_del_Producto, InventarioId;";
        var resultado = new List<ProductoInventarioOpcion>();
        await using var cn = new SqlConnection(_conexion);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            resultado.Add(new ProductoInventarioOpcion
            {
                InventarioId = r.GetInt32(0), Nombre = r.GetString(1),
                Codigo = r.GetString(2), Categoria = r.IsDBNull(3) ? null : r.GetString(3)
            });
        return resultado;
    }

    public async Task<List<ProductoProveedorResumen>> ListarVinculosAsync()
    {
        const string sql = @"
SELECT pp.ProveedorId, p.RazonSocial, pp.InventarioId, i.Nombre_del_Producto, i.Codigo
FROM dbo.ProveedorProductos pp
JOIN dbo.ProveedoresHospitalarios p ON p.ProveedorId = pp.ProveedorId
JOIN dbo.Inventario i ON i.InventarioId = pp.InventarioId
ORDER BY p.RazonSocial, i.Nombre_del_Producto;";
        var resultado = new List<ProductoProveedorResumen>();
        await using var cn = new SqlConnection(_conexion);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            resultado.Add(new ProductoProveedorResumen
            {
                ProveedorId = r.GetInt32(0), Proveedor = r.GetString(1),
                InventarioId = r.GetInt32(2), Producto = r.GetString(3),
                Codigo = r.GetString(4)
            });
        return resultado;
    }

    public async Task<List<AbastecimientoResumen>> ListarAbastecimientosAsync()
    {
        const string sql = @"
SELECT TOP (200) a.AbastecimientoId, a.ProveedorId, p.RazonSocial,
       i.Nombre_del_Producto, a.Referencia, a.FechaRecepcion,
       a.Cantidad, a.CostoUnitario
FROM dbo.AbastecimientosHospitalarios a
JOIN dbo.ProveedoresHospitalarios p ON p.ProveedorId = a.ProveedorId
JOIN dbo.Inventario i ON i.InventarioId = a.InventarioId
ORDER BY a.FechaRecepcion DESC, a.AbastecimientoId DESC;";
        var resultado = new List<AbastecimientoResumen>();
        await using var cn = new SqlConnection(_conexion);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(sql, cn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            resultado.Add(new AbastecimientoResumen
            {
                AbastecimientoId = r.GetInt32(0), ProveedorId = r.GetInt32(1),
                Proveedor = r.GetString(2), Producto = r.GetString(3),
                Referencia = r.GetString(4), FechaRecepcion = r.GetDateTime(5),
                Cantidad = r.GetInt32(6), CostoUnitario = r.GetDecimal(7)
            });
        return resultado;
    }

    public async Task<int> CrearProveedorAsync(AltaProveedorViewModel p, int actor)
    {
        string nombre = p.RazonSocial.Trim();
        string identificacion = p.Identificacion.Trim().ToUpperInvariant();
        string telefono = p.Telefono.Trim();
        string direccion = p.Direccion.Trim();
        string? correo = string.IsNullOrWhiteSpace(p.Correo) ? null : p.Correo.Trim();

        if (nombre.Length is < 3 or > 100 || identificacion.Length is < 4 or > 30 ||
            telefono.Length is < 8 or > 20 || direccion.Length is < 5 or > 250 ||
            !new[] { "Medicamentos", "Equipos", "Instrumentos", "Mixto" }.Contains(p.Categoria))
            throw new InvalidOperationException("Complete correctamente los datos del proveedor.");

        await using var cn = new SqlConnection(_conexion);
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            const string sql = @"
INSERT INTO dbo.ProveedoresHospitalarios
    (RazonSocial, Identificacion, Telefono, Correo, Direccion, Categoria, UsuarioRegistroId)
OUTPUT INSERTED.ProveedorId
VALUES (@Nombre, @Identificacion, @Telefono, @Correo, @Direccion, @Categoria, @Usuario);";
            int id;
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = nombre;
                cmd.Parameters.Add("@Identificacion", SqlDbType.NVarChar, 30).Value = identificacion;
                cmd.Parameters.Add("@Telefono", SqlDbType.NVarChar, 20).Value = telefono;
                cmd.Parameters.Add("@Correo", SqlDbType.NVarChar, 150).Value = (object?)correo ?? DBNull.Value;
                cmd.Parameters.Add("@Direccion", SqlDbType.NVarChar, 250).Value = direccion;
                cmd.Parameters.Add("@Categoria", SqlDbType.NVarChar, 20).Value = p.Categoria;
                cmd.Parameters.Add("@Usuario", SqlDbType.Int).Value = actor;
                id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
            await AuditarAsync(cn, tx, actor, "INSERT", "ProveedoresHospitalarios", id,
                $"Proveedor registrado: {nombre}; categoría: {p.Categoria}");
            tx.Commit();
            return id;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            tx.Rollback();
            throw new InvalidOperationException("Ya existe un proveedor con esa identificación.", ex);
        }
        catch { tx.Rollback(); throw; }
    }

    public async Task<int> CrearProductoAsync(AltaProductoInventarioViewModel p, int actor)
    {
        string nombre = p.Nombre.Trim();
        string codigo = p.Codigo.Trim().ToUpperInvariant();
        string? descripcion = string.IsNullOrWhiteSpace(p.Descripcion) ? null : p.Descripcion.Trim();
        if (nombre.Length is < 3 or > 200 || codigo.Length is < 3 or > 50 ||
            !new[] { "Medicamentos", "Equipos", "Instrumentos" }.Contains(p.Categoria))
            throw new InvalidOperationException("Complete correctamente los datos del producto.");

        await using var cn = new SqlConnection(_conexion);
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            const string sql = @"
INSERT INTO dbo.Inventario
    (Nombre_del_Producto, Codigo, Categoria, Descripcion, StockMinimo, PrecioVenta, Vendible, Activo)
OUTPUT INSERTED.InventarioId
VALUES (@Nombre, @Codigo, @Categoria, @Descripcion, 0, 0, 0, 1);";
            int id;
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 200).Value = nombre;
                cmd.Parameters.Add("@Codigo", SqlDbType.NVarChar, 50).Value = codigo;
                cmd.Parameters.Add("@Categoria", SqlDbType.NVarChar, 100).Value = p.Categoria;
                cmd.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 300).Value = (object?)descripcion ?? DBNull.Value;
                id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
            await AuditarAsync(cn, tx, actor, "INSERT", "Inventario", id,
                $"Producto interno para abastecimiento: código {codigo}");
            tx.Commit();
            return id;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            tx.Rollback();
            throw new InvalidOperationException("El código de producto ya existe.", ex);
        }
        catch { tx.Rollback(); throw; }
    }

    public async Task CambiarEstadoAsync(int proveedorId, bool activar, int actor)
    {
        await using var cn = new SqlConnection(_conexion);
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            const string sql = @"
UPDATE dbo.ProveedoresHospitalarios
SET Activo = @Activo
WHERE ProveedorId = @Proveedor AND Activo <> @Activo;";
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                cmd.Parameters.Add("@Activo", SqlDbType.Bit).Value = activar;
                cmd.Parameters.Add("@Proveedor", SqlDbType.Int).Value = proveedorId;
                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException("El proveedor ya tiene ese estado o no existe.");
            }
            await AuditarAsync(cn, tx, actor, "UPDATE", "ProveedoresHospitalarios", proveedorId,
                activar ? "Proveedor reactivado." : "Proveedor desactivado.");
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public async Task VincularProductoAsync(VincularProductoProveedorViewModel p, int actor)
    {
        await using var cn = new SqlConnection(_conexion);
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            const string validar = @"
SELECT COUNT(*) FROM dbo.ProveedoresHospitalarios WITH (UPDLOCK, HOLDLOCK)
WHERE ProveedorId = @Proveedor AND Activo = 1;
SELECT COUNT(*) FROM dbo.Inventario WITH (UPDLOCK, HOLDLOCK)
WHERE InventarioId = @Producto AND Activo = 1;";
            using (var cmd = new SqlCommand(validar, cn, tx))
            {
                cmd.Parameters.Add("@Proveedor", SqlDbType.Int).Value = p.ProveedorId;
                cmd.Parameters.Add("@Producto", SqlDbType.Int).Value = p.InventarioId;
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync() || reader.GetInt32(0) != 1)
                    throw new InvalidOperationException("El proveedor no está activo.");
                if (!await reader.NextResultAsync() || !await reader.ReadAsync() || reader.GetInt32(0) != 1)
                    throw new InvalidOperationException("El producto no está activo.");
            }

            const string sql = @"
INSERT INTO dbo.ProveedorProductos (ProveedorId, InventarioId, UsuarioRegistroId)
VALUES (@Proveedor, @Producto, @Usuario);";
            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                cmd.Parameters.Add("@Proveedor", SqlDbType.Int).Value = p.ProveedorId;
                cmd.Parameters.Add("@Producto", SqlDbType.Int).Value = p.InventarioId;
                cmd.Parameters.Add("@Usuario", SqlDbType.Int).Value = actor;
                await cmd.ExecuteNonQueryAsync();
            }
            await AuditarAsync(cn, tx, actor, "INSERT", "ProveedorProductos", p.ProveedorId,
                $"Producto #{p.InventarioId} asociado al proveedor #{p.ProveedorId}");
            tx.Commit();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            tx.Rollback();
            throw new InvalidOperationException("El producto ya estaba asociado al proveedor.", ex);
        }
        catch { tx.Rollback(); throw; }
    }

    public async Task<int> RegistrarAbastecimientoAsync(RegistrarAbastecimientoViewModel p, int actor)
    {
        if (!p.FechaRecepcion.HasValue || p.FechaRecepcion.Value.Date > DateTime.Today ||
            p.FechaRecepcion.Value.Date < new DateTime(2000, 1, 1) ||
            p.Cantidad <= 0 || p.Cantidad > 1000000 ||
            p.CostoUnitario < 0 || p.CostoUnitario > 9999999999999.99m ||
            string.IsNullOrWhiteSpace(p.Referencia) || p.Referencia.Trim().Length > 60)
            throw new InvalidOperationException("Revise la fecha, cantidad, costo y referencia.");

        await using var cn = new SqlConnection(_conexion);
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            const string validar = @"
SELECT p.RazonSocial
FROM dbo.ProveedoresHospitalarios p WITH (UPDLOCK, HOLDLOCK)
JOIN dbo.ProveedorProductos pp WITH (UPDLOCK, HOLDLOCK) ON pp.ProveedorId = p.ProveedorId
JOIN dbo.Inventario i WITH (UPDLOCK, HOLDLOCK) ON i.InventarioId = pp.InventarioId
WHERE p.ProveedorId = @Proveedor AND pp.InventarioId = @Producto
  AND p.Activo = 1 AND i.Activo = 1;";
            string nombre;
            using (var cmd = new SqlCommand(validar, cn, tx))
            {
                cmd.Parameters.Add("@Proveedor", SqlDbType.Int).Value = p.ProveedorId;
                cmd.Parameters.Add("@Producto", SqlDbType.Int).Value = p.InventarioId;
                var valor = await cmd.ExecuteScalarAsync();
                if (valor is null or DBNull)
                    throw new InvalidOperationException("Proveedor/producto inactivo o sin asociación registrada.");
                nombre = (string)valor;
            }

            const string insertarMovimiento = @"
INSERT INTO dbo.MovimientosInventario
    (InventarioId, TipoMovimiento, Cantidad, CostoUnitario, Proveedor, UsuarioId, Observacion)
OUTPUT INSERTED.MovimientoId
VALUES (@Producto, N'EntradaCompra', @Cantidad, @Costo, @Proveedor, @Usuario, @Observacion);";
            long movimientoId;
            using (var cmd = new SqlCommand(insertarMovimiento, cn, tx))
            {
                cmd.Parameters.Add("@Producto", SqlDbType.Int).Value = p.InventarioId;
                cmd.Parameters.Add("@Cantidad", SqlDbType.Int).Value = p.Cantidad;
                cmd.Parameters.Add("@Costo", SqlDbType.Decimal).Value = p.CostoUnitario;
                cmd.Parameters["@Costo"].Precision = 15;
                cmd.Parameters["@Costo"].Scale = 2;
                cmd.Parameters.Add("@Proveedor", SqlDbType.NVarChar, 100).Value = nombre;
                cmd.Parameters.Add("@Usuario", SqlDbType.Int).Value = actor;
                cmd.Parameters.Add("@Observacion", SqlDbType.NVarChar, 250).Value =
                    $"Abastecimiento ref: {p.Referencia.Trim()}";
                movimientoId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            }

            const string insertar = @"
INSERT INTO dbo.AbastecimientosHospitalarios
    (ProveedorId, InventarioId, Cantidad, CostoUnitario, Referencia, FechaRecepcion,
     Observacion, MovimientoId, UsuarioRegistroId)
OUTPUT INSERTED.AbastecimientoId
VALUES (@Proveedor, @Producto, @Cantidad, @Costo, @Referencia, @Fecha,
        @Observacion, @Movimiento, @Usuario);";
            int id;
            using (var cmd = new SqlCommand(insertar, cn, tx))
            {
                cmd.Parameters.Add("@Proveedor", SqlDbType.Int).Value = p.ProveedorId;
                cmd.Parameters.Add("@Producto", SqlDbType.Int).Value = p.InventarioId;
                cmd.Parameters.Add("@Cantidad", SqlDbType.Int).Value = p.Cantidad;
                cmd.Parameters.Add("@Costo", SqlDbType.Decimal).Value = p.CostoUnitario;
                cmd.Parameters["@Costo"].Precision = 15;
                cmd.Parameters["@Costo"].Scale = 2;
                cmd.Parameters.Add("@Referencia", SqlDbType.NVarChar, 60).Value = p.Referencia.Trim();
                cmd.Parameters.Add("@Fecha", SqlDbType.Date).Value = p.FechaRecepcion.Value.Date;
                cmd.Parameters.Add("@Observacion", SqlDbType.NVarChar, 250).Value =
                    string.IsNullOrWhiteSpace(p.Observacion) ? DBNull.Value : p.Observacion.Trim();
                cmd.Parameters.Add("@Movimiento", SqlDbType.BigInt).Value = movimientoId;
                cmd.Parameters.Add("@Usuario", SqlDbType.Int).Value = actor;
                id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
            await AuditarAsync(cn, tx, actor, "INSERT", "AbastecimientosHospitalarios", id,
                $"Proveedor #{p.ProveedorId}, producto #{p.InventarioId}, movimiento #{movimientoId}");
            tx.Commit();
            return id;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            tx.Rollback();
            throw new InvalidOperationException("Esta referencia de entrega ya está registrada para ese proveedor y producto.", ex);
        }
        catch { tx.Rollback(); throw; }
    }

    private static async Task AuditarAsync(
        SqlConnection cn, SqlTransaction tx, int actor,
        string accion, string tabla, int id, string detalle)
    {
        const string sql = @"
INSERT INTO dbo.BitacoraAuditoria
    (UsuarioId, Accion, Tabla_Modificada, RegistroId, VNuevos, Objeto)
VALUES (@Usuario, @Accion, @Tabla, @Registro, @Detalle, N'ProveedoresController');";
        using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.Add("@Usuario", SqlDbType.Int).Value = actor;
        cmd.Parameters.Add("@Accion", SqlDbType.NVarChar, 100).Value = accion;
        cmd.Parameters.Add("@Tabla", SqlDbType.NVarChar, 100).Value = tabla;
        cmd.Parameters.Add("@Registro", SqlDbType.NVarChar, 100).Value = id.ToString();
        cmd.Parameters.Add("@Detalle", SqlDbType.NVarChar, -1).Value = detalle;
        await cmd.ExecuteNonQueryAsync();
    }
}
