using System.Security.Claims;
using InmoDev.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InmoDev.Controllers;

[Authorize]
public class PagosController : Controller
{
    private readonly RepositorioPago repositorio;
    private readonly RepositorioReserva repositorioReserva;
    private readonly ILogger<PagosController> logger;

    public PagosController(
        RepositorioPago repositorio,
        RepositorioReserva repositorioReserva,
        ILogger<PagosController> logger)
    {
        this.repositorio = repositorio;
        this.repositorioReserva = repositorioReserva;
        this.logger = logger;
    }

    public IActionResult Index(int pagina = 1, int? reservaId = null, string? estado = null)
    {
        const int tamPagina = 10;
        pagina = Math.Max(pagina, 1);
        var total = repositorio.ObtenerCantidad(reservaId, estado);
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)tamPagina);
        ViewBag.ReservaId = reservaId;
        ViewBag.Estado = estado;

        if (TempData.ContainsKey("Mensaje"))
        {
            ViewBag.Mensaje = TempData["Mensaje"];
        }

        var lista = repositorio.ObtenerLista(pagina, tamPagina, reservaId, estado);
        return View(lista);
    }

    public IActionResult PorReserva(int reservaId)
    {
        var reserva = repositorioReserva.ObtenerPorId(reservaId);
        if (reserva == null)
        {
            return NotFound();
        }

        ViewBag.Reserva = reserva;
        if (TempData.ContainsKey("Mensaje"))
        {
            ViewBag.Mensaje = TempData["Mensaje"];
        }

        var pagos = repositorio.ObtenerPorReserva(reservaId);
        return View(pagos);
    }

    public IActionResult Details(int id)
    {
        var pago = repositorio.ObtenerPorId(id);
        if (pago == null)
        {
            return NotFound();
        }

        return View(pago);
    }

    public IActionResult Create(int? reservaId = null)
    {
        var pago = new Pago
        {
            FechaPago = DateTime.Now,
            Concepto = "Pago de alquiler"
        };

        if (reservaId.HasValue)
        {
            var reserva = repositorioReserva.ObtenerPorId(reservaId.Value);
            if (reserva != null)
            {
                pago.ReservaId = reserva.Id;
                pago.Importe = reserva.SaldoPendiente > 0 ? reserva.SaldoPendiente : reserva.MontoSenaRequerido;
            }
        }

        CargarCombos(pago);
        return View(pago);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Pago pago)
    {
        var reserva = repositorioReserva.ObtenerPorId(pago.ReservaId);
        if (reserva == null)
        {
            ModelState.AddModelError("ReservaId", "Debe seleccionar una reserva válida.");
        }

        if (pago.Importe <= 0)
        {
            ModelState.AddModelError("Importe", "El importe debe ser mayor a cero.");
        }

        if (!ModelState.IsValid)
        {
            CargarCombos(pago);
            return View(pago);
        }

        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        pago.UsuarioCreaId = int.TryParse(idClaim, out var uId) ? uId : 1;
        pago.Estado = "Activo";

        repositorio.Alta(pago);
        TempData["Mensaje"] = "Pago registrado correctamente.";

        return RedirectToAction(nameof(PorReserva), new { reservaId = pago.ReservaId });
    }

    public IActionResult Edit(int id)
    {
        var pago = repositorio.ObtenerPorId(id);
        if (pago == null)
        {
            return NotFound();
        }

        if (pago.Estado == "Anulado")
        {
            TempData["Mensaje"] = "No se puede editar un pago que ha sido anulado.";
            return RedirectToAction(nameof(PorReserva), new { reservaId = pago.ReservaId });
        }

        return View(pago);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, string concepto)
    {
        var pago = repositorio.ObtenerPorId(id);
        if (pago == null)
        {
            return NotFound();
        }

        if (pago.Estado == "Anulado")
        {
            TempData["Mensaje"] = "No se puede editar un pago anulado.";
            return RedirectToAction(nameof(PorReserva), new { reservaId = pago.ReservaId });
        }

        if (string.IsNullOrWhiteSpace(concepto))
        {
            ModelState.AddModelError("Concepto", "El concepto no puede estar vacío.");
            return View(pago);
        }

        // Regla: Solo modificamos concepto
        repositorio.ModificarConcepto(id, concepto);
        TempData["Mensaje"] = "Concepto de pago actualizado correctamente.";

        return RedirectToAction(nameof(PorReserva), new { reservaId = pago.ReservaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Anular(int id, string? returnUrl = null)
    {
        var pago = repositorio.ObtenerPorId(id);
        if (pago == null)
        {
            return NotFound();
        }

        if (pago.Estado == "Anulado")
        {
            TempData["Mensaje"] = "El pago ya se encuentra anulado.";
        }
        else
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var usuarioId = int.TryParse(idClaim, out var uId) ? uId : 1;

            repositorio.Anular(id, usuarioId);
            TempData["Mensaje"] = $"El pago #{id} fue anulado correctamente (Baja lógica).";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(PorReserva), new { reservaId = pago.ReservaId });
    }

    [HttpGet]
    public IActionResult BuscarReservas(string? q)
    {
        return Json(repositorio.ObtenerReservasSelect(q));
    }

    private void CargarCombos(Pago? pago = null)
    {
        ViewBag.Reservas = new SelectList(repositorio.ObtenerReservasSelect(), "Id", "Texto", pago?.ReservaId);
    }
}
