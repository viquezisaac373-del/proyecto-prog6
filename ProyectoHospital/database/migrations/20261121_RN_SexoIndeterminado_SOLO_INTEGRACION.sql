/* =============================================================================
   Hospinomenal — compatibilidad de sexo registrado al nacer
   SOLO PARA: Hospital_DB4_Integracion (copia QA; jamás la BD original).
   Añade el valor 'Indeterminado' a CK_Pacientes_Genero sin transformar
   pacientes históricos con valor 'Otro'.
   No ejecuta ningún alta, edición o eliminación de pacientes.
   Procedimiento de prueba, sujeto a revisión del responsable de BD.
   ============================================================================= */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'Hospital_DB4_Integracion'
    THROW 52300, N'BLOQUEADO: seleccionar Hospital_DB4_Integracion antes de ejecutar.', 1;
IF @@TRANCOUNT <> 0
    THROW 52301, N'BLOQUEADO: hay una transaccion ajena abierta.', 1;
IF OBJECT_ID(N'dbo.Pacientes', N'U') IS NULL
    THROW 52302, N'BLOQUEADO: no existe dbo.Pacientes.', 1;
IF COL_LENGTH(N'dbo.Pacientes', N'Genero') IS NULL
    THROW 52303, N'BLOQUEADO: no existe Pacientes.Genero.', 1;

DECLARE @Definicion NVARCHAR(MAX);
SELECT @Definicion = definition
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.Pacientes')
  AND name = N'CK_Pacientes_Genero';

IF @Definicion IS NULL
    THROW 52304, N'BLOQUEADO: falta CK_Pacientes_Genero; requiere revision de BD.', 1;

IF @Definicion LIKE N'%Indeterminado%'
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.Pacientes')
          AND name = N'CK_Pacientes_Genero'
          AND (is_disabled = 1 OR is_not_trusted = 1)
    )
        THROW 52305, N'BLOQUEADO: la restriccion actual no es confiable o esta deshabilitada.', 1;

    SELECT N'YA APLICADA' AS Estado,
           DB_NAME() AS BaseDatos,
           COUNT(*) AS PacientesSinModificar,
           @@TRANCOUNT AS TransaccionesAbiertas
    FROM dbo.Pacientes;
END
ELSE
BEGIN
    -- Nunca reemplazar una regla desconocida que pueda incluir validaciones extra.
    IF @Definicion NOT LIKE N'%Masculino%'
       OR @Definicion NOT LIKE N'%Femenino%'
       OR @Definicion NOT LIKE N'%Otro%'
    THROW 52306, N'BLOQUEADO: restriccion de genero no reconocida; revisar SQL.', 1;

    DECLARE @CantidadAntes BIGINT;
    SELECT @CantidadAntes = COUNT_BIG(*) FROM dbo.Pacientes;

    BEGIN TRY
        BEGIN TRANSACTION;

        ALTER TABLE dbo.Pacientes
            DROP CONSTRAINT CK_Pacientes_Genero;

        ALTER TABLE dbo.Pacientes WITH CHECK
            ADD CONSTRAINT CK_Pacientes_Genero
            CHECK (Genero IN
                (N'Masculino', N'Femenino', N'Otro', N'Indeterminado'));

        ALTER TABLE dbo.Pacientes
            CHECK CONSTRAINT CK_Pacientes_Genero;

        IF EXISTS (
            SELECT 1
            FROM sys.check_constraints
            WHERE parent_object_id = OBJECT_ID(N'dbo.Pacientes')
              AND name = N'CK_Pacientes_Genero'
              AND (is_disabled = 1 OR is_not_trusted = 1)
        )
            THROW 52307, N'La restriccion no quedo habilitada/confiable.', 1;

        IF (SELECT COUNT_BIG(*) FROM dbo.Pacientes) <> @CantidadAntes
            THROW 52308, N'El conteo de pacientes cambio inesperadamente.', 1;

        COMMIT TRANSACTION;

        SELECT N'APROBADA: SOLO AMPLIACION DE CHECK' AS Estado,
               DB_NAME() AS BaseDatos,
               @CantidadAntes AS PacientesSinModificar,
               @@TRANCOUNT AS TransaccionesAbiertas;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
