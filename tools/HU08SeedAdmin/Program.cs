using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Data;
using System.Security.Cryptography;

// HU08: utilidad EXCLUSIVA para crear/restablecer el admin de pruebas.
// Comparte el UserSecretsId del proyecto MVC, pero no registra credenciales
// ni contraseñas en código fuente o archivos del repositorio.
const string userSecretsId = "588565c9-c065-4d8a-8869-d8f190b00a94";
const string usuario = "admin";
const string rol = "Administrador";

var configuracion = new ConfigurationBuilder()
    .AddUserSecrets(userSecretsId)
    .AddEnvironmentVariables()
    .Build();

string? cadena = configuracion.GetConnectionString("HospitalDB");
if (string.IsNullOrWhiteSpace(cadena))
{
    Console.Error.WriteLine("Configure ConnectionStrings:HospitalDB en los secretos de usuario del proyecto MVC.");
    return;
}

Console.Write("Solo para una base de PRUEBAS. ¿Continuar? (s/N): ");
if (!string.Equals(Console.ReadLine()?.Trim(), "s", StringComparison.OrdinalIgnoreCase))
    return;

await using var conexion = new SqlConnection(cadena);
try
{
    await conexion.OpenAsync();

    // La columna aparece después de ejecutar la migración de HU08.
    await using (var comprobar = new SqlCommand(
        "SELECT COL_LENGTH(N'dbo.Usuarios', N'PasswordHashIdentity')", conexion))
    {
        if (await comprobar.ExecuteScalarAsync() is DBNull or null)
        {
            Console.Error.WriteLine("Falta ejecutar la migración 20261107_HU08_PasswordHasher.sql.");
            return;
        }
    }

    int? usuarioExistente;
    await using (var buscar = new SqlCommand(
        "SELECT UsuarioId FROM dbo.Usuarios WHERE Usuario = @Usuario", conexion))
    {
        buscar.Parameters.Add("@Usuario", SqlDbType.NVarChar, 50).Value = usuario;
        object? encontrado = await buscar.ExecuteScalarAsync();
        usuarioExistente = encontrado is null or DBNull ? null : Convert.ToInt32(encontrado);
    }

    if (usuarioExistente.HasValue)
    {
        Console.Write("El admin ya existe; se RESTABLECERÁ su contraseña. ¿Confirmar? (s/N): ");
        if (!string.Equals(Console.ReadLine()?.Trim(), "s", StringComparison.OrdinalIgnoreCase))
            return;
    }

    // Contraseña única, aleatoria y de alta entropía (se muestra una sola vez).
    string password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
    var hasher = new PasswordHasher<object>(
        Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = 210_000
        }));
    string passwordHash = hasher.HashPassword(new object(), password);

    await using var transaccion = (SqlTransaction)await conexion.BeginTransactionAsync();
    try
    {
        int usuarioId;
        if (usuarioExistente.HasValue)
        {
            await using var actualizar = new SqlCommand(@"
                UPDATE dbo.Usuarios
                SET PasswordHashIdentity = @Hash, PasswordHash = NULL,
                    PasswordSalt = NULL, IntentosFallidos = 0,
                    BloqueadoHasta = NULL, Activo = 1,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UsuarioId = @Id;", conexion, transaccion);
            actualizar.Parameters.Add("@Hash", SqlDbType.NVarChar, 256).Value = passwordHash;
            actualizar.Parameters.Add("@Id", SqlDbType.Int).Value = usuarioExistente.Value;
            if (await actualizar.ExecuteNonQueryAsync() != 1)
                throw new InvalidOperationException("No se pudo restablecer la cuenta.");
            usuarioId = usuarioExistente.Value;
        }
        else
        {
            await using var insertar = new SqlCommand(@"
                INSERT INTO dbo.Usuarios (Usuario, PasswordHashIdentity)
                OUTPUT INSERTED.UsuarioId
                VALUES (@Usuario, @Hash);", conexion, transaccion);
            insertar.Parameters.Add("@Usuario", SqlDbType.NVarChar, 50).Value = usuario;
            insertar.Parameters.Add("@Hash", SqlDbType.NVarChar, 256).Value = passwordHash;
            usuarioId = Convert.ToInt32(await insertar.ExecuteScalarAsync());
        }

        await using (var crearRol = new SqlCommand(@"
            IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre_del_Rol = @Rol)
                INSERT INTO dbo.Roles (Nombre_del_Rol, Descripcion)
                VALUES (@Rol, N'Acceso administrativo de pruebas.');
            UPDATE dbo.Roles SET Activo = 1 WHERE Nombre_del_Rol = @Rol;",
            conexion, transaccion))
        {
            crearRol.Parameters.Add("@Rol", SqlDbType.NVarChar, 50).Value = rol;
            await crearRol.ExecuteNonQueryAsync();
        }

        await using (var asignarRol = new SqlCommand(@"
            INSERT INTO dbo.UsuarioRol (UsuarioId, RolId)
            SELECT @UsuarioId, r.RolId FROM dbo.Roles r
            WHERE r.Nombre_del_Rol = @Rol
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.UsuarioRol ur
                  WHERE ur.UsuarioId = @UsuarioId AND ur.RolId = r.RolId
              );", conexion, transaccion))
        {
            asignarRol.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            asignarRol.Parameters.Add("@Rol", SqlDbType.NVarChar, 50).Value = rol;
            await asignarRol.ExecuteNonQueryAsync();
        }

        await transaccion.CommitAsync();

        Console.WriteLine();
        Console.WriteLine("Administrador de PRUEBAS configurado correctamente.");
        Console.WriteLine("Usuario: " + usuario);
        Console.WriteLine("Contraseña generada (guárdela en un lugar seguro; no se repetirá):");
        Console.WriteLine(password);
        Console.WriteLine("No suba esta contraseña a GitHub ni a evidencias académicas.");
    }
    catch
    {
        await transaccion.RollbackAsync();
        throw;
    }
}
catch (Exception ex)
{
    // No revelar la cadena de conexión ni la contraseña en mensajes de error.
    Console.Error.WriteLine("No fue posible preparar el usuario de pruebas.");
    Console.Error.WriteLine(ex.GetType().Name + ": " + ex.Message);
}
