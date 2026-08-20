using System.ComponentModel.DataAnnotations;

namespace InmoDev.Models;

public class Propietario
{
    [Key]
    [Display(Name = "Codigo")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    [Display(Name = "Telefono")]
    [StringLength(50)]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Ingrese un email valido")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Display(Name = "Direccion")]
    [StringLength(255)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; } = true;

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; }
}
