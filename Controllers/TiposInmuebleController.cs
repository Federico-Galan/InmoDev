using InmoDev.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace InmoDev.Controllers;

public class TiposInmuebleController : Controller
{
    private readonly IRepositorio<TipoInmueble> repositorio;
    private readonly ILogger<TiposInmuebleController> logger;

    public TiposInmuebleController(IRepositorio<TipoInmueble> repositorio, ILogger<TiposInmuebleController> logger)
    {
        this.repositorio = repositorio;
        this.logger = logger;
    }

    public IActionResult Index(int pagina = 1)
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

    public IActionResult Details(int id)
    {
        var tipo = repositorio.ObtenerPorId(id);
        return tipo == null ? NotFound() : View(tipo);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind("Nombre,Descripcion,Activo")] TipoInmueble tipo)
    {
        Normalizar(tipo);
        if (!ModelState.IsValid)
        {
            return View(tipo);
        }

        try
        {
            repositorio.Alta(tipo);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            logger.LogWarning(ex, "Intento de crear tipo de inmueble duplicado: {Nombre}", tipo.Nombre);
            ModelState.AddModelError(nameof(TipoInmueble.Nombre), "Ya existe un tipo de inmueble con ese nombre.");
            return View(tipo);
        }

        TempData["Mensaje"] = "Tipo de inmueble creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var tipo = repositorio.ObtenerPorId(id);
        return tipo == null ? NotFound() : View(tipo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, [Bind("Id,Nombre,Descripcion,Activo")] TipoInmueble tipo)
    {
        if (id != tipo.Id)
        {
            return BadRequest();
        }

        Normalizar(tipo);
        if (!ModelState.IsValid)
        {
            return View(tipo);
        }

        try
        {
            repositorio.Modificacion(tipo);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            logger.LogWarning(ex, "Intento de actualizar tipo de inmueble duplicado: {Nombre}", tipo.Nombre);
            ModelState.AddModelError(nameof(TipoInmueble.Nombre), "Ya existe un tipo de inmueble con ese nombre.");
            return View(tipo);
        }

        TempData["Mensaje"] = "Tipo de inmueble actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var tipo = repositorio.ObtenerPorId(id);
        return tipo == null ? NotFound() : View(tipo);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        try
        {
            repositorio.Baja(id);
            TempData["Mensaje"] = "Tipo de inmueble eliminado correctamente.";
        }
        catch (MySqlException ex) when (ex.Number == 1451)
        {
            logger.LogWarning(ex, "No se puede eliminar tipo de inmueble con inmuebles asociados: {Id}", id);
            TempData["Mensaje"] = "No se puede eliminar el tipo porque tiene inmuebles asociados.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static void Normalizar(TipoInmueble tipo)
    {
        tipo.Nombre = tipo.Nombre?.Trim() ?? "";
        tipo.Descripcion = string.IsNullOrWhiteSpace(tipo.Descripcion) ? null : tipo.Descripcion.Trim();
    }
}
