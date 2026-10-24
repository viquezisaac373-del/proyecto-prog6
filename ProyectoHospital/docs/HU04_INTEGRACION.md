# Integración HU04 — Interfaz inicial

Se integraron los componentes desarrollados por Rodrigo en la **aplicación oficial**:
`ProyectoHospital/ProyectoHospital.csproj`.

Se conservan el controlador `HomeController` original (incluidas las acciones
`Privacy` y `Error`), la configuración de la solución y la base de datos.
No se añade otro proyecto ASP.NET.

## Archivos de la interfaz
- `Controllers/HomeController.cs`: entrega servicios de muestra mediante `HomeViewModel`.
- `Models/ViewModels/HomeViewModel.cs` y `ServicioHospitalario.cs`: modelo de portada.
- `Views/Home/Index.cshtml`: Home y tarjetas.
- `Views/Shared/_Navbar.cshtml`: navegación reutilizable.
- `Views/Shared/_Layout.cshtml`: incorpora el Navbar en el layout original.
- `wwwroot/css/site.css`: tamaño del logo.
- `wwwroot/images/favicon.svg`: símbolo provisional.

## Para probar en Visual Studio
1. Actualizar la rama y abrir `ProyectoHospital/ProyectoHospital.slnx`.
2. Ejecutar `ProyectoHospital` con F5 (.NET 8).
3. Verificar Home, tarjetas, Navbar, menú móvil, enlaces y favicon.
4. Validar también `/Home/Privacy` y `/Home/Error`.

## Pendientes
- HU03: integrar nombre, logo, favicon y paleta **aprobados**.
- HU04-T06: completar pruebas responsive en Visual Studio/navegador con evidencias.
- El inicio de sesión, contacto y módulos médicos se presentan como
  **próximamente**; no se implementan en esta HU.
- Compilación y revisión QA de Danna pendientes antes de fusionar con `develop`.

Fuente: `feature/hu04-interfaz/Hospital/HospitalWeb` (Rodrigo).

## Demostración de paletas al cliente
La portada incluye tres botones con las propuestas originales de Danna, que
cambian únicamente los estilos en el navegador (sin conexión a BD):
- Azul: `#FFFFFF`, `#E7F1FB`, `#9EC9F3`, `#2F78C8`, `#173A5E`.
- Turquesa: `#F3FBF8`, `#CDEEEE`, `#6FD0CC`, `#1A9E9B`, `#0C4E4C`.
- Tecnológico: `#F7F8FA`, `#D9DEE6`, `#8C96A6`, `#394150`, `#0F172A`.

**La elección no se guarda**; es una demostración para decidir con el cliente.
El logo, nombre y favicon actuales son provisionales y se reemplazarán
cuando el cliente elija una propuesta, en el siguiente avance.
