/* =====================================================================
   HOSPITAL_DB - PROPUESTA DE REVISION (19 tablas) - SQL Server
   Proyecto de Programacion VI - ASP.NET - UTC

   Basado en el script original compartido por el equipo.
   Cambios principales:
     * Medicamentos: tabla propia vinculada 1:1 con Inventario.
     * Clientes: compradores externos y pacientes vinculados al modulo de venta.
     * Visitantes: separacion entre datos del visitante y sus visitas.
     * Recetas/Medicacion referencian Medicamentos, no cualquier articulo.
     * Sin contrasena de administrador precargada; crear usuarios desde la app.
     * Franjas de cita no exceden la hora de cierre.

   IMPORTANTE:
     - Ejecutar solo para instalacion inicial sobre una BD Hospital_DB vacia.
     - No elimina ni reemplaza datos de bases ya existentes.
     - No implementa aun la transaccion completa de venta ni el reagendamiento.
     - No ejecutado en SQL Server: requiere pruebas de los desarrolladores.
     - Mantiene Codigo, Categoria y otros nombres donde fue posible.
   ===================================================================== */

USE master;
GO

IF DB_ID(N'Hospital_DB') IS NULL
    CREATE DATABASE Hospital_DB;
GO

USE Hospital_DB;
GO

/* =====================================================================
   TABLAS
   ===================================================================== */

/* ---- Roles (con los permisos por modulo) -------------------------- */
CREATE TABLE dbo.Roles (
    RolId           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    Nombre_del_Rol  NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Roles_Nombre UNIQUE,
    Descripcion     NVARCHAR(200) NULL,
    PermUsuarios    BIT NOT NULL CONSTRAINT DF_Roles_PUsuarios   DEFAULT (0),
    PermPacientes   BIT NOT NULL CONSTRAINT DF_Roles_PPacientes  DEFAULT (0),
    PermCitas       BIT NOT NULL CONSTRAINT DF_Roles_PCitas      DEFAULT (0),
    PermConsultas   BIT NOT NULL CONSTRAINT DF_Roles_PConsultas  DEFAULT (0),
    PermInventario  BIT NOT NULL CONSTRAINT DF_Roles_PInventario DEFAULT (0),
    PermVentas      BIT NOT NULL CONSTRAINT DF_Roles_PVentas     DEFAULT (0),
    PermVisitas     BIT NOT NULL CONSTRAINT DF_Roles_PVisitas    DEFAULT (0),
    PermBitacoras   BIT NOT NULL CONSTRAINT DF_Roles_PBitacoras  DEFAULT (0),
    PermReportes    BIT NOT NULL CONSTRAINT DF_Roles_PReportes   DEFAULT (0),
    Activo          BIT NOT NULL CONSTRAINT DF_Roles_Activo      DEFAULT (1)
);
GO

/* ---- Pacientes ----------------------------------------------------- */
CREATE TABLE dbo.Pacientes (
    PacienteId              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pacientes PRIMARY KEY,
    Numero_Identificacion   NVARCHAR(25)  NULL,     -- NULL para recien nacidos sin cedula
    Nombre                  NVARCHAR(100) NOT NULL,
    Apellidos               NVARCHAR(130) NOT NULL,
    Fecha_Nacimiento        DATE NOT NULL,
    Genero                  NVARCHAR(15)  NULL,
    TipoSangre              NVARCHAR(3)   NULL,
    Numero_Telefono         NVARCHAR(20)  NULL,
    Correo                  NVARCHAR(200) NULL,
    Direccion               NVARCHAR(250) NULL,
    ContactoEmergencia      NVARCHAR(100) NULL,
    TelefonoEmergencia      NVARCHAR(20)  NULL,
    FechaRegistro           DATETIME2 NOT NULL CONSTRAINT DF_Pacientes_Fecha  DEFAULT (SYSDATETIME()),
    Activo                  BIT NOT NULL       CONSTRAINT DF_Pacientes_Activo DEFAULT (1),
    CONSTRAINT CK_Pacientes_Genero CHECK (Genero IS NULL OR Genero IN (N'Masculino', N'Femenino', N'Otro'))
);
GO

/* ---- Empleados (puesto, salario por hora y jornada del medico) ---- */
CREATE TABLE dbo.Empleados (
    EmpleadoId              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Empleados PRIMARY KEY,
    Numero_Identificacion   NVARCHAR(30)  NOT NULL,
    Nombre                  NVARCHAR(100) NOT NULL,
    Apellidos               NVARCHAR(150) NOT NULL,
    Telefono                NVARCHAR(20)  NULL,
    Correo                  NVARCHAR(150) NULL,
    Puesto                  NVARCHAR(80)  NOT NULL,
    Especialidad            NVARCHAR(100) NULL,
    SalarioHora             DECIMAL(10,2) NOT NULL,   -- colones por hora (MTSS)
    EsMedico                BIT NOT NULL CONSTRAINT DF_Empleados_EsMedico DEFAULT (0),
    HoraEntrada             TIME(0) NULL,             -- jornada del medico (lunes a viernes)
    HoraSalida              TIME(0) NULL,
    DuracionCitaMin         SMALLINT NULL,
    PacienteId              INT NULL CONSTRAINT FK_Empleados_Pacientes REFERENCES dbo.Pacientes (PacienteId),                 -- si el empleado tambien es paciente
    FechaIngreso            DATE NOT NULL CONSTRAINT DF_Empleados_Ingreso DEFAULT (CAST(SYSDATETIME() AS DATE)),
    Activo                  BIT NOT NULL  CONSTRAINT DF_Empleados_Activo  DEFAULT (1),
    CONSTRAINT UQ_Empleados_Identificacion UNIQUE (Numero_Identificacion),
    CONSTRAINT CK_Empleados_Salario CHECK (SalarioHora > 0),
    CONSTRAINT CK_Empleados_Jornada CHECK (
        EsMedico = 0
        OR (HoraEntrada IS NOT NULL AND HoraSalida IS NOT NULL AND HoraSalida > HoraEntrada AND DuracionCitaMin > 0)
    )
);
GO

