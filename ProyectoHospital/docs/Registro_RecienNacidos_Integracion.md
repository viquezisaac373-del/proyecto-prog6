# Hospinomenal — Registro de recién nacidos (rama de trabajo)

Estado: **implementación ampliada en rama de funcionalidad; pendientes compilación y pruebas de regresión de edición/anulación desde Visual Studio**.

## Alcance incorporado
- Formulario específico para nacimientos con madre y médico existentes; campos clínicos opcionales cuando aún no se conocen.
- Alta en una sola operación mediante dbo.usp_RegistrarRecienNacido (genera PacienteId y NacimientoId).
- Identificación oficial pendiente (NULL); tipo de sangre ND asignado por SQL.
- Listado de recién nacidos pendientes de identificación y formulario para completar el expediente mediante dbo.usp_AsignarIdentificacionRecienNacido.
- Controlador con autorización por roles Administrador, Medico/Médico y Secretaria.
- Contexto de usuario SQL en conexión de escritura; no utiliza UsuarioId proporcionado por formularios.
- Acceso desde la barra de navegación para los roles indicados.

## Dependencias comprobadas anteriormente en SQL (no desplegadas por esta rama)
Los procedimientos y las tablas solo se han probado en Hospital_DB4_Integracion:
- dbo.usp_RegistrarRecienNacido
- dbo.usp_AsignarIdentificacionRecienNacido
- dbo.Nacimientos
- dbo.Pacientes, dbo.Empleados
- dbo.TR_Pacientes_Auditoria

No aplicar automáticamente los scripts de migración a otras bases. La rama no modifica esquemas SQL.

## Lista breve de comprobación en Visual Studio
1. Verificar rama y actualizarla; compilar con dotnet build ProyectoHospital/ProyectoHospital.slnx.
2. Comprobar que la aplicación esté configurada para Hospital_DB4_Integracion; no usar datos reales.
3. Iniciar sesión como Administrador: abrir "Recién nacidos" desde el menú.
4. Confirmar que se ven los selectores de madre (paciente activo) y médico activo. Si no hay médico, el formulario se bloquea deliberadamente.
5. Con datos ficticios y médico activo, registrar nacimiento; comprobar que quedan un paciente con identificación NULL/tipo de sangre ND y una fila asociada en Nacimientos.
6. Comprobar una entrada de BitacoraAuditoria para la creación del paciente con el UsuarioId autenticado.
7. Asignar después una identificación numérica única al mismo bebé; comprobar que conserva su PacienteId y NacimientoId.
8. Probar validaciones: fecha futura, madre/médico inactivo, número de identificación duplicado, género/tipo de parto inválidos y acceso sin rol.
9. Verificar que los cambios no afecten pacientes originales ni otras bases.

## Límites conocidos
- No se ejecutó compilación ni prueba de interfaz desde este entorno.
- La copia de integración tenía cero médicos activos en las pruebas previas; será necesario contar con uno antes de probar un alta real desde MVC.
- La tabla de madres muestra pacientes activos para selección: requiere futura mejora con búsqueda/selección especializada y controles clínicos adicionales; no debe interpretarse como verificación de filiación.
- Los nombres exactos de roles (Medico/Médico/Secretaria) deben corresponder con los registros configurados en la base.
- Tipos de documento, reglas específicas de cédula de Costa Rica, avisos por identificación pendiente, actualización de auditoría integral y más pruebas forman parte del trabajo posterior.
- Esta rama proviene de develop y no contiene el código del PR #16 (auditoría del módulo de pacientes). El repositorio de recién nacidos establece su contexto SQL de forma independiente para la prueba.
- Nuevos requerimientos en las historias de UTC Agile: pendiente de incorporación posterior; no se modificaron historias desde esta rama.


## Ampliación para revisión del lunes 23 de noviembre de 2026

El módulo incluye ahora, además de registrar e identificar al recién nacido:
- Listado histórico **de todos los nacimientos** (identificados, pendientes y anulados).
- Edición de datos administrativos del bebé y datos clínicos del nacimiento en una transacción.
- Anulación lógica de **Nacimientos.Activo**, exclusiva del rol Administrador, con motivo obligatorio. **No** desactiva el registro del bebé en Pacientes.
- Auditoría explícita de cambios y anulaciones en dbo.BitacoraAuditoria con UsuarioId de la sesión SQL. La actualización de Pacientes también pasa por su trigger de auditoría.
- Campos de filiación (madre y médico), número de identificación y tipo sanguíneo no son editables desde este formulario.
- No requiere nueva migración SQL: utiliza columnas existentes de Pacientes, Nacimientos y BitacoraAuditoria.

### Revisión local pendiente antes de la demostración
1. Git Bash, sobre `feature/registro-recien-nacidos`: `git fetch origin`; `git merge --ff-only origin/feature/registro-recien-nacidos`; `dotnet build ProyectoHospital/ProyectoHospital.slnx`.
2. Entrar como Administrador a Hospinomenal con User Secrets apuntando a la base local de integración.
3. Listado: comprobar que el recién nacido con identificación ya asignada sigue visible, igual que los pendientes.
4. Edición: abrir el nacimiento de prueba, cambiar un dato clínico no sensible (p. ej. peso ficticio 3200→3250 g), guardar. Comprobar cambios de Nacimientos, mismo PacienteId, y BitacoraAuditoria UPDATE con UsuarioId.
5. Anulación: **usar un nacimiento ficticio distinto** o únicamente un registro creado para anular. Indicar razón explícita. Comprobar Nacimientos.Activo=0, Pacientes.Activo inalterado, evento DESACTIVAR en la bitácora, y fila visible con estado Anulado.
6. Seguridad: el botón Anular se muestra solo a Administrador; el controlador también exige ese rol y el token antifalsificación para los POST.
7. No aplicar escrituras en la BD principal ni crear datos reales para las pruebas.
8. Las pruebas de compilación/ejecución no se han ejecutado en el equipo del desarrollador por este asistente. Los resultados del MVC anteriores solo cubren alta e identificación, no esta ampliación.

### Limitaciones deliberadas
- La anulación de un nacimiento exige políticas clínicas y de auditoría del proyecto antes de utilizarse con expedientes reales; aquí se plantea como anulación administrativa lógica de un registro de prueba.
- No implementa edición de madre/médico, corrección de filiación, reactivación, aprobación clínica, política de doble validación ni historial médico por episodio.
- PR #17 sigue en borrador. La rama se basó originalmente en un develop anterior a la integración de HU13; habrá que resolver cualquier conflicto de integración con cuidado.
- Las nuevas reglas/requerimientos en UTC Agile continúan pendientes de incorporación.
