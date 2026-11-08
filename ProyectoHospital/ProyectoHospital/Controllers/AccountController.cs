using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ProyectoHospital.Models.Entities;
using ProyectoHospital.Models.ViewModels;
using ProyectoHospital.Repositories;
using ProyectoHospital.Services;
using System.Security.Claims;

namespace ProyectoHospital.Controllers
{
    public class AccountController : Controller
    {
        private const string MensajeCredenciales = "Usuario o contraseña incorrectos.";
        private const string MensajeBloqueo = "La cuenta está bloqueada temporalmente por intentos fallidos. Intente de nuevo más tarde.";

        private readonly UsuarioRepository _usuarios;
        private readonly IPasswordHasher<UsuarioRecord> _passwordHasher;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UsuarioRepository usuarios,
            IPasswordHasher<UsuarioRecord> passwordHasher,
            ILogger<AccountController> logger)
        {
            _usuarios = usuarios;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            string nombreUsuario = model.Usuario.Trim();
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            int? usuarioId = null;

            try
            {
                var usuario = await _usuarios.ObtenerPorUsuarioAsync(nombreUsuario);

                if (usuario is null || !usuario.Activo)
                {
                    await _usuarios.RegistrarAccesoAsync(
                        usuario?.UsuarioId, nombreUsuario, ip, "LOGIN", "Fallido",
                        usuario is null ? "Usuario inexistente." : "Usuario inactivo.");

                    return ErrorLogin(model, MensajeCredenciales);
                }

                usuarioId = usuario.UsuarioId;

                if (usuario.Bloqueado)
                {
                    await _usuarios.RegistrarAccesoAsync(
                        usuario.UsuarioId, nombreUsuario, ip, "LOGIN", "Bloqueado",
                        "Intento de ingreso con la cuenta bloqueada.");

                    return ErrorLogin(model, MensajeBloqueo);
                }

                // Preferir PasswordHasher. Se admite el hash legado
                // solo para migrarlo a Identity tras un acceso válido.
                PasswordVerificationResult verificacion = PasswordVerificationResult.Failed;

                if (!string.IsNullOrWhiteSpace(usuario.PasswordHashIdentity))
                {
                    verificacion = _passwordHasher.VerifyHashedPassword(
                        usuario, usuario.PasswordHashIdentity, model.Password);
                }
                else if (usuario.PasswordSalt is not null &&
                         usuario.PasswordHash is not null &&
                         PasswordVerifier.VerifySha512(
                             usuario.PasswordSalt, usuario.PasswordHash, model.Password))
                {
                    verificacion = PasswordVerificationResult.SuccessRehashNeeded;
                }

                if (verificacion == PasswordVerificationResult.Failed)
                {
                    bool bloqueadoAhora = await _usuarios.RegistrarIntentoFallidoAsync(usuario.UsuarioId);

                    await _usuarios.RegistrarAccesoAsync(
                        usuario.UsuarioId, nombreUsuario, ip, "LOGIN",
                        bloqueadoAhora ? "Bloqueado" : "Fallido",
                        bloqueadoAhora
                            ? $"Contraseña incorrecta. Cuenta bloqueada {UsuarioRepository.MinutosBloqueo} minutos."
                            : "Contraseña incorrecta.");

                    return ErrorLogin(model, bloqueadoAhora ? MensajeBloqueo : MensajeCredenciales);
                }

                // Actualizar la representación de la contraseña si era legada
                // o si Identity recomienda un nuevo hash.
                if (verificacion == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    string nuevoHash = _passwordHasher.HashPassword(usuario, model.Password);
                    await _usuarios.GuardarPasswordIdentityAsync(usuario.UsuarioId, nuevoHash);
                }

                var roles = await _usuarios.ObtenerRolesAsync(usuario.UsuarioId);
                if (roles.Count == 0)
                    return ErrorLogin(model, "El usuario no tiene un rol activo asignado.");

                await _usuarios.ReiniciarIntentosAsync(usuario.UsuarioId);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                    new Claim(ClaimTypes.Name, usuario.NombreUsuario)
                };

                foreach (var rol in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, rol));
                }

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Registrar el acceso ANTES de emitir la cookie:
                // si falla la bitácora, no queda una sesión iniciada.
                await _usuarios.RegistrarAccesoAsync(
                    usuario.UsuarioId, usuario.NombreUsuario, ip, "LOGIN", "Exitoso",
                    "Inicio de sesión correcto.");

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar sesión.");

                try
                {
                    await _usuarios.RegistrarErrorAsync(usuarioId, "LOGIN", ex);
                }
                catch (Exception exBitacora)
                {
                    _logger.LogError(exBitacora, "No fue posible registrar el error en BitacoraErrores.");
                }

                return ErrorLogin(model, "No fue posible iniciar sesión. Intente de nuevo o contacte al administrador.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            string nombreUsuario = User.Identity?.Name ?? string.Empty;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            int? usuarioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            try
            {
                await _usuarios.RegistrarAccesoAsync(
                    usuarioId, nombreUsuario, ip, "LOGOUT", "Exitoso",
                    "Cierre de sesión.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible registrar el cierre de sesión en BitacoraAccesos.");
            }

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult ErrorLogin(LoginViewModel model, string mensaje)
        {
            ModelState.AddModelError(string.Empty, mensaje);
            model.Password = string.Empty;
            return View(model);
        }
    }
}
