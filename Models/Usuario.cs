using System.ComponentModel.DataAnnotations;

namespace InmoDev.Models;

public class Usuario
{
    [Key]
    [Display(Name = "Codigo")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingrese un email valido")]
    [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres")]
    public string Email { get; set; } = "";

    [Display(Name = "Contraseña")]
    public string PasswordHash { get; set; } = "";

    [Required(ErrorMessage = "El rol es obligatorio")]
    [RegularExpression("^(Administrador|Empleado)$", ErrorMessage = "El rol debe ser Administrador o Empleado")]
    public string Rol { get; set; } = "Empleado";

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
    public string Nombre { get; set; } = "";

    [Display(Name = "Foto / Avatar")]
    [StringLength(255)]
    public string? Avatar { get; set; }

    public bool Activo { get; set; } = true;

    [Display(Name = "Fecha de creacion")]
    public DateTime FechaCreacion { get; set; }
}