/* ---- Usuarios ------------------------------------------------------ */
CREATE TABLE dbo.Usuarios (
    UsuarioId           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
    Usuario             NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Usuarios_Usuario UNIQUE,
    PasswordHash        VARBINARY(64) NOT NULL,
    PasswordSalt        VARBINARY(16) NOT NULL,
    RolId               INT NOT NULL CONSTRAINT FK_Usuarios_Roles REFERENCES dbo.Roles (RolId),
    EmpleadoId          INT NULL CONSTRAINT FK_Usuarios_Empleados REFERENCES dbo.Empleados (EmpleadoId),
    IntentosFallidos    TINYINT NOT NULL CONSTRAINT DF_Usuarios_Intentos DEFAULT (0),
    BloqueadoHasta      DATETIME2 NULL,
    Activo              BIT NOT NULL CONSTRAINT DF_Usuarios_Activo  DEFAULT (1),
    CreatedAt           DATETIME2 NOT NULL CONSTRAINT DF_Usuarios_Created DEFAULT (SYSUTCDATETIME())
);
GO

/* ---- Inventario general: articulos de venta e insumos medicos ----- */
CREATE TABLE dbo.Inventario (
    InventarioId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Inventario PRIMARY KEY,
    Nombre_del_Producto NVARCHAR(200) NOT NULL,
    Categoria           NVARCHAR(100) NULL,
    Descripcion         NVARCHAR(300) NULL,
    Codigo              NVARCHAR(50) NOT NULL CONSTRAINT UQ_Inventario_Codigo UNIQUE,
    StockMinimo         INT NOT NULL CONSTRAINT DF_Inventario_StockMin DEFAULT (0),
    PrecioVenta         DECIMAL(15,2) NOT NULL CONSTRAINT DF_Inventario_Precio DEFAULT (0),
    Vendible            BIT NOT NULL CONSTRAINT DF_Inventario_Vendible DEFAULT (1),
    FechaVencimiento    DATE NULL,
    Activo              BIT NOT NULL CONSTRAINT DF_Inventario_Activo DEFAULT (1),
    CONSTRAINT CK_Inventario_Precio CHECK (PrecioVenta >= 0),
    CONSTRAINT CK_Inventario_StockMin CHECK (StockMinimo >= 0)
);
GO

/* ---- Medicamentos: atributos clinicos separados del producto ------ */
CREATE TABLE dbo.Medicamentos (
    MedicamentoId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Medicamentos PRIMARY KEY,
    InventarioId        INT NOT NULL CONSTRAINT UQ_Medicamentos_Inventario UNIQUE CONSTRAINT FK_Medicamentos_Inventario REFERENCES dbo.Inventario (InventarioId),
    Presentacion        NVARCHAR(100) NULL,
    ViaAdministracion   NVARCHAR(30) NULL,
    Indicaciones_Uso    NVARCHAR(500) NULL,
    Restricciones       NVARCHAR(500) NULL,
    FrecuenciaHoras     TINYINT NULL,
    Requiere_Receta     BIT NOT NULL CONSTRAINT DF_Medicamentos_Receta DEFAULT (0),
    Activo              BIT NOT NULL CONSTRAINT DF_Medicamentos_Activo DEFAULT (1),
    CONSTRAINT CK_Medicamentos_Frecuencia CHECK (FrecuenciaHoras IS NULL OR FrecuenciaHoras > 0)
);
GO

/* ---- Enfermedades y su medicacion --------------------------------- */
CREATE TABLE dbo.Enfermedades (
    EnfermedadId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Enfermedades PRIMARY KEY,
    Nombre              NVARCHAR(150) NOT NULL CONSTRAINT UQ_Enfermedades_Nombre UNIQUE,
    CodigoCIE10         NVARCHAR(10)  NULL,
    Descripcion         NVARCHAR(400) NULL,
    Sintomas            NVARCHAR(300) NULL,
    Tratamiento_General NVARCHAR(500) NULL,
    Prevencion          NVARCHAR(500) NULL,
    Especialidad        NVARCHAR(100) NULL,
    Activa              BIT NOT NULL CONSTRAINT DF_Enfermedades_Activa DEFAULT (1)
);
GO

CREATE TABLE dbo.Medicacion (   -- tabla intermedia Enfermedad - Medicamento
    MedicacionId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Medicacion PRIMARY KEY,
    EnfermedadId        INT NOT NULL CONSTRAINT FK_Medicacion_Enfermedades REFERENCES dbo.Enfermedades (EnfermedadId),
    MedicamentoId       INT NOT NULL CONSTRAINT FK_Medicacion_Medicamentos REFERENCES dbo.Medicamentos (MedicamentoId),              -- valida que sea un medicamento
    Dosis               NVARCHAR(130) NULL,
    Periodicidad        NVARCHAR(130) NULL,
    IntervaloHoras      TINYINT NULL,
    Tiempo_medicacion   NVARCHAR(130) NULL,
    FechaRegistro       DATETIME2 NOT NULL CONSTRAINT DF_Medicacion_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT UQ_Medicacion_Par UNIQUE (EnfermedadId, MedicamentoId)
);
GO

