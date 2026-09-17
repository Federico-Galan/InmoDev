using System.ComponentModel.DataAnnotations;

namespace InmoDev.Models;

public class Pago
{
    [Key]
    [Display(Name = "Numero de Pago")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Debe seleccionar una reserva")]
    [Display(Name = "Reserva")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una reserva valida")]
    public int ReservaId { get; set; }

    [Required(ErrorMessage = "El concepto es obligatorio")]
    [StringLength(255, ErrorMessage = "El concepto no puede superar los 255 caracteres")]
    [Display(Name = "Concepto de pago")]
    public string Concepto { get; set; } = "Pago de estadia";

    [Required(ErrorMessage = "La fecha de pago es obligatoria")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Fecha de pago")]
    public DateTime FechaPago { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "El importe es obligatorio")]
    [Range(0.01, 9999999999.99, ErrorMessage = "El importe debe ser mayor a cero")]
    [Display(Name = "Importe")]
    public decimal Importe { get; set; }

    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Activo";

    [Display(Name = "Fecha de registro")]
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    [Display(Name = "Usuario creador")]
    public int UsuarioCreaId { get; set; }

    [Display(Name = "Usuario anulador")]
    public int? UsuarioAnulaId { get; set; }

    [Display(Name = "Fecha de anulacion")]
    public DateTime? FechaAnulacion { get; set; }

    // Propiedades de navegación / presentación
    [Display(Name = "Inmueble")]
    public string? InmuebleDireccion { get; set; }

    [Display(Name = "Inquilino")]
    public string? InquilinoNombre { get; set; }

    [Display(Name = "Moneda")]
    public string MonedaPrecio { get; set; } = "ARS";

    [Display(Name = "Registrado por")]
    public string? UsuarioCreaNombre { get; set; }

    [Display(Name = "Anulado por")]
    public string? UsuarioAnulaNombre { get; set; }
}
