/*
 Gestión de cuentas de clientes de Hospinomenal.
 SOLO Hospital_DB4_Integracion. Respaldar previamente; coordinar estructura con responsable de BD.
 Reejecutable. No modifica datos de admin, empleados, pacientes, ventas, ni cuentas existentes.
 La relación evita una cuenta asociada a múltiples clientes y viceversa.
*/
USE Hospital_DB4_Integracion;
GO
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- Verificación DENTRO de la transacción para garantizar rollback si USE falla.
    IF DB_NAME() <> N'Hospital_DB4_Integracion'
        THROW 51001, N'Este script solamente puede ejecutarse en Hospital_DB4_Integracion.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WITH (UPDLOCK, HOLDLOCK)
                   WHERE Nombre_del_Rol = N'Cliente')
    BEGIN
        INSERT INTO dbo.Roles (Nombre_del_Rol, Descripcion, Activo)
        VALUES (N'Cliente', N'Acceso limitado del cliente a servicios comerciales propios.', 1);
    END;

    IF OBJECT_ID(N'dbo.UsuarioCliente', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.UsuarioCliente (
            UsuarioId INT NOT NULL,
            ClienteId INT NOT NULL,
            FechaAsociacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_UsuarioCliente_Fecha DEFAULT SYSUTCDATETIME(),
            CONSTRAINT PK_UsuarioCliente PRIMARY KEY CLUSTERED (UsuarioId),
            CONSTRAINT UQ_UsuarioCliente_Cliente UNIQUE (ClienteId),
            CONSTRAINT FK_UsuarioCliente_Usuario FOREIGN KEY (UsuarioId)
                REFERENCES dbo.Usuarios (UsuarioId),
            CONSTRAINT FK_UsuarioCliente_Cliente FOREIGN KEY (ClienteId)
                REFERENCES dbo.Clientes (ClienteId)
        );
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT RolId, Nombre_del_Rol, Activo FROM dbo.Roles
WHERE Nombre_del_Rol = N'Cliente';
SELECT OBJECT_ID(N'dbo.UsuarioCliente', N'U') AS UsuarioClienteObjectId;
GO