/* ---- Citas (incluye resultado de la consulta y reagendamiento) ---- */
CREATE TABLE dbo.Citas (
    CitaId                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Citas PRIMARY KEY,
    PacienteId              INT NOT NULL CONSTRAINT FK_Citas_Pacientes REFERENCES dbo.Pacientes (PacienteId),
    EmpleadoId              INT NOT NULL CONSTRAINT FK_Citas_Empleados REFERENCES dbo.Empleados (EmpleadoId),              -- medico asignado
    CitaPreviaId            INT NULL CONSTRAINT FK_Citas_CitaPrevia REFERENCES dbo.Citas (CitaId),                  -- cita de la que nace este control
    UsuarioRegistroId       INT NULL CONSTRAINT FK_Citas_Usuarios REFERENCES dbo.Usuarios (UsuarioId),
    Fecha_Hora              DATETIME2(0) NOT NULL,
    TipoCita                NVARCHAR(20) NOT NULL CONSTRAINT DF_Citas_Tipo   DEFAULT (N'Consulta'),
    Estado                  NVARCHAR(30) NOT NULL CONSTRAINT DF_Citas_Estado DEFAULT (N'Programada'),
    Emergencia              BIT NOT NULL CONSTRAINT DF_Citas_Emergencia DEFAULT (0),
    Motivo                  NVARCHAR(300) NOT NULL,
    -- Reagendamiento (solo si otra cita prioritaria la desplazo)
    CitaPrioritariaId       INT NULL CONSTRAINT FK_Citas_Prioritaria REFERENCES dbo.Citas (CitaId),
    FechaHoraOriginal       DATETIME2(0) NULL,
    MotivoReagendamiento    NVARCHAR(250) NULL,
    -- Tiempos de atencion
    HoraLlegada             DATETIME2(0) NULL,
    HoraInicioAtencion      DATETIME2(0) NULL,
    HoraFinAtencion         DATETIME2(0) NULL,
    -- Resultado de la consulta (se llena al atender)
    EnfermedadId            INT NULL CONSTRAINT FK_Citas_Enfermedades REFERENCES dbo.Enfermedades (EnfermedadId),
    Diagnostico             NVARCHAR(500) NULL,
    Observaciones           NVARCHAR(1000) NULL,
    RequiereControl         BIT NOT NULL CONSTRAINT DF_Citas_Control DEFAULT (0),
    FechaControlSugerida    DATE NULL,
    IndicacionesControl     NVARCHAR(500) NULL,
    FechaRegistro           DATETIME2 NOT NULL CONSTRAINT DF_Citas_Registro DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_Citas_Tipo   CHECK (TipoCita IN (N'Consulta', N'Revision', N'Emergencia')),
    CONSTRAINT CK_Citas_Estado CHECK (Estado IN (N'Programada', N'EnEspera', N'EnAtencion', N'Atendida', N'Cancelada', N'NoAsistio')),
    CONSTRAINT CK_Citas_Tiempos CHECK (
        (HoraInicioAtencion IS NULL OR HoraLlegada IS NULL OR HoraInicioAtencion >= HoraLlegada)
        AND (HoraFinAtencion IS NULL OR HoraInicioAtencion IS NULL OR HoraFinAtencion >= HoraInicioAtencion)
    ),
    CONSTRAINT CK_Citas_Control  CHECK (RequiereControl = 0 OR FechaControlSugerida IS NOT NULL),
    CONSTRAINT CK_Citas_Atendida CHECK (Estado <> N'Atendida' OR Diagnostico IS NOT NULL),
    CONSTRAINT CK_Citas_Reagend  CHECK (CitaPrioritariaId IS NULL OR (FechaHoraOriginal IS NOT NULL AND CitaPrioritariaId <> CitaId))
);
GO

CREATE TABLE dbo.Recetas (
    RecetaId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Recetas PRIMARY KEY,
    CitaId              INT NOT NULL CONSTRAINT FK_Recetas_Citas REFERENCES dbo.Citas (CitaId),
    MedicamentoId       INT NOT NULL CONSTRAINT FK_Recetas_Medicamentos REFERENCES dbo.Medicamentos (MedicamentoId),              -- FK asegura que es medicamento
    Dosis               NVARCHAR(60) NOT NULL,
    FrecuenciaHoras     TINYINT NOT NULL,
    DuracionDias        SMALLINT NOT NULL,
    Indicaciones        NVARCHAR(250) NULL,
    CONSTRAINT UQ_Recetas UNIQUE (CitaId, MedicamentoId)
);
GO

/* ---- Nacimientos --------------------------------------------------- */
CREATE TABLE dbo.Nacimientos (
    NacimientoId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Nacimientos PRIMARY KEY,
    MadrePacienteId         INT NOT NULL CONSTRAINT FK_Nacimientos_Madre REFERENCES dbo.Pacientes (PacienteId),
    BebePacienteId          INT NOT NULL CONSTRAINT FK_Nacimientos_Bebe REFERENCES dbo.Pacientes (PacienteId),          -- el bebe se registra como paciente
    EmpleadoId              INT NOT NULL CONSTRAINT FK_Nacimientos_Medico REFERENCES dbo.Empleados (EmpleadoId),          -- medico responsable
    Fecha_Hora_Nacimiento   DATETIME2(0) NOT NULL,
    TipoParto               NVARCHAR(30) NOT NULL,
    Semanas_Gestacion       TINYINT NULL,
    Peso_en_Gramos          INT NULL,
    Talla_Centimetros       DECIMAL(6,2) NULL,
    Apgar1Min               TINYINT NULL,
    Apgar5Min               TINYINT NULL,
    Respiracion             NVARCHAR(100) NULL,
    EstadoSalud             NVARCHAR(150) NULL,
    Mal_formaciones_Observadas NVARCHAR(400) NULL,
    Observaciones_Medicas   NVARCHAR(500) NULL,
    FechaRegistro           DATETIME2 NOT NULL CONSTRAINT DF_Nacimientos_Fecha DEFAULT (SYSDATETIME()),
    Activo                  BIT NOT NULL CONSTRAINT DF_Nacimientos_Activo DEFAULT (1),
    CONSTRAINT UQ_Nacimientos_Bebe UNIQUE (BebePacienteId),
    CONSTRAINT CK_Nacimientos_Personas CHECK (MadrePacienteId <> BebePacienteId),
    CONSTRAINT CK_Nacimientos_Parto CHECK (TipoParto IN (N'Vaginal', N'Cesarea')),
    CONSTRAINT CK_Nacimientos_Apgar CHECK (
        (Apgar1Min IS NULL OR Apgar1Min BETWEEN 0 AND 10)
        AND (Apgar5Min IS NULL OR Apgar5Min BETWEEN 0 AND 10)
    )
);
GO

