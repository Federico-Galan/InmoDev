using System.ComponentModel.DataAnnotations;

namespace InmoDev.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "El correo electronico es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electronico valido")]
    [Display(Name = "Correo electronico")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Clave { get; set; } = "";

    [Display(Name = "Recordarme en este equipo")]
    public bool Recordarme { get; set; }

    public string? ReturnUrl { get; set; }
}
