# QA — Cuentas de clientes (PR #25, actualización del 23/11/2026)

## Contexto y decisiones implementadas

**Fuente del requerimiento:** Proyecto de Hospital, requerimientos expertos n.º 1 (compras y pago), n.º 4 (citas) y n.º 10 (usuarios con roles). El profesor pide el flujo completo de ventas, pero este cambio **solo crea el acceso e identidad de clientes**, no implementa el cobro ni la factura.

**Modelo técnico:**
- `dbo.Clientes`: datos comerciales de clientes.
- `dbo.Usuarios`: credenciales de acceso comunes.
- `dbo.UsuarioRol` y `dbo.Roles`: rol `Cliente`, sin permisos administrativos.
- `dbo.UsuarioCliente`: vinculación uno a uno Usuario–Cliente, con claves foráneas y unicidad.
- `dbo.Pacientes`: expediente clínico independiente; NO se crea ni se consulta al registrar un comprador.
- `dbo.Inventario`: artículos visibles en el catálogo informativo (Activo=1, Vendible=1), sin confirmar existencia ni recibir pedidos.

La ley 8968 (protección de datos personales) justifica minimizar el acceso a información de salud y tratar separadamente expedientes clínicos. No se afirma cumplimiento normativo integral.

El registro público admite únicamente **cédula física nacional de 9 dígitos** o **DIMEX de 11 o 12 dígitos**, sin cero inicial ni guiones, formatos basados en las especificaciones de Hacienda para receptores electrónicos. Aún no admite pasaporte ni persona jurídica; no verifica documento en TSE/Migración. La identidad registrada por internet NO es verificada presencialmente, por lo que no puede utilizarse para acceder a expedientes clínicos o facturación sin controles adicionales.

## Preparación y migración

1. Compilar:
   `dotnet build ProyectoHospital/ProyectoHospital.slnx`.
2. Respaldar `Hospital_DB4_Integracion` y confirmar que `HospitalDB` apunta a esa base.
3. Confirmar que ya esté aplicada `20261122_Roles_GestionUsuarios_SOLO_INTEGRACION.sql`.
4. Ejecutar manualmente **solo en integración** el archivo:
   `database/migrations/20261123_UsuariosClientes_SOLO_INTEGRACION.sql`.
   Agrega el rol `Cliente` y la tabla `UsuarioCliente`. No ejecutarlo sobre `Hospital_DB4`.
5. Cerrar sesiones antiguas y reiniciar la aplicación para probar el navbar.

## Matriz de pruebas manuales

| ID | Prueba | Esperado |
|---|---|---|
| CL-01 | Admin ingresa a /Usuarios después de migración | Se muestra su cuenta y usuarios vinculados a empleado/cliente sin confundirlos |
| CL-02 | Acceder a /CuentaCliente/Registrar desde login | Formulario público con datos y aceptación del tratamiento |
| CL-03 | Registrar cliente ficticio nuevo, cédula de 9 dígitos | Se crean Cliente + Usuario + UsuarioRol Cliente + UsuarioCliente en una transacción |
| CL-04 | Registrar DIMEX ficticio de 11 o 12 dígitos | Acepta formato; no confunde con cédula |
| CL-05 | Repetir identificación existente en Clientes | Rechazo; no crea ni vincula una cuenta a ciegas |
| CL-06 | Repetir nombre de usuario | Rechazo; no queda Cliente huérfano |
| CL-07 | Contraseñas distintas / consentimiento sin marcar | Validación sin inserciones |
| CL-08 | Login de nueva cuenta Cliente | Acceso válido; menú muestra Mi cuenta y Catálogo |
| CL-09 | Cliente intenta /Usuarios, /Pacientes, /RecienNacidos, /Bitacoras | Acceso denegado |
| CL-10 | Cliente intenta /Modulos, /Citas, /Medicamentos | Acceso denegado |
| CL-11 | Catálogo de cliente | Solo productos activos y vendibles; sin botón de compra |
| CL-12 | Admin usa /ClientesUsuarios/Vincular con ClienteId existente sin cuenta | Crea usuario + rol + relación, sin duplicar Clientes |
| CL-13 | Admin intenta vincular cliente por segunda vez | Rechazo, sin duplicar cuentas |
| CL-14 | Login admin y médico tras cambios | Sin regresión de permisos |
| CL-15 | Consulta de bitácoras | Registra alta/vínculo sin contraseña ni hash |

## SQL read-only de verificación

```sql
USE Hospital_DB4_Integracion;
GO
SELECT TOP (50) u.UsuarioId, u.Usuario, u.Activo,
       c.ClienteId, c.Nombre, c.Apellidos, r.Nombre_del_Rol
FROM dbo.UsuarioCliente uc
JOIN dbo.Usuarios u ON u.UsuarioId = uc.UsuarioId
JOIN dbo.Clientes c ON c.ClienteId = uc.ClienteId
JOIN dbo.UsuarioRol ur ON ur.UsuarioId = u.UsuarioId
JOIN dbo.Roles r ON r.RolId = ur.RolId
ORDER BY u.UsuarioId DESC;

SELECT TOP (30) UsuarioId, Accion, Tabla_Modificada, RegistroId, VNuevos, Objeto, FechaHora
FROM dbo.BitacoraAuditoria
WHERE Objeto = N'CuentaClienteController'
ORDER BY AuditoriaId DESC;
```

## Límites y riesgos pendientes

- **No hay módulo completo de compras**: falta carrito/pedido, reserva/validación de inventario, pago, descuentos, generación fiscal de comprobantes electrónicos y conciliación.
- **No hay acceso a expedientes médicos** para clientes, ni autosolicitud de citas: ambos requieren autorización y reglas de privacidad.
- **No hay verificación de identidad oficial** (TSE/DIMEX), ni correo de confirmación ni recuperación de cuenta; no tomar los datos declarados por el solicitante como identidad probada.
- No se ha ejecutado `dotnet build` ni SQL Server desde el entorno que creó este código. Debe hacerse en el equipo Windows antes de poner el PR en Ready.
- `Permisos`/`RolPermiso`: la matriz visual/CRUD de permisos aún no está implementada; la restricción actual se aplica en controladores ASP.NET por rol.
- La cuenta `admin` permanece sin modificar. El PR #24 se mantiene separado.
