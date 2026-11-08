# HU08 — Integración de autenticación y nueva navegación

## Propósito

Esta rama `fix/hu08-seguridad-home` parte del avance `feature/login` de Carlos y agrega
correcciones para evitar integrar una contraseña de administrador fija y SHA-512 directo.

**Flujo propuesto:** Login → Inicio informativo → Panel de módulos.
Las acciones de pacientes se mantienen en HU09; citas y medicamentos aún muestran páginas informativas.

## Puesta en marcha para QA (solo datos ficticios)

1. Actualizar el repositorio y seleccionar esta rama.
2. Confirmar que SQL Server tiene la base **Hospital_DB4**; si no existe,
   crearla con `ProyectoHospital/database/script-database/Hospital_DB4.sql`.
3. Respaldar la base si tiene datos de pruebas. Ejecutar en SSMS:
   `ProyectoHospital/database/migrations/20261107_HU08_PasswordHasher.sql`.
   El archivo usa `USE Hospital_DB4`. Si el nombre local es distinto, ajustar
   la selección de base antes de ejecutarlo. No ejecutar sobre bases ajenas.
4. En Visual Studio, **Administrar secretos de usuario** del proyecto web
   `ProyectoHospital/ProyectoHospital/ProyectoHospital.csproj`.
   Configurar allí `ConnectionStrings:HospitalDB` con el servidor y base propios.
5. En la raíz del repositorio, ejecutar la utilidad de pruebas:

   ```bash
   dotnet run --project tools/HU08SeedAdmin/HU08SeedAdmin.csproj
   ```

   Genera una contraseña aleatoria para `admin` y muestra una sola vez la
   contraseña. Si `admin` ya existe, solicita confirmación ANTES de restablecerla.
   No reutilizar `Admin123!` ni registrar capturas con la nueva contraseña.
6. Compilar y ejecutar el proyecto web con `dotnet run --launch-profile https
   --project ProyectoHospital/ProyectoHospital/ProyectoHospital.csproj`.
7. Verificar el acceso en `/Account/Login`, el inicio informativo en
   `/Home/Index`, el panel en `/Modulos`, y el módulo `/Pacientes`.
   Verificar logout y redirección desde URLs privadas sin sesión.

## Compatibilidad y autorización

- `PasswordHashIdentity` almacena el formato de `PasswordHasher` Identity V3.
  Los campos legados `PasswordHash` y `PasswordSalt` siguen disponibles para
  usuarios anteriores; después de ingresar correctamente se migra el hash y
  se limpian ambos campos legados.
- Se conserva el bloqueo de 5 intentos/15 minutos y el registro de accesos.
- En esta etapa `PacientesController` requiere rol `Administrador`.
  **La matriz de permisos de médicos y recepción debe acordarse con el cliente.**
- No se habilita una cuenta administrativa con contraseña fija en el repositorio.
- El script antiguo `script_HU08_Usuario_Pruebas.sql` queda deprecado.
- La autenticación no convierte la aplicación en un producto apto para datos reales:
  siguen pendientes pruebas de penetración, rotación de claves, despliegue y WCF/CoreWCF.

## Pruebas exigidas antes del merge

- `dotnet build ProyectoHospital/ProyectoHospital/ProyectoHospital.csproj`
- `dotnet run --project tools/HU08SeedAdmin/HU08SeedAdmin.csproj`
  en entorno local de pruebas con migración aplicada.
- Contraseña incorrecta, bloqueo y desbloqueo tras el tiempo definido.
- Inicio válido con usuario de pruebas; cierre y acceso privado sin sesión.
- Rol sin permisos: no debe acceder a `/Pacientes`.
- `/Modulos` enlaza al listado real de HU09 para administradores.
- Registro, edición y desactivación de pacientes ficticios.
- Interfaz en móvil y navegadores compatibles.

No ejecutar migraciones ni utilidades administrativas en producción sin revisión independiente.
