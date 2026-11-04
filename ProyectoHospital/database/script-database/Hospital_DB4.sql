/* ============================================================================
   PROYECTO HOSPITAL - SCRIPT DE CREACION ORDENADO
   Fuente: Hospital_DB4 exportada, mas correcciones verificadas hasta el 04/11/2026.

   Convenciones de presentacion:
   - Columnas y restricciones DEFAULT/CHECK en CREATE TABLE.
   - Claves foraneas dentro de su CREATE TABLE, respetando dependencias.
   - Indices agrupados por tabla: conservan UNIQUE, WHERE e INCLUDE.
   - Vistas, procedimientos y triggers separados y con indentacion legible.
   - Sin corchetes en identificadores; se conservan patrones como '%[^0-9]%'.

   Incluye: 30 tablas, 42 FK, 62 CHECK, 47 DEFAULT, 21 indices,
   6 vistas, 1 secuencia, 10 procedimientos almacenados y 11 triggers.

   SOLO ESQUEMA. No se crean usuarios ni se insertan datos.
   Esta version incorpora ajustes posteriores; requiere validacion desde cero
   en SQL Server antes de considerarla definitiva. NO ejecutar sobre una base existente.
   ============================================================================ */
USE master;
GO
IF DB_ID(N'Hospital_DB4') IS NOT NULL
BEGIN
    PRINT N'ADVERTENCIA: Hospital_DB4 ya existe. Se omite la ejecucion.';
    SET NOEXEC ON;
END;
GO
CREATE DATABASE Hospital_DB4;
GO
USE Hospital_DB4;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- 01. TABLAS: columnas, PK, FK, UNIQUE, DEFAULT y CHECK
-- Se crean en orden de dependencias para definir las FK junto a cada tabla.

-- Tabla dbo.BitacoraAccesos
CREATE TABLE dbo.BitacoraAccesos (
    AccesoId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BitacoraAccesos PRIMARY KEY CLUSTERED,
    UsuarioId INT NULL,
    NombreUsuario NVARCHAR(50) NULL,
    Ip NVARCHAR(45) NULL,
    Accion NVARCHAR(100) NOT NULL CONSTRAINT CK_BitAcc_Accion CHECK ((len(ltrim(rtrim(Accion)))>(0))),
    Resultado NVARCHAR(30) NOT NULL CONSTRAINT CK_BitAcc_Resultado CHECK ((len(ltrim(rtrim(Resultado)))>(0))),
    Mensaje NVARCHAR(500) NULL,
    FechaHora DATETIME2(7) NOT NULL CONSTRAINT DF_BitAcc_Fecha DEFAULT (sysdatetime())
);
GO

-- Tabla dbo.BitacoraAuditoria
CREATE TABLE dbo.BitacoraAuditoria (
    AuditoriaId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BitacoraAuditoria PRIMARY KEY CLUSTERED,
    UsuarioId INT NULL,
    Accion NVARCHAR(100) NOT NULL,
    Tabla_Modificada NVARCHAR(100) NULL,
    RegistroId NVARCHAR(100) NULL,
    VAnteriores NVARCHAR(max) NULL,
    VNuevos NVARCHAR(max) NULL,
    Objeto NVARCHAR(100) NULL,
    FechaHora DATETIME2(7) NOT NULL CONSTRAINT DF_BitAud_Fecha DEFAULT (sysdatetime())
);
GO

-- Tabla dbo.BitacoraErrores
CREATE TABLE dbo.BitacoraErrores (
    ErrorId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BitacoraErrores PRIMARY KEY CLUSTERED,
    UsuarioId INT NULL,
    Accion NVARCHAR(100) NOT NULL,
    Objeto NVARCHAR(100) NOT NULL,
    NumeroError INT NULL,
    Severidad INT NULL,
    Linea INT NULL,
    Mensaje NVARCHAR(2000) NOT NULL,
    Resultado NVARCHAR(30) NOT NULL CONSTRAINT DF_BitErr_Resultado DEFAULT (N'Fallido'),
    FechaHora DATETIME2(7) NOT NULL CONSTRAINT DF_BitErr_Fecha DEFAULT (sysdatetime())
);
GO

-- Tabla dbo.BitacoraNotificaciones
CREATE TABLE dbo.BitacoraNotificaciones (
    NotificacionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BitacoraNotificaciones PRIMARY KEY CLUSTERED,
    PacienteId INT NULL,
    CitaId INT NULL,
    Mensaje NVARCHAR(2000) NOT NULL
        CONSTRAINT CK_BitNot_Mensaje CHECK (LEN(LTRIM(RTRIM(Mensaje))) > 0),
    EstadoEnvio NVARCHAR(30) NOT NULL CONSTRAINT DF_BitNot_Estado DEFAULT (N'Pendiente')
        CONSTRAINT CK_BitNot_Estado CHECK (
            (EstadoEnvio=N'Fallida'
            OR EstadoEnvio=N'Enviada'
            OR EstadoEnvio=N'Pendiente')
        ),
    FechaHora DATETIME2(7) NOT NULL CONSTRAINT DF_BitNot_Fecha DEFAULT (sysdatetime())
);
GO

-- Tabla dbo.Clientes
CREATE TABLE dbo.Clientes (
    ClienteId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Clientes PRIMARY KEY CLUSTERED,
    NumeroIdentificacion NVARCHAR(25) NOT NULL
        CONSTRAINT CK_Clientes_Identificacion_Digitos CHECK (
            (datalength(NumeroIdentificacion)>(0)
            AND NOT (NumeroIdentificacion) collate Latin1_General_100_BIN2 like N'%[^0-9]%'
            AND datalength(NumeroIdentificacion)=datalength(rtrim(NumeroIdentificacion)))
        ),
    Nombre NVARCHAR(100) NOT NULL
        CONSTRAINT CK_Clientes_Nombre_NoVacio CHECK (LEN(LTRIM(RTRIM(Nombre))) > 0),
    Apellidos NVARCHAR(130) NOT NULL
        CONSTRAINT CK_Clientes_Apellidos_NoVacios CHECK (LEN(LTRIM(RTRIM(Apellidos))) > 0),
    Telefono NVARCHAR(20) NOT NULL
        CONSTRAINT CK_Clientes_Telefono_Digitos CHECK (
            (datalength(Telefono)>(0)
            AND NOT (Telefono) collate Latin1_General_100_BIN2 like N'%[^0-9]%'
            AND datalength(Telefono)=datalength(rtrim(Telefono)))
        ),
    Activo BIT NOT NULL CONSTRAINT DF_Clientes_Activo DEFAULT ((1)),
    FechaCreacion DATETIME2(0) NOT NULL DEFAULT (sysutcdatetime()),
    FechaModificacion DATETIME2(0) NULL,
    CONSTRAINT CK_Clientes_Nombre CHECK ((Nombre IS NOT NULL AND Apellidos IS NOT NULL))
);
GO

-- Tabla dbo.Enfermedades
CREATE TABLE dbo.Enfermedades (
    EnfermedadId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Enfermedades PRIMARY KEY CLUSTERED,
    Nombre NVARCHAR(150) NOT NULL CONSTRAINT UQ_Enfermedades_Nombre UNIQUE NONCLUSTERED,
    CodigoCIE10 NVARCHAR(10) NULL,
    Descripcion NVARCHAR(400) NULL,
    Sintomas NVARCHAR(300) NULL,
    Tratamiento_General NVARCHAR(500) NULL,
    Prevencion NVARCHAR(500) NULL,
    Especialidad NVARCHAR(100) NULL,
    Activa BIT NOT NULL CONSTRAINT DF_Enfermedades_Activa DEFAULT ((1))
);
GO

-- Tabla dbo.Inventario
CREATE TABLE dbo.Inventario (
    InventarioId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Inventario PRIMARY KEY CLUSTERED,
    Nombre_del_Producto NVARCHAR(200) NOT NULL,
    Categoria NVARCHAR(100) NULL,
    Descripcion NVARCHAR(300) NULL,
    Codigo NVARCHAR(50) NOT NULL CONSTRAINT UQ_Inventario_Codigo UNIQUE NONCLUSTERED,
    StockMinimo INT NOT NULL CONSTRAINT DF_Inventario_StockMin DEFAULT ((0))
    CONSTRAINT CK_Inventario_StockMin CHECK ((StockMinimo>=(0))),
    PrecioVenta DECIMAL(15, 2) NOT NULL CONSTRAINT DF_Inventario_Precio DEFAULT ((0))
    CONSTRAINT CK_Inventario_Precio CHECK ((PrecioVenta>=(0))),
    Vendible BIT NOT NULL CONSTRAINT DF_Inventario_Vendible DEFAULT ((1)),
    FechaVencimiento DATE NULL,
    Activo BIT NOT NULL CONSTRAINT DF_Inventario_Activo DEFAULT ((1))
);
GO

-- Tabla dbo.Pacientes
CREATE TABLE dbo.Pacientes (
    PacienteId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pacientes PRIMARY KEY CLUSTERED,
    Numero_Identificacion NVARCHAR(25) NULL
        CONSTRAINT CK_Pacientes_Identificacion_Digitos CHECK (
            (Numero_Identificacion IS NULL
            OR datalength(Numero_Identificacion)>(0)
            AND NOT (Numero_Identificacion) collate Latin1_General_100_BIN2 like N'%[^0-9]%')
        ),
    Nombre NVARCHAR(100) NOT NULL,
    Apellidos NVARCHAR(130) NOT NULL,
    Fecha_Nacimiento DATE NOT NULL
        CONSTRAINT CK_Pacientes_FechaNacimiento CHECK (
            (Fecha_Nacimiento<=CONVERT(DATE,sysdatetime()))
        ),
    Genero NVARCHAR(15) NOT NULL
        CONSTRAINT CK_Pacientes_Genero CHECK (
            Genero IN (N'Masculino', N'Femenino', N'Otro')
        ),
    TipoSangre NVARCHAR(3) NOT NULL
        CONSTRAINT CK_Pacientes_TipoSangre CHECK (
            TipoSangre COLLATE Latin1_General_100_BIN2 IN (
                N'A+', N'A-', N'B+', N'B-', N'AB+', N'AB-', N'O+', N'O-', N'ND'
            )
            AND DATALENGTH(TipoSangre) = DATALENGTH(RTRIM(TipoSangre))
        ),
    Numero_Telefono NVARCHAR(20) NOT NULL
        CONSTRAINT CK_Pacientes_Telefono_Digitos CHECK (
            (datalength(Numero_Telefono)>(0)
            AND NOT (Numero_Telefono) collate Latin1_General_100_BIN2 like N'%[^0-9]%')
        ),
    Correo NVARCHAR(200) NOT NULL
        CONSTRAINT CK_Pacientes_Correo CHECK (
            Correo = N'No determinado'
            OR (Correo LIKE N'%_@_%._%'
                AND Correo NOT LIKE N'% %'
                AND Correo NOT LIKE N'%@%@%')
        ),
    Direccion NVARCHAR(250) NOT NULL
        CONSTRAINT CK_Pacientes_Direccion_NoVacia CHECK (
            LEN(LTRIM(RTRIM(Direccion))) > 0
        ),
    ContactoEmergencia NVARCHAR(100) NOT NULL
        CONSTRAINT CK_Pacientes_ContactoEmergencia CHECK (
            (len(ltrim(rtrim(ContactoEmergencia)))>(0))
        ),
    TelefonoEmergencia NVARCHAR(20) NOT NULL
        CONSTRAINT CK_Pacientes_TelefonoEmergencia_Digitos CHECK (
            (datalength(TelefonoEmergencia)>(0)
            AND NOT (TelefonoEmergencia) collate Latin1_General_100_BIN2 like N'%[^0-9]%')
        ),
    FechaRegistro DATETIME2(7) NOT NULL CONSTRAINT DF_Pacientes_Fecha DEFAULT (sysdatetime()),
    Activo BIT NOT NULL CONSTRAINT DF_Pacientes_Activo DEFAULT ((1))
);
GO

