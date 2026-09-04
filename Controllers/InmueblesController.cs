using InmoDev.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;

namespace InmoDev.Controllers;

public class InmueblesController : Controller
{
    private readonly RepositorioInmueble repositorio;
    private readonly RepositorioImagen repositorioImagen;
    private readonly IWebHostEnvironment environment;
    private readonly ILogger<InmueblesController> logger;

    public InmueblesController(
        RepositorioInmueble repositorio,
        RepositorioImagen repositorioImagen,
        IWebHostEnvironment environment,
        ILogger<InmueblesController> logger)
    {
        this.repositorio = repositorio;
        this.repositorioImagen = repositorioImagen;
        this.environment = environment;
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
        var inmueble = repositorio.ObtenerPorId(id);
        return inmueble == null ? NotFound() : View(inmueble);
    }

    public IActionResult Create()
    {
        CargarCombos();
        return View(new Inmueble());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind("PropietarioId,TipoId,Direccion,CupoMaximo,Coordenadas,PrecioPorDia,MonedaPrecio,ImagenPortada,Disponible")] Inmueble inmueble)
    {
        Normalizar(inmueble);
        if (!ModelState.IsValid)
        {
            CargarCombos(inmueble);
            return View(inmueble);
        }

        repositorio.Alta(inmueble);
        TempData["Mensaje"] = "Inmueble creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var inmueble = repositorio.ObtenerPorId(id);
        if (inmueble == null)
        {
            return NotFound();
        }

        CargarCombos(inmueble);
        return View(inmueble);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, [Bind("Id,PropietarioId,TipoId,Direccion,CupoMaximo,Coordenadas,PrecioPorDia,MonedaPrecio,ImagenPortada,Disponible")] Inmueble inmueble)
    {
        if (id != inmueble.Id)
        {
            return BadRequest();
        }

        Normalizar(inmueble);
        if (!ModelState.IsValid)
        {
            CargarCombos(inmueble);
            return View(inmueble);
        }

        repositorio.Modificacion(inmueble);
        TempData["Mensaje"] = "Inmueble actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var inmueble = repositorio.ObtenerPorId(id);
        return inmueble == null ? NotFound() : View(inmueble);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        try
        {
            repositorio.Baja(id);
            TempData["Mensaje"] = "Inmueble eliminado correctamente.";
        }
        catch (MySqlException ex) when (ex.Number == 1451)
        {
            logger.LogWarning(ex, "No se puede eliminar inmueble con reservas asociadas: {Id}", id);
            TempData["Mensaje"] = "No se puede eliminar el inmueble porque tiene reservas o imagenes asociadas.";
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Fotos(int id)
    {
        var inmueble = repositorio.ObtenerPorId(id);
        if (inmueble == null)
        {
            return NotFound();
        }

        ViewBag.Imagenes = repositorioImagen.ObtenerPorInmueble(id);
        return View(inmueble);
    }

    public IActionResult BuscarPropietarios(string? q)
    {
        return Json(repositorio.ObtenerPropietarios(q));
    }

    public IActionResult BuscarTipos(string? q)
    {
        return Json(repositorio.ObtenerTipos(q));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirPortada(int id, IFormFile? imagen)
    {
        var inmueble = repositorio.ObtenerPorId(id);
        if (inmueble == null)
        {
            return NotFound();
        }

        var validacion = ValidarImagen(imagen);
        if (validacion != null)
        {
            return BadRequest(new { mensaje = validacion });
        }

        var url = await GuardarArchivoAsync(id, imagen!);
        repositorio.ActualizarPortada(id, url);
        return Json(new { mensaje = "Portada actualizada correctamente.", url });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirImagenes(int id, List<IFormFile> imagenes)
    {
        var inmueble = repositorio.ObtenerPorId(id);
        if (inmueble == null)
        {
            return NotFound();
        }

        if (imagenes.Count == 0)
        {
            return BadRequest(new { mensaje = "Debe seleccionar al menos una imagen." });
        }

        var urls = new List<string>();
        foreach (var imagen in imagenes)
        {
            var validacion = ValidarImagen(imagen);
            if (validacion != null)
            {
                return BadRequest(new { mensaje = validacion });
            }

            var url = await GuardarArchivoAsync(id, imagen);
            repositorioImagen.Alta(new ImagenInmueble
            {
                InmuebleId = id,
                Url = url,
                EsPortada = false
            });
            urls.Add(url);
        }

        return Json(new { mensaje = "Imagenes subidas correctamente.", imagenes = repositorioImagen.ObtenerPorInmueble(id) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarImagen(int id)
    {
        var imagen = repositorioImagen.ObtenerPorId(id);
        if (imagen == null)
        {
            return NotFound();
        }

        EliminarArchivo(imagen.Url);
        repositorioImagen.Baja(id);
        return Json(new { mensaje = "Imagen eliminada correctamente.", imagenes = repositorioImagen.ObtenerPorInmueble(imagen.InmuebleId) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarPortada(int id)
    {
        var inmueble = repositorio.ObtenerPorId(id);
        if (inmueble == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(inmueble.ImagenPortada))
        {
            EliminarArchivo(inmueble.ImagenPortada);
        }

        repositorio.ActualizarPortada(id, null);
        return Json(new { mensaje = "Portada eliminada correctamente." });
    }

    private void CargarCombos(Inmueble? inmueble = null)
    {
        ViewBag.Propietarios = new SelectList(repositorio.ObtenerPropietarios(), "Id", "Texto", inmueble?.PropietarioId);
        ViewBag.Tipos = new SelectList(repositorio.ObtenerTipos(), "Id", "Texto", inmueble?.TipoId);
    }

    private async Task<string> GuardarArchivoAsync(int inmuebleId, IFormFile imagen)
    {
        var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
        var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
        var carpeta = Path.Combine(environment.WebRootPath, "uploads", "inmuebles", inmuebleId.ToString());
        Directory.CreateDirectory(carpeta);
        var ruta = Path.Combine(carpeta, nombreArchivo);

        await using var stream = System.IO.File.Create(ruta);
        await imagen.CopyToAsync(stream);

        return $"/uploads/inmuebles/{inmuebleId}/{nombreArchivo}";
    }

    private void EliminarArchivo(string url)
    {
        var rutaRelativa = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var ruta = Path.Combine(environment.WebRootPath, rutaRelativa);
        if (System.IO.File.Exists(ruta))
        {
            System.IO.File.Delete(ruta);
        }
    }

    private static string? ValidarImagen(IFormFile? imagen)
    {
        if (imagen == null || imagen.Length == 0)
        {
            return "Debe seleccionar una imagen.";
        }

        if (imagen.Length > 3 * 1024 * 1024)
        {
            return "La imagen no puede superar los 3 MB.";
        }

        var extensionesPermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        var extension = Path.GetExtension(imagen.FileName);
        return extensionesPermitidas.Contains(extension) ? null : "Solo se permiten imagenes JPG, PNG o WEBP.";
    }

    private static void Normalizar(Inmueble inmueble)
    {
        inmueble.Direccion = inmueble.Direccion?.Trim() ?? "";
        inmueble.Coordenadas = string.IsNullOrWhiteSpace(inmueble.Coordenadas) ? null : inmueble.Coordenadas.Trim();
        inmueble.MonedaPrecio = inmueble.MonedaPrecio == "USD" ? "USD" : "ARS";
        inmueble.ImagenPortada = string.IsNullOrWhiteSpace(inmueble.ImagenPortada) ? null : inmueble.ImagenPortada.Trim();
    }
}
