# Control QA — Bitácoras de acciones de pacientes y recién nacidos

Fecha: 22/11/2026. **PR en borrador: no fusionar sin compilar y probar.**

## Qué corresponde al módulo de Carlos

**Bitácoras → Auditoría** consulta `dbo.BitacoraAuditoria`, con acciones
de diferentes módulos del sistema. **Bitácoras → Accesos** es otra fuente
(`vw_BitacoraAccesos`) destinada al inicio y cierre de sesión y bloqueos.
No corresponde registrar cada acción clínica en Accesos.

## Revisión estática de `develop` antes de este cambio

- Altas y mantenimiento de pacientes usan `SqlAuditContext`;
  para auditar INSERT/UPDATE/DESACTIVAR en `Pacientes` el SQL de integración
  necesita `TR_Pacientes_Auditoria` habilitado. El script inicial del
  repositorio no incluye ese trigger: **verificar su instalación**.
- Altas de recién nacidos ejecutan `usp_RegistrarRecienNacido`, que
  crea `Pacientes` y `Nacimientos`. No existía un INSERT explícito en
  BitacoraAuditoria para la fila de `Nacimientos`.
- Edición y anulación de nacimientos ya incluyen entradas explícitas
  `UPDATE` y `DESACTIVAR` en `Tabla_Modificada=Nacimientos`.
- Asignación de identificación del bebé depende del trigger de
  `Pacientes`; la confirmación de tipo sanguíneo añade
  `CONFIRMACION_TIPIFICACION` con referencia clínica.
- Si `BitacoraAuditoria.UsuarioId` queda NULL en eventos de pacientes,
  hay que verificar `SESSION_CONTEXT(N'UsuarioId')` en la conexión
  **real** de MVC; simularlo con T-SQL no prueba ese extremo.

## Corrección propuesta

Al registrar un recién nacido, insertar el evento
`Accion='INSERT'`, `Tabla_Modificada='Nacimientos'` en la misma
transacción que crea al paciente y al nacimiento, incluyendo
`UsuarioId` autenticado y JSON de las relaciones esenciales.
El INSERT es condicional para evitar duplicidad si ya lo registra otro
mecanismo de auditoría. No cambia la tabla ni el procedimiento SQL.

## Comprobación de solo lectura en Hospital_DB4_Integracion

```sql
USE Hospital_DB4_Integracion;
GO
SELECT DB_NAME() AS Base,
       t.name AS TriggerAuditoriaPacientes,
       t.is_disabled AS Deshabilitado
FROM sys.triggers AS t
WHERE t.object_id = OBJECT_ID(N'dbo.TR_Pacientes_Auditoria');

SELECT TOP (50)
    b.AuditoriaId, b.FechaHora,
    b.Accion, b.Tabla_Modificada, b.RegistroId,
    b.UsuarioId, u.Usuario, b.Objeto,
    LEFT(b.VNuevos, 250) AS ResumenNuevos
FROM dbo.BitacoraAuditoria AS b
LEFT JOIN dbo.Usuarios AS u ON u.UsuarioId = b.UsuarioId
WHERE b.Tabla_Modificada IN (N'Pacientes', N'Nacimientos')
ORDER BY b.AuditoriaId DESC;
```

No ejecutar una acción real solo para generar la bitácora en la base principal;
usar datos ficticios en integración.

## Casos QA antes de fusionar

1. Compilar `.NET 8` sin errores ni advertencias.
2. Ingresar como usuario autorizado de pruebas y crear un nacimiento ficticio
   en `Hospital_DB4_Integracion`. Deben existir eventos INSERT tanto de
   `Pacientes` (trigger, si instalado) como de `Nacimientos` (esta corrección),
   con `UsuarioId` válido y sin duplicados.
3. Editar un nacimiento: aparece UPDATE en Nacimientos y, si cambian
   datos de Pacientes, también en Pacientes.
4. Asignar identificación pendiente: UPDATE en Pacientes.
5. Confirmar ND a un tipo sanguíneo con referencia: evento
   CONFIRMACION_TIPIFICACION con UsuarioId; también UPDATE del trigger.
6. Anular nacimiento de prueba: DESACTIVAR en Nacimientos, con motivo.
7. En la interfaz de Carlos, entrar con usuario administrador,
   abrir **Bitácoras → Auditoría** y filtrar por Acción/fecha. No buscar
   operaciones clínicas en «Accesos».
8. Verificar que si se produce un error o rollback de la transacción
   no queden eventos de auditoría de operaciones fallidas.
9. Danna debe documentar el resultado y adjuntar capturas. No declarar
   cobertura total de todas las acciones ni auditoría de consultas
   sin definir criterios de aceptación específicos.

**Importante:** revisar posibles registros duplicados si existen triggers
de Nacimientos adicionales en la instalación real. El repositorio
no demuestra que la base local de Danna o Isaac tenga exactamente la
misma estructura de `develop`.
