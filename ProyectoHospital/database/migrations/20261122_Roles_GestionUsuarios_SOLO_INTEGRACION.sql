/*
 Gestión de usuarios - preparación de roles.
 Ejecutar MANUALMENTE y solo en Hospital_DB4_Integracion tras respaldar la base.
 No altera Usuarios, Empleados, contraseñas ni cuentas existentes.
 Reejecutable. Coordinar con responsable de BD antes de distribuir a otras bases.
*/
USE Hospital_DB4_Integracion;
GO
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'Hospital_DB4_Integracion'
    THROW 51000, N'Este script únicamente se puede ejecutar en Hospital_DB4_Integracion.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Roles WITH (UPDLOCK, HOLDLOCK)
        WHERE Nombre_del_Rol IN (N'Medico', N'Médico')
    )
        INSERT INTO dbo.Roles (Nombre_del_Rol, Descripcion, Activo)
        VALUES (N'Medico', N'Profesional médico autorizado para funciones clínicas.', 1);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Roles WITH (UPDLOCK, HOLDLOCK)
        WHERE Nombre_del_Rol IN (N'Secretaria', N'Secretaría')
    )
        INSERT INTO dbo.Roles (Nombre_del_Rol, Descripcion, Activo)
        VALUES (N'Secretaria', N'Personal administrativo de recepción.', 1);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT RolId, Nombre_del_Rol, Activo
FROM dbo.Roles
ORDER BY RolId;
GO
