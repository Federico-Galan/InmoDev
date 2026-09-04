using MySqlConnector;

namespace InmoDev.Models;

public class RepositorioTipoInmueble : RepositorioBase, IRepositorio<TipoInmueble>
{
    public RepositorioTipoInmueble(IConfiguration configuration) : base(configuration)
    {
    }

    public int Alta(TipoInmueble tipo)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            INSERT INTO TiposInmueble (Nombre, Descripcion, Activo)
            VALUES (@nombre, @descripcion, @activo);
            SELECT LAST_INSERT_ID();
            """;
        using var command = new MySqlCommand(sql, connection);
        CargarParametros(command, tipo);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        tipo.Id = id;
        return id;
    }

    public int Baja(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "DELETE FROM TiposInmueble WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public int Modificacion(TipoInmueble tipo)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            UPDATE TiposInmueble
            SET Nombre = @nombre,
                Descripcion = @descripcion,
                Activo = @activo
            WHERE Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", tipo.Id);
        CargarParametros(command, tipo);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public IList<TipoInmueble> ObtenerLista(int pagina = 1, int tamPagina = 10)
    {
        var tipos = new List<TipoInmueble>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT Id, Nombre, Descripcion, Activo
            FROM TiposInmueble
            ORDER BY Nombre
            LIMIT @tamPagina OFFSET @desde
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tamPagina", tamPagina);
        command.Parameters.AddWithValue("@desde", (pagina - 1) * tamPagina);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tipos.Add(Mapear(reader));
        }
        return tipos;
    }

    public int ObtenerCantidad()
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "SELECT COUNT(*) FROM TiposInmueble";
        using var command = new MySqlCommand(sql, connection);
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public TipoInmueble? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT Id, Nombre, Descripcion, Activo
            FROM TiposInmueble
            WHERE Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    private static void CargarParametros(MySqlCommand command, TipoInmueble tipo)
    {
        command.Parameters.AddWithValue("@nombre", tipo.Nombre);
        command.Parameters.AddWithValue("@descripcion", (object?)tipo.Descripcion ?? DBNull.Value);
        command.Parameters.AddWithValue("@activo", tipo.Activo);
    }

    private static TipoInmueble Mapear(MySqlDataReader reader)
    {
        return new TipoInmueble
        {
            Id = reader.GetInt32("Id"),
            Nombre = reader.GetString("Nombre"),
            Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion")) ? null : reader.GetString("Descripcion"),
            Activo = reader.GetBoolean("Activo")
        };
    }
}
