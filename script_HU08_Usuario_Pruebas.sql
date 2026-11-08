/*
 DEPRECADO - HU08 (seguridad de cuentas)

 Este archivo ya no genera usuarios con contraseñas compartidas.
 NO ejecutar el antiguo script que insertaba admin / Admin123!.

 1) Cree Hospital_DB4 a partir del esquema del proyecto (si aún no existe).
 2) Ejecute ProyectoHospital/database/migrations/20261107_HU08_PasswordHasher.sql.
 3) Configure ConnectionStrings:HospitalDB con secretos de usuario.
 4) Ejecute la utilidad: dotnet run --project tools/HU08SeedAdmin/HU08SeedAdmin.csproj

 La herramienta genera una contraseña aleatoria y guarda solamente su hash
 con PasswordHasher (Identity V3). No se introducen credenciales en GitHub.
*/
