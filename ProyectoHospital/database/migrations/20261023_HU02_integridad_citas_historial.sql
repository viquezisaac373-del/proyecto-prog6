/* HU02: aplicar en una base Hospital_DB YA EXISTENTE.
   Desarrollo/prueba exclusivamente. Guardar respaldo antes de ejecutar.
   No elimina tablas ni datos. CREATE OR ALTER conserva el nombre del trigger.
   Cambio probado en SQL Server Express (capturas de UTC Agile, 23/10/2026).
   No sustituye la creación inicial del esquema.
*/
USE Hospital_DB;
GO

/* 1. Mantener la validación de historial para INSERT y UPDATE. */
CREATE OR ALTER TRIGGER dbo.TR_HistorialMedico_ValidaCita
ON dbo.HistorialMedico
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF EXISTS (
            SELECT 1
            FROM inserted AS i
            JOIN dbo.Citas AS c ON c.CitaId = i.CitaId
            WHERE c.Estado <> N'Atendida' OR c.PacienteId <> i.PacienteId
        )
            THROW 50010, N'El historial requiere cita atendida del mismo paciente.', 1;

        IF EXISTS (
            SELECT 1
            FROM inserted AS i
            JOIN dbo.Nacimientos AS n ON n.NacimientoId = i.NacimientoId
            WHERE n.BebePacienteId <> i.PacienteId
        )
            THROW 50011, N'El registro de nacimiento debe corresponder al bebe paciente.', 1;
    END TRY
    BEGIN CATCH
        /* Conserva el control de errores existente; confirmar en pruebas
           cómo se registran fallos cuando un trigger revierte la transacción. */
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        INSERT INTO dbo.Bitacoras
            (TipoBitacora, Accion, Objeto, NumeroError, Severidad,
             Linea, Mensaje, Resultado)
        VALUES
            (N'Error', N'TRIGGER', N'TR_HistorialMedico_ValidaCita',
             ERROR_NUMBER(), ERROR_SEVERITY(), ERROR_LINE(),
             ERROR_MESSAGE(), N'Fallido');
        THROW;
    END CATCH
END;
GO

/* 2. Rechazar asignaciones directas de citas a empleados no médicos,
      y evitar cambios incompatibles con historiales existentes. */
CREATE OR ALTER TRIGGER dbo.TR_Citas_ValidarMedicoEHistorial
ON dbo.Citas
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(EmpleadoId)
       AND EXISTS (
           SELECT 1
           FROM inserted AS i
           JOIN dbo.Empleados AS e ON e.EmpleadoId = i.EmpleadoId
           WHERE e.EsMedico <> 1 OR e.Activo <> 1
       )
    BEGIN
        ;THROW 51020, N'La cita requiere un empleado activo con EsMedico = 1.', 1;
    END;

    /* Una cita con historial no admite otro paciente ni estado no atendido. */
    IF (UPDATE(PacienteId) OR UPDATE(Estado))
       AND EXISTS (
           SELECT 1
           FROM inserted AS i
           JOIN dbo.HistorialMedico AS h ON h.CitaId = i.CitaId
           WHERE h.PacienteId <> i.PacienteId OR i.Estado <> N'Atendida'
       )
    BEGIN
        ;THROW 51021, N'No puede modificarse la cita: existe historial clinico vinculado.', 1;
    END;
END;
GO

/* Validación: cuatro filas; dos eventos por trigger; Deshabilitado = 0. */
SELECT t.name AS TriggerNombre, e.type_desc AS Evento, t.is_disabled AS Deshabilitado
FROM sys.triggers AS t
JOIN sys.trigger_events AS e ON e.object_id = t.object_id
WHERE t.parent_id IN (OBJECT_ID(N'dbo.HistorialMedico'),OBJECT_ID(N'dbo.Citas'))
  AND t.name IN (N'TR_HistorialMedico_ValidaCita',N'TR_Citas_ValidarMedicoEHistorial')
ORDER BY t.name,e.type_desc;
GO
