using MySqlConnector;

namespace InmoDev.Models;

public class RepositorioInmueble : RepositorioBase, IRepositorio<Inmueble>
{
    public RepositorioInmueble(IConfiguration configuration) : base(configuration)
    {
    }

    public int Alta(Inmueble inmueble)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            INSERT INTO Inmueble (PropietarioId, TipoId, Direccion, CupoMaximo, Coordenadas, PrecioPorDia, MonedaPrecio, ImagenPortada, Disponible)
            VALUES (@propietarioId, @tipoId, @direccion, @cupoMaximo, @coordenadas, @precioPorDia, @monedaPrecio, @imagenPortada, @disponible);
            SELECT LAST_INSERT_ID();
            """;
        using var command = new MySqlCommand(sql, connection);
        CargarParametros(command, inmueble);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        inmueble.Id = id;
        return id;
    }

    public int Baja(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "DELETE FROM Inmueble WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public int Modificacion(Inmueble inmueble)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            UPDATE Inmueble
            SET PropietarioId = @propietarioId,
                TipoId = @tipoId,
                Direccion = @direccion,
                CupoMaximo = @cupoMaximo,
                Coordenadas = @coordenadas,
                PrecioPorDia = @precioPorDia,
                MonedaPrecio = @monedaPrecio,
                ImagenPortada = @imagenPortada,
                Disponible = @disponible
            WHERE Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", inmueble.Id);
        CargarParametros(command, inmueble);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public IList<Inmueble> ObtenerLista(int pagina = 1, int tamPagina = 10)
    {
        var inmuebles = new List<Inmueble>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT i.Id, i.PropietarioId, i.TipoId, i.Direccion, i.CupoMaximo, i.Coordenadas,
                   i.PrecioPorDia, i.MonedaPrecio, i.ImagenPortada, i.Disponible, i.FechaRegistro,
                   p.Nombre AS PropietarioNombre, t.Nombre AS TipoNombre
            FROM Inmueble i
            INNER JOIN Propietarios p ON p.Id = i.PropietarioId
            INNER JOIN TiposInmueble t ON t.Id = i.TipoId
            ORDER BY i.Direccion
            LIMIT @tamPagina OFFSET @desde
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tamPagina", tamPagina);
        command.Parameters.AddWithValue("@desde", (pagina - 1) * tamPagina);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            inmuebles.Add(Mapear(reader));
        }
        return inmuebles;
    }

    public int ObtenerCantidad()
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "SELECT COUNT(*) FROM Inmueble";
        using var command = new MySqlCommand(sql, connection);
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public Inmueble? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT i.Id, i.PropietarioId, i.TipoId, i.Direccion, i.CupoMaximo, i.Coordenadas,
                   i.PrecioPorDia, i.MonedaPrecio, i.ImagenPortada, i.Disponible, i.FechaRegistro,
                   p.Nombre AS PropietarioNombre, t.Nombre AS TipoNombre
            FROM Inmueble i
            INNER JOIN Propietarios p ON p.Id = i.PropietarioId
            INNER JOIN TiposInmueble t ON t.Id = i.TipoId
            WHERE i.Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public int ActualizarPortada(int id, string? imagenPortada)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "UPDATE Inmueble SET ImagenPortada = @imagenPortada WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@imagenPortada", (object?)imagenPortada ?? DBNull.Value);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public IList<OpcionSelect> ObtenerPropietarios(string? busqueda = null, int limite = 25)
    {
        return ObtenerOpciones("""
            SELECT Id, Nombre AS Texto
            FROM Propietarios
            WHERE Activo = TRUE AND (@busqueda IS NULL OR Nombre LIKE CONCAT('%', @busqueda, '%'))
            ORDER BY Nombre
            LIMIT @limite
            """, busqueda, limite);
    }

    public IList<OpcionSelect> ObtenerTipos(string? busqueda = null, int limite = 25)
    {
        return ObtenerOpciones("""
            SELECT Id, Nombre AS Texto
            FROM TiposInmueble
            WHERE Activo = TRUE AND (@busqueda IS NULL OR Nombre LIKE CONCAT('%', @busqueda, '%'))
            ORDER BY Nombre
            LIMIT @limite
            """, busqueda, limite);
    }

    private IList<OpcionSelect> ObtenerOpciones(string sql, string? busqueda, int limite)
    {
        var opciones = new List<OpcionSelect>();
        using var connection = new MySqlConnection(connectionString);
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@busqueda", string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : busqueda.Trim());
        command.Parameters.AddWithValue("@limite", limite);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            opciones.Add(new OpcionSelect
            {
                Id = reader.GetInt32("Id"),
                Texto = reader.GetString("Texto")
            });
        }
        return opciones;
    }

    private static void CargarParametros(MySqlCommand command, Inmueble inmueble)
    {
        command.Parameters.AddWithValue("@propietarioId", inmueble.PropietarioId);
        command.Parameters.AddWithValue("@tipoId", inmueble.TipoId);
        command.Parameters.AddWithValue("@direccion", inmueble.Direccion);
        command.Parameters.AddWithValue("@cupoMaximo", (object?)inmueble.CupoMaximo ?? DBNull.Value);
        command.Parameters.AddWithValue("@coordenadas", (object?)inmueble.Coordenadas ?? DBNull.Value);
        command.Parameters.AddWithValue("@precioPorDia", inmueble.PrecioPorDia);
        command.Parameters.AddWithValue("@monedaPrecio", inmueble.MonedaPrecio);
        command.Parameters.AddWithValue("@imagenPortada", (object?)inmueble.ImagenPortada ?? DBNull.Value);
        command.Parameters.AddWithValue("@disponible", inmueble.Disponible);
    }

    private static Inmueble Mapear(MySqlDataReader reader)
    {
        return new Inmueble
        {
            Id = reader.GetInt32("Id"),
            PropietarioId = reader.GetInt32("PropietarioId"),
            TipoId = reader.GetInt32("TipoId"),
            Direccion = reader.GetString("Direccion"),
            CupoMaximo = reader.IsDBNull(reader.GetOrdinal("CupoMaximo")) ? null : reader.GetInt32("CupoMaximo"),
            Coordenadas = reader.IsDBNull(reader.GetOrdinal("Coordenadas")) ? null : reader.GetString("Coordenadas"),
            PrecioPorDia = reader.GetDecimal("PrecioPorDia"),
            MonedaPrecio = reader.GetString("MonedaPrecio"),
            ImagenPortada = reader.IsDBNull(reader.GetOrdinal("ImagenPortada")) ? null : reader.GetString("ImagenPortada"),
            Disponible = reader.GetBoolean("Disponible"),
            FechaRegistro = reader.GetDateTime("FechaRegistro"),
            PropietarioNombre = reader.GetString("PropietarioNombre"),
            TipoNombre = reader.GetString("TipoNombre")
        };
    }
}
