using InmoDev.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace InmoDev.Controllers;

[Authorize]
public class InquilinosController : Controller
{
    private readonly IRepositorio<Inquilino> repositorio;
    private readonly ILogger<InquilinosController> logger;

    public InquilinosController(IRepositorio<Inquilino> repositorio, ILogger<InquilinosController> logger)
    {
        this.repositorio = repositorio;
        this.logger = logger;
    }

    public IActionResult Index(int pagina = 1)
    {
        try
        {
            const int tamPagina = 10;
            pagina = Math.Max(pagina, 1);
            var total = repositorio.ObtenerCantidad();
            ViewBag.Pagina = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)tamPagina);

            if (TempData.ContainsKey("Mensaje"))
            {
                ViewBag.Mensaje = TempData["Mensaje"];
            }

            return View(repositorio.ObtenerLista(pagina, tamPagina));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al listar inquilinos");
            throw;
        }
    }

    public IActionResult Details(int id)
    {
        var inquilino = repositorio.ObtenerPorId(id);
        return inquilino == null ? NotFound() : View(inquilino);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind("DNI,NombreCompleto,Telefono,Email,Direccion")] Inquilino inquilino)
    {
        Normalizar(inquilino);
        if (!ModelState.IsValid)
        {
            return View(inquilino);
        }

        try
        {
            repositorio.Alta(inquilino);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            logger.LogWarning(ex, "Intento de crear inquilino duplicado: {DNI} / {Email}", inquilino.DNI, inquilino.Email);
            AgregarErrorDuplicado(ex);
            return View(inquilino);
        }

        TempData["Mensaje"] = "Inquilino creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var inquilino = repositorio.ObtenerPorId(id);
        return inquilino == null ? NotFound() : View(inquilino);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, [Bind("Id,DNI,NombreCompleto,Telefono,Email,Direccion")] Inquilino inquilino)
    {
        if (id != inquilino.Id)
        {
            return BadRequest();
        }

        Normalizar(inquilino);
        if (!ModelState.IsValid)
        {
            return View(inquilino);
        }

        try
        {
            repositorio.Modificacion(inquilino);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            logger.LogWarning(ex, "Intento de actualizar inquilino duplicado: {DNI} / {Email}", inquilino.DNI, inquilino.Email);
            AgregarErrorDuplicado(ex);
            return View(inquilino);
        }

        TempData["Mensaje"] = "Inquilino actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Delete(int id)
    {
        var inquilino = repositorio.ObtenerPorId(id);
        return inquilino == null ? NotFound() : View(inquilino);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public IActionResult DeleteConfirmed(int id)
    {
        repositorio.Baja(id);
        TempData["Mensaje"] = "Inquilino eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private void AgregarErrorDuplicado(MySqlException ex)
    {
        if (ex.Message.Contains("UQ_Inquilinos_DNI", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(Inquilino.DNI), "Ya existe un inquilino registrado con ese DNI.");
            return;
        }

        if (ex.Message.Contains("UQ_Inquilinos_Email", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(Inquilino.Email), "Ya existe un inquilino registrado con ese email.");
            return;
        }

        ModelState.AddModelError(string.Empty, "Ya existe un inquilino registrado con esos datos.");
    }

    private static void Normalizar(Inquilino inquilino)
    {
        inquilino.DNI = inquilino.DNI?.Trim() ?? "";
        inquilino.NombreCompleto = inquilino.NombreCompleto?.Trim() ?? "";
        inquilino.Telefono = string.IsNullOrWhiteSpace(inquilino.Telefono) ? null : inquilino.Telefono.Trim();
        inquilino.Email = string.IsNullOrWhiteSpace(inquilino.Email) ? null : inquilino.Email.Trim().ToLowerInvariant();
        inquilino.Direccion = string.IsNullOrWhiteSpace(inquilino.Direccion) ? null : inquilino.Direccion.Trim();
    }
}
