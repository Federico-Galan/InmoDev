using System.ComponentModel.DataAnnotations;

namespace InmoDev.Models;

public class Reserva : IValidatableObject
{
    [Key]
    [Display(Name = "Codigo")]
    public int Id { get; set; }

    [Display(Name = "Inquilino")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un inquilino")]
    public int InquilinoId { get; set; }

    [Display(Name = "Inmueble")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un inmueble")]
    public int InmuebleId { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha desde")]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La fecha de fin es obligatoria")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha hasta")]
    public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(7);

    [DataType(DataType.Date)]
    [Display(Name = "Fecha fin real")]
    public DateTime? FechaFinReal { get; set; }

    [Display(Name = "Precio por dia")]
    [Range(0.01, 9999999999.99, ErrorMessage = "El monto diario debe ser mayor a cero")]
    public decimal MontoPorDia { get; set; }

    [Display(Name = "% Seña / Anticipo")]
    [Range(0, 100, ErrorMessage = "El porcentaje debe estar entre 0 y 100")]
    public decimal? PorcentajeSena { get; set; }

    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Vigente";

    [Display(Name = "Fecha de creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    [Display(Name = "Usuario creador")]
    public int UsuarioCreadoId { get; set; }

    [Display(Name = "Usuario finalizador")]
    public int? UsuarioFinalizaId { get; set; }

    [Display(Name = "Fecha finalizacion")]
    public DateTime? FechaFinalizacion { get; set; }

    [Display(Name = "Multa aplicada")]
    public decimal? MultaAplicada { get; set; }

    // Propiedades de navegación y presentación
    [Display(Name = "Inquilino")]
    public string? InquilinoNombre { get; set; }

    [Display(Name = "DNI")]
    public string? InquilinoDni { get; set; }

    [Display(Name = "Inmueble")]
    public string? InmuebleDireccion { get; set; }

    [Display(Name = "Propietario")]
    public string? PropietarioNombre { get; set; }

    [Display(Name = "Moneda")]
    public string MonedaPrecio { get; set; } = "ARS";

    [Display(Name = "Creado por")]
    public string? UsuarioCreadorNombre { get; set; }

    [Display(Name = "Finalizado por")]
    public string? UsuarioFinalizaNombre { get; set; }

    [Display(Name = "Total pagado")]
    public decimal TotalPagado { get; set; }

    // Cálculos auxiliares
    [Display(Name = "Dias totales")]
    public int CantidadDias => Math.Max(1, (FechaFin.Date - FechaInicio.Date).Days + 1);

    [Display(Name = "Monto total")]
    public decimal MontoTotalEstimado => CantidadDias * MontoPorDia;

    [Display(Name = "Seña requerida")]
    public decimal MontoSenaRequerido => MontoTotalEstimado * ((PorcentajeSena ?? 0) / 100m);

    [Display(Name = "Saldo pendiente")]
    public decimal SaldoPendiente => Math.Max(0, (MontoTotalEstimado + (MultaAplicada ?? 0)) - TotalPagado);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaFin.Date < FechaInicio.Date)
        {
            yield return new ValidationResult("La fecha de finalización debe ser posterior o igual a la fecha de inicio.", new[] { nameof(FechaFin) });
        }
    }
}
