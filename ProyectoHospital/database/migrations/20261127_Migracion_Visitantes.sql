-- Migracion exclusiva del modulo de visitantes
-- Seleccionar expresamente la base de datos destino en SSMS.
-- Por seguridad, cambiar el valor siguiente solo tras revisar la BD destino.

-- IMPORTANTE:
-- Seleccionar la base de datos de trabajo en SSMS.
-- Reemplazar el valor de @BaseDestino por su nombre.
-- Ejemplo: Hospital_DB4_Integracion.

DECLARE @BaseDestino SYSNAME = N'REEMPLAZAR_CON_BASE_DESTINO';
IF DB_NAME() <> @BaseDestino OR @BaseDestino = N'REEMPLAZAR_CON_BASE_DESTINO'
    THROW 51000, 'Seleccione y confirme la base de datos destino.', 1;
IF @@TRANCOUNT <> 0
    THROW 51001, 'Ejecute la migracion sin transacciones abiertas.', 1;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;
    -- 1. Comprobar tablas y duplicados sin eliminar datos.
    IF OBJECT_ID(N'dbo.Visitantes', N'U') IS NULL
        OR OBJECT_ID(N'dbo.RegistroVisitas', N'U') IS NULL
        OR OBJECT_ID(N'dbo.Pacientes', N'U') IS NULL
        THROW 51002, 'Faltan tablas requeridas.', 1;
    IF EXISTS (SELECT NumeroIdentificacion FROM dbo.Visitantes
        GROUP BY NumeroIdentificacion HAVING COUNT(*) > 1)
        THROW 51003, 'Existen identificaciones duplicadas.', 1;
    IF EXISTS (SELECT VisitanteId FROM dbo.RegistroVisitas
        WHERE FechaHoraSalida IS NULL GROUP BY VisitanteId HAVING COUNT(*) > 1)
        THROW 51004, 'Existen visitas activas duplicadas.', 1;

    -- 2. Confirmar la restriccion unica existente de identificacion.
    IF NOT EXISTS (SELECT 1 FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID(N'dbo.Visitantes') AND i.is_unique = 1
        AND i.is_disabled = 0 AND i.has_filter = 0 AND i.is_hypothetical = 0
        AND ic.key_ordinal = 1 AND c.name = N'NumeroIdentificacion'
        AND NOT EXISTS (SELECT 1 FROM sys.index_columns k WHERE k.object_id = i.object_id
            AND k.index_id = i.index_id AND k.key_ordinal > 1))
        THROW 51005, 'Falta la proteccion unica de identificacion.', 1;

    -- 3. Crear el indice filtrado si no existe.
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RegistroVisitas')
        AND name = N'UX_RegistroVisitas_VisitaActiva')
        CREATE UNIQUE INDEX UX_RegistroVisitas_VisitaActiva
        ON dbo.RegistroVisitas (VisitanteId) WHERE FechaHoraSalida IS NULL;

    -- Confirmar que el indice existente o creado tiene la definicion esperada.
    IF NOT EXISTS (SELECT 1 FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID(N'dbo.RegistroVisitas')
        AND i.name = N'UX_RegistroVisitas_VisitaActiva' AND i.is_unique = 1
        AND i.is_disabled = 0 AND i.has_filter = 1 AND i.is_hypothetical = 0
        AND ic.key_ordinal = 1 AND c.name = N'VisitanteId'
        AND UPPER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(i.filter_definition,
            N'[', N''), N']', N''), N'(', N''), N')', N''), N' ', N'')) = N'FECHAHORASALIDAISNULL'
        AND NOT EXISTS (SELECT 1 FROM sys.index_columns k WHERE k.object_id = i.object_id
            AND k.index_id = i.index_id AND k.key_ordinal > 1))
        THROW 51006, 'El indice de visitas activas no tiene la definicion esperada.', 1;

    -- 4. Instalar procedimiento almacenado.
    EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.usp_RegistrarEntradaVisitante
    @NumeroIdentificacion NVARCHAR(25),
    @Nombre NVARCHAR(100),
    @Apellidos NVARCHAR(200),
    @Telefono NVARCHAR(20),
    @Motivo_Visita NVARCHAR(300),
    @PacienteId INT = NULL,
    @UsuarioEntradaId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @VisitanteId INT;
    DECLARE @NombreRegistrado NVARCHAR(100), @ApellidosRegistrados NVARCHAR(200);
    DECLARE @TelefonoRegistrado NVARCHAR(20), @TelefonoIngresado NVARCHAR(20);
    BEGIN TRY
        BEGIN TRANSACTION;
        -- Validar campos obligatorios.
        IF NULLIF(TRIM(@NumeroIdentificacion), '''') IS NULL
            OR NULLIF(TRIM(@Nombre), '''') IS NULL
            OR NULLIF(TRIM(@Apellidos), '''') IS NULL
            OR NULLIF(TRIM(@Telefono), '''') IS NULL
            OR NULLIF(TRIM(@Motivo_Visita), '''') IS NULL
            THROW 50001, ''Complete los campos obligatorios.'', 1;

        -- Validar existencia del paciente.
        IF @PacienteId IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.Pacientes WHERE PacienteId = @PacienteId)
            THROW 50002, ''El paciente no existe.'', 1;

        -- Recuperar y proteger los datos originales.
        SELECT @VisitanteId = VisitanteId, @NombreRegistrado = Nombre,
            @ApellidosRegistrados = Apellidos, @TelefonoRegistrado = Telefono
        FROM dbo.Visitantes WITH (UPDLOCK, HOLDLOCK)
        WHERE NumeroIdentificacion = @NumeroIdentificacion;

        IF @VisitanteId IS NOT NULL
        BEGIN
            SET @TelefonoIngresado = TRIM(@Telefono);
            SET @TelefonoRegistrado = TRIM(@TelefonoRegistrado);
            IF LEN(@TelefonoRegistrado) = 8
                SET @TelefonoRegistrado = ''506'' + @TelefonoRegistrado;
            IF LEN(@TelefonoIngresado) = 8
                SET @TelefonoIngresado = ''506'' + @TelefonoIngresado;
            IF TRIM(@NombreRegistrado) COLLATE Latin1_General_100_CI_AS
                <> TRIM(@Nombre) COLLATE Latin1_General_100_CI_AS
                OR TRIM(@ApellidosRegistrados) COLLATE Latin1_General_100_CI_AS
                <> TRIM(@Apellidos) COLLATE Latin1_General_100_CI_AS
                OR @TelefonoRegistrado <> @TelefonoIngresado
                THROW 50006, ''Esta identificacion ya esta registrada con otros datos personales.'', 1;
        END;

        IF @VisitanteId IS NULL
        BEGIN
            INSERT INTO dbo.Visitantes (NumeroIdentificacion, Nombre, Apellidos, Telefono)
            VALUES (@NumeroIdentificacion, @Nombre, @Apellidos, @Telefono);
            SET @VisitanteId = CONVERT(INT, SCOPE_IDENTITY());
        END;

        -- Impedir entradas simultaneas.
        IF EXISTS (SELECT 1 FROM dbo.RegistroVisitas WITH (UPDLOCK, HOLDLOCK)
            WHERE VisitanteId = @VisitanteId AND FechaHoraSalida IS NULL)
            THROW 50003, ''El visitante ya tiene una entrada activa.'', 1;

        INSERT INTO dbo.RegistroVisitas (VisitanteId, PacienteId, Motivo_Visita,
            FechaHoraEntrada, UsuarioEntradaId)
        VALUES (@VisitanteId, @PacienteId, @Motivo_Visita, SYSDATETIME(), @UsuarioEntradaId);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;';

    -- 5. Instalar procedimiento almacenado.
    EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.usp_RegistrarSalidaVisitante
    @RegistroVisitaId INT,
    @UsuarioSalidaId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS (SELECT 1 FROM dbo.RegistroVisitas WITH (UPDLOCK, HOLDLOCK)
            WHERE RegistroVisitaId = @RegistroVisitaId)
            THROW 50004, ''El registro de visita no existe.'', 1;
        IF EXISTS (SELECT 1 FROM dbo.RegistroVisitas
            WHERE RegistroVisitaId = @RegistroVisitaId AND FechaHoraSalida IS NOT NULL)
            THROW 50005, ''La salida ya fue registrada.'', 1;
        UPDATE dbo.RegistroVisitas
        SET FechaHoraSalida = SYSDATETIME(), UsuarioSalidaId = @UsuarioSalidaId
        WHERE RegistroVisitaId = @RegistroVisitaId;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;';

    -- 6. Instalar procedimiento almacenado.
    EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.usp_ConsultarVisitantes
    @Buscar NVARCHAR(200) = NULL,
    @FechaDesde DATE = NULL,
    @FechaHasta DATE = NULL,
    @Estado NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Validar filtros.
    IF @FechaDesde IS NOT NULL AND @FechaHasta IS NOT NULL AND @FechaDesde > @FechaHasta
        THROW 50008, ''El rango de fechas no es valido.'', 1;
    IF @Estado IS NOT NULL AND @Estado NOT IN (N''Activa'', N''Finalizada'')
        THROW 50007, ''El estado solicitado no es valido.'', 1;

    SELECT RV.RegistroVisitaId, V.VisitanteId, V.NumeroIdentificacion,
        V.Nombre, V.Apellidos, V.Telefono, RV.PacienteId,
        CONCAT(P.Nombre, '' '', P.Apellidos) AS Paciente,
        RV.Motivo_Visita, RV.FechaHoraEntrada, RV.FechaHoraSalida,
        RV.UsuarioEntradaId, RV.UsuarioSalidaId,
        CASE WHEN RV.FechaHoraSalida IS NULL THEN ''Activa'' ELSE ''Finalizada'' END AS Estado,
        DATEDIFF(MINUTE, RV.FechaHoraEntrada,
            ISNULL(RV.FechaHoraSalida, SYSDATETIME())) AS DuracionMinutos
    FROM dbo.RegistroVisitas RV
    INNER JOIN dbo.Visitantes V ON RV.VisitanteId = V.VisitanteId
    LEFT JOIN dbo.Pacientes P ON RV.PacienteId = P.PacienteId
    WHERE (@Buscar IS NULL OR V.NumeroIdentificacion LIKE ''%'' + @Buscar + ''%''
        OR V.Nombre LIKE ''%'' + @Buscar + ''%''
        OR V.Apellidos LIKE ''%'' + @Buscar + ''%''
        OR CONCAT(V.Nombre, '' '', V.Apellidos) LIKE ''%'' + @Buscar + ''%'')
        AND (@FechaDesde IS NULL OR RV.FechaHoraEntrada >= @FechaDesde)
        AND (@FechaHasta IS NULL OR CAST(RV.FechaHoraEntrada AS DATE) <= @FechaHasta)
        AND (@Estado IS NULL OR (@Estado = N''Activa'' AND RV.FechaHoraSalida IS NULL)
            OR (@Estado = N''Finalizada'' AND RV.FechaHoraSalida IS NOT NULL))
    ORDER BY RV.FechaHoraEntrada DESC;
END;';

    COMMIT TRANSACTION;
    PRINT 'Migracion del modulo de visitantes completada.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
