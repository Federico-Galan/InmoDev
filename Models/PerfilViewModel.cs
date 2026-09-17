using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace InmoDev.Models;

public class PerfilViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
    [Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El correo electronico es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingrese un correo valido")]
    [Display(Name = "Correo electronico")]
    public string Email { get; set; } = "";

    [Display(Name = "Rol")]
    public string Rol { get; set; } = "";

    [Display(Name = "Avatar actual")]
    public string? AvatarActual { get; set; }

    [Display(Name = "Subir nuevo avatar")]
    public IFormFile? FotoAvatar { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string? ClaveActual { get; set; }

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres")]
    [Display(Name = "Nueva contraseña")]
    public string? NuevaClave { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nueva contraseña")]
    [Compare("NuevaClave", ErrorMessage = "Las contraseñas no coinciden")]
    public string? ConfirmarClave { get; set; }
}

public class UsuarioFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El correo electronico es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electronico valido")]
    [Display(Name = "Correo electronico")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "El rol es obligatorio")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = "Empleado";

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string? Clave { get; set; }

    [Display(Name = "Avatar")]
    public string? Avatar { get; set; }

    public IFormFile? FotoAvatar { get; set; }

    [Display(Name = "Usuario activo")]
    public bool Activo { get; set; } = true;
}