/* ---- Historial medico ---------------------------------------------- */
CREATE TABLE dbo.HistorialMedico (
    HistorialId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HistorialMedico PRIMARY KEY,
    PacienteId      INT NOT NULL CONSTRAINT FK_Historial_Pacientes REFERENCES dbo.Pacientes (PacienteId),
    CitaId          INT NULL CONSTRAINT FK_Historial_Citas REFERENCES dbo.Citas (CitaId),
    NacimientoId    INT NULL CONSTRAINT FK_Historial_Nacimientos REFERENCES dbo.Nacimientos (NacimientoId),
    TipoRegistro    NVARCHAR(20) NOT NULL,
    Descripcion     NVARCHAR(1000) NOT NULL,
    FechaRegistro   DATETIME2 NOT NULL CONSTRAINT DF_Historial_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_Historial_Tipo CHECK (TipoRegistro IN (N'Antecedente', N'Evolucion', N'Alergia', N'Cirugia')),
    -- No hay historial sin una cita terminada, salvo un nacimiento
    CONSTRAINT CK_Historial_Origen CHECK (
        (CitaId IS NOT NULL AND NacimientoId IS NULL)
        OR (CitaId IS NULL AND NacimientoId IS NOT NULL)
    )
);
GO

/* ---- Clientes: comprador puede ser paciente o persona externa ----- */
CREATE TABLE dbo.Clientes (
    ClienteId             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Clientes PRIMARY KEY,
    PacienteId            INT NULL CONSTRAINT FK_Clientes_Pacientes REFERENCES dbo.Pacientes (PacienteId),                 -- vincula comprador con paciente existente
    NumeroIdentificacion  NVARCHAR(25) NULL,
    Nombre                NVARCHAR(100) NULL,
    Apellidos             NVARCHAR(130) NULL,
    Telefono              NVARCHAR(20) NULL,
    Activo                BIT NOT NULL CONSTRAINT DF_Clientes_Activo DEFAULT (1),
    CONSTRAINT CK_Clientes_Identificacion CHECK (
        PacienteId IS NOT NULL OR (Nombre IS NOT NULL AND Apellidos IS NOT NULL)
    )
);
GO

/* ---- Ventas: factura simplificada en el encabezado ---------------- */
CREATE TABLE dbo.Ventas (
    VentaId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ventas PRIMARY KEY,
    ClienteId       INT NOT NULL CONSTRAINT FK_Ventas_Clientes REFERENCES dbo.Clientes (ClienteId),
    EmpleadoId      INT NOT NULL CONSTRAINT FK_Ventas_Empleados REFERENCES dbo.Empleados (EmpleadoId),
    Fecha_Venta     DATETIME2 NOT NULL CONSTRAINT DF_Ventas_Fecha DEFAULT (SYSDATETIME()),
    Estado          NVARCHAR(40) NOT NULL CONSTRAINT DF_Ventas_Estado DEFAULT (N'Pendiente'),
    DescuentoPct    DECIMAL(5,2) NOT NULL CONSTRAINT DF_Ventas_Desc DEFAULT (0),
    ImpuestoPct     DECIMAL(5,2) NOT NULL CONSTRAINT DF_Ventas_IVA DEFAULT (0),
    -- PENDIENTE: reglas tributarias por producto a validar con PO.
    NumeroFactura   NVARCHAR(20) NULL,
    MetodoPago      NVARCHAR(20) NULL,
    FechaFactura    DATETIME2 NULL,
    CONSTRAINT CK_Ventas_Estado CHECK (Estado IN (N'Pendiente', N'Pagada', N'Anulada')),
    CONSTRAINT CK_Ventas_Pct CHECK (DescuentoPct BETWEEN 0 AND 100 AND ImpuestoPct BETWEEN 0 AND 100),
    CONSTRAINT CK_Ventas_Metodo CHECK (MetodoPago IS NULL OR MetodoPago IN (N'Efectivo', N'Tarjeta', N'SINPE', N'Transferencia')),
    CONSTRAINT CK_Ventas_Pagada CHECK (Estado <> N'Pagada' OR (NumeroFactura IS NOT NULL AND MetodoPago IS NOT NULL AND FechaFactura IS NOT NULL))
);
GO

CREATE TABLE dbo.ArticulosVentas (
    ArticulosVentasId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ArticulosVentas PRIMARY KEY,
    VentaId             INT NOT NULL CONSTRAINT FK_ArticulosVentas_Ventas REFERENCES dbo.Ventas (VentaId),
    InventarioId        INT NOT NULL CONSTRAINT FK_ArticulosVentas_Inventario REFERENCES dbo.Inventario (InventarioId),
    Cantidad            INT NOT NULL,
    Precio_Unitario     DECIMAL(15,2) NOT NULL,    -- precio al momento de la venta
    CONSTRAINT UQ_ArticulosVentas UNIQUE (VentaId, InventarioId),
    CONSTRAINT CK_ArticulosVentas_Cant   CHECK (Cantidad > 0),
    CONSTRAINT CK_ArticulosVentas_Precio CHECK (Precio_Unitario >= 0)
);
GO

/* ---- Movimientos de inventario ------------------------------------- */
CREATE TABLE dbo.MovimientosInventario (
    MovimientoId        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientosInventario PRIMARY KEY,
    InventarioId        INT NOT NULL CONSTRAINT FK_Mov_Inventario REFERENCES dbo.Inventario (InventarioId),
    TipoMovimiento      NVARCHAR(20) NOT NULL,
    Cantidad            INT NOT NULL,
    CostoUnitario       DECIMAL(15,2) NULL,
    Proveedor           NVARCHAR(100) NULL,        -- solo en entradas por compra
    ArticulosVentasId   INT NULL CONSTRAINT FK_Mov_Articulos REFERENCES dbo.ArticulosVentas (ArticulosVentasId),                  -- solo en salidas por venta
    UsuarioId           INT NULL CONSTRAINT FK_Mov_Usuarios REFERENCES dbo.Usuarios (UsuarioId),
    FechaHora           DATETIME2 NOT NULL CONSTRAINT DF_Mov_Fecha DEFAULT (SYSDATETIME()),
    Observacion         NVARCHAR(250) NULL,
    CONSTRAINT CK_Mov_Tipo CHECK (TipoMovimiento IN (N'EntradaCompra', N'SalidaVenta', N'SalidaUsoMedico', N'AjusteEntrada', N'AjusteSalida')),
    CONSTRAINT CK_Mov_Cantidad CHECK (Cantidad > 0)
);
GO

