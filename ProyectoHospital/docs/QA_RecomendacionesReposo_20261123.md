# QA – Recomendaciones médicas de reposo (Costa Rica)

## Distinción legal obligatoria

En Costa Rica las recomendaciones de medicina privada no se convierten por sí solas en incapacidades oficiales de la CCSS o el INS. Las normas de la Caja y su tratamiento institucional se deben respetar. Este módulo **solamente registra recomendaciones internas y genera una constancia informativa imprimible**. Nunca debe afirmarse ante el PO que emite incapacidades oficialmente homologadas, con subsidio o firma digital certificada.

Referencias para revisión jurídica:
- CCSS: Reglamento para el otorgamiento de incapacidades y licencias a los beneficiarios del Seguro de Salud.
- PGR/SINALEVI: criterios sobre recomendaciones de incapacidad de médicos particulares y su admisión, modificación o denegación por la CCSS.

## Dependencias

- PR #25: cuentas vinculadas a `Empleados` con rol `Medico`.
- PR #26: horarios médicos. Esta rama se apila encima para facilitar prueba en la instalación local.
- Base destino de integración: `Hospital_DB4_Integracion`, que ya contiene los médicos con EmpleadoId 4 y 5.
- Código no compila ni se prueba automáticamente desde el entorno de creación de GitHub.

## Procedimiento

1. Guardar copia de seguridad de integración.
2. Cambiar a `feat/recomendaciones-reposo-medico-20261123` y compilar con `dotnet build ProyectoHospital/ProyectoHospital.slnx`.
3. Ejecutar manualmente `database/migrations/20261123_RecomendacionesReposo_SOLO_INTEGRACION.sql` **solamente en** `Hospital_DB4_Integracion`.
4. Cerrar sesión de admin e iniciar como `medicoprueba02`.
5. Abrir en el navbar «Reposo médico».

## Pruebas

| Caso | Acción | Resultado esperado |
|---|---|---|
| IR-01 | Médico #5 abre módulo | Lista pacientes activos, historial propio, formulario |
| IR-02 | Registrar recomendación a paciente activo (datos de prueba) | Se crea registro; redirige a comprobante |
| IR-03 | Ver comprobante | Muestra paciente, fechas, médico, justificación y leyenda NO OFICIAL |
| IR-04 | Imprimir | Formato de impresión legible, sin menús de navegación |
| IR-05 | Fecha final anterior a inicial | Rechazo; no se inserta nada |
| IR-06 | Paciente inactivo o manipulado en POST | Rechazo desde servidor |
| IR-07 | Repetir misma pareja paciente/médico/fechas | Rechazo; no duplica |
| IR-08 | Ingresar con admin | Ruta denegada para emitir recomendación |
| IR-09 | Ingresar con cliente | Ruta denegada |
| IR-10 | Entrar como otro médico | No ve documentos clínicos del médico anterior |
| IR-11 | Abrir comprobante de otro médico vía ID | HTTP 404, sin revelar contenidos |
| IR-12 | Verificar auditoría | Usuario responsable correcto, sin justificación clínica |
| IR-13 | Intentar POST de médico desactivado | Fallo de autorización o validación; sin alta |
| IR-14 | Cita sin vinculación | No se inventa relación con una cita, se muestra que es registro independiente |

## SQL read-only para comprobar

```sql
USE Hospital_DB4_Integracion;
GO
SELECT RecomendacionId, PacienteId, EmpleadoId, UsuarioRegistroId,
       FechaInicio, FechaFin, FechaRegistro
FROM dbo.RecomendacionesReposo
ORDER BY RecomendacionId DESC;

SELECT TOP (20) AuditoriaId, UsuarioId, Accion, Tabla_Modificada,
       RegistroId, Objeto, FechaHora, VNuevos
FROM dbo.BitacoraAuditoria
WHERE Objeto = N'RecomendacionesReposoController'
ORDER BY AuditoriaId DESC;
```

## Pendientes (no prometer en la review)

- Firma digital certificada, integración con CCSS/INS y documentación oficial.
- Reglas complejas de justificación laboral y subsidios.
- Asociación opcional con cita clínica (no hay mantenimiento de citas funcional integrado en esta rama).
- Control de acceso granular por tratamiento clínico de paciente.
- Correcciones/anulaciones y trazabilidad clínica avanzada.
