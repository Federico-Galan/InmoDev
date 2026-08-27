using InmoDev.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

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
    public IActionResult Create([Bind("Nombre,Telefono,Email,Direccion,Activo")] Propietario propietario)
    {
        if (!ValidarParaGuardar(propietario))
        {
            return View(propietario);
        }

        try
        {
            repositorio.Alta(propietario);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            logger.LogWarning(ex, "Intento de crear propietario con email duplicado: {Email}", propietario.Email);
            ModelState.AddModelError(nameof(Propietario.Email), "Ya existe un propietario registrado con ese email.");
            return View(propietario);
        }

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
    public IActionResult Edit(int id, [Bind("Id,Nombre,Telefono,Email,Direccion,Activo")] Propietario propietario)
    {
        if (id != propietario.Id)
        {
            return BadRequest();
        }

        if (!ValidarParaGuardar(propietario))
        {
            return View(propietario);
        }

        try
        {
            repositorio.Modificacion(propietario);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            logger.LogWarning(ex, "Intento de actualizar propietario con email duplicado: {Email}", propietario.Email);
            ModelState.AddModelError(nameof(Propietario.Email), "Ya existe un propietario registrado con ese email.");
            return View(propietario);
        }

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

    private bool ValidarParaGuardar(Propietario propietario)
    {
        Normalizar(propietario);

        ModelState.Clear();
        TryValidateModel(propietario);
        ValidarReglasDeSeguridad(propietario);

        return ModelState.IsValid;
    }

    private void ValidarReglasDeSeguridad(Propietario propietario)
    {
        if (propietario.Nombre.Any(char.IsDigit) || propietario.Nombre.Any(c => !char.IsLetter(c) && !char.IsWhiteSpace(c)))
        {
            ModelState.AddModelError(nameof(Propietario.Nombre), "No se pueden cargar numeros ni simbolos en el nombre.");
        }

        var emailPartes = propietario.Email.Split('@');
        if (emailPartes.Length != 2 || !emailPartes[0].Any(char.IsLetter))
        {
            ModelState.AddModelError(nameof(Propietario.Email), "El email no puede ser solo numerico.");
            return;
        }

        if (emailPartes[1] is not ("gmail.com" or "hotmail.com"))
        {
            ModelState.AddModelError(nameof(Propietario.Email), "Solo se permiten emails @gmail.com o @hotmail.com.");
        }
    }

    private static void Normalizar(Propietario propietario)
    {
        propietario.Nombre = propietario.Nombre?.Trim() ?? "";
        propietario.Telefono = propietario.Telefono?.Trim() ?? "";
        propietario.Email = propietario.Email?.Trim().ToLowerInvariant() ?? "";
        propietario.Direccion = propietario.Direccion?.Trim() ?? "";
    }
}
