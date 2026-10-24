# HU02 — Validación de integridad y decisiones de normalización

**Revisión:** 23 de octubre de 2026. **Origen:** `Hospital_DB.sql`, rama `develop` y pruebas en SSMS proporcionadas por el equipo.

## Hallazgos y soluciones

1. **Citas con personal no médico.** El procedimiento `sp_RegistrarCita` ya revisaba `EsMedico=1`, pero un `INSERT` directo en `Citas` permitía asignar personal no médico. El trigger `TR_Citas_ValidarMedicoEHistorial` controla `INSERT` y cambios de `EmpleadoId`.
2. **Historial inconsistente después de modificarlo.** El trigger `TR_HistorialMedico_ValidaCita` se extendió a `AFTER INSERT, UPDATE`. Además, el nuevo trigger de `Citas` impide cambiar paciente/estado si contradicen historiales existentes.
3. **Clientes y pacientes.** `Pacientes` conserva los datos clínicos e identificación de pacientes. `Clientes` representa la relación comercial: puede ser un comprador externo o vincularse opcionalmente con `Pacientes`. **Regla a precisar con el PO:** si el registro de `Clientes` debe conservar datos comerciales independientes o información histórica de facturación. Si se pretende preservar datos fiscales exactos de una factura, deberá capturarse la información de emisión para que no dependa de cambios posteriores. No se añadió ninguna regla arbitraria de igualdad de nombres.

## Evidencia real en SQL Server Express

- 19 tablas hospitalarias, 19 PK y 35 FK, sin FK deshabilitadas o sin confianza (excluir `sysdiagrams`, tabla de SSMS).
- 30 restricciones CHECK válidas, 11 UNIQUE y 6 índices únicos adicionales.
- Prueba después de la corrección: ambos triggers configurados para `INSERT` y `UPDATE`, habilitados.
- `INSERT` de cita con empleado no médico: rechazado por SQL Server con error **51020**.
- `UPDATE` de historial vinculándolo a un paciente diferente: rechazado con error **50010**.
- Transacciones pendientes al terminar: **0**.

Las capturas de las consultas y pruebas se adjuntarán en UTC Agile. No se guardan datos ficticios permanentes en estas pruebas, pero algunos valores `IDENTITY` pueden avanzar.

## Límites de cobertura y seguimiento

- Estas pruebas demuestran las reglas específicas enumeradas; no constituyen una certificación de todos los casos, integridad clínica o de la **3FN estricta**.
- El trigger de citas no impide cambiar `EsMedico` o `Activo` desde la tabla `Empleados` cuando ya existan citas. La política de desactivación/reclasificación de empleados y la cobertura de otras modificaciones quedan para la lógica de gestión de empleados.
- `HistorialMedico.PacienteId` junto con `CitaId` puede ser una redundancia derivable de `Citas.PacienteId`. Se conserva por consulta y trazabilidad, con regla de consistencia para ambos lados; validar la interpretación de 3FN del modelo con QA y PO.
- Los triggers aseguran integridad en las operaciones probadas, pero su manejo de errores y registro en `Bitacoras` deben probarse por separado con transacciones reales.
- El sistema aún requiere la implementación completa de ventas, acceso, auditoría, emergencias y reglas adicionales de los sprints posteriores.

## Aplicación

- Para instalar desde cero: usar `ProyectoHospital/database/script-database/Hospital_DB.sql` actualizado.
- Para actualizar una base existente: usar `ProyectoHospital/database/migrations/20261023_HU02_integridad_citas_historial.sql` y **no** ejecutar otra vez el script inicial.
- Este PR no modifica la base del equipo automáticamente: cada instancia se migra de forma controlada.
