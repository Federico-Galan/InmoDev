using System.ComponentModel.DataAnnotations;

namespace InmoDev.Models;

public class Propietario
{
    [Key]
    [Display(Name = "Codigo")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    [RegularExpression(@"^[A-Za-z\u00C1\u00C9\u00CD\u00D3\u00DA\u00E1\u00E9\u00ED\u00F3\u00FA\u00D1\u00F1\s]{2,100}$", ErrorMessage = "No se pueden cargar numeros ni simbolos en el nombre")]
    public string Nombre { get; set; } = "";

    [Display(Name = "Telefono")]
    [Required(ErrorMessage = "El telefono es obligatorio")]
    [StringLength(50, ErrorMessage = "El telefono no puede superar los 50 caracteres")]
    [RegularExpression(@"^[0-9+\-\s()]{6,50}$", ErrorMessage = "Ingrese un telefono valido")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingrese un email valido")]
    [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres")]
    [RegularExpression(@"^(?=[^@]*[A-Za-z])[A-Za-z0-9._%+\-]+@(gmail|hotmail)\.com$", ErrorMessage = "El email debe ser @gmail.com o @hotmail.com y no puede ser solo numerico")]
    public string Email { get; set; } = "";

    [Display(Name = "Direccion")]
    [Required(ErrorMessage = "La direccion es obligatoria")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "La direccion debe tener entre 5 y 255 caracteres")]
    public string Direccion { get; set; } = "";

    public bool Activo { get; set; } = true;

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; }
}
