using MySqlConnector;

namespace InmoDev.Models;

public class RepositorioPropietario : RepositorioBase, IRepositorio<Propietario>
{
    public RepositorioPropietario(IConfiguration configuration) : base(configuration)
    {
    }

    public int Alta(Propietario propietario)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            INSERT INTO Propietarios (Nombre, Telefono, Email, Direccion, Activo)
            VALUES (@nombre, @telefono, @email, @direccion, @activo);
            SELECT LAST_INSERT_ID();
            """;
        using var command = new MySqlCommand(sql, connection);
        CargarParametros(command, propietario);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        propietario.Id = id;
        return id;
    }

    public int Baja(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "DELETE FROM Propietarios WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public int Modificacion(Propietario propietario)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            UPDATE Propietarios
            SET Nombre = @nombre,
                Telefono = @telefono,
                Email = @email,
                Direccion = @direccion,
                Activo = @activo
            WHERE Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", propietario.Id);
        CargarParametros(command, propietario);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public IList<Propietario> ObtenerLista(int pagina = 1, int tamPagina = 10)
    {
        var propietarios = new List<Propietario>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT Id, Nombre, Telefono, Email, Direccion, Activo, FechaRegistro
            FROM Propietarios
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
            propietarios.Add(Mapear(reader));
        }
        return propietarios;
    }

    public int ObtenerCantidad()
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "SELECT COUNT(*) FROM Propietarios";
        using var command = new MySqlCommand(sql, connection);
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public Propietario? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT Id, Nombre, Telefono, Email, Direccion, Activo, FechaRegistro
            FROM Propietarios
            WHERE Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    private static void CargarParametros(MySqlCommand command, Propietario propietario)
    {
        command.Parameters.AddWithValue("@nombre", propietario.Nombre);
        command.Parameters.AddWithValue("@telefono", propietario.Telefono);
        command.Parameters.AddWithValue("@email", propietario.Email);
        command.Parameters.AddWithValue("@direccion", propietario.Direccion);
        command.Parameters.AddWithValue("@activo", propietario.Activo);
    }

    private static Propietario Mapear(MySqlDataReader reader)
    {
        return new Propietario
        {
            Id = reader.GetInt32("Id"),
            Nombre = reader.GetString("Nombre"),
            Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? "" : reader.GetString("Telefono"),
            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? "" : reader.GetString("Email"),
            Direccion = reader.IsDBNull(reader.GetOrdinal("Direccion")) ? "" : reader.GetString("Direccion"),
            Activo = reader.GetBoolean("Activo"),
            FechaRegistro = reader.GetDateTime("FechaRegistro")
        };
    }
}
