using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProyectoHospital.Models.Entities;
using ProyectoHospital.Models.ViewModels;
using ProyectoHospital.Repositories;
using System.Security.Claims;

namespace ProyectoHospital.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class ClientesUsuariosController : Controller
{
    private readonly ClienteCuentaRepository _clientes;
    private readonly IPasswordHasher<UsuarioRecord> _hasher;
    private readonly ILogger<ClientesUsuariosController> _logger;

    public ClientesUsuariosController(
        ClienteCuentaRepository clientes,
        IPasswordHasher<UsuarioRecord> hasher,
        ILogger<ClientesUsuariosController> logger)
    {
        _clientes = clientes;
        _hasher = hasher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Vincular()
    {
        return View(new VincularClienteViewModel
        {
            Clientes = await _clientes.ListarSinCuentaAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vincular(VincularClienteViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            modelo.Clientes = await _clientes.ListarSinCuentaAsync();
            return View(modelo);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int actorId) || actorId <= 0)
            return Forbid();

        try
        {
            string usuario = modelo.NombreUsuario.Trim();
            string hash = _hasher.HashPassword(
                new UsuarioRecord { NombreUsuario = usuario }, modelo.Clave);
            await _clientes.VincularPorAdministradorAsync(modelo, hash, actorId);
            TempData["Exito"] = "Cuenta de cliente vinculada correctamente.";
            return RedirectToAction("Index", "Usuarios");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            ModelState.AddModelError(string.Empty, "El usuario o la relación ya existen.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible vincular la cuenta del cliente.");
            ModelState.AddModelError(string.Empty, "No se pudo vincular al cliente.");
        }

        modelo.Clave = string.Empty;
        modelo.ConfirmacionClave = string.Empty;
        ModelState.Remove(nameof(modelo.Clave));
        ModelState.Remove(nameof(modelo.ConfirmacionClave));
        modelo.Clientes = await _clientes.ListarSinCuentaAsync();
        return View(modelo);
    }
}
