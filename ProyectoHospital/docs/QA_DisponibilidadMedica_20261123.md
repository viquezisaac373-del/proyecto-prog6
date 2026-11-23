# QA — Disponibilidad médica por días y reporte semanal

## Alcance

- Administrador: registra bloques semanales y desactiva bloques cuando no dejen citas activas sin horario.
- Administrador, Medico/Médico, Secretaria: consultan disponibilidad y capacidad semanal teórica.
- Un médico puede tener varios bloques por día **si no se solapan**.
- Los intervalos contiguos [08:00,12:00) y [12:00,16:00) son permitidos.
- Registro de auditoría de altas y bajas lógicas con UsuarioId de operador autenticado.
- No modifica citas ni expone expedientes clínicos.

## Dependencias y base de trabajo

Esta rama **parte del commit del PR #25**, porque requiere autenticación y médicos creados desde la aplicación.
El PR de horarios debe tener como base **feat/gestion-usuarios-medicos-20261122**.
PR #24 sigue separado. No fusionar a develop sin integrar ambas ramas en orden y realizar regresión.

La base actual de integración parece contener `dbo.HorariosMedicos`, pero esa tabla no figura
en el esquema público de `database/script-database/Hospital_DB4.sql`. La migración
agrega una tabla aislada `DisponibilidadMedicaSemanal` para no alterar la tabla de estructura desconocida.
La consolidación de ambos conceptos queda **pendiente** de coordinar con SQL.

## Instalación SOLO en base de integración

1. Respaldar Hospital_DB4_Integracion.
2. Cambiar a la rama de horarios, actualizar, compilar:
   `dotnet build ProyectoHospital/ProyectoHospital.slnx`.
3. Abrir en SSMS:
   `ProyectoHospital/database/migrations/20261123_DisponibilidadMedicaSemanal_SOLO_INTEGRACION.sql`.
4. Revisar la línea `USE Hospital_DB4_Integracion` y ejecutar F5, sin modificar Hospital_DB4.
5. Abrir `/DisponibilidadMedica` con admin.

## Escenarios para validar

| Caso | Acción | Resultado esperado |
|---|---|---|
| HM-01 | Admin abre módulo | Ve médicos activos #4 y #5 y formulario |
| HM-02 | Registrar Lunes 08:00–12:00 para #5 | Se crea un bloque |
| HM-03 | Registrar Lunes 12:00–16:00 para #5 | Se crea segundo bloque, sin solapamiento |
| HM-04 | Registrar Lunes 11:30–13:00 para #5 | Rechazo claro por solapamiento, sin bloque nuevo |
| HM-05 | Registrar Lunes 17:00–07:00 | Validación y rechazo |
| HM-06 | Registrar martes 08:00–16:00 | Se crea otro día sin afectar lunes |
| HM-07 | Filtrar #5 | Muestra únicamente sus bloques y suma horas/cupos teóricos |
| HM-07b | Filtrar especialidad Medicina Familiar | Muestra solo los bloques de médicos con esa especialidad |
| HM-08 | Ver como Médico | Puede consultar sin formulario de creación ni baja |
| HM-09 | Ver como Cliente | HTTP 403/Acceso denegado |
| HM-10 | Desactivar bloque sin citas futuras coincidentes | Pasa a inactivo; desaparece de la lista; auditoría |
| HM-11 | Crear otro bloque en el intervalo desactivado | Permitido |
| HM-12 | Desactivar bloque con cita futura activa coincidente | Rechazado |
| HM-13 | Crear en paralelo dos bloques solapados (misma jornada) | Solo uno se registra; la transacción serializa por médico/día |

**Pendientes de integración:** citas con duraciones variables, feriados, ausencias y validación de turnos efectivos en el agendamiento. No afirmar que el reporte muestra cupos reales disponibles.

## SQL de consulta (solo lectura)

```sql
USE Hospital_DB4_Integracion;
SELECT d.BloqueId, d.EmpleadoId, e.Nombre, e.Apellidos,
       d.DiaSemana, d.HoraInicio, d.HoraFin, d.Activo
FROM dbo.DisponibilidadMedicaSemanal d
JOIN dbo.Empleados e ON e.EmpleadoId = d.EmpleadoId
ORDER BY d.EmpleadoId, d.DiaSemana, d.HoraInicio;

SELECT TOP (20) AuditoriaId, UsuarioId, Accion, RegistroId, VNuevos, FechaHora
FROM dbo.BitacoraAuditoria
WHERE Objeto = N'DisponibilidadMedicaController'
ORDER BY AuditoriaId DESC;
```
