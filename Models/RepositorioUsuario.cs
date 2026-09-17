using MySqlConnector;

namespace InmoDev.Models;

public class RepositorioUsuario : RepositorioBase, IRepositorio<Usuario>
{
    public RepositorioUsuario(IConfiguration configuration) : base(configuration)
    {
    }

    public int Alta(Usuario usuario)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            INSERT INTO Usuarios (Email, PasswordHash, Rol, Nombre, Avatar, Activo)
            VALUES (@email, @passwordHash, @rol, @nombre, @avatar, @activo);
            SELECT LAST_INSERT_ID();
            """;
        using var command = new MySqlCommand(sql, connection);
        CargarParametros(command, usuario);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        usuario.Id = id;
        return id;
    }

    public int Baja(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "DELETE FROM Usuarios WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public int Modificacion(Usuario usuario)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            UPDATE Usuarios
            SET Email = @email,
                Rol = @rol,
                Nombre = @nombre,
                Avatar = @avatar,
                Activo = @activo
            WHERE Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", usuario.Id);
        CargarParametros(command, usuario);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public IList<Usuario> ObtenerLista(int pagina = 1, int tamPagina = 10)
    {
        return ObtenerLista(pagina, tamPagina, null);
    }

    public IList<Usuario> ObtenerLista(int pagina = 1, int tamPagina = 10, string? busqueda = null)
    {
        var usuarios = new List<Usuario>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT Id, Email, PasswordHash, Rol, Nombre, Avatar, Activo, FechaCreacion
            FROM Usuarios
            WHERE (@busqueda IS NULL OR Nombre LIKE CONCAT('%', @busqueda, '%') OR Email LIKE CONCAT('%', @busqueda, '%'))
            ORDER BY Nombre
            LIMIT @tamPagina OFFSET @desde
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@busqueda", string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : busqueda.Trim());
        command.Parameters.AddWithValue("@tamPagina", tamPagina);
        command.Parameters.AddWithValue("@desde", (pagina - 1) * tamPagina);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            usuarios.Add(Mapear(reader));
        }
        return usuarios;
    }

    public int ObtenerCantidad()
    {
        return ObtenerCantidad(null);
    }

    public int ObtenerCantidad(string? busqueda = null)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT COUNT(*) FROM Usuarios
            WHERE (@busqueda IS NULL OR Nombre LIKE CONCAT('%', @busqueda, '%') OR Email LIKE CONCAT('%', @busqueda, '%'))
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@busqueda", string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : busqueda.Trim());
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public Usuario? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "SELECT Id, Email, PasswordHash, Rol, Nombre, Avatar, Activo, FechaCreacion FROM Usuarios WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public Usuario? ObtenerPorEmail(string email)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "SELECT Id, Email, PasswordHash, Rol, Nombre, Avatar, Activo, FechaCreacion FROM Usuarios WHERE LOWER(Email) = LOWER(@email)";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@email", email.Trim());
        connection.Open();
        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public int ActualizarPerfil(int id, string nombre, string? avatar)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "UPDATE Usuarios SET Nombre = @nombre, Avatar = @avatar WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@nombre", nombre);
        command.Parameters.AddWithValue("@avatar", (object?)avatar ?? DBNull.Value);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public int ActualizarClave(int id, string passwordHash)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "UPDATE Usuarios SET PasswordHash = @passwordHash WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public int CambiarEstado(int id, bool activo)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "UPDATE Usuarios SET Activo = @activo WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@activo", activo);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    private static void CargarParametros(MySqlCommand command, Usuario usuario)
    {
        command.Parameters.AddWithValue("@email", usuario.Email.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("@passwordHash", usuario.PasswordHash);
        command.Parameters.AddWithValue("@rol", usuario.Rol);
        command.Parameters.AddWithValue("@nombre", usuario.Nombre.Trim());
        command.Parameters.AddWithValue("@avatar", (object?)usuario.Avatar ?? DBNull.Value);
        command.Parameters.AddWithValue("@activo", usuario.Activo);
    }

    private static Usuario Mapear(MySqlDataReader reader)
    {
        return new Usuario
        {
            Id = reader.GetInt32("Id"),
            Email = reader.GetString("Email"),
            PasswordHash = reader.GetString("PasswordHash"),
            Rol = reader.GetString("Rol"),
            Nombre = reader.GetString("Nombre"),
            Avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? null : reader.GetString("Avatar"),
            Activo = reader.GetBoolean("Activo"),
            FechaCreacion = reader.GetDateTime("FechaCreacion")
        };
    }
}