-- Tabla dbo.Permisos
CREATE TABLE dbo.Permisos (
    PermisoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Permisos PRIMARY KEY CLUSTERED,
    Codigo NVARCHAR(60) NOT NULL CONSTRAINT UQ_Permisos_Codigo UNIQUE NONCLUSTERED,
    Descripcion NVARCHAR(200) NULL,
    FechaCreacion DATETIME2(0) NOT NULL DEFAULT (sysutcdatetime())
);
GO

-- Tabla dbo.Roles
CREATE TABLE dbo.Roles (
    RolId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED,
    Nombre_del_Rol NVARCHAR(50) NOT NULL CONSTRAINT UQ_Roles_Nombre UNIQUE NONCLUSTERED,
    Descripcion NVARCHAR(200) NULL,
    FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_Roles_Creado DEFAULT (sysutcdatetime()),
    FechaModificacion DATETIME2(0) NULL,
    Activo BIT NOT NULL CONSTRAINT DF_Roles_Activo DEFAULT ((1))
);
GO

-- Tabla dbo.Visitantes
CREATE TABLE dbo.Visitantes (
    VisitanteId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Visitantes PRIMARY KEY CLUSTERED,
    NumeroIdentificacion NVARCHAR(25) NOT NULL CONSTRAINT UQ_Visitantes_Identificacion UNIQUE NONCLUSTERED
        CONSTRAINT CK_Visitantes_Identificacion_Digitos CHECK (
            (datalength(NumeroIdentificacion)>(0)
            AND NOT (NumeroIdentificacion) collate Latin1_General_100_BIN2 like N'%[^0-9]%'
            AND datalength(NumeroIdentificacion)=datalength(rtrim(NumeroIdentificacion)))
        ),
    Nombre NVARCHAR(100) NOT NULL,
    Apellidos NVARCHAR(200) NOT NULL,
    Telefono NVARCHAR(20) NOT NULL
        CONSTRAINT CK_Visitantes_Telefono_Digitos CHECK (
            (datalength(Telefono)>(0)
            AND NOT (Telefono) collate Latin1_General_100_BIN2 like N'%[^0-9]%'
            AND datalength(Telefono)=datalength(rtrim(Telefono)))
        )
);
GO

-- Tabla dbo.ClientePaciente
CREATE TABLE dbo.ClientePaciente (
    ClienteId INT NOT NULL,
    PacienteId INT NOT NULL,
    FechaAsociacion DATETIME2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ClientePaciente PRIMARY KEY CLUSTERED (ClienteId, PacienteId),
    CONSTRAINT FK_ClientePaciente_Cliente FOREIGN KEY (ClienteId)
        REFERENCES dbo.Clientes (ClienteId),
    CONSTRAINT FK_ClientePaciente_Paciente FOREIGN KEY (PacienteId)
        REFERENCES dbo.Pacientes (PacienteId)
);
GO

-- Tabla dbo.Empleados
CREATE TABLE dbo.Empleados (
    EmpleadoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Empleados PRIMARY KEY CLUSTERED,
    Numero_Identificacion NVARCHAR(30) NOT NULL CONSTRAINT UQ_Empleados_Identificacion UNIQUE NONCLUSTERED
    CONSTRAINT CK_Empleados_Identificacion_Digitos CHECK (
        (datalength(Numero_Identificacion)>(0)
        AND NOT (Numero_Identificacion) collate Latin1_General_100_BIN2 like N'%[^0-9]%'
        AND datalength(Numero_Identificacion)=datalength(rtrim(Numero_Identificacion)))
    ),
    Nombre NVARCHAR(100) NOT NULL,
    Apellidos NVARCHAR(150) NOT NULL,
    Telefono NVARCHAR(20) NOT NULL
        CONSTRAINT CK_Empleados_Telefono_Digitos CHECK (
            (datalength(Telefono)>(0)
            AND NOT (Telefono) collate Latin1_General_100_BIN2 like N'%[^0-9]%'
            AND datalength(Telefono)=datalength(rtrim(Telefono)))
        ),
    Correo NVARCHAR(150) NULL,
    Puesto NVARCHAR(80) NOT NULL,
    Especialidad NVARCHAR(100) NULL,
    SalarioHora DECIMAL(10, 2) NOT NULL CONSTRAINT CK_Empleados_Salario CHECK ((SalarioHora>(0))),
    EsMedico BIT NOT NULL CONSTRAINT DF_Empleados_EsMedico DEFAULT ((0)),
    HoraEntrada TIME(0) NULL,
    HoraSalida TIME(0) NULL,
    DuracionCitaMin SMALLINT NULL,
    PacienteId INT NULL,
    FechaIngreso DATE NOT NULL CONSTRAINT DF_Empleados_Ingreso DEFAULT (CONVERT(DATE,sysdatetime())),
    Activo BIT NOT NULL CONSTRAINT DF_Empleados_Activo DEFAULT ((1)),
    CONSTRAINT CK_Empleados_Jornada CHECK (
        (EsMedico=(0)
        OR HoraEntrada IS NOT NULL
        AND HoraSalida IS NOT NULL
        AND HoraSalida>HoraEntrada
        AND DuracionCitaMin>(0))
    ),
    CONSTRAINT FK_Empleados_Pacientes FOREIGN KEY (PacienteId)
        REFERENCES dbo.Pacientes (PacienteId)
);
GO

-- Tabla dbo.Medicamentos
CREATE TABLE dbo.Medicamentos (
    MedicamentoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Medicamentos PRIMARY KEY CLUSTERED,
    InventarioId INT NOT NULL CONSTRAINT UQ_Medicamentos_Inventario UNIQUE NONCLUSTERED,
    Presentacion NVARCHAR(100) NULL,
    ViaAdministracion NVARCHAR(30) NULL,
    Indicaciones_Uso NVARCHAR(500) NULL,
    Restricciones NVARCHAR(500) NULL,
    FrecuenciaHoras TINYINT NULL
        CONSTRAINT CK_Medicamentos_Frecuencia CHECK (
            (FrecuenciaHoras IS NULL
            OR FrecuenciaHoras>(0))
        ),
    Requiere_Receta BIT NOT NULL CONSTRAINT DF_Medicamentos_Receta DEFAULT ((0)),
    Activo BIT NOT NULL CONSTRAINT DF_Medicamentos_Activo DEFAULT ((1)),
    CONSTRAINT FK_Medicamentos_Inventario FOREIGN KEY (InventarioId)
        REFERENCES dbo.Inventario (InventarioId)
);
GO

-- Tabla dbo.PacientesEnfermedades
CREATE TABLE dbo.PacientesEnfermedades (
    PacienteId INT NOT NULL,
    EnfermedadId INT NOT NULL,
    FechaIdentificacion DATE NOT NULL CONSTRAINT DF_PacEnf_Fecha DEFAULT (CONVERT(DATE,sysdatetime())),
    Activa BIT NOT NULL CONSTRAINT DF_PacEnf_Activa DEFAULT ((1)),
    EpisodioId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PacientesEnfermedades PRIMARY KEY CLUSTERED,
    CONSTRAINT FK_PacEnf_Enfermedades FOREIGN KEY (EnfermedadId)
        REFERENCES dbo.Enfermedades (EnfermedadId),
    CONSTRAINT FK_PacEnf_Pacientes FOREIGN KEY (PacienteId)
        REFERENCES dbo.Pacientes (PacienteId)
);
GO

-- Tabla dbo.RolPermiso
CREATE TABLE dbo.RolPermiso (
    RolId INT NOT NULL,
    PermisoId INT NOT NULL,
    FechaAsignacion DATETIME2(0) NOT NULL DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_RolPermiso PRIMARY KEY CLUSTERED (RolId, PermisoId),
    CONSTRAINT FK_RolPermiso_Permiso FOREIGN KEY (PermisoId)
        REFERENCES dbo.Permisos (PermisoId),
    CONSTRAINT FK_RolPermiso_Rol FOREIGN KEY (RolId)
        REFERENCES dbo.Roles (RolId)
);
GO

-- Tabla dbo.Medicacion
CREATE TABLE dbo.Medicacion (
    MedicacionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Medicacion PRIMARY KEY CLUSTERED,
    EnfermedadId INT NOT NULL,
    MedicamentoId INT NOT NULL,
    Dosis NVARCHAR(130) NULL,
    Periodicidad NVARCHAR(130) NULL,
    IntervaloHoras TINYINT NULL
        CONSTRAINT CK_Medicacion_IntervaloHoras CHECK (
            (IntervaloHoras IS NULL
            OR IntervaloHoras>(0))
        ),
    Tiempo_medicacion NVARCHAR(130) NULL,
    FechaRegistro DATETIME2(7) NOT NULL CONSTRAINT DF_Medicacion_Fecha DEFAULT (sysdatetime()),
    CONSTRAINT UQ_Medicacion_Par UNIQUE NONCLUSTERED (EnfermedadId, MedicamentoId),
    CONSTRAINT FK_Medicacion_Enfermedades FOREIGN KEY (EnfermedadId)
        REFERENCES dbo.Enfermedades (EnfermedadId),
    CONSTRAINT FK_Medicacion_Medicamentos FOREIGN KEY (MedicamentoId)
        REFERENCES dbo.Medicamentos (MedicamentoId)
);
GO