/* ---- Visitantes y registros de entradas/salidas --------------------- */
CREATE TABLE dbo.Visitantes (
    VisitanteId           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Visitantes PRIMARY KEY,
    NumeroIdentificacion NVARCHAR(25) NOT NULL CONSTRAINT UQ_Visitantes_Identificacion UNIQUE,
    Nombre               NVARCHAR(100) NOT NULL,
    Apellidos            NVARCHAR(200) NOT NULL,
    Telefono             NVARCHAR(20) NULL
);
GO

CREATE TABLE dbo.RegistroVisitas (
    RegistroVisitaId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RegistroVisitas PRIMARY KEY,
    VisitanteId           INT NOT NULL CONSTRAINT FK_RegVis_Visitantes REFERENCES dbo.Visitantes (VisitanteId),
    PacienteId            INT NULL CONSTRAINT FK_RegVis_Pacientes REFERENCES dbo.Pacientes (PacienteId),
    Motivo_Visita         NVARCHAR(300) NOT NULL,
    FechaHoraEntrada      DATETIME2(0) NOT NULL CONSTRAINT DF_RegistroVisitas_Entrada DEFAULT (SYSDATETIME()),
    FechaHoraSalida       DATETIME2(0) NULL,
    UsuarioEntradaId      INT NULL CONSTRAINT FK_RegVis_UsuarioEntrada REFERENCES dbo.Usuarios (UsuarioId),
    UsuarioSalidaId       INT NULL CONSTRAINT FK_RegVis_UsuarioSalida REFERENCES dbo.Usuarios (UsuarioId),
    CONSTRAINT CK_RegVis_Salida CHECK (FechaHoraSalida IS NULL OR FechaHoraSalida >= FechaHoraEntrada)
);
GO

/* ---- Bitacora unica: auditoria, errores, accesos y notificaciones -- */
CREATE TABLE dbo.Bitacoras (
    BitacoraId          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bitacoras PRIMARY KEY,
    TipoBitacora        NVARCHAR(15) NOT NULL,
    UsuarioId           INT NULL CONSTRAINT FK_Bitacoras_Usuarios REFERENCES dbo.Usuarios (UsuarioId),
    PacienteId          INT NULL CONSTRAINT FK_Bitacoras_Pacientes REFERENCES dbo.Pacientes (PacienteId),                  -- solo en notificaciones
    CitaId              INT NULL CONSTRAINT FK_Bitacoras_Citas REFERENCES dbo.Citas (CitaId),                  -- solo en notificaciones
    Accion              NVARCHAR(100) NOT NULL,
    Tabla_Modificada    NVARCHAR(100) NULL,        -- auditoria
    RegistroId          NVARCHAR(100) NULL,        -- auditoria
    VAnteriores         NVARCHAR(MAX) NULL,        -- auditoria
    VNuevos             NVARCHAR(MAX) NULL,        -- auditoria
    Objeto              NVARCHAR(100) NULL,        -- errores: procedimiento o trigger
    NumeroError         INT NULL,                  -- errores
    Severidad           INT NULL,                  -- errores
    Linea               INT NULL,                  -- errores
    Mensaje             NVARCHAR(2000) NULL,       -- errores y notificaciones
    Ip                  NVARCHAR(45) NULL,         -- accesos
    Resultado           NVARCHAR(30) NOT NULL CONSTRAINT DF_Bitacoras_Resultado DEFAULT (N'Exitoso'),
    FechaHora           DATETIME2 NOT NULL CONSTRAINT DF_Bitacoras_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_Bitacoras_Tipo CHECK (TipoBitacora IN (N'Auditoria', N'Error', N'Acceso', N'Notificacion')),
    CONSTRAINT CK_Bitacoras_Error CHECK (TipoBitacora <> N'Error' OR (Objeto IS NOT NULL AND Mensaje IS NOT NULL)),
    CONSTRAINT CK_Bitacoras_Notif CHECK (TipoBitacora <> N'Notificacion' OR (PacienteId IS NOT NULL AND Mensaje IS NOT NULL))
);
GO

/* =====================================================================
   INDICES
   ===================================================================== */
-- Evita pacientes duplicados por identificacion (permite NULL en recien nacidos)
CREATE UNIQUE INDEX UX_Pacientes_Identificacion
    ON dbo.Pacientes (Numero_Identificacion) WHERE Numero_Identificacion IS NOT NULL;
GO
-- Un paciente solo puede estar ligado a un empleado
CREATE UNIQUE INDEX UX_Empleados_Paciente
    ON dbo.Empleados (PacienteId) WHERE PacienteId IS NOT NULL;
GO
-- Impide dos citas activas del mismo medico en la misma fecha y hora
CREATE UNIQUE INDEX UX_Citas_Medico_FechaHora
    ON dbo.Citas (EmpleadoId, Fecha_Hora) WHERE Estado <> N'Cancelada';
GO
-- Numero de factura unico (solo ventas facturadas)
CREATE UNIQUE INDEX UX_Ventas_NumeroFactura
    ON dbo.Ventas (NumeroFactura) WHERE NumeroFactura IS NOT NULL;
GO

CREATE INDEX IX_Citas_Paciente_Fecha  ON dbo.Citas (PacienteId, Fecha_Hora);
CREATE INDEX IX_Citas_Fecha           ON dbo.Citas (Fecha_Hora) INCLUDE (EmpleadoId, Estado);
CREATE INDEX IX_Historial_Paciente    ON dbo.HistorialMedico (PacienteId, FechaRegistro);
CREATE INDEX IX_Mov_Inventario        ON dbo.MovimientosInventario (InventarioId) INCLUDE (TipoMovimiento, Cantidad);
CREATE INDEX IX_ArticulosVentas_Venta ON dbo.ArticulosVentas (VentaId);
CREATE INDEX IX_RegVis_Entrada        ON dbo.RegistroVisitas (FechaHoraEntrada);
CREATE INDEX IX_RegVis_Visitante      ON dbo.RegistroVisitas (VisitanteId, FechaHoraEntrada);
CREATE UNIQUE INDEX UX_Clientes_Paciente ON dbo.Clientes (PacienteId) WHERE PacienteId IS NOT NULL;
CREATE UNIQUE INDEX UX_Clientes_Identificacion ON dbo.Clientes (NumeroIdentificacion) WHERE NumeroIdentificacion IS NOT NULL;
CREATE INDEX IX_Bitacoras_Tipo_Fecha  ON dbo.Bitacoras (TipoBitacora, FechaHora);
GO

