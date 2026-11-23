# QA — Avisos internos de recién nacidos en Bitácoras → Notificaciones

**Fecha:** 22/11/2026. **Estado:** cambio de código preparado, pendiente de compilación y pruebas.  
**Rama:** `feat/notificaciones-recien-nacidos-bitacora-20261122`.

## Propósito

El módulo de Carlos separa:
- **Acciones:** auditoría de inserciones, ediciones, anulaciones y tipificaciones
  (`BitacoraAuditoria`).
- **Notificaciones:** registro de mensajes y estado de envío
  (`BitacoraNotificaciones`).

Este cambio integra recién nacidos a la segunda pestaña **sin fingir entregas externas**.

## Avisos generados

| Operación real en MVC | Aviso interno |
|---|---|
| Registro de RN sin identificación | `RN | Alta registrada con identificación oficial pendiente` |
| Registro de RN con tipo `ND` | `RN | Alta registrada con tipo sanguíneo ND, pendiente de tipificación` |
| Asignación posterior de cédula | `RN | Identificación oficial asignada al expediente` |
| Confirmación posterior de tipo sanguíneo | `RN | Tipificación sanguínea confirmada` |

Cada aviso lleva `PacienteId`, `CitaId = NULL` y un `Mensaje` sin
nombres, cédulas, teléfonos, referencias de laboratorio ni resultado de laboratorio.
Se almacena con `EstadoEnvio='Pendiente'`, puesto que **no se envía** correo/SMS.
La pantalla reinterpreta la presentación de estos registros como
`Interna (sin envío)`, con un texto explicativo. Los estados existentes de
notificaciones de citas se conservan.

Los avisos se insertan **dentro de la misma transacción SQL** que su operación
principal. Si esta falla, no debe permanecer el aviso.

**No es un gestor de tareas pendientes:** los mensajes son eventos históricos;
no se borran ni cambian de estado cuando se resuelve la identificación o la
tipificación. El estado pendiente actual se verifica en el módulo RN.

## Antes de probar

- Compilar `ProyectoHospital/ProyectoHospital.slnx` en .NET 8.
- Revisar que `HospitalDB` apunte a **Hospital_DB4_Integracion**.
- Probar solo con datos ficticios. No cambiar `Hospital_DB4`.
- Usar usuario administrador de prueba; la consulta de bitácoras requiere ese rol.
- No se agrega migración: ya existen tabla, columnas y CHECK
  `CK_BitNot_Estado` con `Pendiente` en la base del proyecto.
- No hay migración de avisos de RN existentes: la creación es **prospectiva**,
  a partir de la versión del código que contiene este cambio.

## Casos de QA

| ID | Prueba | Resultado esperado |
|---|---|---|
| RN-NOT-01 | Registrar bebé nuevo con identificación NULL y sangre ND | 2 avisos internos en Notificaciones, mismo PacienteId, CitaId NULL |
| RN-NOT-02 | Registrar bebé con sangre confirmada y referencia clínica | Solo aviso de identificación pendiente (1) |
| RN-NOT-03 | Asignar ID nacional al RN de prueba | 1 aviso histórico adicional de asignación |
| RN-NOT-04 | Completar tipo sanguíneo desde ND | 1 aviso histórico adicional de confirmación |
| RN-NOT-05 | Intentar ID duplicada o confirmar tipo sin resultado | Operación rechazada; no se crea aviso |
| RN-NOT-06 | Bitácoras → Notificaciones, filtro «Interna (sin envío)» | Se muestran únicamente los avisos internos RN |
| RN-NOT-07 | Bitácoras → Acciones | Se conservan eventos de auditoría con UsuarioId correcto; los avisos no sustituyen auditoría |
| RN-NOT-08 | Registro de notificación de cita ya existente | Mantiene su estado real Enviada, Pendiente o Fallida |
| RN-NOT-09 | Verificar transacciones y duplicados | Cada operación confirmada genera solo su aviso correspondiente |

### Consulta SQL de solo lectura para QA

```sql
USE [Hospital_DB4_Integracion];
GO
SELECT TOP (40)
    n.NotificacionId, n.FechaHora, n.PacienteId, n.CitaId,
    n.EstadoEnvio, n.Mensaje
FROM dbo.BitacoraNotificaciones AS n
WHERE n.Mensaje LIKE N'RN | %'
ORDER BY n.NotificacionId DESC;
```

El valor `EstadoEnvio='Pendiente'` indica que **no hubo envío**;
no debe presentarse como mensaje entregado. En la UI aparece
`Interna (sin envío)`.

## Pendientes de producto

- El aviso por superar 30 días desde el nacimiento **no se graba
  automáticamente** en BitacoraNotificaciones: el listado de RN ya calcula
  ese plazo administrativo provisional. La política definitiva, frecuencia
  de recordatorios y responsable de atenderlos requieren aprobación del PO.
- No se implementa correo, SMS, tareas programadas ni seguimiento externo.
- No se genera bitácora retroactiva de RN históricos.
- Si se desea un verdadero ciclo `Abierta → Atendida` se requiere una tabla
  o columna separada de `EstadoEnvio`, cuya semántica actual es **entrega**,
  no gestión del expediente.

**No fusionar sin compilación y pruebas funcionales aprobadas por Danna.**
