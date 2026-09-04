using System.ComponentModel.DataAnnotations;

namespace InmoDev.Models;

public class ImagenInmueble
{
    [Key]
    [Display(Name = "Codigo")]
    public int Id { get; set; }

    public int InmuebleId { get; set; }

    [Required]
    [StringLength(500)]
    public string Url { get; set; } = "";

    [Display(Name = "Es portada")]
    public bool EsPortada { get; set; }

    public int? Orden { get; set; }

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; }
}