-- Tabla dbo.Nacimientos
CREATE TABLE dbo.Nacimientos (
    NacimientoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Nacimientos PRIMARY KEY CLUSTERED,
    MadrePacienteId INT NOT NULL,
    BebePacienteId INT NOT NULL CONSTRAINT UQ_Nacimientos_Bebe UNIQUE NONCLUSTERED,
    EmpleadoId INT NOT NULL,
    Fecha_Hora_Nacimiento DATETIME2(0) NOT NULL,
    TipoParto NVARCHAR(30) NOT NULL
        CONSTRAINT CK_Nacimientos_Parto CHECK (
            (TipoParto=N'Cesarea'
            OR TipoParto=N'Vaginal')
        ),
    Semanas_Gestacion TINYINT NULL
        CONSTRAINT CK_Nacimientos_Gestacion CHECK (
            (Semanas_Gestacion IS NULL
            OR Semanas_Gestacion>(0))
        ),
    Peso_en_Gramos INT NULL
        CONSTRAINT CK_Nacimientos_Peso CHECK (
            (Peso_en_Gramos IS NULL
            OR Peso_en_Gramos>(0))
        ),
    Talla_Centimetros DECIMAL(6, 2) NULL
        CONSTRAINT CK_Nacimientos_Talla CHECK (
            (Talla_Centimetros IS NULL
            OR Talla_Centimetros>(0))
        ),
    Apgar1Min TINYINT NULL,
    Apgar5Min TINYINT NULL,
    Respiracion NVARCHAR(100) NULL,
    EstadoSalud NVARCHAR(150) NULL,
    Mal_formaciones_Observadas NVARCHAR(400) NULL,
    Observaciones_Medicas NVARCHAR(500) NULL,
    FechaRegistro DATETIME2(7) NOT NULL CONSTRAINT DF_Nacimientos_Fecha DEFAULT (sysdatetime()),
    Activo BIT NOT NULL CONSTRAINT DF_Nacimientos_Activo DEFAULT ((1)),
    CONSTRAINT CK_Nacimientos_Apgar CHECK (
        ((Apgar1Min IS NULL
        OR Apgar1Min>=(0)
        AND Apgar1Min<=(10))
        AND (Apgar5Min IS NULL
        OR Apgar5Min>=(0)
        AND Apgar5Min<=(10)))
    ),
    CONSTRAINT CK_Nacimientos_Personas CHECK ((MadrePacienteId<>BebePacienteId)),
    CONSTRAINT FK_Nacimientos_Bebe FOREIGN KEY (BebePacienteId)
        REFERENCES dbo.Pacientes (PacienteId),
    CONSTRAINT FK_Nacimientos_Madre FOREIGN KEY (MadrePacienteId)
        REFERENCES dbo.Pacientes (PacienteId),
    CONSTRAINT FK_Nacimientos_Medico FOREIGN KEY (EmpleadoId)
        REFERENCES dbo.Empleados (EmpleadoId)
);
GO

-- Tabla dbo.Usuarios
CREATE TABLE dbo.Usuarios (
    UsuarioId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY CLUSTERED,
    Usuario NVARCHAR(50) NOT NULL CONSTRAINT UQ_Usuarios_Usuario UNIQUE NONCLUSTERED,
    PasswordHash VARBINARY(64) NOT NULL,
    PasswordSalt VARBINARY(16) NOT NULL,
    EmpleadoId INT NULL,
    IntentosFallidos TINYINT NOT NULL CONSTRAINT DF_Usuarios_Intentos DEFAULT ((0)),
    BloqueadoHasta DATETIME2(7) NULL,
    Activo BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT ((1)),
    CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_Usuarios_Created DEFAULT (sysutcdatetime()),
    UpdatedAt DATETIME2(7) NULL,
    CONSTRAINT FK_Usuarios_Empleados FOREIGN KEY (EmpleadoId)
        REFERENCES dbo.Empleados (EmpleadoId)
);
GO

-- Tabla dbo.Ventas
CREATE TABLE dbo.Ventas (
    VentaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ventas PRIMARY KEY CLUSTERED,
    EmpleadoId INT NOT NULL,
    Fecha_Venta DATETIME2(7) NOT NULL CONSTRAINT DF_Ventas_Fecha DEFAULT (sysdatetime()),
    Estado NVARCHAR(40) NOT NULL CONSTRAINT DF_Ventas_Estado DEFAULT (N'Pendiente')
        CONSTRAINT CK_Ventas_Estado CHECK ((Estado=N'Anulada' OR Estado=N'Pagada' OR Estado=N'Pendiente')),
    DescuentoPct DECIMAL(5, 2) NOT NULL CONSTRAINT DF_Ventas_Desc DEFAULT ((0)),
    ImpuestoPct DECIMAL(5, 2) NOT NULL CONSTRAINT DF_Ventas_IVA DEFAULT ((0)),
    NumeroFactura NVARCHAR(20) NULL,
    MetodoPago NVARCHAR(20) NULL
        CONSTRAINT CK_Ventas_Metodo CHECK (
            (MetodoPago IS NULL
            OR (MetodoPago=N'Transferencia'
            OR MetodoPago=N'SINPE'
            OR MetodoPago=N'Tarjeta'
            OR MetodoPago=N'Efectivo'))
        ),
    FechaFactura DATETIME2(7) NULL,
    CONSTRAINT CK_Ventas_Pagada CHECK (
        (Estado<>N'Pagada'
        OR NumeroFactura IS NOT NULL
        AND MetodoPago IS NOT NULL
        AND FechaFactura IS NOT NULL)
    ),
    CONSTRAINT CK_Ventas_Pct CHECK (
        (DescuentoPct>=(0)
        AND DescuentoPct<=(100)
        AND (ImpuestoPct>=(0)
        AND ImpuestoPct<=(100)))
    ),
    CONSTRAINT FK_Ventas_Empleados FOREIGN KEY (EmpleadoId)
        REFERENCES dbo.Empleados (EmpleadoId)
);
GO

-- Tabla dbo.ArticulosVentas
CREATE TABLE dbo.ArticulosVentas (
    ArticulosVentasId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ArticulosVentas PRIMARY KEY CLUSTERED,
    VentaId INT NOT NULL,
    InventarioId INT NOT NULL,
    Cantidad INT NOT NULL CONSTRAINT CK_ArticulosVentas_Cant CHECK ((Cantidad>(0))),
    Precio_Unitario DECIMAL(15, 2) NOT NULL CONSTRAINT CK_ArticulosVentas_Precio CHECK ((Precio_Unitario>=(0))),
    CONSTRAINT UQ_ArticulosVentas UNIQUE NONCLUSTERED (VentaId, InventarioId),
    CONSTRAINT FK_ArticulosVentas_Inventario FOREIGN KEY (InventarioId)
        REFERENCES dbo.Inventario (InventarioId),
    CONSTRAINT FK_ArticulosVentas_Ventas FOREIGN KEY (VentaId)
        REFERENCES dbo.Ventas (VentaId)
);
GO

-- Tabla dbo.Citas
CREATE TABLE dbo.Citas (
    CitaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Citas PRIMARY KEY CLUSTERED,
    PacienteId INT NOT NULL,
    EmpleadoId INT NOT NULL,
    UsuarioRegistroId INT NULL,
    Fecha_Hora DATETIME2(0) NOT NULL,
    TipoCita NVARCHAR(20) NOT NULL CONSTRAINT DF_Citas_Tipo DEFAULT (N'Consulta')
    CONSTRAINT CK_Citas_Tipo CHECK ((TipoCita=N'Emergencia' OR TipoCita=N'Revision' OR TipoCita=N'Consulta')),
    Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_Citas_Estado DEFAULT (N'Programada')
    CONSTRAINT CK_Citas_Estado CHECK (
        (Estado=N'NoAsistio'
        OR Estado=N'Cancelada'
        OR Estado=N'Atendida'
        OR Estado=N'EnAtencion'
        OR Estado=N'EnEspera'
        OR Estado=N'Programada')
    ),
    Motivo NVARCHAR(300) NOT NULL,
    HoraLlegada DATETIME2(0) NULL,
    HoraInicioAtencion DATETIME2(0) NULL,
    HoraFinAtencion DATETIME2(0) NULL,
    EnfermedadId INT NULL,
    Diagnostico NVARCHAR(500) NULL,
    Observaciones NVARCHAR(1000) NULL,
    RequiereControl BIT NOT NULL CONSTRAINT DF_Citas_Control DEFAULT ((0)),
    FechaControlSugerida DATE NULL,
    IndicacionesControl NVARCHAR(500) NULL,
    FechaRegistro DATETIME2(7) NOT NULL CONSTRAINT DF_Citas_Registro DEFAULT (sysdatetime()),
    CONSTRAINT CK_Citas_Atendida CHECK ((Estado<>N'Atendida' OR Diagnostico IS NOT NULL)),
    CONSTRAINT CK_Citas_Control CHECK ((RequiereControl=(0) OR FechaControlSugerida IS NOT NULL)),
    CONSTRAINT CK_Citas_Tiempos CHECK (
        ((HoraInicioAtencion IS NULL
        OR HoraLlegada IS NULL
        OR HoraInicioAtencion>=HoraLlegada)
        AND (HoraFinAtencion IS NULL
        OR HoraInicioAtencion IS NULL
        OR HoraFinAtencion>=HoraInicioAtencion))
    ),
    CONSTRAINT FK_Citas_Empleados FOREIGN KEY (EmpleadoId)
        REFERENCES dbo.Empleados (EmpleadoId),
    CONSTRAINT FK_Citas_Enfermedades FOREIGN KEY (EnfermedadId)
        REFERENCES dbo.Enfermedades (EnfermedadId),
    CONSTRAINT FK_Citas_Pacientes FOREIGN KEY (PacienteId)
        REFERENCES dbo.Pacientes (PacienteId),
    CONSTRAINT FK_Citas_Usuarios FOREIGN KEY (UsuarioRegistroId)
        REFERENCES dbo.Usuarios (UsuarioId)
);
GO

-- Tabla dbo.DetalleVentasClientes
CREATE TABLE dbo.DetalleVentasClientes (
    VentaId INT NOT NULL CONSTRAINT PK_DetalleVentasClientes PRIMARY KEY CLUSTERED,
    ClienteId INT NOT NULL,
    NombreFacturacion NVARCHAR(230) NOT NULL
        CONSTRAINT CK_DetVentaCliente_Nombre CHECK (
            LEN(LTRIM(RTRIM(NombreFacturacion))) > 0
        ),
    IdentificacionFacturacion NVARCHAR(25) NULL
        CONSTRAINT CK_DetVentaCliente_Identificacion CHECK (
            IdentificacionFacturacion IS NULL
            OR LEN(LTRIM(RTRIM(IdentificacionFacturacion))) > 0
        ),
    FechaVinculacion DATETIME2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_DetVentaCliente_IdFact_Digitos CHECK (
        IdentificacionFacturacion IS NULL
        OR (
            DATALENGTH(IdentificacionFacturacion) > 0
            AND IdentificacionFacturacion COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^0-9]%'
            AND DATALENGTH(IdentificacionFacturacion) = DATALENGTH(RTRIM(IdentificacionFacturacion))
        )
    ),
    CONSTRAINT FK_DetVentaCliente_Cliente FOREIGN KEY (ClienteId)
        REFERENCES dbo.Clientes (ClienteId),
    CONSTRAINT FK_DetVentaCliente_Venta FOREIGN KEY (VentaId)
        REFERENCES dbo.Ventas (VentaId)
);
GO

