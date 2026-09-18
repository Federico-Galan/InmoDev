using System.Security.Claims;
using InmoDev.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InmoDev.Controllers;

public class UsuariosController : Controller
{
    private readonly RepositorioUsuario repositorio;
    private readonly IWebHostEnvironment environment;
    private readonly ILogger<UsuariosController> logger;
    private readonly IPasswordHasher<Usuario> passwordHasher;

    public UsuariosController(
        RepositorioUsuario repositorio,
        IWebHostEnvironment environment,
        ILogger<UsuariosController> logger,
        IPasswordHasher<Usuario> passwordHasher)
    {
        this.repositorio = repositorio;
        this.environment = environment;
        this.logger = logger;
        this.passwordHasher = passwordHasher;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usuario = repositorio.ObtenerPorEmail(model.Email);
        if (usuario == null || !usuario.Activo)
        {
            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos, o la cuenta esta inactiva.");
            return View(model);
        }

        var verifyResult = passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, model.Clave);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol),
            new("Avatar", usuario.Avatar ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.Recordarme,
            ExpiresUtc = model.Recordarme ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        logger.LogInformation("Usuario {Email} inicio sesion con rol {Rol}", usuario.Email, usuario.Rol);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Denegado()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult Perfil()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var id))
        {
            return RedirectToAction("Login");
        }

        var usuario = repositorio.ObtenerPorId(id);
        if (usuario == null)
        {
            return NotFound();
        }

        var vm = new PerfilViewModel
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol,
            AvatarActual = usuario.Avatar
        };

        if (TempData.ContainsKey("Mensaje"))
        {
            ViewBag.Mensaje = TempData["Mensaje"];
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(PerfilViewModel model)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var id) || id != model.Id)
        {
            return Forbid();
        }

        var usuario = repositorio.ObtenerPorId(id);
        if (usuario == null)
        {
            return NotFound();
        }

        model.Rol = usuario.Rol;
        model.Email = usuario.Email;
        model.AvatarActual = usuario.Avatar;

        if (!string.IsNullOrWhiteSpace(model.NuevaClave))
        {
            if (string.IsNullOrWhiteSpace(model.ClaveActual))
            {
                ModelState.AddModelError("ClaveActual", "Debe ingresar su contraseña actual para establecer una nueva.");
            }
            else
            {
                var check = passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, model.ClaveActual);
                if (check == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("ClaveActual", "La contraseña actual es incorrecta.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string? nuevoAvatar = usuario.Avatar;
        if (model.FotoAvatar != null && model.FotoAvatar.Length > 0)
        {
            var validacion = ValidarImagen(model.FotoAvatar);
            if (validacion != null)
            {
                ModelState.AddModelError("FotoAvatar", validacion);
                return View(model);
            }
            nuevoAvatar = await GuardarAvatarAsync(usuario.Id, model.FotoAvatar, usuario.Avatar);
        }

        repositorio.ActualizarPerfil(usuario.Id, model.Nombre.Trim(), nuevoAvatar);

        if (!string.IsNullOrWhiteSpace(model.NuevaClave))
        {
            var nuevoHash = passwordHasher.HashPassword(usuario, model.NuevaClave);
            repositorio.ActualizarClave(usuario.Id, nuevoHash);
        }

        TempData["Mensaje"] = "Perfil actualizado correctamente. Los cambios se reflejaran en su proxima sesion o recarga.";
        return RedirectToAction(nameof(Perfil));
    }

    // ==========================================
    // ABM DE USUARIOS (Exclusivo Administrador)
    // ==========================================

    [Authorize(Roles = "Administrador")]
    public IActionResult Index(int pagina = 1, string? busqueda = null)
    {
        const int tamPagina = 10;
        pagina = Math.Max(pagina, 1);
        var total = repositorio.ObtenerCantidad(busqueda);
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)tamPagina);
        ViewBag.Busqueda = busqueda;

        if (TempData.ContainsKey("Mensaje"))
        {
            ViewBag.Mensaje = TempData["Mensaje"];
        }

        return View(repositorio.ObtenerLista(pagina, tamPagina, busqueda));
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Create()
    {
        return View(new UsuarioFormViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Clave) || model.Clave.Length < 6)
        {
            ModelState.AddModelError("Clave", "La contraseña es obligatoria y debe tener al menos 6 caracteres.");
        }

        var existente = repositorio.ObtenerPorEmail(model.Email);
        if (existente != null)
        {
            ModelState.AddModelError("Email", "Ya existe un usuario registrado con este correo.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usuario = new Usuario
        {
            Nombre = model.Nombre.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            Rol = model.Rol,
            Activo = model.Activo
        };

        usuario.PasswordHash = passwordHasher.HashPassword(usuario, model.Clave!);
        var id = repositorio.Alta(usuario);

        if (model.FotoAvatar != null && model.FotoAvatar.Length > 0)
        {
            var avatarUrl = await GuardarAvatarAsync(id, model.FotoAvatar);
            repositorio.ActualizarPerfil(id, usuario.Nombre, avatarUrl);
        }

        TempData["Mensaje"] = "Usuario creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Edit(int id)
    {
        var usuario = repositorio.ObtenerPorId(id);
        if (usuario == null)
        {
            return NotFound();
        }

        var vm = new UsuarioFormViewModel
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol,
            Avatar = usuario.Avatar,
            Activo = usuario.Activo
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UsuarioFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var usuario = repositorio.ObtenerPorId(id);
        if (usuario == null)
        {
            return NotFound();
        }

        var existente = repositorio.ObtenerPorEmail(model.Email);
        if (existente != null && existente.Id != id)
        {
            ModelState.AddModelError("Email", "Ya existe otro usuario con este correo electronico.");
        }

        if (!ModelState.IsValid)
        {
            model.Avatar = usuario.Avatar;
            return View(model);
        }

        usuario.Nombre = model.Nombre.Trim();
        usuario.Email = model.Email.Trim().ToLowerInvariant();
        usuario.Rol = model.Rol;
        usuario.Activo = model.Activo;

        if (model.FotoAvatar != null && model.FotoAvatar.Length > 0)
        {
            var avatarAnterior = usuario.Avatar;
            usuario.Avatar = await GuardarAvatarAsync(id, model.FotoAvatar, avatarAnterior);
        }

        repositorio.Modificacion(usuario);

        if (!string.IsNullOrWhiteSpace(model.Clave))
        {
            if (model.Clave.Length < 6)
            {
                ModelState.AddModelError("Clave", "La nueva contraseña debe tener al menos 6 caracteres.");
                return View(model);
            }
            var nuevoHash = passwordHasher.HashPassword(usuario, model.Clave);
            repositorio.ActualizarClave(id, nuevoHash);
        }

        TempData["Mensaje"] = "Usuario actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Delete(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id.ToString())
        {
            TempData["Mensaje"] = "No puede eliminarse a usted mismo.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = repositorio.ObtenerPorId(id);
        return usuario == null ? NotFound() : View(usuario);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id.ToString())
        {
            TempData["Mensaje"] = "No puede eliminarse a usted mismo.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            repositorio.Baja(id);
            TempData["Mensaje"] = "Usuario eliminado correctamente.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo eliminar usuario {Id}", id);
            TempData["Mensaje"] = "No se puede eliminar el usuario porque tiene auditoria asociada. Puede desactivarlo.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<string> GuardarAvatarAsync(int usuarioId, IFormFile avatar, string? avatarAnterior = null)
    {
        var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
        var nombreArchivo = $"avatar_{usuarioId}_{Guid.NewGuid():N}{extension}";
        var carpeta = Path.Combine(environment.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(carpeta);
        var ruta = Path.Combine(carpeta, nombreArchivo);

        await using var stream = System.IO.File.Create(ruta);
        await avatar.CopyToAsync(stream);

        // Doc 19: si el usuario ya tenia un avatar previo, el archivo anterior se elimina
        // del disco para evitar acumulacion de huerfanos.
        if (!string.IsNullOrWhiteSpace(avatarAnterior)
            && avatarAnterior.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var rutaAnterior = Path.Combine(environment.WebRootPath, avatarAnterior.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(rutaAnterior))
                {
                    System.IO.File.Delete(rutaAnterior);
                }
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "No se pudo eliminar el avatar anterior {Avatar}", avatarAnterior);
            }
        }

        return $"/uploads/avatars/{nombreArchivo}";
    }

    private static string? ValidarImagen(IFormFile? imagen)
    {
        if (imagen == null || imagen.Length == 0) return null;
        if (imagen.Length > 2 * 1024 * 1024) return "La imagen no puede superar los 2 MB.";
        var ext = Path.GetExtension(imagen.FileName).ToLowerInvariant();
        var permitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        return permitidas.Contains(ext) ? null : "Formato invalido. Solo JPG, PNG o WEBP.";
    }
}
