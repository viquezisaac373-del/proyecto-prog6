using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.ViewModels;
using System.Data;

namespace ProyectoHospital.Repositories;

/// <summary>
/// Cuentas de clientes separadas de empleados y expedientes clínicos.
/// Toda alta/vinculación se confirma junto con roles y auditoría.
/// Requiere la migración 20261123_UsuariosClientes_SOLO_INTEGRACION.sql.
/// </summary>
public sealed class ClienteCuentaRepository
{
    private readonly string _cadena;

    public ClienteCuentaRepository(IConfiguration configuration)
    {
        _cadena = configuration.GetConnectionString("HospitalDB")
            ?? throw new InvalidOperationException("No existe la conexión HospitalDB.");
    }

    public async Task<int> RegistrarAsync(RegistroClienteViewModel modelo, string hashIdentity)
    {
        await using var conexion = new SqlConnection(_cadena);
        await conexion.OpenAsync();
        using var tx = conexion.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            int rolId = await ObtenerRolClienteAsync(conexion, tx);
            await VerificarUsuarioLibreAsync(conexion, tx, modelo.NombreUsuario);

            const string comprobarCliente = @"
SELECT COUNT(1) FROM dbo.Clientes WITH (UPDLOCK, HOLDLOCK)
WHERE NumeroIdentificacion = @NumeroIdentificacion;";
            using (var cmd = new SqlCommand(comprobarCliente, conexion, tx))
            {
                cmd.Parameters.Add("@NumeroIdentificacion", SqlDbType.NVarChar, 25).Value = modelo.NumeroIdentificacion.Trim();
                if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0)
                    throw new InvalidOperationException(
                        "Esta identificación ya está registrada como cliente. Para asociar una cuenta, contacte a administración; no se vinculan expedientes sin verificar identidad.");
            }

            const string sqlCliente = @"
INSERT INTO dbo.Clientes (NumeroIdentificacion, Nombre, Apellidos, Telefono, Activo)
OUTPUT INSERTED.ClienteId
VALUES (@NumeroIdentificacion, @Nombre, @Apellidos, @Telefono, 1);";
            int clienteId;
            using (var cmd = new SqlCommand(sqlCliente, conexion, tx))
            {
                cmd.Parameters.Add("@NumeroIdentificacion", SqlDbType.NVarChar, 25).Value = modelo.NumeroIdentificacion.Trim();
                cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = modelo.Nombre.Trim();
                cmd.Parameters.Add("@Apellidos", SqlDbType.NVarChar, 130).Value = modelo.Apellidos.Trim();
                cmd.Parameters.Add("@Telefono", SqlDbType.NVarChar, 20).Value = modelo.Telefono.Trim();
                clienteId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            int usuarioId = await CrearCuentaClienteAsync(
                conexion, tx, modelo.NombreUsuario, hashIdentity, clienteId, rolId);

            await AuditarAsync(conexion, tx, usuarioId, usuarioId,
                "REGISTRO_CLIENTE", $"Alta de cliente #{clienteId} y cuenta de acceso.");
            tx.Commit();
            return usuarioId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<int> VincularPorAdministradorAsync(VincularClienteViewModel modelo, string hashIdentity, int operadorId)
    {
        await using var conexion = new SqlConnection(_cadena);
        await conexion.OpenAsync();
        using var tx = conexion.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            int rolId = await ObtenerRolClienteAsync(conexion, tx);
            await VerificarUsuarioLibreAsync(conexion, tx, modelo.NombreUsuario);

            const string sqlDisponible = @"
SELECT COUNT(1) FROM dbo.Clientes c WITH (UPDLOCK, HOLDLOCK)
WHERE c.ClienteId = @ClienteId AND c.Activo = 1
  AND NOT EXISTS (
    SELECT 1 FROM dbo.UsuarioCliente uc WITH (UPDLOCK, HOLDLOCK)
    WHERE uc.ClienteId = c.ClienteId
  );";
            using (var cmd = new SqlCommand(sqlDisponible, conexion, tx))
            {
                cmd.Parameters.Add("@ClienteId", SqlDbType.Int).Value = modelo.ClienteId;
                if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 1)
                    throw new InvalidOperationException("El cliente está inactivo, no existe o ya tiene una cuenta.");
            }

            int usuarioId = await CrearCuentaClienteAsync(
                conexion, tx, modelo.NombreUsuario, hashIdentity, modelo.ClienteId, rolId);
            await AuditarAsync(conexion, tx, operadorId, usuarioId,
                "VINCULAR_CLIENTE", $"Vinculación del cliente #{modelo.ClienteId} a nueva cuenta.");
            tx.Commit();
            return usuarioId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<List<ClienteDisponible>> ListarSinCuentaAsync()
    {
        const string sql = @"
SELECT c.ClienteId, CONCAT(c.Nombre, N' ', c.Apellidos), c.NumeroIdentificacion
FROM dbo.Clientes c
WHERE c.Activo = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.UsuarioCliente uc WHERE uc.ClienteId = c.ClienteId)
ORDER BY c.ClienteId;";
        var lista = new List<ClienteDisponible>();
        await using var conexion = new SqlConnection(_cadena);
        await conexion.OpenAsync();
        using var cmd = new SqlCommand(sql, conexion);
        await using var lector = await cmd.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            lista.Add(new ClienteDisponible
            {
                ClienteId = lector.GetInt32(0),
                NombreCompleto = lector.GetString(1),
                Identificacion = lector.GetString(2)
            });
        return lista;
    }

