# QA — Proveedores hospitalarios y abastecimientos

## Referencia del PO (16/11/2026)

"Control de proveedores hospitalarios completo": registrar proveedores de medicamentos,
equipos e instrumentos médicos, mantener contacto, productos suministrados y estado de
actividad, consultar historial de abastecimientos y emitir reporte de proveedores activos
e inactivos. El requisito se extrae del PDF `Requerimientos Hospital`.

## Alcance implementado (sujeto a QA)

- Registrar proveedores con identificación única, razón social, tipo, teléfono, correo y dirección.
- Cambiar el estado activo/inactivo sin eliminar historia.
- Alta interna de productos faltantes en `Inventario` (activo, `Vendible = 0`, `PrecioVenta = 0`).
- Asociar un producto de inventario a cada proveedor (N:M).
- Registrar abastecimientos con proveedor + producto previamente asociados, referencia, fecha,
  cantidad y costo unitario.
- Cada abastecimiento inserta `EntradaCompra` en `MovimientosInventario` y su asiento en
  `AbastecimientosHospitalarios`, **en una transacción única** junto con auditoría.
- Historial de últimas 200 entregas (consulta limitada por pantalla).
- Reporte imprimible de proveedores activos, inactivos o todos.
- Todas las rutas están restringidas al rol **Administrador**.

## Dependencias, seguridad y limitaciones

Esta rama parte del PR #27, que a su vez parte de #26 y #25.
Integrar en ese orden tras resolver los QA; no fusionar ramas apiladas directamente
a `develop` sin revisión. No alterar bases distintas de `Hospital_DB4_Integracion`.

La tabla `Inventario` de `develop` no tiene columna `StockActual`. La entrada queda
registrada como movimiento, pero **no se promete existencia física ni sincronización
automática con otros sistemas de inventario**.
El alta interna no crea automáticamente un registro especializado en `dbo.Medicamentos`.
El historial de pantalla muestra **hasta 200 registros**, no una exportación exhaustiva.

## Instalación

1. Respaldar `Hospital_DB4_Integracion`.
2. Cambiar a `feat/proveedores-abastecimientos-20261123`.
3. Ejecutar `dotnet build ProyectoHospital/ProyectoHospital.slnx`.
4. Ejecutar manualmente y solo en la base de integración
   `database/migrations/20261123_ProveedoresAbastecimientos_SOLO_INTEGRACION.sql`.
5. Ingresar con `admin` y abrir Proveedores.

## Pruebas funcionales

| Caso | Acción | Resultado esperado |
|---|---|---|
| PV-01 | Crear proveedor de prueba, identificación nueva | Alta visible con estado Activo |
| PV-02 | Registrar de nuevo la misma identificación | Mensaje de duplicado; no crea segundo proveedor |
| PV-03 | Crear producto interno de prueba con código nuevo | Se agrega a Inventario, no vendible |
| PV-04 | Asociar producto al proveedor | Figura en la relación de productos |
| PV-05 | Repetir la asociación | Se rechaza por duplicado |
| PV-06 | Registrar entrega ref. ENT-PRUEBA-001, cantidad 10, costo 1500 | Aparece en historial y movimiento EntradaCompra |
| PV-07 | Registrar la misma referencia/proveedor/producto | Se rechaza sin segundo movimiento (rollback) |
| PV-08 | Registrar abastecimiento con producto no asociado (POST manipulado) | Se rechaza |
| PV-09 | Desactivar proveedor | Aparece Inactivo, historial se conserva |
| PV-10 | Registrar entrega con proveedor inactivo | Se rechaza |
| PV-11 | Reactivar proveedor | Vuelve a Activo |
| PV-12 | Reporte estado Activos / Inactivos | Listados y contadores correctos |
| PV-13 | Login como médico/cliente, ir a /Proveedores | Acceso denegado |
| PV-14 | Consultar Bitácoras → Acciones | Usuario admin y operaciones quedan registrados |

## SQL de comprobación

```sql
USE Hospital_DB4_Integracion;
GO

SELECT ProveedorId, RazonSocial, Identificacion, Categoria, Activo
FROM dbo.ProveedoresHospitalarios ORDER BY ProveedorId;

SELECT pp.ProveedorId, pp.InventarioId, i.Nombre_del_Producto, i.Vendible
FROM dbo.ProveedorProductos pp
JOIN dbo.Inventario i ON i.InventarioId = pp.InventarioId
ORDER BY pp.ProveedorId, pp.InventarioId;

SELECT a.AbastecimientoId, a.ProveedorId, a.InventarioId,
       a.Cantidad, a.CostoUnitario, a.Referencia, a.MovimientoId,
       m.TipoMovimiento, m.Cantidad AS CantidadMovimiento
FROM dbo.AbastecimientosHospitalarios a
JOIN dbo.MovimientosInventario m ON m.MovimientoId = a.MovimientoId
ORDER BY a.AbastecimientoId DESC;

SELECT TOP(20) AuditoriaId, UsuarioId, Accion, Tabla_Modificada, RegistroId, FechaHora
FROM dbo.BitacoraAuditoria
WHERE Objeto = N'ProveedoresController' ORDER BY AuditoriaId DESC;
```

## Pendientes para operación real

Validación de identificación fiscal con fuente oficial, reglas sobre productos regulados,
aprobación de proveedores, devoluciones y ajuste de entregas, reportes históricos sin límite
y conciliación contable o existencias físicas. No se implementan compras ni pagos.