/* =====================================================================
   DATOS INICIALES
   ===================================================================== */

-- Roles con sus permisos
INSERT INTO dbo.Roles (Nombre_del_Rol, Descripcion,
    PermUsuarios, PermPacientes, PermCitas, PermConsultas, PermInventario, PermVentas, PermVisitas, PermBitacoras, PermReportes)
SELECT b.Nombre, b.Descripcion, b.p1, b.p2, b.p3, b.p4, b.p5, b.p6, b.p7, b.p8, b.p9
FROM (VALUES
    (N'Admin',      N'Control total del sistema',                    1,1,1,1,1,1,1,1,1),
    (N'Medico',     N'Atiende citas y registra consultas',           0,1,1,1,0,0,0,0,0),
    (N'Enfermeria', N'Apoyo clinico y control de pacientes',         0,1,1,1,0,0,0,0,0),
    (N'Recepcion',  N'Citas, visitantes y registro de pacientes',    0,1,1,0,0,0,1,0,0),
    (N'Inventario', N'Control de productos y medicamentos',          0,0,0,0,1,0,0,0,0),
    (N'Caja',       N'Ventas y facturacion',                         0,0,0,0,0,1,0,0,0),
    (N'Usuario',    N'Acceso basico de consulta',                    0,0,0,0,0,0,0,0,1)
) AS b(Nombre, Descripcion, p1, p2, p3, p4, p5, p6, p7, p8, p9)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.Nombre_del_Rol = b.Nombre);
GO

-- 15 enfermedades (minimo exigido por el enunciado)
INSERT INTO dbo.Enfermedades (Nombre, Descripcion, Sintomas, Tratamiento_General, Prevencion, Especialidad)
SELECT b.Nombre, b.Descripcion, b.Sintomas, b.Tratamiento, b.Prevencion, b.Especialidad
FROM (VALUES
    (N'Hipertension arterial', N'Presion arterial elevada de forma sostenida', N'Dolor de cabeza, mareos', N'Antihipertensivos y control de sal', N'Dieta sana, ejercicio', N'Medicina interna'),
    (N'Diabetes mellitus tipo 2', N'Alteracion del metabolismo de la glucosa', N'Sed excesiva, fatiga, orinar seguido', N'Dieta, ejercicio y antidiabeticos', N'Peso saludable, ejercicio', N'Endocrinologia'),
    (N'Asma', N'Inflamacion cronica de las vias respiratorias', N'Tos, silbidos, falta de aire', N'Broncodilatadores y corticoides inhalados', N'Evitar alergenos', N'Neumologia'),
    (N'Gripe (influenza)', N'Infeccion viral respiratoria', N'Fiebre, tos, dolor muscular', N'Reposo, hidratacion, antipireticos', N'Vacuna anual', N'Medicina general'),
    (N'Dengue', N'Infeccion viral transmitida por mosquito', N'Fiebre alta, dolor de ojos, erupcion', N'Hidratacion y control medico', N'Eliminar criaderos de mosquito', N'Infectologia'),
    (N'Neumonia', N'Infeccion de los pulmones', N'Fiebre, tos con flema, dolor toracico', N'Antibioticos y reposo', N'Vacunas, no fumar', N'Neumologia'),
    (N'Gastritis', N'Inflamacion de la mucosa del estomago', N'Ardor, nauseas, dolor abdominal', N'Protectores gastricos, dieta', N'Evitar irritantes', N'Gastroenterologia'),
    (N'Infeccion urinaria', N'Infeccion de las vias urinarias', N'Ardor al orinar, urgencia', N'Antibioticos e hidratacion', N'Hidratacion, higiene', N'Urologia'),
    (N'Anemia', N'Baja concentracion de hemoglobina', N'Cansancio, palidez, mareos', N'Hierro y dieta rica en hierro', N'Alimentacion balanceada', N'Hematologia'),
    (N'Migrana', N'Cefalea recurrente intensa', N'Dolor pulsatil, sensibilidad a la luz', N'Analgesicos y tratamiento preventivo', N'Identificar desencadenantes', N'Neurologia'),
    (N'Gastroenteritis', N'Infeccion del tracto digestivo', N'Diarrea, vomito, fiebre', N'Hidratacion oral', N'Higiene de manos y alimentos', N'Medicina general'),
    (N'Hipotiroidismo', N'Tiroides poco activa', N'Cansancio, aumento de peso, frio', N'Levotiroxina', N'Control periodico', N'Endocrinologia'),
    (N'Artritis', N'Inflamacion de las articulaciones', N'Dolor, rigidez, hinchazon', N'Antiinflamatorios y fisioterapia', N'Actividad fisica moderada', N'Reumatologia'),
    (N'Bronquitis', N'Inflamacion de los bronquios', N'Tos persistente, flema', N'Reposo, hidratacion, broncodilatadores', N'No fumar', N'Neumologia'),
    (N'Conjuntivitis', N'Inflamacion de la conjuntiva del ojo', N'Enrojecimiento, picazon, secrecion', N'Gotas oftalmicas', N'Higiene de manos', N'Oftalmologia')
) AS b(Nombre, Descripcion, Sintomas, Tratamiento, Prevencion, Especialidad)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Enfermedades e WHERE e.Nombre = b.Nombre);
GO

-- Por seguridad, NO se crea un administrador con una contrasena conocida.
-- El alta del primer usuario se debe realizar desde la aplicacion o un
-- script de aprovisionamiento SEPARADO que use un hasher robusto compatible
-- con ASP.NET Identity. Nunca subir secretos a GitHub.

/* =====================================================================
   VIEWS (valores calculados: no se guardan en tablas)
   ===================================================================== */

