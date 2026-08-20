using InmoDev.Models;
using Microsoft.AspNetCore.Mvc;

namespace InmoDev.Controllers;

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
    public IActionResult Create(Inquilino inquilino)
    {
        if (!ModelState.IsValid)
        {
            return View(inquilino);
        }

        repositorio.Alta(inquilino);
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
    public IActionResult Edit(int id, Inquilino inquilino)
    {
        if (id != inquilino.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(inquilino);
        }

        repositorio.Modificacion(inquilino);
        TempData["Mensaje"] = "Inquilino actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var inquilino = repositorio.ObtenerPorId(id);
        return inquilino == null ? NotFound() : View(inquilino);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        repositorio.Baja(id);
        TempData["Mensaje"] = "Inquilino eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
