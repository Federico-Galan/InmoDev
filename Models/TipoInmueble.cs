using System.ComponentModel.DataAnnotations;

namespace InmoDev.Models;

public class TipoInmueble
{
    [Key]
    [Display(Name = "Codigo")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; set; } = "";

    [Display(Name = "Descripcion")]
    [StringLength(255, ErrorMessage = "La descripcion no puede superar los 255 caracteres")]
    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}