-- Stock actual = entradas - salidas
CREATE OR ALTER VIEW dbo.vw_StockInventario
AS
SELECT
    i.InventarioId,
    i.Codigo,
    i.Nombre_del_Producto,
    i.StockMinimo,
    s.StockActual,
    CASE WHEN s.StockActual <= i.StockMinimo THEN 1 ELSE 0 END AS BajoMinimo
FROM dbo.Inventario i
CROSS APPLY (
    SELECT ISNULL(SUM(CASE WHEN m.TipoMovimiento IN (N'EntradaCompra', N'AjusteEntrada') THEN m.Cantidad
                           ELSE -m.Cantidad END), 0) AS StockActual
    FROM dbo.MovimientosInventario m
    WHERE m.InventarioId = i.InventarioId
) s;
GO

-- Totales de venta/factura: subtotal - descuento + impuesto
CREATE OR ALTER VIEW dbo.vw_VentaTotales
AS
SELECT
    v.VentaId,
    v.NumeroFactura,
    v.Estado,
    t.Subtotal,
    CAST(t.Subtotal * v.DescuentoPct / 100.0 AS DECIMAL(15,2)) AS Descuento,
    CAST((t.Subtotal - t.Subtotal * v.DescuentoPct / 100.0) * v.ImpuestoPct / 100.0 AS DECIMAL(15,2)) AS Impuesto,
    CAST((t.Subtotal - t.Subtotal * v.DescuentoPct / 100.0) * (1 + v.ImpuestoPct / 100.0) AS DECIMAL(15,2)) AS Total
FROM dbo.Ventas v
CROSS APPLY (
    SELECT ISNULL(SUM(a.Cantidad * a.Precio_Unitario), 0) AS Subtotal
    FROM dbo.ArticulosVentas a
    WHERE a.VentaId = v.VentaId
) t;
GO

-- Tiempo de espera de cada cita (alerta si pasa de 60 minutos)
CREATE OR ALTER VIEW dbo.vw_EsperaCitas
AS
SELECT
    c.CitaId,
    c.PacienteId,
    c.EmpleadoId,
    c.TipoCita,
    c.HoraLlegada,
    c.HoraInicioAtencion,
    DATEDIFF(MINUTE, c.HoraLlegada, c.HoraInicioAtencion) AS MinutosEspera,
    CASE WHEN DATEDIFF(MINUTE, c.HoraLlegada, c.HoraInicioAtencion) > 60 THEN 1 ELSE 0 END AS ExcedeUnaHora
FROM dbo.Citas c
WHERE c.HoraLlegada IS NOT NULL AND c.HoraInicioAtencion IS NOT NULL;
GO

-- Medicamentos disponibles para seleccion en ventas
CREATE OR ALTER VIEW dbo.vw_MedicamentosVendibles
AS
SELECT m.MedicamentoId, m.InventarioId, i.Codigo, i.Nombre_del_Producto,
       i.PrecioVenta, m.Presentacion, m.Requiere_Receta
FROM dbo.Medicamentos m
JOIN dbo.Inventario i ON i.InventarioId = m.InventarioId
WHERE i.Activo = 1 AND i.Vendible = 1 AND m.Activo = 1;
GO

-- Duracion de cada visita; separa persona de cada visita
CREATE OR ALTER VIEW dbo.vw_DuracionVisitas
AS
SELECT
    rv.RegistroVisitaId,
    vt.VisitanteId,
    vt.NumeroIdentificacion,
    vt.Nombre,
    vt.Apellidos,
    rv.PacienteId,
    rv.FechaHoraEntrada,
    rv.FechaHoraSalida,
    DATEDIFF(MINUTE, rv.FechaHoraEntrada, ISNULL(rv.FechaHoraSalida, SYSDATETIME())) AS MinutosEnHospital,
    CASE WHEN rv.FechaHoraSalida IS NULL THEN N'Adentro' ELSE N'Salio' END AS EstadoVisita
FROM dbo.RegistroVisitas rv
JOIN dbo.Visitantes vt ON vt.VisitanteId = rv.VisitanteId;
GO

/* =====================================================================
   TRIGGER: el historial por cita solo se llena si la cita fue atendida
   ===================================================================== */
CREATE OR ALTER TRIGGER dbo.TR_HistorialMedico_ValidaCita
ON dbo.HistorialMedico
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF EXISTS (
            SELECT 1
            FROM inserted i
            JOIN dbo.Citas c ON c.CitaId = i.CitaId
            WHERE c.Estado <> N'Atendida' OR c.PacienteId <> i.PacienteId
        )
            THROW 50010, N'El historial requiere cita atendida del mismo paciente.', 1;

        IF EXISTS (
            SELECT 1 FROM inserted i
            JOIN dbo.Nacimientos n ON n.NacimientoId = i.NacimientoId
            WHERE n.BebePacienteId <> i.PacienteId
        )
            THROW 50011, N'El registro de nacimiento debe corresponder al bebe paciente.', 1;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        INSERT INTO dbo.Bitacoras (TipoBitacora, Accion, Objeto, NumeroError, Severidad, Linea, Mensaje, Resultado)
        VALUES (N'Error', N'TRIGGER', N'TR_HistorialMedico_ValidaCita', ERROR_NUMBER(), ERROR_SEVERITY(), ERROR_LINE(), ERROR_MESSAGE(), N'Fallido');

        THROW;
    END CATCH
END
GO

