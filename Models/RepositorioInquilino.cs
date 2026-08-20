using MySqlConnector;

namespace InmoDev.Models;

public class RepositorioInquilino : RepositorioBase, IRepositorio<Inquilino>
{
    public RepositorioInquilino(IConfiguration configuration) : base(configuration)
    {
    }

    public int Alta(Inquilino inquilino)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            INSERT INTO Inquilinos (DNI, NombreCompleto, Telefono, Email, Direccion)
            VALUES (@dni, @nombreCompleto, @telefono, @email, @direccion);
            SELECT LAST_INSERT_ID();
            """;
        using var command = new MySqlCommand(sql, connection);
        CargarParametros(command, inquilino);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        inquilino.Id = id;
        return id;
    }

    public int Baja(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "DELETE FROM Inquilinos WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public int Modificacion(Inquilino inquilino)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            UPDATE Inquilinos
            SET DNI = @dni,
                NombreCompleto = @nombreCompleto,
                Telefono = @telefono,
                Email = @email,
                Direccion = @direccion
            WHERE Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", inquilino.Id);
        CargarParametros(command, inquilino);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public IList<Inquilino> ObtenerLista(int pagina = 1, int tamPagina = 10)
    {
        var inquilinos = new List<Inquilino>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT Id, DNI, NombreCompleto, Telefono, Email, Direccion, FechaRegistro
            FROM Inquilinos
            ORDER BY NombreCompleto
            LIMIT @tamPagina OFFSET @desde
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tamPagina", tamPagina);
        command.Parameters.AddWithValue("@desde", (pagina - 1) * tamPagina);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            inquilinos.Add(Mapear(reader));
        }
        return inquilinos;
    }

    public int ObtenerCantidad()
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "SELECT COUNT(*) FROM Inquilinos";
        using var command = new MySqlCommand(sql, connection);
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public Inquilino? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT Id, DNI, NombreCompleto, Telefono, Email, Direccion, FechaRegistro
            FROM Inquilinos
            WHERE Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    private static void CargarParametros(MySqlCommand command, Inquilino inquilino)
    {
        command.Parameters.AddWithValue("@dni", inquilino.DNI);
        command.Parameters.AddWithValue("@nombreCompleto", inquilino.NombreCompleto);
        command.Parameters.AddWithValue("@telefono", (object?)inquilino.Telefono ?? DBNull.Value);
        command.Parameters.AddWithValue("@email", (object?)inquilino.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@direccion", (object?)inquilino.Direccion ?? DBNull.Value);
    }

    private static Inquilino Mapear(MySqlDataReader reader)
    {
        return new Inquilino
        {
            Id = reader.GetInt32("Id"),
            DNI = reader.GetString("DNI"),
            NombreCompleto = reader.GetString("NombreCompleto"),
            Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? null : reader.GetString("Telefono"),
            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString("Email"),
            Direccion = reader.IsDBNull(reader.GetOrdinal("Direccion")) ? null : reader.GetString("Direccion"),
            FechaRegistro = reader.GetDateTime("FechaRegistro")
        };
    }
}
