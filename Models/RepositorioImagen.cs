using MySqlConnector;

namespace InmoDev.Models;

public class RepositorioImagen : RepositorioBase
{
    public RepositorioImagen(IConfiguration configuration) : base(configuration)
    {
    }

    public int Alta(ImagenInmueble imagen)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            INSERT INTO Imagenes (InmuebleId, Url, EsPortada, Orden)
            VALUES (@inmuebleId, @url, @esPortada, @orden);
            SELECT LAST_INSERT_ID();
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@inmuebleId", imagen.InmuebleId);
        command.Parameters.AddWithValue("@url", imagen.Url);
        command.Parameters.AddWithValue("@esPortada", imagen.EsPortada);
        command.Parameters.AddWithValue("@orden", (object?)imagen.Orden ?? DBNull.Value);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        imagen.Id = id;
        return id;
    }

    public int Baja(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "DELETE FROM Imagenes WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public ImagenInmueble? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "SELECT Id, InmuebleId, Url, EsPortada, Orden, FechaRegistro FROM Imagenes WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public IList<ImagenInmueble> ObtenerPorInmueble(int inmuebleId)
    {
        var imagenes = new List<ImagenInmueble>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT Id, InmuebleId, Url, EsPortada, Orden, FechaRegistro
            FROM Imagenes
            WHERE InmuebleId = @inmuebleId
            ORDER BY COALESCE(Orden, 9999), Id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@inmuebleId", inmuebleId);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            imagenes.Add(Mapear(reader));
        }
        return imagenes;
    }

    private static ImagenInmueble Mapear(MySqlDataReader reader)
    {
        return new ImagenInmueble
        {
            Id = reader.GetInt32("Id"),
            InmuebleId = reader.GetInt32("InmuebleId"),
            Url = reader.GetString("Url"),
            EsPortada = reader.GetBoolean("EsPortada"),
            Orden = reader.IsDBNull(reader.GetOrdinal("Orden")) ? null : reader.GetInt32("Orden"),
            FechaRegistro = reader.GetDateTime("FechaRegistro")
        };
    }
}
