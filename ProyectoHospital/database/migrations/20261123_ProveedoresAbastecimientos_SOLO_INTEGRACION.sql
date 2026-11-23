/*
 Proveedores hospitalarios, productos suministrados y abastecimientos.
 Solo Hospital_DB4_Integracion. Requiere respaldo y revisión de responsable DB.
 Los ingresos se registran también en dbo.MovimientosInventario como EntradaCompra.
*/
USE Hospital_DB4_Integracion;
GO
SET XACT_ABORT ON;
GO
BEGIN TRY
    BEGIN TRANSACTION;

    IF DB_NAME() <> N'Hospital_DB4_Integracion'
        THROW 51301, N'Solo ejecutar en Hospital_DB4_Integracion.', 1;

    IF OBJECT_ID(N'dbo.ProveedoresHospitalarios', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProveedoresHospitalarios
        (
            ProveedorId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProveedoresHospitalarios PRIMARY KEY,
            RazonSocial NVARCHAR(100) NOT NULL,
            Identificacion NVARCHAR(30) NOT NULL
                CONSTRAINT UQ_ProveedoresHospitalarios_Identificacion UNIQUE,
            Telefono NVARCHAR(20) NOT NULL,
            Correo NVARCHAR(150) NULL,
            Direccion NVARCHAR(250) NOT NULL,
            Categoria NVARCHAR(20) NOT NULL,
            Activo BIT NOT NULL CONSTRAINT DF_ProveedoresHospitalarios_Activo DEFAULT (1),
            UsuarioRegistroId INT NOT NULL,
            FechaRegistro DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProveedoresHospitalarios_Fecha DEFAULT SYSDATETIME(),
            CONSTRAINT FK_ProveedoresHospitalarios_Usuarios
                FOREIGN KEY (UsuarioRegistroId) REFERENCES dbo.Usuarios(UsuarioId),
            CONSTRAINT CK_ProveedoresHospitalarios_Categoria
                CHECK (Categoria IN (N'Medicamentos', N'Equipos', N'Instrumentos', N'Mixto')),
            CONSTRAINT CK_ProveedoresHospitalarios_Nombre
                CHECK (LEN(LTRIM(RTRIM(RazonSocial))) >= 3)
        );
    END;

    IF OBJECT_ID(N'dbo.ProveedorProductos', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProveedorProductos
        (
            ProveedorId INT NOT NULL,
            InventarioId INT NOT NULL,
            FechaVinculacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProveedorProductos_Fecha DEFAULT SYSDATETIME(),
            UsuarioRegistroId INT NOT NULL,
            CONSTRAINT PK_ProveedorProductos PRIMARY KEY (ProveedorId, InventarioId),
            CONSTRAINT FK_ProveedorProductos_Proveedor
                FOREIGN KEY (ProveedorId) REFERENCES dbo.ProveedoresHospitalarios(ProveedorId),
            CONSTRAINT FK_ProveedorProductos_Inventario
                FOREIGN KEY (InventarioId) REFERENCES dbo.Inventario(InventarioId),
            CONSTRAINT FK_ProveedorProductos_Usuario
                FOREIGN KEY (UsuarioRegistroId) REFERENCES dbo.Usuarios(UsuarioId)
        );
    END;

    IF OBJECT_ID(N'dbo.AbastecimientosHospitalarios', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AbastecimientosHospitalarios
        (
            AbastecimientoId INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_AbastecimientosHospitalarios PRIMARY KEY,
            ProveedorId INT NOT NULL,
            InventarioId INT NOT NULL,
            Cantidad INT NOT NULL,
            CostoUnitario DECIMAL(15,2) NOT NULL,
            Referencia NVARCHAR(60) NOT NULL,
            FechaRecepcion DATE NOT NULL,
            Observacion NVARCHAR(250) NULL,
            MovimientoId BIGINT NOT NULL
                CONSTRAINT UQ_AbastecimientosHospitalarios_Movimiento UNIQUE,
            UsuarioRegistroId INT NOT NULL,
            FechaRegistro DATETIME2(0) NOT NULL
                CONSTRAINT DF_AbastecimientosHospitalarios_Fecha DEFAULT SYSDATETIME(),
            CONSTRAINT FK_AbastecimientosHospitalarios_ProductoProveedor
                FOREIGN KEY (ProveedorId, InventarioId)
                REFERENCES dbo.ProveedorProductos(ProveedorId, InventarioId),
            CONSTRAINT FK_AbastecimientosHospitalarios_Movimiento
                FOREIGN KEY (MovimientoId) REFERENCES dbo.MovimientosInventario(MovimientoId),
            CONSTRAINT FK_AbastecimientosHospitalarios_Usuario
                FOREIGN KEY (UsuarioRegistroId) REFERENCES dbo.Usuarios(UsuarioId),
            CONSTRAINT UQ_AbastecimientosHospitalarios_Entrega
                UNIQUE (ProveedorId, InventarioId, Referencia),
            CONSTRAINT CK_AbastecimientosHospitalarios_Cantidad CHECK (Cantidad > 0),
            CONSTRAINT CK_AbastecimientosHospitalarios_Costo CHECK (CostoUnitario >= 0)
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AbastecimientosHospitalarios')
          AND name = N'IX_Abastecimientos_ProveedorFecha'
    )
        CREATE INDEX IX_Abastecimientos_ProveedorFecha
        ON dbo.AbastecimientosHospitalarios(ProveedorId, FechaRecepcion DESC);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
SELECT OBJECT_ID(N'dbo.ProveedoresHospitalarios', N'U') AS TablaProveedores,
       OBJECT_ID(N'dbo.ProveedorProductos', N'U') AS TablaProductos,
       OBJECT_ID(N'dbo.AbastecimientosHospitalarios', N'U') AS TablaAbastecimientos;
GO