-- Tabla dbo.RegistroVisitas
CREATE TABLE dbo.RegistroVisitas (
    RegistroVisitaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RegistroVisitas PRIMARY KEY CLUSTERED,
    VisitanteId INT NOT NULL,
    PacienteId INT NULL,
    Motivo_Visita NVARCHAR(300) NOT NULL,
    FechaHoraEntrada DATETIME2(0) NOT NULL CONSTRAINT DF_RegistroVisitas_Entrada DEFAULT (sysdatetime()),
    FechaHoraSalida DATETIME2(0) NULL,
    UsuarioEntradaId INT NULL,
    UsuarioSalidaId INT NULL,
    CONSTRAINT CK_RegVis_Salida CHECK ((FechaHoraSalida IS NULL OR FechaHoraSalida>=FechaHoraEntrada)),
    CONSTRAINT FK_RegVis_Pacientes FOREIGN KEY (PacienteId)
        REFERENCES dbo.Pacientes (PacienteId),
    CONSTRAINT FK_RegVis_UsuarioEntrada FOREIGN KEY (UsuarioEntradaId)
        REFERENCES dbo.Usuarios (UsuarioId),
    CONSTRAINT FK_RegVis_UsuarioSalida FOREIGN KEY (UsuarioSalidaId)
        REFERENCES dbo.Usuarios (UsuarioId),
    CONSTRAINT FK_RegVis_Visitantes FOREIGN KEY (VisitanteId)
        REFERENCES dbo.Visitantes (VisitanteId)
);
GO

-- Tabla dbo.UsuarioRol
CREATE TABLE dbo.UsuarioRol (
    UsuarioId INT NOT NULL,
    RolId INT NOT NULL,
    FechaAsignacion DATETIME2(0) NOT NULL DEFAULT (sysutcdatetime()),
    CONSTRAINT PK_UsuarioRol PRIMARY KEY CLUSTERED (UsuarioId, RolId),
    CONSTRAINT FK_UsuarioRol_Rol FOREIGN KEY (RolId)
        REFERENCES dbo.Roles (RolId),
    CONSTRAINT FK_UsuarioRol_Usuario FOREIGN KEY (UsuarioId)
        REFERENCES dbo.Usuarios (UsuarioId)
);
GO

-- Tabla dbo.HistorialMedico
CREATE TABLE dbo.HistorialMedico (
    HistorialId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HistorialMedico PRIMARY KEY CLUSTERED,
    PacienteId INT NOT NULL,
    CitaId INT NULL,
    NacimientoId INT NULL,
    TipoRegistro NVARCHAR(20) NOT NULL
        CONSTRAINT CK_Historial_Tipo CHECK (
            (TipoRegistro=N'Cirugia'
            OR TipoRegistro=N'Alergia'
            OR TipoRegistro=N'Evolucion'
            OR TipoRegistro=N'Antecedente')
        ),
    Descripcion NVARCHAR(1000) NOT NULL
        CONSTRAINT CK_Historial_Descripcion CHECK (
            (len(ltrim(rtrim(Descripcion)))>(0))
        ),
    FechaRegistro DATETIME2(7) NOT NULL CONSTRAINT DF_Historial_Fecha DEFAULT (sysdatetime()),
    CONSTRAINT CK_Historial_Origen CHECK (
        (CitaId IS NOT NULL
        AND NacimientoId IS NULL
        OR CitaId IS NULL
        AND NacimientoId IS NOT NULL
        OR CitaId IS NULL
        AND NacimientoId IS NULL
        AND (TipoRegistro=N'Alergia'
        OR TipoRegistro=N'Antecedente'))
    ),
    CONSTRAINT FK_Historial_Citas FOREIGN KEY (CitaId)
        REFERENCES dbo.Citas (CitaId),
    CONSTRAINT FK_Historial_Nacimientos FOREIGN KEY (NacimientoId)
        REFERENCES dbo.Nacimientos (NacimientoId),
    CONSTRAINT FK_Historial_Pacientes FOREIGN KEY (PacienteId)
        REFERENCES dbo.Pacientes (PacienteId)
);
GO

-- Tabla dbo.MovimientosInventario
CREATE TABLE dbo.MovimientosInventario (
    MovimientoId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientosInventario PRIMARY KEY CLUSTERED,
    InventarioId INT NOT NULL,
    TipoMovimiento NVARCHAR(20) NOT NULL
        CONSTRAINT CK_Mov_Tipo CHECK (
            (TipoMovimiento=N'AjusteSalida'
            OR TipoMovimiento=N'AjusteEntrada'
            OR TipoMovimiento=N'SalidaUsoMedico'
            OR TipoMovimiento=N'SalidaVenta'
            OR TipoMovimiento=N'EntradaCompra')
        ),
    Cantidad INT NOT NULL CONSTRAINT CK_Mov_Cantidad CHECK ((Cantidad>(0))),
    CostoUnitario DECIMAL(15, 2) NULL,
    Proveedor NVARCHAR(100) NULL,
    ArticulosVentasId INT NULL,
    UsuarioId INT NULL,
    FechaHora DATETIME2(7) NOT NULL CONSTRAINT DF_Mov_Fecha DEFAULT (sysdatetime()),
    Observacion NVARCHAR(250) NULL,
    CONSTRAINT CK_Mov_SalidaVenta_ArticuloObligatorio CHECK (
        (TipoMovimiento<>N'SalidaVenta'
        OR ArticulosVentasId IS NOT NULL)
    ),
    CONSTRAINT FK_Mov_Articulos FOREIGN KEY (ArticulosVentasId)
        REFERENCES dbo.ArticulosVentas (ArticulosVentasId),
    CONSTRAINT FK_Mov_Inventario FOREIGN KEY (InventarioId)
        REFERENCES dbo.Inventario (InventarioId),
    CONSTRAINT FK_Mov_Usuarios FOREIGN KEY (UsuarioId)
        REFERENCES dbo.Usuarios (UsuarioId)
);
GO

-- Tabla dbo.ReagendamientosCitas
CREATE TABLE dbo.ReagendamientosCitas (
    ReagendamientoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReagendamientosCitas PRIMARY KEY CLUSTERED,
    CitaId INT NOT NULL,
    CitaPrioritariaId INT NULL,
    FechaHoraAnterior DATETIME2(0) NOT NULL,
    FechaHoraNueva DATETIME2(0) NOT NULL,
    Motivo NVARCHAR(250) NOT NULL CONSTRAINT CK_Reag_Motivo CHECK ((len(ltrim(rtrim(Motivo)))>(0))),
    UsuarioId INT NULL,
    FechaRegistro DATETIME2(7) NOT NULL CONSTRAINT DF_Reag_Fecha DEFAULT (sysdatetime()),
    CONSTRAINT CK_Reag_Diferentes CHECK ((FechaHoraAnterior<>FechaHoraNueva)),
    CONSTRAINT CK_Reag_Prioridad CHECK ((CitaPrioritariaId IS NULL OR CitaPrioritariaId<>CitaId)),
    CONSTRAINT FK_Reag_Cita FOREIGN KEY (CitaId)
        REFERENCES dbo.Citas (CitaId),
    CONSTRAINT FK_Reag_Prioritaria FOREIGN KEY (CitaPrioritariaId)
        REFERENCES dbo.Citas (CitaId),
    CONSTRAINT FK_Reag_Usuario FOREIGN KEY (UsuarioId)
        REFERENCES dbo.Usuarios (UsuarioId)
);
GO

-- Tabla dbo.Recetas
CREATE TABLE dbo.Recetas (
    RecetaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Recetas PRIMARY KEY CLUSTERED,
    CitaId INT NOT NULL,
    MedicamentoId INT NOT NULL,
    Dosis NVARCHAR(60) NOT NULL CONSTRAINT CK_Recetas_Dosis CHECK ((len(ltrim(rtrim(Dosis)))>(0))),
    FrecuenciaHoras TINYINT NOT NULL CONSTRAINT CK_Recetas_FrecuenciaHoras CHECK ((FrecuenciaHoras>(0))),
    DuracionDias SMALLINT NOT NULL CONSTRAINT CK_Recetas_DuracionDias CHECK ((DuracionDias>(0))),
    Indicaciones NVARCHAR(250) NULL,
    CONSTRAINT UQ_Recetas UNIQUE NONCLUSTERED (CitaId, MedicamentoId),
    CONSTRAINT FK_Recetas_Citas FOREIGN KEY (CitaId)
        REFERENCES dbo.Citas (CitaId),
    CONSTRAINT FK_Recetas_Medicamentos FOREIGN KEY (MedicamentoId)
        REFERENCES dbo.Medicamentos (MedicamentoId)
);
GO

-- Tabla dbo.SeguimientosCitas
CREATE TABLE dbo.SeguimientosCitas (
    SeguimientoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeguimientosCitas PRIMARY KEY CLUSTERED,
    CitaOrigenId INT NOT NULL,
    CitaSeguimientoId INT NOT NULL CONSTRAINT UQ_Seg_Seguimiento UNIQUE NONCLUSTERED,
    CONSTRAINT CK_Seg_Diferentes CHECK ((CitaOrigenId<>CitaSeguimientoId)),
    CONSTRAINT FK_Seg_Origen FOREIGN KEY (CitaOrigenId)
        REFERENCES dbo.Citas (CitaId),
    CONSTRAINT FK_Seg_Seguimiento FOREIGN KEY (CitaSeguimientoId)
        REFERENCES dbo.Citas (CitaId)
);
GO

-- 02. INDICES: consultas, filtros UNIQUE y columnas INCLUDE
-- Indices independientes porque requieren CREATE INDEX en SQL Server.

-- Indices de dbo.ArticulosVentas
CREATE NONCLUSTERED INDEX IX_ArticulosVentas_Venta ON dbo.ArticulosVentas (VentaId);
GO

-- Indices de dbo.BitacoraAccesos
CREATE NONCLUSTERED INDEX IX_BitAcc_Fecha ON dbo.BitacoraAccesos (FechaHora);
GO

-- Indices de dbo.BitacoraAuditoria
CREATE NONCLUSTERED INDEX IX_BitAud_Fecha ON dbo.BitacoraAuditoria (FechaHora);
GO

-- Indices de dbo.BitacoraErrores
CREATE NONCLUSTERED INDEX IX_BitErr_Fecha ON dbo.BitacoraErrores (FechaHora);
GO

-- Indices de dbo.BitacoraNotificaciones
CREATE NONCLUSTERED INDEX IX_BitNot_Fecha ON dbo.BitacoraNotificaciones (FechaHora);
GO

-- Indices de dbo.Citas
CREATE NONCLUSTERED INDEX IX_Citas_Fecha
    ON dbo.Citas (Fecha_Hora)
    INCLUDE (EmpleadoId, Estado);
GO

