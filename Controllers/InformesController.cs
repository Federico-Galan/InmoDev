using InmoDev.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InmoDev.Controllers;

[Authorize]
public class InformesController : Controller
{
    private readonly RepositorioInforme repositorio;
    private readonly ILogger<InformesController> logger;

    public InformesController(RepositorioInforme repositorio, ILogger<InformesController> logger)
    {
        this.repositorio = repositorio;
        this.logger = logger;
    }

    // =========================================================================
    // PANEL PRINCIPAL DE INFORMES
    // =========================================================================
    public IActionResult Index()
    {
        return View();
    }

    // =========================================================================
    // REPORTE 1: Inmuebles con dueños (filtro por disponibilidad)
    // =========================================================================
    public IActionResult Reporte1(bool? disponible = null)
    {
        ViewBag.Disponible = disponible;
        var lista = repositorio.InmueblesConDuenios(disponible);
        return View(lista);
    }

    // =========================================================================
    // REPORTE 2: Inmuebles por propietario
    // =========================================================================
    public IActionResult Reporte2(int? propietarioId = null)
    {
        ViewBag.PropietarioId = propietarioId;
        ViewBag.Propietarios = new SelectList(repositorio.ObtenerPropietariosSelect(), "Id", "Texto", propietarioId);

        var lista = propietarioId.HasValue
            ? repositorio.InmueblesPorPropietario(propietarioId.Value)
            : new List<Inmueble>();

        return View(lista);
    }

    // =========================================================================
    // REPORTE 3: Ranking de inmuebles más reservados en los últimos 365 días
    // =========================================================================
    public IActionResult Reporte3()
    {
        var lista = repositorio.RankingInmueblesMasReservados();
        return View(lista);
    }

    // =========================================================================
    // REPORTE 4: Inmuebles sin reservas en los últimos X días
    // =========================================================================
    public IActionResult Reporte4(int dias = 30)
    {
        if (dias <= 0) dias = 30;
        ViewBag.Dias = dias;
        var lista = repositorio.InmueblesSinReservas(dias);
        return View(lista);
    }

    // =========================================================================
    // REPORTE 5: Listado de reservas vigentes (filtro por rango de fechas)
    // =========================================================================
    public IActionResult Reporte5(DateTime? desde = null, DateTime? hasta = null)
    {
        ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
        ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
        var lista = repositorio.ReservasVigentes(desde, hasta);
        return View(lista);
    }

    // =========================================================================
    // REPORTE 6: Reservas que terminan dentro de los próximos X días
    // =========================================================================
    public IActionResult Reporte6(int dias = 30)
    {
        if (dias <= 0) dias = 30;
        ViewBag.Dias = dias;
        var lista = repositorio.ReservasPorVencer(dias);
        return View(lista);
    }

    // =========================================================================
    // REPORTE 7: Detalle de pagos de una reserva con carga rápida
    // =========================================================================
    public IActionResult Reporte7(int? reservaId = null)
    {
        ViewBag.ReservaId = reservaId;
        ViewBag.Reservas = new SelectList(repositorio.ObtenerReservasSelect(), "Id", "Texto", reservaId);

        if (reservaId.HasValue)
        {
            ViewBag.Reserva = repositorio.ObtenerReservaDetallada(reservaId.Value);
            var pagos = repositorio.PagosPorReserva(reservaId.Value);
            return View(pagos);
        }

        return View(new List<Pago>());
    }

    // =========================================================================
    // REPORTE 8: Consulta de disponibilidad entre dos fechas
    // =========================================================================
    public IActionResult Reporte8(DateTime? fechaInicio = null, DateTime? fechaFin = null)
    {
        var inicio = fechaInicio ?? DateTime.Today;
        var fin = fechaFin ?? DateTime.Today.AddDays(7);

        if (fin < inicio)
        {
            ModelState.AddModelError("", "La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        ViewBag.FechaInicio = inicio.ToString("yyyy-MM-dd");
        ViewBag.FechaFin = fin.ToString("yyyy-MM-dd");

        var lista = (fin >= inicio)
            ? repositorio.InmueblesDisponiblesEnRango(inicio, fin)
            : new List<Inmueble>();

        return View(lista);
    }
}
