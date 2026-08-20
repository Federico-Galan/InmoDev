namespace InmoDev.Models;

public interface IRepositorio<T>
{
    int Alta(T entidad);
    int Baja(int id);
    int Modificacion(T entidad);
    IList<T> ObtenerLista(int pagina = 1, int tamPagina = 10);
    int ObtenerCantidad();
    T? ObtenerPorId(int id);
}
