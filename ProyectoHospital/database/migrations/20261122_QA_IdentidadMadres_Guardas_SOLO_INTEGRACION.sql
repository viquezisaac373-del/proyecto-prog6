/* =============================================================================
   Hospinomenal — integridad de altas nuevas y relacion madre-bebe
   SOLO Hospital_DB4_Integracion | NO ejecutar en Hospital_DB4
   Este script agrega TRIGGERS, no cambia ni elimina registros existentes.
   Requiere revisión/aprobación de José Steven antes de ejecutar.
   Regla de edad: 10 años a fecha de nacimiento del bebé (PROVISIONAL QA);
   NO es un límite legal ni clínico universal. Casos excepcionales requieren
   flujo médico autorizado aún no implementado. SQL NO verifica filiación real.
   ============================================================================= */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'Hospital_DB4_Integracion'
    THROW 52331, N'BLOQUEADO: esta migracion es SOLO para Hospital_DB4_Integracion.', 1;
IF @@TRANCOUNT <> 0
    THROW 52332, N'BLOQUEADO: existe una transaccion ajena abierta.', 1;
IF OBJECT_ID(N'dbo.Pacientes', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Nacimientos', N'U') IS NULL
    THROW 52333, N'BLOQUEADO: faltan tablas requeridas.', 1;
IF COL_LENGTH(N'dbo.Pacientes', N'Numero_Identificacion') IS NULL
   OR COL_LENGTH(N'dbo.Pacientes', N'Fecha_Nacimiento') IS NULL
   OR COL_LENGTH(N'dbo.Nacimientos', N'MadrePacienteId') IS NULL
   OR COL_LENGTH(N'dbo.Nacimientos', N'Fecha_Hora_Nacimiento') IS NULL
    THROW 52334, N'BLOQUEADO: esquema incompatible.', 1;

DECLARE @AntesPacientes BIGINT, @AntesNacimientos BIGINT;
SELECT @AntesPacientes = COUNT_BIG(*) FROM dbo.Pacientes;
SELECT @AntesNacimientos = COUNT_BIG(*) FROM dbo.Nacimientos;

IF OBJECT_ID(N'dbo.TR_Pacientes_ValidarCedulaNueva', N'TR') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.sql_modules
        WHERE object_id = OBJECT_ID(N'dbo.TR_Pacientes_ValidarCedulaNueva')
          AND definition LIKE N'%52340%'
    )
        THROW 52335, N'BLOQUEADO: existe trigger de cedula diferente. Revisar manualmente.', 1;
END;

IF OBJECT_ID(N'dbo.TR_Nacimientos_ValidarMadreEdadQA', N'TR') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.sql_modules
        WHERE object_id = OBJECT_ID(N'dbo.TR_Nacimientos_ValidarMadreEdadQA')
          AND definition LIKE N'%52341%'
    )
        THROW 52336, N'BLOQUEADO: existe trigger de maternidad diferente. Revisar manualmente.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.TR_Pacientes_ValidarCedulaNueva', N'TR') IS NULL
        EXEC (N'
            CREATE TRIGGER dbo.TR_Pacientes_ValidarCedulaNueva
            ON dbo.Pacientes AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                -- Solo IDENTIFICACIONES nuevas o modificadas. Se conservan
                -- documentos históricos de otras longitudes y expedientes RN NULL.
                IF EXISTS (
                    SELECT 1 FROM inserted AS i
                    LEFT JOIN deleted AS d ON d.PacienteId = i.PacienteId
                    WHERE i.Numero_Identificacion IS NOT NULL
                      AND (d.PacienteId IS NULL OR
                           ISNULL(i.Numero_Identificacion, N'''') <>
                           ISNULL(d.Numero_Identificacion, N''''))
                      AND (
                          DATALENGTH(i.Numero_Identificacion) <> 18
                          OR i.Numero_Identificacion COLLATE Latin1_General_100_BIN2
                             LIKE N''%[^0-9]%''
                          OR i.Numero_Identificacion = N''000000000''
                      )
                )
                    THROW 52340, N''Nuevas cedulas: exactamente 9 digitos ASCII, distintos de ceros.'', 1;
            END;
        ');

    IF OBJECT_ID(N'dbo.TR_Nacimientos_ValidarMadreEdadQA', N'TR') IS NULL
        EXEC (N'
            CREATE TRIGGER dbo.TR_Nacimientos_ValidarMadreEdadQA
            ON dbo.Nacimientos AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                -- Aplica a nuevas relaciones o cambios de madre / fecha del
                -- nacimiento, NO a actualizaciones clinicas de nacimientos viejos.
                IF EXISTS (
                    SELECT 1 FROM inserted AS i
                    LEFT JOIN deleted AS d ON d.NacimientoId = i.NacimientoId
                    INNER JOIN dbo.Pacientes AS p ON p.PacienteId = i.MadrePacienteId
                    WHERE (d.NacimientoId IS NULL
                           OR i.MadrePacienteId <> d.MadrePacienteId
                           OR i.Fecha_Hora_Nacimiento <> d.Fecha_Hora_Nacimiento)
                      AND (
                          p.Genero <> N''Femenino''
                          OR p.Activo <> 1
                          OR DATEADD(YEAR, 10, p.Fecha_Nacimiento) >
                             CONVERT(DATE, i.Fecha_Hora_Nacimiento)
                      )
                )
                    THROW 52341, N''Relacion de madre no valida segun regla QA provisional.'', 1;
            END;
        ');

    IF (SELECT COUNT_BIG(*) FROM dbo.Pacientes) <> @AntesPacientes
       OR (SELECT COUNT_BIG(*) FROM dbo.Nacimientos) <> @AntesNacimientos
        THROW 52342, N'BLOQUEADO: cambiaron los conteos durante la migracion.', 1;

    IF EXISTS (
        SELECT 1 FROM sys.triggers
        WHERE object_id IN (
            OBJECT_ID(N'dbo.TR_Pacientes_ValidarCedulaNueva'),
            OBJECT_ID(N'dbo.TR_Nacimientos_ValidarMadreEdadQA')
        ) AND is_disabled = 1
    )
        THROW 52343, N'Trigger nuevo deshabilitado.', 1;

    COMMIT TRANSACTION;

    SELECT N'APROBADA: TRIGGERS INSTALADOS SIN CAMBIAR REGISTROS' AS Estado,
           DB_NAME() AS BaseDatos, @AntesPacientes AS PacientesConservados,
           @AntesNacimientos AS NacimientosConservados,
           @@TRANCOUNT AS TransaccionesAbiertas;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
