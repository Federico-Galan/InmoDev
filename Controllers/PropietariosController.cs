using InmoDev.Models;
using Microsoft.AspNetCore.Mvc;

namespace InmoDev.Controllers;

public class PropietariosController : Controller
{
    private readonly IRepositorio<Propietario> repositorio;
    private readonly ILogger<PropietariosController> logger;

    public PropietariosController(IRepositorio<Propietario> repositorio, ILogger<PropietariosController> logger)
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
            logger.LogError(ex, "Error al listar propietarios");
            throw;
        }
    }

    public IActionResult Details(int id)
    {
        var propietario = repositorio.ObtenerPorId(id);
        return propietario == null ? NotFound() : View(propietario);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Propietario propietario)
    {
        if (!ModelState.IsValid)
        {
            return View(propietario);
        }

        repositorio.Alta(propietario);
        TempData["Mensaje"] = "Propietario creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var propietario = repositorio.ObtenerPorId(id);
        return propietario == null ? NotFound() : View(propietario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Propietario propietario)
    {
        if (id != propietario.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(propietario);
        }

        repositorio.Modificacion(propietario);
        TempData["Mensaje"] = "Propietario actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var propietario = repositorio.ObtenerPorId(id);
        return propietario == null ? NotFound() : View(propietario);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        repositorio.Baja(id);
        TempData["Mensaje"] = "Propietario eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
