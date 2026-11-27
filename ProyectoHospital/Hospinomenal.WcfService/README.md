# HU25 — Servicio CoreWCF de horarios médicos

Este es el servicio SOAP de demostración académica de Hospinomenal (.NET 8). La aplicación MVC y el servicio SOAP corren como **dos proyectos independientes** en la misma solución, tal como en la práctica de clase de la semana 8. No modifica la autenticación existente.

## Preparación

1. Trabaje sobre una **base de datos de integración o una copia**, nunca sobre la original. El servicio solo ejecuta consultas de lectura.
2. Configure el secreto local del servicio (su cadena de conexión propia):

   ```bash
   dotnet user-secrets set "ConnectionStrings:HospitalDB" "Server=...;Database=Hospital_DB4_Integracion;Integrated Security=True;Encrypt=True;TrustServerCertificate=True" --project ProyectoHospital/Hospinomenal.WcfService/Hospinomenal.WcfService.csproj
   ```

   Use su instancia real de SQL Server y preferiblemente autenticación integrada. No copie contraseñas ni secretos al repositorio.
3. Verifique que estén aplicadas las migraciones necesarias de disponibilidad médica semanal y que existan médicos con bloques activos.
4. En dos terminales independientes, desde la raíz del repositorio:

   ```bash
   dotnet run --project ProyectoHospital/Hospinomenal.WcfService/Hospinomenal.WcfService.csproj
   dotnet run --project ProyectoHospital/ProyectoHospital/ProyectoHospital.csproj
   ```

5. Compruebe el documento WSDL en `http://127.0.0.1:5108/HorariosMedicos.svc?wsdl`. Desde Hospinomenal, inicie sesión con un rol autorizado y vaya a **Panel de módulos → Consulta WCF de horarios**.
6. Introduzca un `EmpleadoId` de un médico activo con bloques semanales. Confirme que la pantalla devuelva los días y horas que están registrados en SQL Server.

## Flujo técnico

`MVC /HorariosWcf/Index → HorariosMedicosWcfClient → SOAP HTTP → CoreWCF /HorariosMedicos.svc → SQL Server`.

- El contrato `IHorariosMedicosService` expone la operación `ConsultarHorarios(int medicoId)`.
- El servicio SQL consulta solo `dbo.DisponibilidadMedicaSemanal` y `dbo.Empleados`.
- El endpoint lee `ConnectionStrings:HospitalDB` desde secretos/configuración local.
- El cliente MVC lee `Wcf:HorariosEndpoint` desde `appsettings.Development.json`.

## Pruebas propuestas

- Consultar un médico válido con bloques activos: devuelve días y horas.
- Consultar un médico sin bloques activos: mensaje informativo, sin excepción.
- Enviar `medicoId=0`: validación de entrada en MVC.
- Detener el servicio CoreWCF: MVC muestra un error controlado.
- Intentar acceder a `/HorariosWcf/Index` sin sesión o sin rol permitido: acceso denegado.
- Cotejar los resultados con la tabla de disponibilidad semanal y capturar la URL `?wsdl`.

## Restricciones de seguridad

El servicio se enlaza por defecto a `127.0.0.1:5108` y utiliza HTTP/SOAP sin credenciales **solo para demostración local**. No se debe publicar en Internet ni en una red sin TLS y autenticación/autorización de servicios. El cliente MVC impide conexiones HTTP a hosts que no sean loopback. Los horarios semanales no son cupos libres para citas: no descuentan reservas, feriados ni ausencias.

## Registro de avance en UTC Agile

- HU25-T01: arquitectura y contrato SOAP — preparado.
- HU25-T02: implementación del servicio — código inicial.
- HU25-T03: cliente MVC y pantalla — código inicial.
- HU25-T04: validaciones y manejo de errores — código inicial.
- HU25-T05: pruebas y evidencias — **pendiente** hasta ejecutar y validar en SQL Server.

No marcar la historia como terminada hasta que compile y pase la prueba de extremo a extremo.