CREATE NONCLUSTERED INDEX IX_Citas_Paciente_Fecha ON dbo.Citas (PacienteId, Fecha_Hora);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Citas_Medico_FechaHora
    ON dbo.Citas (EmpleadoId, Fecha_Hora)
    WHERE (Estado<>N'Cancelada');
GO

-- Indices de dbo.Clientes
CREATE UNIQUE NONCLUSTERED INDEX UX_Clientes_Identificacion
    ON dbo.Clientes (NumeroIdentificacion)
    WHERE (NumeroIdentificacion IS NOT NULL);
GO

-- Indices de dbo.Empleados
CREATE UNIQUE NONCLUSTERED INDEX UX_Empleados_Paciente
    ON dbo.Empleados (PacienteId)
    WHERE (PacienteId IS NOT NULL);
GO

-- Indices de dbo.HistorialMedico
CREATE NONCLUSTERED INDEX IX_Historial_Paciente ON dbo.HistorialMedico (PacienteId, FechaRegistro);
GO

-- Indices de dbo.MovimientosInventario
CREATE NONCLUSTERED INDEX IX_Mov_Inventario
    ON dbo.MovimientosInventario (InventarioId)
    INCLUDE (TipoMovimiento, Cantidad);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Mov_SalidaVenta_Articulo
    ON dbo.MovimientosInventario (ArticulosVentasId)
    WHERE (TipoMovimiento=N'SalidaVenta' AND ArticulosVentasId IS NOT NULL);
GO

-- Indices de dbo.Pacientes
CREATE UNIQUE NONCLUSTERED INDEX UX_Pacientes_Identificacion
    ON dbo.Pacientes (Numero_Identificacion)
    WHERE (Numero_Identificacion IS NOT NULL);
GO

-- Indices de dbo.PacientesEnfermedades
CREATE NONCLUSTERED INDEX IX_PacEnf_Enfermedad ON dbo.PacientesEnfermedades (EnfermedadId);
GO

-- Indices de dbo.ReagendamientosCitas
CREATE NONCLUSTERED INDEX IX_Reag_Cita_Fecha ON dbo.ReagendamientosCitas (CitaId, FechaRegistro);
GO

-- Indices de dbo.RegistroVisitas
CREATE NONCLUSTERED INDEX IX_RegVis_Entrada ON dbo.RegistroVisitas (FechaHoraEntrada);
GO

CREATE NONCLUSTERED INDEX IX_RegVis_Visitante ON dbo.RegistroVisitas (VisitanteId, FechaHoraEntrada);
GO

-- Indices de dbo.SeguimientosCitas
CREATE NONCLUSTERED INDEX IX_Seg_Origen ON dbo.SeguimientosCitas (CitaOrigenId);
GO

-- Indices de dbo.Usuarios
CREATE UNIQUE NONCLUSTERED INDEX UX_Usuarios_Empleado
    ON dbo.Usuarios (EmpleadoId)
    WHERE EmpleadoId IS NOT NULL;
GO

-- Indices de dbo.Ventas
CREATE UNIQUE NONCLUSTERED INDEX UX_Ventas_NumeroFactura
    ON dbo.Ventas (NumeroFactura)
    WHERE (NumeroFactura IS NOT NULL);
GO

-- 03. SECUENCIA


CREATE SEQUENCE dbo.Seq_FacturaInterna 
 AS BIGINT
 START WITH 1
 INCREMENT BY 1
 MINVALUE -9223372036854775808
 MAXVALUE 9223372036854775807
 NO CACHE
GO

-- 04. VISTAS


-- Por seguridad, NO se crea un administrador con una contrasena conocida.
-- El alta del primer usuario se debe realizar desde la aplicacion o un
-- script de aprovisionamiento SEPARADO que use un hasher robusto compatible
-- con ASP.NET Identity. Nunca subir secretos a GitHub.

/* =====================================================================
   VIEWS (valores calculados: no se guardan en tablas)
   ===================================================================== */

-- Stock actual = entradas - salidas
CREATE VIEW dbo.vw_StockInventario
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
CREATE VIEW dbo.vw_VentaTotales
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
CREATE VIEW dbo.vw_EsperaCitas
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
CREATE VIEW dbo.vw_MedicamentosVendibles
AS
SELECT
    m.MedicamentoId,
    m.InventarioId,
    i.Codigo,
    i.Nombre_del_Producto,
    i.PrecioVenta,
    m.Presentacion,
    m.Requiere_Receta
FROM dbo.Medicamentos m
JOIN dbo.Inventario i ON i.InventarioId = m.InventarioId
WHERE i.Activo = 1 AND i.Vendible = 1 AND m.Activo = 1;
GO

-- Duracion de cada visita; separa persona de cada visita
CREATE VIEW dbo.vw_DuracionVisitas
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

-- Vista de auditoria de relaciones, para comprobar que se ajustaron las FK.
CREATE VIEW dbo.vw_AuditoriaRelaciones AS
SELECT OBJECT_NAME(f.parent_object_id) TablaOrigen,
       OBJECT_NAME(f.referenced_object_id) TablaDestino,
       f.name NombreFK
FROM sys.foreign_keys f;
GO

-- 05. PROCEDIMIENTOS ALMACENADOS


CREATE PROCEDURE dbo.usp_CrearVentaBorrador
    @ClienteId INT,
    @EmpleadoId INT,
    @VentaId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.Clientes
            WHERE ClienteId = @ClienteId AND Activo = 1
        )
            THROW 51021, N'Cliente no existe o esta inactivo.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.Empleados
            WHERE EmpleadoId = @EmpleadoId AND Activo = 1
        )
            THROW 51022, N'Empleado no existe o esta inactivo.', 1;

        INSERT INTO dbo.Ventas (EmpleadoId)
        VALUES (@EmpleadoId);

        SET @VentaId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT INTO dbo.DetalleVentasClientes
            (VentaId, ClienteId, NombreFacturacion, IdentificacionFacturacion)
        SELECT
            @VentaId,
            ClienteId,
            CONCAT(Nombre, N' ', Apellidos),
            NumeroIdentificacion
        FROM dbo.Clientes
        WHERE ClienteId = @ClienteId;

        INSERT INTO dbo.BitacoraAuditoria
            (Accion, Tabla_Modificada, RegistroId, Objeto)
        VALUES
            (N'INSERT', N'Ventas', CONVERT(NVARCHAR(100), @VentaId), N'usp_CrearVentaBorrador');

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE PROCEDURE dbo.usp_EmitirFactura
    @VentaId INT,
    @MetodoPago NVARCHAR(30),
    @UsuarioId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Estado NVARCHAR(30);
    DECLARE @FacturaActual NVARCHAR(100);
    DECLARE @Consecutivo BIGINT;
    DECLARE @NumeroFactura NVARCHAR(40);
    DECLARE @CantidadArticulos INT;
    DECLARE @ProductosBloqueados BIGINT;

    BEGIN TRY

        -- 1. Validar metodo de pago
        IF @MetodoPago IS NULL
           OR @MetodoPago NOT IN (
                N'Efectivo',
                N'Tarjeta',
                N'SINPE',
                N'Transferencia'
           )
        BEGIN
            THROW 51020, 'Metodo de pago no valido.', 1;
        END;

        BEGIN TRANSACTION;

        -- 2. Bloquear y validar la venta
        SELECT
            @Estado = Estado,
            @FacturaActual = NumeroFactura
        FROM dbo.Ventas WITH (UPDLOCK, HOLDLOCK)
        WHERE VentaId = @VentaId;

        IF @Estado IS NULL
            THROW 51021, 'La venta no existe.', 1;

        IF @Estado <> N'Pendiente'
            THROW 51022, 'La venta no esta pendiente.', 1;

        IF @FacturaActual IS NOT NULL
            THROW 51023, 'La venta ya tiene numero de factura.', 1;

        -- 3. Comprobar que tenga articulos
        SELECT @CantidadArticulos = COUNT(*)
        FROM dbo.ArticulosVentas WITH (UPDLOCK, HOLDLOCK)
        WHERE VentaId = @VentaId;

        IF @CantidadArticulos = 0
            THROW 51024,
                'No se puede facturar una venta sin articulos.', 1;

        -- 4. Bloquear los productos involucrados
        -- mientras se valida y descuenta su stock
        SELECT @ProductosBloqueados = COUNT_BIG(*)
        FROM dbo.Inventario AS i WITH (UPDLOCK, HOLDLOCK)
        WHERE EXISTS (
            SELECT 1
            FROM dbo.ArticulosVentas AS a
            WHERE a.VentaId = @VentaId
              AND a.InventarioId = i.InventarioId
        );

        -- 5. Validar productos activos,
        -- vendibles y no vencidos
        IF EXISTS (
            SELECT 1
            FROM dbo.ArticulosVentas AS a
            INNER JOIN dbo.Inventario AS i
                ON a.InventarioId = i.InventarioId
            WHERE a.VentaId = @VentaId
              AND (
                  i.Activo = 0
                  OR i.Vendible = 0
                  OR (
                      i.FechaVencimiento IS NOT NULL
                      AND i.FechaVencimiento
                          < CONVERT(DATE, SYSDATETIME())
                  )
              )
        )
        BEGIN
            THROW 51036,
                'La venta contiene productos inactivos, no vendibles o vencidos.',
                1;
        END;

        -- 6. Evitar salidas duplicadas
        IF EXISTS (
            SELECT 1
            FROM dbo.ArticulosVentas AS a
            INNER JOIN dbo.MovimientosInventario AS m
                ON a.ArticulosVentasId = m.ArticulosVentasId
            WHERE a.VentaId = @VentaId
              AND m.TipoMovimiento = N'SalidaVenta'
        )
        BEGIN
            THROW 51037,
                'La venta contiene articulos con salidas ya registradas.',
                1;
        END;

        -- 7. Comprobar existencias suficientes
        -- Agrupar por producto para evitar sobreventa
        IF EXISTS (
            SELECT 1
            FROM (
                SELECT
                    InventarioId,
                    SUM(CONVERT(BIGINT, Cantidad))
                        AS CantidadRequerida
                FROM dbo.ArticulosVentas
                WHERE VentaId = @VentaId
                GROUP BY InventarioId
            ) AS r
            LEFT JOIN dbo.vw_StockInventario AS s
                ON r.InventarioId = s.InventarioId
            WHERE ISNULL(s.StockActual, 0)
                < r.CantidadRequerida
        )
        BEGIN
            THROW 51038,
                'No hay suficientes existencias para facturar.',
                1;
        END;

        -- 8. Generar numero interno de factura
        SELECT @Consecutivo =
            NEXT VALUE FOR dbo.Seq_FacturaInterna;

        SET @NumeroFactura = N'FAC-' +
            CASE
                WHEN @Consecutivo < 1000000
                THEN RIGHT(
                    N'000000' +
                    CONVERT(NVARCHAR(20), @Consecutivo),
                    6
                )
                ELSE CONVERT(NVARCHAR(20), @Consecutivo)
            END;

        -- 9. Registrar salidas de inventario
        INSERT INTO dbo.MovimientosInventario
        (
            InventarioId,
            ArticulosVentasId,
            TipoMovimiento,
            Cantidad,
            UsuarioId,
            Observacion
        )
        SELECT
            a.InventarioId,
            a.ArticulosVentasId,
            N'SalidaVenta',
            a.Cantidad,
            @UsuarioId,
            CONCAT(N'Factura interna ', @NumeroFactura)
        FROM dbo.ArticulosVentas AS a
        WHERE a.VentaId = @VentaId;

        -- 10. Facturar la venta
        UPDATE dbo.Ventas
        SET
            Estado = N'Pagada',
            NumeroFactura = @NumeroFactura,
            MetodoPago = @MetodoPago,
            FechaFactura = SYSDATETIME()
        WHERE VentaId = @VentaId;

        -- 11. Registrar auditoria
        INSERT INTO dbo.BitacoraAuditoria
        (
            UsuarioId,
            Accion,
            Tabla_Modificada,
            RegistroId,
            VAnteriores,
            VNuevos,
            Objeto
        )
        VALUES
        (
            @UsuarioId,
            N'UPDATE',
            N'Ventas',
            CONVERT(NVARCHAR(100), @VentaId),
            N'Estado=Pendiente; NumeroFactura=NULL',
            CONCAT(
                N'Estado=Pagada; NumeroFactura=',
                @NumeroFactura,
                N'; MetodoPago=',
                @MetodoPago
            ),
            N'usp_EmitirFactura'
        );

        COMMIT TRANSACTION;

        -- 12. Mostrar resultado
        SELECT
            VentaId,
            Estado,
            NumeroFactura,
            MetodoPago,
            FechaFactura
        FROM dbo.Ventas
        WHERE VentaId = @VentaId;

    END TRY
    BEGIN CATCH

        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;

    END CATCH;
