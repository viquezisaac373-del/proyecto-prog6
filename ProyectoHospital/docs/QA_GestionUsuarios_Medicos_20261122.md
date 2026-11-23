# QA — Gestión de usuarios y empleados (rama feat/gestion-usuarios-medicos-20261122)

## Objetivo
Permitir que el administrador registre empleados, cree usuarios con roles y vincule empleados previamente registrados, sin INSERT manual en dbo.Usuarios. La autenticación continúa utilizando AccountController y PasswordHasher (Identity V3).

## Alcance de esta entrega
- Listado de cuentas, empleado relacionado, roles y estado.
- Alta atómica de empleado + usuario + rol.
- Vincular cuenta a empleado existente (sin duplicar su ficha).
- Activar/desactivar cuenta, con protección de la propia cuenta y del último administrador activo.
- Auditoría en dbo.BitacoraAuditoria del operador autenticado. Nunca se registra la contraseña ni el hash.
- Acceso restringido a Administrador.

**Fuera de alcance de esta primera versión:** modificación de datos personales, restablecimiento de contraseñas, administración de permisos granulares o cambio de roles de cuentas existentes. No está garantizado que esto resuelva por sí solo todos los mensajes 'Sin registro' en otros módulos: investigar la consulta que muestra el mensaje y la identidad que se audita.

## Preparación manual (solo integración)
1. Conservar un respaldo de Hospital_DB4_Integracion.
2. Verificar la migración de PasswordHasher mediante la consulta de solo lectura:
```sql
USE Hospital_DB4_Integracion;
SELECT c.name, c.is_nullable, t.name AS Tipo
FROM sys.columns c
JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.Usuarios')
  AND c.name IN (N'PasswordHash', N'PasswordSalt', N'PasswordHashIdentity');
```
Se esperan las tres columnas, y que PasswordHash, PasswordSalt permitan NULL.
3. Ejecutar manualmente `database/migrations/20261122_Roles_GestionUsuarios_SOLO_INTEGRACION.sql`. No ejecutar sobre Hospital_DB4 ni compartir como cambio aprobado de BD sin coordinar con José Steven.
4. Ejecutar `dotnet build ProyectoHospital/ProyectoHospital.slnx`.
5. Confirmar que la cadena HospitalDB apunte a Hospital_DB4_Integracion.

## Casos de prueba
| Caso | Acción | Esperado |
|---|---|---|
| UG-01 | Entrar como admin y abrir Usuarios | Lista y rol Administrador visibles; admin mantiene EmpleadoId NULL |
| UG-02 | Vincular empleado #4 (médico ya registrado) a nuevo usuario con rol Medico | Nueva fila Usuarios con EmpleadoId 4, fila UsuarioRol y bitácora |
| UG-03 | Iniciar sesión con la nueva cuenta médica | Autenticación exitosa; rol Medico aplicado |
| UG-04 | Abrir /Usuarios siendo Medico | Acceso denegado |
| UG-05 | Crear médico nuevo desde formulario con horario y especialidad | Nuevo Empleado + Usuario + UsuarioRol, transacción completa |
| UG-06 | Crear médico con hora de salida anterior a entrada | Error de validación, sin registros nuevos |
| UG-07 | Crear usuario con nombre repetido | Rechazo, sin empleado huérfano |
| UG-08 | Vincular dos cuentas al mismo empleado | Segunda operación rechazada, sin duplicados |
| UG-09 | Intentar desactivar la cuenta admin de la sesión | Rechazo |
| UG-10 | Desactivar/activar una cuenta de prueba | Login denegado al desactivar y permitido al reactivar |
| UG-11 | Crear usuario y verificar bitácora | UsuarioId operador real y referencia de cuenta creada; sin contraseña |
| UG-12 | Sin rol Médico activo | Alta médica bloqueada por selección de rol, sin cambios parciales |

## Consultas de solo lectura
```sql
USE Hospital_DB4_Integracion;
SELECT u.UsuarioId, u.Usuario, u.EmpleadoId, u.Activo, e.EsMedico, r.Nombre_del_Rol
FROM dbo.Usuarios u
LEFT JOIN dbo.Empleados e ON e.EmpleadoId = u.EmpleadoId
LEFT JOIN dbo.UsuarioRol ur ON ur.UsuarioId = u.UsuarioId
LEFT JOIN dbo.Roles r ON r.RolId = ur.RolId
ORDER BY u.UsuarioId;

SELECT TOP (30) AuditoriaId, UsuarioId, Accion, Tabla_Modificada, RegistroId, VNuevos, Objeto, FechaHora
FROM dbo.BitacoraAuditoria
WHERE Objeto = N'UsuariosController'
ORDER BY AuditoriaId DESC;
```

## Entrega y limitaciones
No se ha ejecutado compilación .NET ni pruebas contra SQL Server en el entorno que generó estos cambios. No fusionar sin reproducir UG-01 a UG-11, prueba de regresión de login y revisión de Danna. La base de datos no se modificó automáticamente.
