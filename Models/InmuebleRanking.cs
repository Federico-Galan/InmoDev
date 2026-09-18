namespace InmoDev.Models;

public class InmuebleRanking
{
    public int Id { get; set; }
    public string Direccion { get; set; } = "";
    public string TipoNombre { get; set; } = "";
    public string PropietarioNombre { get; set; } = "";
    public decimal PrecioPorDia { get; set; }
    public string MonedaPrecio { get; set; } = "ARS";
    public int CantidadReservas { get; set; }
    public int TotalDiasReservados { get; set; }
    public decimal TotalRecaudado { get; set; }
}
