/*
 HU08 - Migración de credenciales a PasswordHasher (ASP.NET Core Identity).
 Ejecutar una sola vez en una COPIA/entorno de pruebas antes de usar
 la rama fix/hu08-seguridad-home. No crea ni revela contraseñas.
 Las cuentas previas SHA-512 podrán migrar al iniciar sesión correctamente.
 Recomendación: respaldar Hospital_DB4 antes de ejecutar.
*/
USE Hospital_DB4;
GO

IF COL_LENGTH(N'dbo.Usuarios', N'PasswordHashIdentity') IS NULL
BEGIN
    ALTER TABLE dbo.Usuarios
    ADD PasswordHashIdentity NVARCHAR(256) NULL;
END;
GO

-- Permitir limpiar los hashes antiguos tras la migración de cada cuenta.
ALTER TABLE dbo.Usuarios ALTER COLUMN PasswordHash VARBINARY(64) NULL;
GO
ALTER TABLE dbo.Usuarios ALTER COLUMN PasswordSalt VARBINARY(16) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_Usuarios_TienePasswordHash'
      AND parent_object_id = OBJECT_ID(N'dbo.Usuarios')
)
BEGIN
    ALTER TABLE dbo.Usuarios
    ADD CONSTRAINT CK_Usuarios_TienePasswordHash CHECK (
        PasswordHashIdentity IS NOT NULL
        OR (PasswordHash IS NOT NULL AND PasswordSalt IS NOT NULL)
    );
END;
GO

PRINT N'HU08: migración de estructura de contraseñas completada.';
GO