END;
GO

CREATE PROCEDURE dbo.usp_ReagendarCita
    @CitaId INT,
    @FechaHoraNueva DATETIME2,
    @Motivo NVARCHAR(MAX),
    @UsuarioId INT = NULL,
    @CitaPrioritariaId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Validar datos
        IF @CitaId IS NULL
            THROW 51030,
                N'El identificador de la cita es obligatorio.', 1;

        IF @FechaHoraNueva IS NULL
            THROW 51031,
                N'La nueva fecha y hora son obligatorias.', 1;

        IF @Motivo IS NULL
           OR LEN(LTRIM(RTRIM(@Motivo))) = 0
            THROW 51032,
                N'El motivo del reagendamiento es obligatorio.', 1;

        -- 2. Obtener y bloquear la cita
        DECLARE @FechaHoraAnterior DATETIME2;

        SELECT @FechaHoraAnterior = Fecha_Hora
        FROM dbo.Citas WITH (UPDLOCK, HOLDLOCK)
        WHERE CitaId = @CitaId;

        IF @FechaHoraAnterior IS NULL
            THROW 51033,
                N'La cita indicada no existe.', 1;

        IF @FechaHoraAnterior = @FechaHoraNueva
            THROW 51034,
                N'La nueva fecha debe ser diferente de la anterior.', 1;

        -- 3. Actualizar la cita
        UPDATE dbo.Citas
        SET Fecha_Hora = @FechaHoraNueva
        WHERE CitaId = @CitaId;

        -- 4. Registrar el historial del reagendamiento
        INSERT INTO dbo.ReagendamientosCitas
        (
            CitaId,
            CitaPrioritariaId,
            FechaHoraAnterior,
            FechaHoraNueva,
            Motivo,
            UsuarioId
        )
        VALUES
        (
            @CitaId,
            @CitaPrioritariaId,
            @FechaHoraAnterior,
            @FechaHoraNueva,
            @Motivo,
            @UsuarioId
        );

        -- 5. NUEVO: Registrar auditoria
        INSERT INTO dbo.BitacoraAuditoria
        (
            UsuarioId,
            Accion,
            Tabla_Modificada,
            RegistroId,
            VAnteriores,
            VNuevos,
            Objeto
        )
        VALUES
        (
            @UsuarioId,
            N'UPDATE',
            N'Citas',
            CONVERT(NVARCHAR(100), @CitaId),
            CONCAT(
                N'Fecha_Hora=',
                CONVERT(NVARCHAR(30), @FechaHoraAnterior, 126)
            ),
            CONCAT(
                N'Fecha_Hora=',
                CONVERT(NVARCHAR(30), @FechaHoraNueva, 126)
            ),
            N'usp_ReagendarCita'
        );

        -- 6. Confirmar las tres operaciones
        COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH

        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;

    END CATCH;
END;
GO

/* =====================================================================
   PROCEDIMIENTO: agendar una cita
     - Verifica paciente y medico
     - Verifica que la hora este dentro de la jornada (lunes a viernes)
     - Si el espacio esta ocupado, informa la siguiente hora libre del dia
     - TRY...CATCH con ROLLBACK y registro del error en BitacoraErrores
   ===================================================================== */
CREATE PROCEDURE dbo.usp_RegistrarCita
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
        -- La cita previa ya no se almacena dentro de Citas.
        -- Validar que corresponda al mismo paciente si se informa.
        IF @CitaPreviaId IS NOT NULL AND NOT EXISTS(
            SELECT 1 FROM dbo.Citas
            WHERE CitaId = @CitaPreviaId AND PacienteId = @PacienteId
        ) THROW 50005, N'La cita previa no pertenece al paciente.', 1;

        INSERT INTO dbo.Citas (PacienteId, EmpleadoId, UsuarioRegistroId, Fecha_Hora, TipoCita, Motivo)
        VALUES (@PacienteId, @EmpleadoId, @UsuarioId, @Fecha_Hora, @TipoCita, @Motivo);

        SET @CitaId = CONVERT(INT, SCOPE_IDENTITY());

        IF @CitaPreviaId IS NOT NULL
            INSERT INTO dbo.SeguimientosCitas(CitaOrigenId, CitaSeguimientoId)
            VALUES(@CitaPreviaId, @CitaId);

        INSERT INTO dbo.BitacoraAuditoria (UsuarioId, Accion, Tabla_Modificada, RegistroId, VNuevos, Objeto)
        VALUES (@UsuarioId, N'INSERT', N'Citas', CAST(@CitaId AS NVARCHAR(100)),
                CONCAT(N'PacienteId=', @PacienteId, N'; EmpleadoId=', @EmpleadoId, N'; Fecha=', CONVERT(NVARCHAR(19), @Fecha_Hora, 120)),
                N'usp_RegistrarCita');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        DECLARE @ErrNum INT = ERROR_NUMBER(), @ErrSev INT = ERROR_SEVERITY(),
                @ErrLinea INT = ERROR_LINE(), @ErrMsg NVARCHAR(2000) = ERROR_MESSAGE();
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Registrar el error despues del rollback sin ocultar el error original.
        BEGIN TRY
            INSERT INTO dbo.BitacoraErrores(UsuarioId, Accion, Objeto, NumeroError, Severidad, Linea, Mensaje)
            VALUES(@UsuarioId, N'PROCEDIMIENTO', N'usp_RegistrarCita', @ErrNum, @ErrSev, @ErrLinea, @ErrMsg);
        END TRY
        BEGIN CATCH
            SET NOCOUNT ON; -- Mantener el error original.
        END CATCH;
        THROW;
    END CATCH
END
GO

CREATE PROCEDURE dbo.usp_VincularClientePaciente
    @ClienteId INT,
    @PacienteId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.Clientes WHERE ClienteId = @ClienteId
        ) OR NOT EXISTS (
            SELECT 1 FROM dbo.Pacientes WHERE PacienteId = @PacienteId
        )
            THROW 51020, N'Cliente o paciente inexistente.', 1;

        INSERT INTO dbo.ClientePaciente (ClienteId, PacienteId)
        VALUES (@ClienteId, @PacienteId);

        INSERT INTO dbo.BitacoraAuditoria
            (Accion, Tabla_Modificada, RegistroId, Objeto)
        VALUES
            (N'VINCULAR', N'ClientePaciente', CONVERT(NVARCHAR(100), @ClienteId), N'usp_VincularClientePaciente');

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK;
        THROW;
    END CATCH
END;
GO



-- Procedimientos de registro controlado de pacientes.

