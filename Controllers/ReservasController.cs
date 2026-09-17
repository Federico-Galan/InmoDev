using System.Security.Claims;
using InmoDev.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;

namespace InmoDev.Controllers;

[Authorize]
public class ReservasController : Controller
{
    private readonly RepositorioReserva repositorio;
    private readonly RepositorioInmueble repositorioInmueble;
    private readonly RepositorioTipoInmueble repositorioTipo;
    private readonly ILogger<ReservasController> logger;

    public ReservasController(
        RepositorioReserva repositorio,
        RepositorioInmueble repositorioInmueble,
        RepositorioTipoInmueble repositorioTipo,
        ILogger<ReservasController> logger)
    {
        this.repositorio = repositorio;
        this.repositorioInmueble = repositorioInmueble;
        this.repositorioTipo = repositorioTipo;
        this.logger = logger;
    }

    public IActionResult Index(
        int pagina = 1,
        string? estado = null,
        int? inmuebleId = null,
        int? inquilinoId = null,
        DateTime? desde = null,
        DateTime? hasta = null)
    {
        const int tamPagina = 10;
        pagina = Math.Max(pagina, 1);
        var total = repositorio.ObtenerCantidad(estado, inmuebleId, inquilinoId, desde, hasta);
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)tamPagina);
        ViewBag.Estado = estado;
        ViewBag.InmuebleId = inmuebleId;
        ViewBag.InquilinoId = inquilinoId;
        ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
        ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");

        if (TempData.ContainsKey("Mensaje"))
        {
            ViewBag.Mensaje = TempData["Mensaje"];
        }

        var lista = repositorio.ObtenerLista(pagina, tamPagina, estado, inmuebleId, inquilinoId, desde, hasta);
        return View(lista);
    }

    public IActionResult Details(int id)
    {
        var reserva = repositorio.ObtenerPorId(id);
        if (reserva == null)
        {
            return NotFound();
        }

        if (TempData.ContainsKey("Mensaje"))
        {
            ViewBag.Mensaje = TempData["Mensaje"];
        }

        return View(reserva);
    }

    public IActionResult Create(int? inmuebleId = null, DateTime? fechaInicio = null, DateTime? fechaFin = null)
    {
        var reserva = new Reserva
        {
            FechaInicio = fechaInicio ?? DateTime.Today,
            FechaFin = fechaFin ?? DateTime.Today.AddDays(7)
        };

        if (inmuebleId.HasValue)
        {
            var inmueble = repositorioInmueble.ObtenerPorId(inmuebleId.Value);
            if (inmueble != null)
            {
                reserva.InmuebleId = inmueble.Id;
                reserva.MontoPorDia = inmueble.PrecioPorDia;
                reserva.PorcentajeSena = inmueble.PorcentajeReserva;
                reserva.MonedaPrecio = inmueble.MonedaPrecio;
            }
        }

        CargarCombos(reserva);
        return View(reserva);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Reserva reserva)
    {
        if (reserva.FechaFin < reserva.FechaInicio)
        {
            ModelState.AddModelError("FechaFin", "La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        var inmueble = repositorioInmueble.ObtenerPorId(reserva.InmuebleId);
        if (inmueble == null)
        {
            ModelState.AddModelError("InmuebleId", "El inmueble seleccionado no existe.");
        }
        else if (!inmueble.Disponible)
        {
            ModelState.AddModelError("InmuebleId", "El inmueble seleccionado se encuentra con oferta suspendida.");
        }
        else
        {
            if (reserva.MontoPorDia <= 0)
            {
                reserva.MontoPorDia = inmueble.PrecioPorDia;
            }
            if (!reserva.PorcentajeSena.HasValue)
            {
                reserva.PorcentajeSena = inmueble.PorcentajeReserva;
            }
        }

        if (repositorio.InmuebleEstaOcupado(reserva.InmuebleId, reserva.FechaInicio, reserva.FechaFin))
        {
            ModelState.AddModelError(string.Empty, "El inmueble ya se encuentra reservado u ocupado en el rango de fechas seleccionado.");
        }

        if (!ModelState.IsValid)
        {
            CargarCombos(reserva);
            return View(reserva);
        }

        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        reserva.UsuarioCreadoId = int.TryParse(idClaim, out var uId) ? uId : 1;
        reserva.Estado = "Vigente";

        var id = repositorio.Alta(reserva);
        TempData["Mensaje"] = "Reserva creada exitosamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public IActionResult Edit(int id)
    {
        var reserva = repositorio.ObtenerPorId(id);
        if (reserva == null)
        {
            return NotFound();
        }

        if (reserva.Estado == "Finalizada" || reserva.Estado == "Cancelada")
        {
            TempData["Mensaje"] = $"No se puede editar una reserva con estado {reserva.Estado}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        CargarCombos(reserva);
        return View(reserva);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Reserva reserva)
    {
        if (id != reserva.Id)
        {
            return BadRequest();
        }

        var original = repositorio.ObtenerPorId(id);
        if (original == null)
        {
            return NotFound();
        }

        if (reserva.FechaFin < reserva.FechaInicio)
        {
            ModelState.AddModelError("FechaFin", "La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        if (repositorio.InmuebleEstaOcupado(reserva.InmuebleId, reserva.FechaInicio, reserva.FechaFin, id))
        {
            ModelState.AddModelError(string.Empty, "El inmueble se encuentra ocupado en ese rango de fechas por otra reserva.");
        }

        if (!ModelState.IsValid)
        {
            CargarCombos(reserva);
            return View(reserva);
        }

        original.InquilinoId = reserva.InquilinoId;
        original.InmuebleId = reserva.InmuebleId;
        original.FechaInicio = reserva.FechaInicio;
        original.FechaFin = reserva.FechaFin;
        original.MontoPorDia = reserva.MontoPorDia;
        original.PorcentajeSena = reserva.PorcentajeSena;
        original.Estado = reserva.Estado;

        repositorio.Modificacion(original);
        TempData["Mensaje"] = "Reserva actualizada correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Delete(int id)
    {
        var reserva = repositorio.ObtenerPorId(id);
        return reserva == null ? NotFound() : View(reserva);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        try
        {
            repositorio.Baja(id);
            TempData["Mensaje"] = "Reserva eliminada correctamente.";
        }
        catch (MySqlException ex) when (ex.Number == 1451)
        {
            logger.LogWarning(ex, "No se puede eliminar reserva con pagos asociados: {Id}", id);
            TempData["Mensaje"] = "No se puede eliminar la reserva porque tiene pagos asociados. Debe anular los pagos primero.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // BÚSQUEDA INTERACTIVA DE INMUEBLES LIBRES
    // ==========================================

    [HttpGet]
    public IActionResult BuscarDisponibles(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? tipoId = null,
        int? cupoMinimo = null,
        decimal? precioMaximo = null,
        string? moneda = null)
    {
        var inicio = fechaInicio ?? DateTime.Today;
        var fin = fechaFin ?? DateTime.Today.AddDays(7);

        ViewBag.FechaInicio = inicio.ToString("yyyy-MM-dd");
        ViewBag.FechaFin = fin.ToString("yyyy-MM-dd");
        ViewBag.TipoId = tipoId;
        ViewBag.CupoMinimo = cupoMinimo;
        ViewBag.PrecioMaximo = precioMaximo;
        ViewBag.Moneda = moneda;
        ViewBag.Tipos = new SelectList(repositorioInmueble.ObtenerTipos(), "Id", "Texto", tipoId);

        var disponibles = repositorio.ObtenerInmueblesDisponibles(inicio, fin, tipoId, cupoMinimo, precioMaximo, moneda);
        return View(disponibles);
    }

    public IActionResult BuscarInmuebles(string? q)
    {
        return Json(repositorioInmueble.ObtenerInmueblesSelect(q));
    }

    public IActionResult BuscarInquilinos(string? q)
    {
        return Json(repositorio.ObtenerInquilinosSelect(q));
    }

    [HttpGet]
    public IActionResult ObtenerInfoInmueble(int id)
    {
        var inmueble = repositorioInmueble.ObtenerPorId(id);
        if (inmueble == null) return NotFound();
        return Json(new
        {
            precio = inmueble.PrecioPorDia,
            moneda = inmueble.MonedaPrecio,
            porcentaje = inmueble.PorcentajeReserva
        });
    }

    private void CargarCombos(Reserva? reserva = null)
    {
        ViewBag.Inmuebles = new SelectList(repositorioInmueble.ObtenerInmueblesSelect(), "Id", "Texto", reserva?.InmuebleId);
        ViewBag.Inquilinos = new SelectList(repositorio.ObtenerInquilinosSelect(), "Id", "Texto", reserva?.InquilinoId);
    }
}
