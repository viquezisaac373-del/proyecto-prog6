/*
 Módulo interno de recomendaciones de reposo.
 NO emite incapacidades oficiales CCSS/INS.
 Solo Hospital_DB4_Integracion; ejecutar manualmente tras respaldo.
*/
USE Hospital_DB4_Integracion;
GO
SET XACT_ABORT ON;
GO
BEGIN TRY
    BEGIN TRANSACTION;
    IF DB_NAME() <> N'Hospital_DB4_Integracion'
        THROW 51201, N'La migración solo se puede ejecutar en Hospital_DB4_Integracion.', 1;

    IF OBJECT_ID(N'dbo.RecomendacionesReposo', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.RecomendacionesReposo
        (
            RecomendacionId INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_RecomendacionesReposo PRIMARY KEY,
            PacienteId INT NOT NULL,
            EmpleadoId INT NOT NULL,
            UsuarioRegistroId INT NOT NULL,
            FechaInicio DATE NOT NULL,
            FechaFin DATE NOT NULL,
            Justificacion NVARCHAR(500) NOT NULL,
            FechaRegistro DATETIME2(0) NOT NULL
                CONSTRAINT DF_RecomendacionesReposo_Fecha DEFAULT SYSDATETIME(),
            CONSTRAINT FK_RecomendacionesReposo_Paciente FOREIGN KEY (PacienteId)
                REFERENCES dbo.Pacientes(PacienteId),
            CONSTRAINT FK_RecomendacionesReposo_Empleado FOREIGN KEY (EmpleadoId)
                REFERENCES dbo.Empleados(EmpleadoId),
            CONSTRAINT FK_RecomendacionesReposo_Usuario FOREIGN KEY (UsuarioRegistroId)
                REFERENCES dbo.Usuarios(UsuarioId),
            CONSTRAINT CK_RecomendacionesReposo_Fechas CHECK (FechaFin >= FechaInicio),
            CONSTRAINT CK_RecomendacionesReposo_Motivo CHECK
                (LEN(LTRIM(RTRIM(Justificacion))) >= 10),
            CONSTRAINT UQ_RecomendacionesReposo_Duplicado UNIQUE
                (PacienteId, EmpleadoId, FechaInicio, FechaFin)
        );
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RecomendacionesReposo')
          AND name = N'IX_RecomendacionesReposo_MedicoFecha'
    )
    BEGIN
        CREATE INDEX IX_RecomendacionesReposo_MedicoFecha
        ON dbo.RecomendacionesReposo (EmpleadoId, FechaRegistro DESC);
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
SELECT OBJECT_ID(N'dbo.RecomendacionesReposo', N'U') AS TablaCreada;
SELECT COUNT(*) AS RecomendacionesActuales FROM dbo.RecomendacionesReposo;
GO
