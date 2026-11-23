# QA de Hospinomenal — Cambios propuestos 22/11/2026

**Origen:** observaciones de Danna durante la prueba de RN, y alcance provisional solicitado por el Scrum Master.  
**Estado:** rama de trabajo; requiere compilación y QA en SQL Server antes de fusionar.  
**Precaución:** no realizar pruebas con datos clínicos reales ni aplicar migraciones a Hospital_DB4 principal.

## 1. Identificación oficial

- **Alta nueva de paciente:** exclusivamente cédula costarricense de **9 dígitos numéricos**.
- **Asignación de identificación de RN:** exclusivamente cédula de **9 dígitos**; filtro de caracteres en navegador y comprobación en servidor.
- **Legacy:** se conservan los números históricos más largos para poder consultar y editar contacto; el mantenimiento ordinario no altera identificaciones.
- **SQL:** la migración protegida `20261122_QA_IdentidadMadres_Guardas_SOLO_INTEGRACION.sql` prohíbe **nuevas** identificaciones fuera de 9 dígitos, sin cambiar las existentes. Aún no está aplicada.
- **Fuera de alcance:** validar existencia/identidad de la persona contra el TSE, DIMEX, pasaportes, tipos de documento y regularización de históricos. Lo anterior pertenece a HU12 / futuro backlog.
- Fuente oficial sobre formato de 9 dígitos: TSE, *Comportamiento del electorado costarricense*, https://www.tse.go.cr/pdf/ifed/comportamiento_electorado_costarricense.pdf

## 2. Elegibilidad de la madre

- El selector de madres muestra expedientes activos, provisionalmente `Genero=Femenino`, con **10 años cumplidos** a la fecha de consulta.
- El controlador valida la edad **a la fecha del nacimiento** (no solo edad actual).
- La migración SQL de QA agrega una protección sobre nuevas relaciones de nacimiento para impedir una asociación menor de 10 años o con un expediente no habilitado.
- **La edad de 10 años es un límite provisional de QA**, no criterio legal/biológico universal ni validación de filiación. La OMS estudia embarazos en el grupo de 10–14 años, por lo cual no se debe prohibir a priori el registro de todos los embarazos adolescentes.
- Los casos con menos de 10 años y cualquier excepción clínica/legal deben tener proceso alternativo restringido de registro y auditoría aprobado por el PO y asesoría clínica. **Ese proceso excepcional aún no existe**; no usar esto con pacientes reales.
- **Deuda de diseño:** el género administrativo no prueba quién dio a luz. Se necesita relación de gestación/filiación explícita y verificable, con excepciones legítimas.

## 3. Tipo sanguíneo del recién nacido

- En el alta: seleccionar resultado confirmado (8 grupos ABO/Rh, o RHN si compatible con SQL) o **ND** si no se conoce.
- Si se registra resultado confirmado, el servidor exige referencia del informe de laboratorio.
- **Atomicidad:** el SP actual crea bebé+parto con ND; cuando se eligió un tipo confirmado, el repositorio actualiza el dato y escribe la referencia en auditoría dentro de **una misma transacción externa**. Si algún paso falla, se revierte toda la alta.
- En seguimiento: aparece el grupo en el listado y un aviso si permanece ND. Personal autorizado puede utilizar *Completar tipo* para guardar el grupo confirmado y referencia de laboratorio, con transacción y `SESSION_CONTEXT(UsuarioId)`.
- El aviso de **30 días** es una propuesta de seguimiento administrativo que el PO debe aprobar, **no un plazo clínico ni legal verificado**; no bloquea la atención.
- No se implementó envío automático de mensajes de correo/SMS ni una tarea en segundo plano. Los avisos se muestran cuando alguien abre el listado.
- Fuente para decisión de no forzar tipificación a todos: AAP, guías de hiperbilirrubinemia e información sobre toma de muestras neonatales (no implica obligación universal en fecha fija).

## 4. QA — Casos de prueba mínimos

| Caso | Procedimiento | Esperado |
|---|---|---|
| QA-ID-01 | Alta general 9 dígitos válidos | Acepta si resto de datos válidos; nueva ID única |
| QA-ID-02 | Alta general 10, 12 o 25 dígitos; letras | Rechaza; no persiste |
| QA-ID-03 | Editar contacto de paciente histórico con ID 12 dígitos | Se permite sin modificar identificación |
| QA-ID-04 | Asignar ID RN 9 dígitos | Guarda en mismo PacienteId |
| QA-ID-05 | Intentar letras, 8 o 10 dígitos, o ID duplicada | Rechaza en MVC y, después de migración, también en SQL |
| QA-MAD-01 | Intentar elegir recién nacida como madre | No aparece; POST manipulado rechazado |
| QA-MAD-02 | Paciente de 10 años o más a la fecha de nacimiento (ficticia) | Requiere validación clínica; si cumple demás requisitos admite |
| QA-MAD-03 | Persona menor de 10 años | Rechazo ordinario; proceso excepcional pendiente |
| QA-SANG-01 | RN ND por falta de resultado | Registra ND y aviso en listado |
| QA-SANG-02 | RN grupo confirmado + referencia laboratorio | Se guarda con auditoría y mismo nacimiento |
| QA-SANG-03 | RN grupo confirmado sin referencia | Rechaza |
| QA-SANG-04 | Completar ND a tipo confirmado | Actualiza mismo paciente y crea auditoría |
| QA-SANG-05 | Completar ND repetido / tipo ilegal / rol no autorizado | Rechaza, sin cambios |
| QA-SANG-06 | Pasados 30 días con ND | Aviso de seguimiento administrativo en listado; no afirmar aviso automático |

## 5. Dependencias para aprobar/fusionar

1. `dotnet restore` y `dotnet build ProyectoHospital/ProyectoHospital.slnx` (Windows/.NET 8), sin advertencias.
2. Revisar migración SQL con José Steven, ejecutar solo sobre `Hospital_DB4_Integracion` después de respaldo y prevalidación.
3. Probar las operaciones anteriores en MVC con datos de prueba, revisar BitacoraAuditoria y `@@TRANCOUNT`.
4. Confirmar que `RHN` está autorizado por el `CHECK` de tipo sanguíneo en la copia de integración; no asumir que un nuevo esquema construido desde el script principal lo admite.
5. El Product Owner debe ratificar las decisiones temporales: edad 10 años, notificación administrativa de 30 días, acceso a correcciones clínicas y documentos extranjeros.
6. Registrar las incidencias en UTC Agile como correcciones nuevas sin declarar prematuramente completadas las HU previas.
7. PR #15 de HU12 permanece en **borrador**, no fusionar ni cerrar hasta resolver su solapamiento de validaciones y decidir tipificación documental.

**Importante:** el PR #21 ya fue fusionado en develop. Esta rama se creó después de ese merge para evitar sobrescribir cambios aprobados.
