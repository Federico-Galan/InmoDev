using MySqlConnector;

namespace InmoDev.Models;

public class RepositorioPago : RepositorioBase, IRepositorio<Pago>
{
    public RepositorioPago(IConfiguration configuration) : base(configuration)
    {
    }

    public int Alta(Pago pago)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            INSERT INTO Pagos (ReservaId, Concepto, FechaPago, Importe, Estado, UsuarioCreaId)
            VALUES (@reservaId, @concepto, @fechaPago, @importe, @estado, @usuarioCreaId);
            SELECT LAST_INSERT_ID();
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@reservaId", pago.ReservaId);
        command.Parameters.AddWithValue("@concepto", pago.Concepto.Trim());
        command.Parameters.AddWithValue("@fechaPago", pago.FechaPago);
        command.Parameters.AddWithValue("@importe", pago.Importe);
        command.Parameters.AddWithValue("@estado", string.IsNullOrWhiteSpace(pago.Estado) ? "Activo" : pago.Estado);
        command.Parameters.AddWithValue("@usuarioCreaId", pago.UsuarioCreaId);

        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        pago.Id = id;
        return id;
    }

    public int Modificacion(Pago pago)
    {
        // Regla estricta: Solo se puede modificar el concepto
        return ModificarConcepto(pago.Id, pago.Concepto);
    }

    public int ModificarConcepto(int id, string concepto)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "UPDATE Pagos SET Concepto = @concepto WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@concepto", concepto.Trim());
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public int Baja(int id)
    {
        // Por defecto en IRepositorio llamamos a anulación lógica si no se pasa usuario
        return Anular(id, 1);
    }

    public int Anular(int id, int usuarioAnulaId)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            UPDATE Pagos
            SET Estado = 'Anulado',
                UsuarioAnulaId = @usuarioAnulaId,
                FechaAnulacion = NOW()
            WHERE Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@usuarioAnulaId", usuarioAnulaId);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public IList<Pago> ObtenerLista(int pagina = 1, int tamPagina = 10)
    {
        return ObtenerLista(pagina, tamPagina, null, null);
    }

    public IList<Pago> ObtenerLista(int pagina = 1, int tamPagina = 10, int? reservaId = null, string? estado = null)
    {
        var lista = new List<Pago>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT p.Id, p.ReservaId, p.Concepto, p.FechaPago, p.Importe, p.Estado, p.FechaCreacion,
                   p.UsuarioCreaId, p.UsuarioAnulaId, p.FechaAnulacion,
                   im.Direccion AS InmuebleDireccion, im.MonedaPrecio,
                   iq.NombreCompleto AS InquilinoNombre,
                   uc.Nombre AS UsuarioCreaNombre,
                   ua.Nombre AS UsuarioAnulaNombre
            FROM Pagos p
            INNER JOIN Reservas r ON r.Id = p.ReservaId
            INNER JOIN Inmueble im ON im.Id = r.InmuebleId
            INNER JOIN Inquilinos iq ON iq.Id = r.InquilinoId
            LEFT JOIN Usuarios uc ON uc.Id = p.UsuarioCreaId
            LEFT JOIN Usuarios ua ON ua.Id = p.UsuarioAnulaId
            WHERE (@reservaId IS NULL OR p.ReservaId = @reservaId)
              AND (@estado IS NULL OR p.Estado = @estado)
            ORDER BY p.FechaPago DESC
            LIMIT @tamPagina OFFSET @offset
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@reservaId", (object?)reservaId ?? DBNull.Value);
        command.Parameters.AddWithValue("@estado", string.IsNullOrWhiteSpace(estado) ? DBNull.Value : estado.Trim());
        command.Parameters.AddWithValue("@tamPagina", tamPagina);
        command.Parameters.AddWithValue("@offset", (pagina - 1) * tamPagina);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(Mapear(reader));
        }
        return lista;
    }

    public int ObtenerCantidad()
    {
        return ObtenerCantidad(null, null);
    }

    public int ObtenerCantidad(int? reservaId = null, string? estado = null)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT COUNT(*)
            FROM Pagos p
            WHERE (@reservaId IS NULL OR p.ReservaId = @reservaId)
              AND (@estado IS NULL OR p.Estado = @estado)
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@reservaId", (object?)reservaId ?? DBNull.Value);
        command.Parameters.AddWithValue("@estado", string.IsNullOrWhiteSpace(estado) ? DBNull.Value : estado.Trim());

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public IList<Pago> ObtenerPorReserva(int reservaId)
    {
        var lista = new List<Pago>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT p.Id, p.ReservaId, p.Concepto, p.FechaPago, p.Importe, p.Estado, p.FechaCreacion,
                   p.UsuarioCreaId, p.UsuarioAnulaId, p.FechaAnulacion,
                   im.Direccion AS InmuebleDireccion, im.MonedaPrecio,
                   iq.NombreCompleto AS InquilinoNombre,
                   uc.Nombre AS UsuarioCreaNombre,
                   ua.Nombre AS UsuarioAnulaNombre
            FROM Pagos p
            INNER JOIN Reservas r ON r.Id = p.ReservaId
            INNER JOIN Inmueble im ON im.Id = r.InmuebleId
            INNER JOIN Inquilinos iq ON iq.Id = r.InquilinoId
            LEFT JOIN Usuarios uc ON uc.Id = p.UsuarioCreaId
            LEFT JOIN Usuarios ua ON ua.Id = p.UsuarioAnulaId
            WHERE p.ReservaId = @reservaId
            ORDER BY p.FechaPago DESC
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@reservaId", reservaId);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(Mapear(reader));
        }
        return lista;
    }

    public Pago? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT p.Id, p.ReservaId, p.Concepto, p.FechaPago, p.Importe, p.Estado, p.FechaCreacion,
                   p.UsuarioCreaId, p.UsuarioAnulaId, p.FechaAnulacion,
                   im.Direccion AS InmuebleDireccion, im.MonedaPrecio,
                   iq.NombreCompleto AS InquilinoNombre,
                   uc.Nombre AS UsuarioCreaNombre,
                   ua.Nombre AS UsuarioAnulaNombre
            FROM Pagos p
            INNER JOIN Reservas r ON r.Id = p.ReservaId
            INNER JOIN Inmueble im ON im.Id = r.InmuebleId
            INNER JOIN Inquilinos iq ON iq.Id = r.InquilinoId
            LEFT JOIN Usuarios uc ON uc.Id = p.UsuarioCreaId
            LEFT JOIN Usuarios ua ON ua.Id = p.UsuarioAnulaId
            WHERE p.Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        connection.Open();
        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public decimal ObtenerTotalPagado(int reservaId)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "SELECT COALESCE(SUM(Importe), 0) FROM Pagos WHERE ReservaId = @reservaId AND Estado != 'Anulado'";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@reservaId", reservaId);
        connection.Open();
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public IList<OpcionSelect> ObtenerReservasSelect(string? busqueda = null, int limite = 30)
    {
        var opciones = new List<OpcionSelect>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT r.Id, CONCAT('Reserva #', r.Id, ' - ', im.Direccion, ' (', iq.NombreCompleto, ')') AS Texto
            FROM Reservas r
            INNER JOIN Inmueble im ON im.Id = r.InmuebleId
            INNER JOIN Inquilinos iq ON iq.Id = r.InquilinoId
            WHERE (@busqueda IS NULL
                   OR CAST(r.Id AS CHAR) LIKE CONCAT('%', @busqueda, '%')
                   OR im.Direccion LIKE CONCAT('%', @busqueda, '%')
                   OR iq.NombreCompleto LIKE CONCAT('%', @busqueda, '%'))
            ORDER BY r.Id DESC
            LIMIT @limite
            """;
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

    private static Pago Mapear(MySqlDataReader reader)
    {
        return new Pago
        {
            Id = reader.GetInt32("Id"),
            ReservaId = reader.GetInt32("ReservaId"),
            Concepto = reader.GetString("Concepto"),
            FechaPago = reader.GetDateTime("FechaPago"),
            Importe = reader.GetDecimal("Importe"),
            Estado = reader.GetString("Estado"),
            FechaCreacion = reader.GetDateTime("FechaCreacion"),
            UsuarioCreaId = reader.GetInt32("UsuarioCreaId"),
            UsuarioAnulaId = reader.IsDBNull(reader.GetOrdinal("UsuarioAnulaId")) ? null : reader.GetInt32("UsuarioAnulaId"),
            FechaAnulacion = reader.IsDBNull(reader.GetOrdinal("FechaAnulacion")) ? null : reader.GetDateTime("FechaAnulacion"),
            InmuebleDireccion = reader.GetString("InmuebleDireccion"),
            MonedaPrecio = reader.GetString("MonedaPrecio"),
            InquilinoNombre = reader.GetString("InquilinoNombre"),
            UsuarioCreaNombre = reader.IsDBNull(reader.GetOrdinal("UsuarioCreaNombre")) ? null : reader.GetString("UsuarioCreaNombre"),
            UsuarioAnulaNombre = reader.IsDBNull(reader.GetOrdinal("UsuarioAnulaNombre")) ? null : reader.GetString("UsuarioAnulaNombre")
        };
    }
}
