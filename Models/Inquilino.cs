using System.ComponentModel.DataAnnotations;

namespace InmoDev.Models;

public class Inquilino
{
    [Key]
    [Display(Name = "Codigo")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El DNI es obligatorio")]
    [Display(Name = "DNI")]
    [StringLength(20)]
    public string DNI { get; set; } = "";

    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [Display(Name = "Nombre completo")]
    [StringLength(150)]
    public string NombreCompleto { get; set; } = "";

    [Display(Name = "Telefono")]
    [StringLength(50)]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Ingrese un email valido")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Display(Name = "Direccion")]
    [StringLength(255)]
    public string? Direccion { get; set; }

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; }
}