/* =====================================================================
   PROCEDIMIENTO: agendar una cita
     - Verifica paciente y medico
     - Verifica que la hora este dentro de la jornada (lunes a viernes)
     - Si el espacio esta ocupado, informa la siguiente hora libre del dia
     - TRY...CATCH con ROLLBACK y registro del error en Bitacoras
   ===================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_RegistrarCita
    @PacienteId     INT,
    @EmpleadoId     INT,
    @Fecha_Hora     DATETIME2(0),
    @TipoCita       NVARCHAR(20) = N'Consulta',
    @Motivo         NVARCHAR(300),
    @CitaPreviaId   INT = NULL,
    @UsuarioId      INT = NULL,
    @CitaId         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Pacientes WHERE PacienteId = @PacienteId AND Activo = 1)
            THROW 50001, N'El paciente no existe o esta inactivo.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.Empleados WHERE EmpleadoId = @EmpleadoId AND Activo = 1 AND EsMedico = 1)
            THROW 50002, N'El medico no existe, esta inactivo o no es personal medico.', 1;

        -- Franjas del dia segun la jornada del medico (1 = lunes ... 7 = domingo)
        DECLARE @Dia DATE = CAST(@Fecha_Hora AS DATE);
        DECLARE @DiaSemana TINYINT = (DATEDIFF(DAY, '19000101', @Dia) % 7) + 1;
        DECLARE @Franjas TABLE (Inicio DATETIME2(0) PRIMARY KEY);

        IF @DiaSemana <= 5
        BEGIN
            ;WITH Slots AS (
                SELECT
                    DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), e.HoraEntrada), CAST(@Dia AS DATETIME2(0))) AS Inicio,
                    DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), e.HoraSalida),  CAST(@Dia AS DATETIME2(0))) AS Fin,
                    e.DuracionCitaMin AS Dur
                FROM dbo.Empleados e
                WHERE e.EmpleadoId = @EmpleadoId
                UNION ALL
                SELECT DATEADD(MINUTE, Dur, Inicio), Fin, Dur
                FROM Slots
                WHERE DATEADD(MINUTE, Dur, DATEADD(MINUTE, Dur, Inicio)) <= Fin
            )
            INSERT INTO @Franjas (Inicio)
            SELECT DISTINCT Inicio FROM Slots
            WHERE DATEADD(MINUTE, Dur, Inicio) <= Fin
            OPTION (MAXRECURSION 500);
        END

        IF NOT EXISTS (SELECT 1 FROM @Franjas WHERE Inicio = @Fecha_Hora)
            THROW 50003, N'La hora solicitada esta fuera del horario del medico.', 1;

        -- Espacio ocupado: sugerir la siguiente hora libre del mismo dia
        IF EXISTS (
            SELECT 1
            FROM dbo.Citas WITH (UPDLOCK, HOLDLOCK)
            WHERE EmpleadoId = @EmpleadoId AND Fecha_Hora = @Fecha_Hora AND Estado <> N'Cancelada'
        )
        BEGIN
            DECLARE @Proxima DATETIME2(0) = (
                SELECT MIN(f.Inicio)
                FROM @Franjas f
                WHERE f.Inicio > @Fecha_Hora
                  AND NOT EXISTS (
                        SELECT 1 FROM dbo.Citas c
                        WHERE c.EmpleadoId = @EmpleadoId AND c.Fecha_Hora = f.Inicio AND c.Estado <> N'Cancelada')
            );

            DECLARE @Msg NVARCHAR(400) =
                CASE WHEN @Proxima IS NULL
                     THEN N'Horario ocupado y no hay mas espacios ese dia. Elija otra fecha.'
                     ELSE CONCAT(N'Horario ocupado. Proxima hora libre ese dia: ', CONVERT(NVARCHAR(5), CAST(@Proxima AS TIME), 108))
                END;
            THROW 50004, @Msg, 1;
        END

        -- Si el paciente ya existe, la cita es una fila nueva en la misma tabla
        INSERT INTO dbo.Citas (PacienteId, EmpleadoId, CitaPreviaId, UsuarioRegistroId, Fecha_Hora, TipoCita, Motivo)
        VALUES (@PacienteId, @EmpleadoId, @CitaPreviaId, @UsuarioId, @Fecha_Hora, @TipoCita, @Motivo);

        SET @CitaId = SCOPE_IDENTITY();

        INSERT INTO dbo.Bitacoras (TipoBitacora, UsuarioId, Accion, Tabla_Modificada, RegistroId, VNuevos, Objeto)
        VALUES (N'Auditoria', @UsuarioId, N'INSERT', N'Citas', CAST(@CitaId AS NVARCHAR(100)),
                CONCAT(N'PacienteId=', @PacienteId, N'; EmpleadoId=', @EmpleadoId, N'; Fecha=', CONVERT(NVARCHAR(19), @Fecha_Hora, 120)),
                N'sp_RegistrarCita');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- El registro del error va DESPUES del rollback para que no se deshaga
        INSERT INTO dbo.Bitacoras (TipoBitacora, UsuarioId, Accion, Objeto, NumeroError, Severidad, Linea, Mensaje, Resultado)
        VALUES (N'Error', @UsuarioId, N'PROCEDIMIENTO', N'sp_RegistrarCita', ERROR_NUMBER(), ERROR_SEVERITY(), ERROR_LINE(), ERROR_MESSAGE(), N'Fallido');

        THROW;
    END CATCH
END
GO

/* =====================================================================
   COMPROBACIONES NO DESTRUCTIVAS (consulta de prueba)
   ===================================================================== */
SELECT COUNT(*) AS TablasCreadas FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo'); -- 19
SELECT COUNT(*) AS EnfermedadesIniciales FROM dbo.Enfermedades; -- 15
SELECT name AS NombreTabla FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo') ORDER BY name;
SELECT name AS NombreVista FROM sys.views WHERE schema_id = SCHEMA_ID(N'dbo') ORDER BY name;
GO

/* PENDIENTES DE IMPLEMENTACION / NO PROBAR EN PRODUCCION:
   - Compra completa: verificar stock, restricciones, pago, factura, salida
     de inventario y bitacora en una transaccion, con rollback en errores.
   - No permitir venta de articulos con Vendible = 0; medicamentos que
     requieran receta requieren una validacion definida por el PO.
   - Notificaciones por cada cita desplazada y manejo de emergencias.
   - Reglas de autorizacion en ASP.NET; no confiar solo en la interfaz.
   - Regla de espera <= 60 min; vista solo informa, no la impone.
   - Comprobacion de solapamientos si se usan INSERT directos o horarios
     excepcionales; el procedimiento controla franjas de un medico.
   - Rendimiento exigido en vistas y procedimientos; medir en SQL Server.
   - La aplicacion debe implementar roles, validacion y bitacoras reales.
*/