CREATE OR ALTER PROCEDURE dbo.usp_RegistrarPaciente
    @NumeroIdentificacion NVARCHAR(25),
    @Nombre NVARCHAR(100),
    @Apellidos NVARCHAR(130),
    @FechaNacimiento DATE,
    @Genero NVARCHAR(15),
    @TipoSangre NVARCHAR(3),
    @NumeroTelefono NVARCHAR(20),
    @Correo NVARCHAR(200),
    @Direccion NVARCHAR(250),
    @ContactoEmergencia NVARCHAR(100),
    @TelefonoEmergencia NVARCHAR(20),
    @PacienteId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @PacienteId = NULL;

    IF @NumeroIdentificacion IS NULL
       OR DATALENGTH(@NumeroIdentificacion) = 0
       OR @NumeroIdentificacion COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9]%'
       OR DATALENGTH(@NumeroIdentificacion) <> DATALENGTH(RTRIM(@NumeroIdentificacion))
        THROW 51100, N'El paciente normal necesita una identificacion numerica valida.', 1;

    IF NULLIF(LTRIM(RTRIM(@Nombre)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Apellidos)), N'') IS NULL
        THROW 51101, N'Nombre y apellidos son obligatorios.', 1;

    IF @FechaNacimiento IS NULL OR @FechaNacimiento > CONVERT(DATE, SYSDATETIME())
        THROW 51102, N'La fecha de nacimiento no es valida.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.Pacientes (
            Numero_Identificacion, Nombre, Apellidos, Fecha_Nacimiento,
            Genero, TipoSangre, Numero_Telefono, Correo, Direccion,
            ContactoEmergencia, TelefonoEmergencia
        ) VALUES (
            @NumeroIdentificacion, @Nombre, @Apellidos, @FechaNacimiento,
            @Genero, @TipoSangre, @NumeroTelefono, @Correo, @Direccion,
            @ContactoEmergencia, @TelefonoEmergencia
        );

        SET @PacienteId = CONVERT(INT, SCOPE_IDENTITY());
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @PacienteId = NULL;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RegistrarRecienNacido
    @MadrePacienteId INT,
    @EmpleadoId INT,
    @Nombre NVARCHAR(100),
    @Apellidos NVARCHAR(130),
    @Genero NVARCHAR(15),
    @NumeroTelefono NVARCHAR(20),
    @Direccion NVARCHAR(250),
    @ContactoEmergencia NVARCHAR(100),
    @TelefonoEmergencia NVARCHAR(20),
    @FechaHoraNacimiento DATETIME2,
    @TipoParto NVARCHAR(30),
    @SemanasGestacion TINYINT = NULL,
    @PesoEnGramos INT = NULL,
    @TallaCentimetros DECIMAL(6,2) = NULL,
    @Apgar1Min TINYINT = NULL,
    @Apgar5Min TINYINT = NULL,
    @Respiracion NVARCHAR(100) = NULL,
    @EstadoSalud NVARCHAR(150) = NULL,
    @Malformaciones NVARCHAR(400) = NULL,
    @ObservacionesMedicas NVARCHAR(500) = NULL,
    @BebePacienteId INT = NULL OUTPUT,
    @NacimientoId INT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @BebePacienteId = NULL;
    SET @NacimientoId = NULL;

    IF NULLIF(LTRIM(RTRIM(@Nombre)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Apellidos)), N'') IS NULL
        THROW 51030, N'El nombre y los apellidos del bebe son obligatorios.', 1;

    IF @FechaHoraNacimiento IS NULL OR @FechaHoraNacimiento > SYSDATETIME()
        THROW 51032, N'La fecha de nacimiento no es valida.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Pacientes
        WHERE PacienteId = @MadrePacienteId AND Activo = 1
    )
        THROW 51033, N'La madre no existe o esta inactiva.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Empleados
        WHERE EmpleadoId = @EmpleadoId AND Activo = 1 AND EsMedico = 1
    )
        THROW 51034, N'El medico no existe, esta inactivo o no es medico.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.Pacientes (
            Numero_Identificacion, Nombre, Apellidos, Fecha_Nacimiento,
            Genero, TipoSangre, Numero_Telefono, Correo, Direccion,
            ContactoEmergencia, TelefonoEmergencia
        ) VALUES (
            NULL, @Nombre, @Apellidos, CONVERT(DATE, @FechaHoraNacimiento),
            @Genero, N'ND', @NumeroTelefono, N'No determinado', @Direccion,
            @ContactoEmergencia, @TelefonoEmergencia
        );

        SET @BebePacienteId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT INTO dbo.Nacimientos (
            MadrePacienteId, BebePacienteId, EmpleadoId,
            Fecha_Hora_Nacimiento, TipoParto, Semanas_Gestacion,
            Peso_en_Gramos, Talla_Centimetros, Apgar1Min, Apgar5Min,
            Respiracion, EstadoSalud, Mal_formaciones_Observadas,
            Observaciones_Medicas
        ) VALUES (
            @MadrePacienteId, @BebePacienteId, @EmpleadoId,
            @FechaHoraNacimiento, @TipoParto, @SemanasGestacion,
            @PesoEnGramos, @TallaCentimetros, @Apgar1Min, @Apgar5Min,
            @Respiracion, @EstadoSalud, @Malformaciones,
            @ObservacionesMedicas
        );

        SET @NacimientoId = CONVERT(INT, SCOPE_IDENTITY());
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @BebePacienteId = NULL;
        SET @NacimientoId = NULL;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AsignarIdentificacionRecienNacido
    @PacienteId INT,
    @NumeroIdentificacion NVARCHAR(25)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @NumeroIdentificacion IS NULL
       OR DATALENGTH(@NumeroIdentificacion) = 0
       OR @NumeroIdentificacion COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9]%'
       OR DATALENGTH(@NumeroIdentificacion) <> DATALENGTH(RTRIM(@NumeroIdentificacion))
        THROW 51110, N'La identificacion oficial debe contener solo digitos.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.Pacientes AS p WITH (UPDLOCK, HOLDLOCK)
            JOIN dbo.Nacimientos AS n ON n.BebePacienteId = p.PacienteId
            WHERE p.PacienteId = @PacienteId
              AND p.Numero_Identificacion IS NULL
        )
            THROW 51111, N'El paciente no es un recien nacido pendiente de identificacion.', 1;

        UPDATE dbo.Pacientes
        SET Numero_Identificacion = @NumeroIdentificacion
        WHERE PacienteId = @PacienteId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO


-- Cambio seguro de una cita activa a emergencia, con auditoria.
CREATE OR ALTER PROCEDURE dbo.usp_ConvertirCitaAEmergencia
    @CitaId INT,
    @Motivo NVARCHAR(500),
    @UsuarioId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TipoAnterior NVARCHAR(30);
    DECLARE @EstadoActual NVARCHAR(30);

    BEGIN TRY
        IF NULLIF(LTRIM(RTRIM(@Motivo)), N'') IS NULL
            THROW 51070, N'El motivo del cambio es obligatorio.', 1;

        BEGIN TRANSACTION;

        SELECT @TipoAnterior = TipoCita, @EstadoActual = Estado
        FROM dbo.Citas WITH (UPDLOCK, HOLDLOCK)
        WHERE CitaId = @CitaId;

        IF @TipoAnterior IS NULL
            THROW 51071, N'La cita indicada no existe.', 1;
        IF @TipoAnterior = N'Emergencia'
            THROW 51072, N'La cita ya esta registrada como emergencia.', 1;
        IF @EstadoActual NOT IN (N'Programada', N'EnEspera', N'EnAtencion')
            THROW 51073, N'El estado de la cita no permite este cambio.', 1;

        UPDATE dbo.Citas
        SET TipoCita = N'Emergencia'
        WHERE CitaId = @CitaId;

        INSERT INTO dbo.BitacoraAuditoria (
            UsuarioId, Accion, Tabla_Modificada,
            RegistroId, VAnteriores, VNuevos, Objeto
        ) VALUES (
            @UsuarioId,
            N'CAMBIO_A_EMERGENCIA',
            N'Citas',
            CONVERT(NVARCHAR(100), @CitaId),
            CONCAT(N'TipoCita=', @TipoAnterior),
            CONCAT(N'TipoCita=Emergencia; Motivo=', LTRIM(RTRIM(@Motivo))),
            N'usp_ConvertirCitaAEmergencia'
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Registro de notificaciones validado, sin FK desde la bitacora.
CREATE OR ALTER PROCEDURE dbo.usp_RegistrarNotificacion
    @PacienteId INT = NULL,
    @CitaId INT = NULL,
    @Mensaje NVARCHAR(MAX),
    @EstadoEnvio NVARCHAR(20) = N'Pendiente',
    @NotificacionId BIGINT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @PacienteDeCita INT;

    BEGIN TRY
        IF @Mensaje IS NULL OR LEN(LTRIM(RTRIM(@Mensaje))) = 0
            THROW 51120, N'El mensaje no puede estar vacio.', 1;

        IF @EstadoEnvio IS NULL OR @EstadoEnvio NOT IN (
            N'Pendiente', N'Enviada', N'Fallida'
        )
            THROW 51121, N'El estado de envio no es valido.', 1;

        -- La columna Mensaje es NVARCHAR(2000); evitar truncamientos.
        IF LEN(@Mensaje) > 2000
            THROW 51125, N'El mensaje supera el maximo de 2000 caracteres.', 1;

        BEGIN TRANSACTION;

        IF @CitaId IS NOT NULL
        BEGIN
            SELECT @PacienteDeCita = PacienteId
            FROM dbo.Citas WITH (UPDLOCK, HOLDLOCK)
            WHERE CitaId = @CitaId;

            IF @PacienteDeCita IS NULL
                THROW 51122, N'La cita indicada no existe.', 1;

            IF @PacienteId IS NULL
                SET @PacienteId = @PacienteDeCita;
            ELSE IF @PacienteId <> @PacienteDeCita
                THROW 51123, N'La cita no pertenece al paciente indicado.', 1;
        END;

        IF @PacienteId IS NOT NULL AND NOT EXISTS (
            SELECT 1
            FROM dbo.Pacientes WITH (UPDLOCK, HOLDLOCK)
            WHERE PacienteId = @PacienteId
        )
            THROW 51124, N'El paciente indicado no existe.', 1;

        INSERT INTO dbo.BitacoraNotificaciones (
            PacienteId, CitaId, Mensaje, EstadoEnvio, FechaHora
        ) VALUES (
            @PacienteId, @CitaId, LTRIM(RTRIM(@Mensaje)),
            @EstadoEnvio, SYSDATETIME()
        );

        SET @NotificacionId = CONVERT(BIGINT, SCOPE_IDENTITY());
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- 06. TRIGGERS


CREATE TRIGGER dbo.TR_ArticulosVentas_ProtegerPagadas
ON dbo.ArticulosVentas
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Impedir agregar o modificar articulos
    -- que pertenezcan a ventas pagadas.
    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        INNER JOIN dbo.Ventas AS v
            ON i.VentaId = v.VentaId
        WHERE v.Estado = N'Pagada'
    )
    OR EXISTS (
        -- Impedir modificar, mover o eliminar
        -- articulos de ventas ya pagadas.
        SELECT 1
        FROM deleted AS d
        INNER JOIN dbo.Ventas AS v
            ON d.VentaId = v.VentaId
        WHERE v.Estado = N'Pagada'
    )
    BEGIN
        THROW 51040,
            'No se pueden modificar los articulos de una venta pagada.',
            1;
    END;
END;
GO

CREATE TRIGGER dbo.TR_ArticulosVentas_ProtegerSalida
ON dbo.ArticulosVentas
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Proteger el producto vendido
    IF UPDATE(InventarioId)
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM inserted AS i
            INNER JOIN dbo.MovimientosInventario AS m
                ON i.ArticulosVentasId = m.ArticulosVentasId
            WHERE m.TipoMovimiento = N'SalidaVenta'
              AND i.InventarioId <> m.InventarioId
        )
        BEGIN
            THROW 51031,
                'No se puede cambiar el producto porque ya existe una salida de inventario.',
                1;
        END;
    END;

    -- 2. Proteger la cantidad vendida
    IF UPDATE(Cantidad)
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM inserted AS i
            INNER JOIN dbo.MovimientosInventario AS m
                ON i.ArticulosVentasId = m.ArticulosVentasId
            WHERE m.TipoMovimiento = N'SalidaVenta'
              AND i.Cantidad <> m.Cantidad
        )
        BEGIN
            THROW 51033,
                'No se puede modificar la cantidad porque ya existe una salida de inventario.',
                1;
        END;
    END;
END;
GO

CREATE TRIGGER dbo.TR_Citas_ProtegerHistorialMedico
ON dbo.Citas
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Solo revisar cambios de paciente o estado.
    IF NOT (
        UPDATE(PacienteId)
        OR UPDATE(Estado)
    )
        RETURN;

    -- Evitar inconsistencias con el historial médico.
    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        INNER JOIN dbo.HistorialMedico AS h
            ON h.CitaId = i.CitaId
        WHERE
            h.PacienteId <> i.PacienteId
            OR i.Estado <> N'Atendida'
    )
    BEGIN
        THROW 51016,
            N'No se puede modificar la cita: existen registros de historial medico asociados que requieren el mismo paciente y estado Atendida.',
            1;
    END;
END;
GO