    public async Task<ClienteCuentaResumen?> ObtenerMiCuentaAsync(int usuarioId)
    {
        const string sql = @"
SELECT c.ClienteId, u.Usuario, c.Nombre, c.Apellidos, c.NumeroIdentificacion
FROM dbo.UsuarioCliente uc
JOIN dbo.Usuarios u ON u.UsuarioId = uc.UsuarioId
JOIN dbo.Clientes c ON c.ClienteId = uc.ClienteId
WHERE uc.UsuarioId = @UsuarioId AND u.Activo = 1 AND c.Activo = 1;";
        await using var conexion = new SqlConnection(_cadena);
        await conexion.OpenAsync();
        using var cmd = new SqlCommand(sql, conexion);
        cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
        await using var lector = await cmd.ExecuteReaderAsync();
        if (!await lector.ReadAsync()) return null;
        return new ClienteCuentaResumen
        {
            ClienteId = lector.GetInt32(0),
            Usuario = lector.GetString(1),
            Nombre = lector.GetString(2),
            Apellidos = lector.GetString(3),
            Identificacion = lector.GetString(4)
        };
    }

    public async Task<List<ProductoCatalogo>> ListarCatalogoAsync()
    {
        const string sql = @"
SELECT TOP (100) InventarioId, Nombre_del_Producto, Descripcion, PrecioVenta
FROM dbo.Inventario
WHERE Activo = 1 AND Vendible = 1
ORDER BY Nombre_del_Producto, InventarioId;";
        var items = new List<ProductoCatalogo>();
        await using var conexion = new SqlConnection(_cadena);
        await conexion.OpenAsync();
        using var cmd = new SqlCommand(sql, conexion);
        await using var lector = await cmd.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            items.Add(new ProductoCatalogo
            {
                InventarioId = lector.GetInt32(0),
                Nombre = lector.GetString(1),
                Descripcion = lector.IsDBNull(2) ? null : lector.GetString(2),
                PrecioVenta = lector.GetDecimal(3)
            });
        return items;
    }

    private static async Task<int> ObtenerRolClienteAsync(SqlConnection conexion, SqlTransaction tx)
    {
        const string sql = @"
SELECT RolId FROM dbo.Roles WITH (HOLDLOCK)
WHERE Nombre_del_Rol = N'Cliente' AND Activo = 1;";
        using var cmd = new SqlCommand(sql, conexion, tx);
        return (int?)await cmd.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("El rol Cliente todavía no está disponible en esta base de datos.");
    }

    private static async Task VerificarUsuarioLibreAsync(SqlConnection conexion, SqlTransaction tx, string usuario)
    {
        const string sql = @"
SELECT COUNT(1) FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK)
WHERE Usuario = @Usuario;";
        using var cmd = new SqlCommand(sql, conexion, tx);
        cmd.Parameters.Add("@Usuario", SqlDbType.NVarChar, 50).Value = usuario.Trim();
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 0)
            throw new InvalidOperationException("El nombre de usuario ya existe.");
    }

    private static async Task<int> CrearCuentaClienteAsync(
        SqlConnection conexion, SqlTransaction tx, string usuario, string hashIdentity,
        int clienteId, int rolId)
    {
        const string sqlUsuario = @"
INSERT INTO dbo.Usuarios
    (Usuario, PasswordHash, PasswordSalt, PasswordHashIdentity, EmpleadoId, Activo)
OUTPUT INSERTED.UsuarioId
VALUES (@Usuario, NULL, NULL, @Hash, NULL, 1);";
        int usuarioId;
        using (var cmd = new SqlCommand(sqlUsuario, conexion, tx))
        {
            cmd.Parameters.Add("@Usuario", SqlDbType.NVarChar, 50).Value = usuario.Trim();
            cmd.Parameters.Add("@Hash", SqlDbType.NVarChar, 256).Value = hashIdentity;
            usuarioId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        const string sqlRoles = @"
INSERT INTO dbo.UsuarioRol (UsuarioId, RolId) VALUES (@UsuarioId, @RolId);";
        using (var cmd = new SqlCommand(sqlRoles, conexion, tx))
        {
            cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            cmd.Parameters.Add("@RolId", SqlDbType.Int).Value = rolId;
            await cmd.ExecuteNonQueryAsync();
        }

        const string sqlVinculacion = @"
INSERT INTO dbo.UsuarioCliente (UsuarioId, ClienteId)
VALUES (@UsuarioId, @ClienteId);";
        using (var cmd = new SqlCommand(sqlVinculacion, conexion, tx))
        {
            cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            cmd.Parameters.Add("@ClienteId", SqlDbType.Int).Value = clienteId;
            await cmd.ExecuteNonQueryAsync();
        }

        return usuarioId;
    }

    private static async Task AuditarAsync(
        SqlConnection conexion, SqlTransaction tx, int actorId, int usuarioId,
        string accion, string descripcion)
    {
        const string sql = @"
INSERT INTO dbo.BitacoraAuditoria (UsuarioId, Accion, Tabla_Modificada, RegistroId, VNuevos, Objeto)
VALUES (@ActorId, @Accion, N'UsuarioCliente', @Registro, @Descripcion, N'CuentaClienteController');";
        using var cmd = new SqlCommand(sql, conexion, tx);
        cmd.Parameters.Add("@ActorId", SqlDbType.Int).Value = actorId;
        cmd.Parameters.Add("@Accion", SqlDbType.NVarChar, 100).Value = accion;
        cmd.Parameters.Add("@Registro", SqlDbType.NVarChar, 100).Value = usuarioId.ToString();
        cmd.Parameters.Add("@Descripcion", SqlDbType.NVarChar, -1).Value = descripcion;
        await cmd.ExecuteNonQueryAsync();
    }
}
