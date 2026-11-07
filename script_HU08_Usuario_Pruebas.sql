USE Hospital_DB4;
GO

SET NOCOUNT ON;

DECLARE @Usuario NVARCHAR(50) = N'admin';
DECLARE @Password NVARCHAR(100) = N'Admin123!';
DECLARE @NombreRol NVARCHAR(50) = N'Administrador';

BEGIN TRY
    BEGIN TRAN;

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre_del_Rol = @NombreRol)
    BEGIN
        INSERT INTO dbo.Roles (Nombre_del_Rol, Descripcion)
        VALUES (@NombreRol, N'Acceso completo al Sistema Hospitalario.');
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Usuario = @Usuario)
    BEGIN
        DECLARE @Salt VARBINARY(16) = CRYPT_GEN_RANDOM(16);
        DECLARE @Hash VARBINARY(64) = HASHBYTES('SHA2_512', @Salt + CONVERT(VARBINARY(4000), @Password));

        INSERT INTO dbo.Usuarios (Usuario, PasswordHash, PasswordSalt)
        VALUES (@Usuario, @Hash, @Salt);
    END;

    DECLARE @UsuarioId INT = (SELECT UsuarioId FROM dbo.Usuarios WHERE Usuario = @Usuario);
    DECLARE @RolId INT = (SELECT RolId FROM dbo.Roles WHERE Nombre_del_Rol = @NombreRol);

    IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioRol WHERE UsuarioId = @UsuarioId AND RolId = @RolId)
    BEGIN
        INSERT INTO dbo.UsuarioRol (UsuarioId, RolId)
        VALUES (@UsuarioId, @RolId);
    END;

    COMMIT;

    SELECT u.UsuarioId, u.Usuario, u.Activo, r.Nombre_del_Rol
    FROM dbo.Usuarios u
    JOIN dbo.UsuarioRol ur ON ur.UsuarioId = u.UsuarioId
    JOIN dbo.Roles r ON r.RolId = ur.RolId
    WHERE u.Usuario = @Usuario;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;

    INSERT INTO dbo.BitacoraErrores (Accion, Objeto, NumeroError, Severidad, Linea, Mensaje)
    VALUES (N'SEED_USUARIO_ADMIN', N'HU08_usuario_administrador.sql',
            ERROR_NUMBER(), ERROR_SEVERITY(), ERROR_LINE(), ERROR_MESSAGE());

    THROW;
END CATCH;
GO

/*Desbloquer el usuario*/
UPDATE dbo.Usuarios SET IntentosFallidos = 0, BloqueadoHasta = NULL WHERE Usuario = N'admin';
/*Ver la tabla Usuraios*/
SELECT UsuarioId, Usuario, PasswordHash, PasswordSalt, IntentosFallidos, BloqueadoHasta, Activo FROM dbo.Usuarios;
/*Ver la bitacura de intentos de inicio de cesion*/
SELECT * FROM dbo.BitacoraAccesos ORDER BY FechaHora DESC;