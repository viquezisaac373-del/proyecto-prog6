# Hospinomenal — Registro de recién nacidos (rama de trabajo)

Estado: **implementación inicial en rama de funcionalidad; pendiente de compilación y pruebas desde Visual Studio**.

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
