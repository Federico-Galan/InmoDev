using System.ComponentModel.DataAnnotations;

namespace InmoDev.Models;

public class Inmueble
{
    [Key]
    [Display(Name = "Codigo")]
    public int Id { get; set; }

    [Display(Name = "Propietario")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un propietario")]
    public int PropietarioId { get; set; }

    [Display(Name = "Tipo")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un tipo de inmueble")]
    public int TipoId { get; set; }

    [Display(Name = "Direccion")]
    [Required(ErrorMessage = "La direccion es obligatoria")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "La direccion debe tener entre 5 y 255 caracteres")]
    public string Direccion { get; set; } = "";

    [Display(Name = "Cupo maximo")]
    [Range(1, int.MaxValue, ErrorMessage = "El cupo debe ser mayor a cero")]
    public int? CupoMaximo { get; set; }

    [StringLength(255, ErrorMessage = "Las coordenadas no pueden superar los 255 caracteres")]
    [RegularExpression(@"^-?\d{1,2}(\.\d+)?\s*,\s*-?\d{1,3}(\.\d+)?$", ErrorMessage = "Ingrese coordenadas en formato latitud,longitud")]
    public string? Coordenadas { get; set; }

    [Display(Name = "Precio por dia")]
    [Range(0, 9999999999.99, ErrorMessage = "El precio por dia no puede ser negativo")]
    public decimal PrecioPorDia { get; set; }

    [Display(Name = "Moneda")]
    [Required(ErrorMessage = "La moneda es obligatoria")]
    [RegularExpression("^(ARS|USD)$", ErrorMessage = "Seleccione una moneda valida")]
    public string MonedaPrecio { get; set; } = "ARS";

    [Display(Name = "Imagen de portada")]
    [StringLength(255, ErrorMessage = "La imagen de portada no puede superar los 255 caracteres")]
    public string? ImagenPortada { get; set; }

    public bool Disponible { get; set; } = true;

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; }

    [Display(Name = "Propietario")]
    public string? PropietarioNombre { get; set; }

    [Display(Name = "Tipo")]
    public string? TipoNombre { get; set; }
}