CREATE TRIGGER dbo.TR_Citas_ProtegerSeguimientos
ON dbo.Citas
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Ejecutar la validacion solamente
    -- cuando se actualice PacienteId.
    IF NOT UPDATE(PacienteId)
        RETURN;

    -- Comprobar si alguna cita modificada
    -- queda relacionada con otro paciente.
    IF EXISTS (
        SELECT 1
        FROM dbo.SeguimientosCitas AS sc
        INNER JOIN dbo.Citas AS origen
            ON origen.CitaId = sc.CitaOrigenId
        INNER JOIN dbo.Citas AS seguimiento
            ON seguimiento.CitaId = sc.CitaSeguimientoId
        WHERE origen.PacienteId <> seguimiento.PacienteId
          AND (
              sc.CitaOrigenId IN (
                  SELECT CitaId FROM inserted
              )
              OR sc.CitaSeguimientoId IN (
                  SELECT CitaId FROM inserted
              )
          )
    )
    BEGIN
        THROW 51015,
            N'No se puede cambiar el paciente: existen seguimientos incompatibles.',
            1;
    END;
END;
GO

CREATE TRIGGER dbo.TR_HistorialMedico_ValidaCita
ON dbo.HistorialMedico
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY

        -- 1. Validar que la cita este atendida
        -- y corresponda al mismo paciente
        IF EXISTS (
            SELECT 1
            FROM inserted AS i
            INNER JOIN dbo.Citas AS c
                ON c.CitaId = i.CitaId
            WHERE c.Estado <> N'Atendida'
               OR c.PacienteId <> i.PacienteId
        )
        BEGIN
            THROW 50010,
                N'El historial requiere cita atendida del mismo paciente.',
                1;
        END;

        -- 2. Validar que el nacimiento
        -- corresponda al bebe paciente
        IF EXISTS (
            SELECT 1
            FROM inserted AS i
            INNER JOIN dbo.Nacimientos AS n
                ON n.NacimientoId = i.NacimientoId
            WHERE n.BebePacienteId <> i.PacienteId
        )
        BEGIN
            THROW 50011,
                N'El registro de nacimiento debe corresponder al bebe paciente.',
                1;
        END;

    END TRY
    BEGIN CATCH

        DECLARE
            @ErrNum INT = ERROR_NUMBER(),
            @ErrSev INT = ERROR_SEVERITY(),
            @ErrLinea INT = ERROR_LINE(),
            @ErrMsg NVARCHAR(2000) = ERROR_MESSAGE();

        -- 3. Revertir la operacion incorrecta
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- 4. Intentar registrar el error
        -- sin crear relaciones con otras tablas
        BEGIN TRY

            INSERT INTO dbo.BitacoraErrores
            (
                Accion,
                Objeto,
                NumeroError,
                Severidad,
                Linea,
                Mensaje
            )
            VALUES
            (
                N'TRIGGER',
                N'TR_HistorialMedico_ValidaCita',
                @ErrNum,
                @ErrSev,
                @ErrLinea,
                @ErrMsg
            );

        END TRY
        BEGIN CATCH
            SET NOCOUNT ON;
        END CATCH;

        -- 5. Devolver el error original
        THROW;

    END CATCH;
END;
GO

CREATE TRIGGER dbo.TR_Movimientos_NoStockNegativo
ON dbo.MovimientosInventario
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProductosBloqueados BIGINT;

    -- Bloquear los productos afectados durante la validacion
    SELECT @ProductosBloqueados = COUNT_BIG(*)
    FROM dbo.Inventario WITH (UPDLOCK, HOLDLOCK)
    WHERE InventarioId IN (
        SELECT InventarioId FROM inserted
        UNION
        SELECT InventarioId FROM deleted
    );

    -- Verificar el stock calculado despues del movimiento
    IF EXISTS (
        SELECT 1
        FROM dbo.vw_StockInventario AS s
        WHERE s.InventarioId IN (
            SELECT InventarioId FROM inserted
            UNION
            SELECT InventarioId FROM deleted
        )
        AND s.StockActual < 0
    )
    BEGIN
        THROW 51035,
            'El movimiento no se permite porque dejaria el inventario con stock negativo.',
            1;
    END;
END;
GO

CREATE TRIGGER dbo.TR_Movimientos_ProductoCorrecto
ON dbo.MovimientosInventario
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Validar que el producto coincida
    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        INNER JOIN dbo.ArticulosVentas AS a
            ON i.ArticulosVentasId = a.ArticulosVentasId
        WHERE i.TipoMovimiento = N'SalidaVenta'
          AND i.InventarioId <> a.InventarioId
    )
    BEGIN
        THROW 51030,
            'El producto de la salida no coincide con el articulo vendido.',
            1;
    END;

    -- 2. Validar que la cantidad coincida
    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        INNER JOIN dbo.ArticulosVentas AS a
            ON i.ArticulosVentasId = a.ArticulosVentasId
        WHERE i.TipoMovimiento = N'SalidaVenta'
          AND i.Cantidad <> a.Cantidad
    )
    BEGIN
        THROW 51032,
            'La cantidad descontada no coincide con la cantidad vendida.',
            1;
    END;
END;
GO

CREATE TRIGGER dbo.TR_Movimientos_ProtegerEliminacion
ON dbo.MovimientosInventario
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM deleted
        WHERE TipoMovimiento = N'SalidaVenta'
    )
    BEGIN
        THROW 51034,
            'No se puede eliminar una salida de venta. Debe conservarse el historial de inventario.',
            1;
    END;
END;
GO

CREATE TRIGGER
    dbo.TR_Nacimientos_ProtegerHistorialMedico
ON dbo.Nacimientos
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Solo validar cambios del bebé.
    IF NOT UPDATE(BebePacienteId)
        RETURN;

    -- Impedir inconsistencias con el historial médico.
    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        INNER JOIN dbo.HistorialMedico AS h
            ON h.NacimientoId = i.NacimientoId
        WHERE h.PacienteId <> i.BebePacienteId
    )
    BEGIN
        THROW 51017,
            N'No se puede cambiar el bebe: existen registros de historial medico asociados a otro paciente.',
            1;
    END;
END;
GO

CREATE TRIGGER dbo.TR_Seguimiento_MismoPaciente
ON dbo.SeguimientosCitas
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Validar que ambas citas sean
    -- del mismo paciente
    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        INNER JOIN dbo.Citas AS a
            ON a.CitaId = i.CitaOrigenId
        INNER JOIN dbo.Citas AS b
            ON b.CitaId = i.CitaSeguimientoId
        WHERE a.PacienteId <> b.PacienteId
    )
    BEGIN
        THROW 51013,
            N'Seguimiento requiere citas del mismo paciente.',
            1;
    END;

    -- 2. Registrar los caminos de seguimiento
    DECLARE @Rutas TABLE (
        OrigenId INT NOT NULL,
        DestinoId INT NOT NULL,
        PRIMARY KEY (OrigenId, DestinoId)
    );

    INSERT INTO @Rutas (OrigenId, DestinoId)
    SELECT CitaOrigenId, CitaSeguimientoId
    FROM inserted;

    DECLARE @NuevasRutas INT = 1;

    -- 3. Recorrer las relaciones de seguimiento
    WHILE @NuevasRutas > 0
    BEGIN

        -- Si una ruta vuelve a su origen,
        -- existe un seguimiento circular
        IF EXISTS (
            SELECT 1
            FROM @Rutas
            WHERE OrigenId = DestinoId
        )
        BEGIN
            THROW 51014,
                N'No se permiten seguimientos circulares.',
                1;
        END;

        -- Buscar el siguiente nivel de seguimiento
        INSERT INTO @Rutas (OrigenId, DestinoId)
        SELECT DISTINCT
            r.OrigenId,
            s.CitaSeguimientoId
        FROM @Rutas AS r
        INNER JOIN dbo.SeguimientosCitas AS s
            WITH (UPDLOCK, HOLDLOCK)
            ON s.CitaOrigenId = r.DestinoId
        WHERE NOT EXISTS (
            SELECT 1
            FROM @Rutas AS existente
            WHERE existente.OrigenId = r.OrigenId
              AND existente.DestinoId = s.CitaSeguimientoId
        );

        SET @NuevasRutas = @@ROWCOUNT;

    END;

    -- 4. Comprobacion final
    IF EXISTS (
        SELECT 1
        FROM @Rutas
        WHERE OrigenId = DestinoId
    )
    BEGIN
        THROW 51014,
            N'No se permiten seguimientos circulares.',
            1;
    END;

END;
GO

CREATE TRIGGER dbo.TR_Ventas_ProtegerPagadas
ON dbo.Ventas
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Impedir modificar o eliminar ventas
    -- que ya estaban pagadas.
    IF EXISTS (
        SELECT 1
        FROM deleted
        WHERE Estado = N'Pagada'
    )
    BEGIN
        THROW 51039,
            'No se puede modificar o eliminar una venta ya pagada.',
            1;
    END;
END;
GO

-- 07. ROL DE REGISTRO CONTROLADO (sin usuario conectado)
-- Rol para la cuenta NO privilegiada del ejecutable. No crea logins ni usuarios reales.
-- Solo al integrar la aplicacion, asignar su usuario de BD a este rol.
IF DATABASE_PRINCIPAL_ID(N'rol_registro_pacientes') IS NULL
    CREATE ROLE rol_registro_pacientes AUTHORIZATION dbo;
GO
GRANT EXECUTE ON OBJECT::dbo.usp_RegistrarPaciente TO rol_registro_pacientes;
GRANT EXECUTE ON OBJECT::dbo.usp_RegistrarRecienNacido TO rol_registro_pacientes;
GRANT EXECUTE ON OBJECT::dbo.usp_AsignarIdentificacionRecienNacido TO rol_registro_pacientes;
GRANT EXECUTE ON OBJECT::dbo.usp_RegistrarNotificacion TO rol_registro_pacientes;
-- La aplicacion usa procedimientos; no INSERT directo sobre estas tablas.
DENY INSERT ON OBJECT::dbo.Pacientes TO rol_registro_pacientes;
DENY INSERT ON OBJECT::dbo.Nacimientos TO rol_registro_pacientes;
DENY INSERT ON OBJECT::dbo.BitacoraNotificaciones TO rol_registro_pacientes;
DENY UPDATE ON OBJECT::dbo.Pacientes (Numero_Identificacion) TO rol_registro_pacientes;
GO
-- ATENCION: el rol no protege conexiones dbo/db_owner/sysadmin. La cuenta
-- del ejecutable debe ser de privilegios limitados y pertenecer a este rol.

-- El SP de cambio a emergencia no se concede al rol de registro; autorizarlo
-- solamente a un rol clinico validado en la integracion del ejecutable.

-- FIN DEL SCRIPT. No contiene operaciones DROP ni datos de prueba.

-- Restaurar ejecucion normal despues de la comprobacion inicial.
SET NOEXEC OFF;
GO
