/*
 HU: horarios médicos por día, prevención de solapamiento, reporte semanal.
 Ejecutar MANUALMENTE solo en Hospital_DB4_Integracion, con respaldo previo.
 No sustituye ni altera dbo.HorariosMedicos, tabla existente en algunos entornos
 cuya estructura no está publicada en el script base de develop.
 Coordinar normalización futura con el responsable de base de datos.
*/
USE Hospital_DB4_Integracion;
GO
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF DB_NAME() <> N'Hospital_DB4_Integracion'
        THROW 51101, N'Esta migración solo se puede ejecutar en Hospital_DB4_Integracion.', 1;

    IF OBJECT_ID(N'dbo.DisponibilidadMedicaSemanal', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.DisponibilidadMedicaSemanal
        (
            BloqueId INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_DisponibilidadMedicaSemanal PRIMARY KEY,
            EmpleadoId INT NOT NULL,
            DiaSemana INT NOT NULL,
            HoraInicio TIME(0) NOT NULL,
            HoraFin TIME(0) NOT NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_DisponibilidadMedicaSemanal_Activo DEFAULT (1),
            UsuarioRegistroId INT NOT NULL,
            FechaRegistro DATETIME2(0) NOT NULL
                CONSTRAINT DF_DisponibilidadMedicaSemanal_Fecha DEFAULT (SYSDATETIME()),
            FechaActualizacion DATETIME2(0) NULL,
            CONSTRAINT CK_DisponibilidadMedicaSemanal_Dia
                CHECK (DiaSemana BETWEEN 1 AND 7),
            CONSTRAINT CK_DisponibilidadMedicaSemanal_Horas
                CHECK (HoraInicio < HoraFin),
            CONSTRAINT FK_DisponibilidadMedicaSemanal_Empleados
                FOREIGN KEY (EmpleadoId) REFERENCES dbo.Empleados(EmpleadoId),
            CONSTRAINT FK_DisponibilidadMedicaSemanal_Usuarios
                FOREIGN KEY (UsuarioRegistroId) REFERENCES dbo.Usuarios(UsuarioId)
        );
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.DisponibilidadMedicaSemanal')
          AND name = N'UX_DisponibilidadMedicaSemanal_InicioActivo'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_DisponibilidadMedicaSemanal_InicioActivo
        ON dbo.DisponibilidadMedicaSemanal(EmpleadoId, DiaSemana, HoraInicio)
        WHERE Activo = 1;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT OBJECT_ID(N'dbo.DisponibilidadMedicaSemanal', N'U') AS TablaCreada;
SELECT COUNT(*) AS BloquesActuales FROM dbo.DisponibilidadMedicaSemanal;
GO
