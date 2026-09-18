using MySqlConnector;

namespace InmoDev.Models;

public class RepositorioReserva : RepositorioBase, IRepositorio<Reserva>
{
    public RepositorioReserva(IConfiguration configuration) : base(configuration)
    {
    }

    public int Alta(Reserva reserva)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            INSERT INTO Reservas (
                InquilinoId, InmuebleId, FechaInicio, FechaFin, FechaFinReal,
                MontoPorDia, PorcentajeSena, Estado, UsuarioCreadoId,
                UsuarioFinalizaId, FechaFinalizacion, MultaAplicada
            )
            VALUES (
                @inquilinoId, @inmuebleId, @fechaInicio, @fechaFin, @fechaFinReal,
                @montoPorDia, @porcentajeSena, @estado, @usuarioCreadoId,
                @usuarioFinalizaId, @fechaFinalizacion, @multaAplicada
            );
            SELECT LAST_INSERT_ID();
            """;
        using var command = new MySqlCommand(sql, connection);
        CargarParametros(command, reserva);
        connection.Open();
        var id = Convert.ToInt32(command.ExecuteScalar());
        reserva.Id = id;
        return id;
    }

    public int Baja(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = "DELETE FROM Reservas WHERE Id = @id";
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public int Modificacion(Reserva reserva)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            UPDATE Reservas
            SET InquilinoId = @inquilinoId,
                InmuebleId = @inmuebleId,
                FechaInicio = @fechaInicio,
                FechaFin = @fechaFin,
                FechaFinReal = @fechaFinReal,
                MontoPorDia = @montoPorDia,
                PorcentajeSena = @porcentajeSena,
                Estado = @estado,
                UsuarioFinalizaId = @usuarioFinalizaId,
                FechaFinalizacion = @fechaFinalizacion,
                MultaAplicada = @multaAplicada
            WHERE Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", reserva.Id);
        CargarParametros(command, reserva);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public int TerminarAnticipada(int id, DateTime fechaFinReal, decimal multa, int usuarioFinalizaId)
    {
        using var connection = new MySqlConnection(connectionString);
        const string sql = """
            UPDATE Reservas
            SET FechaFinReal = @fechaFinReal,
                MultaAplicada = @multaAplicada,
                Estado = 'Finalizada',
                UsuarioFinalizaId = @usuarioFinalizaId,
                FechaFinalizacion = NOW()
            WHERE Id = @id AND Estado = 'Vigente'
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@fechaFinReal", fechaFinReal.Date);
        command.Parameters.AddWithValue("@multaAplicada", multa);
        command.Parameters.AddWithValue("@usuarioFinalizaId", usuarioFinalizaId);
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public IList<Reserva> ObtenerLista(int pagina = 1, int tamPagina = 10)
    {
        return ObtenerLista(pagina, tamPagina, null, null, null, null, null);
    }

    public IList<Reserva> ObtenerLista(
        int pagina = 1,
        int tamPagina = 10,
        string? estado = null,
        int? inmuebleId = null,
        int? inquilinoId = null,
        DateTime? desde = null,
        DateTime? hasta = null)
    {
        var lista = new List<Reserva>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT r.Id, r.InquilinoId, r.InmuebleId, r.FechaInicio, r.FechaFin, r.FechaFinReal,
                   r.MontoPorDia, r.PorcentajeSena, r.Estado, r.FechaCreacion,
                   r.UsuarioCreadoId, r.UsuarioFinalizaId, r.FechaFinalizacion, r.MultaAplicada,
                   iq.NombreCompleto AS InquilinoNombre, iq.DNI AS InquilinoDni,
                   im.Direccion AS InmuebleDireccion, im.MonedaPrecio,
                   p.Nombre AS PropietarioNombre,
                   uc.Nombre AS UsuarioCreadorNombre,
                   uf.Nombre AS UsuarioFinalizaNombre,
                   COALESCE((SELECT SUM(Importe) FROM Pagos WHERE ReservaId = r.Id AND Estado != 'Anulado'), 0) AS TotalPagado
            FROM Reservas r
            INNER JOIN Inquilinos iq ON iq.Id = r.InquilinoId
            INNER JOIN Inmueble im ON im.Id = r.InmuebleId
            INNER JOIN Propietarios p ON p.Id = im.PropietarioId
            LEFT JOIN Usuarios uc ON uc.Id = r.UsuarioCreadoId
            LEFT JOIN Usuarios uf ON uf.Id = r.UsuarioFinalizaId
            WHERE (@estado IS NULL OR r.Estado = @estado)
              AND (@inmuebleId IS NULL OR r.InmuebleId = @inmuebleId)
              AND (@inquilinoId IS NULL OR r.InquilinoId = @inquilinoId)
              AND (@desde IS NULL OR r.FechaFin >= @desde)
              AND (@hasta IS NULL OR r.FechaInicio <= @hasta)
            ORDER BY r.FechaInicio DESC
            LIMIT @tamPagina OFFSET @offset
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@estado", string.IsNullOrWhiteSpace(estado) ? DBNull.Value : estado.Trim());
        command.Parameters.AddWithValue("@inmuebleId", (object?)inmuebleId ?? DBNull.Value);
        command.Parameters.AddWithValue("@inquilinoId", (object?)inquilinoId ?? DBNull.Value);
        command.Parameters.AddWithValue("@desde", (object?)desde?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@hasta", (object?)hasta?.Date ?? DBNull.Value);
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
        return ObtenerCantidad(null, null, null, null, null);
    }

    public int ObtenerCantidad(
        string? estado = null,
        int? inmuebleId = null,
        int? inquilinoId = null,
        DateTime? desde = null,
        DateTime? hasta = null)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT COUNT(*)
            FROM Reservas r
            WHERE (@estado IS NULL OR r.Estado = @estado)
              AND (@inmuebleId IS NULL OR r.InmuebleId = @inmuebleId)
              AND (@inquilinoId IS NULL OR r.InquilinoId = @inquilinoId)
              AND (@desde IS NULL OR r.FechaFin >= @desde)
              AND (@hasta IS NULL OR r.FechaInicio <= @hasta)
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@estado", string.IsNullOrWhiteSpace(estado) ? DBNull.Value : estado.Trim());
        command.Parameters.AddWithValue("@inmuebleId", (object?)inmuebleId ?? DBNull.Value);
        command.Parameters.AddWithValue("@inquilinoId", (object?)inquilinoId ?? DBNull.Value);
        command.Parameters.AddWithValue("@desde", (object?)desde?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@hasta", (object?)hasta?.Date ?? DBNull.Value);

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public Reserva? ObtenerPorId(int id)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT r.Id, r.InquilinoId, r.InmuebleId, r.FechaInicio, r.FechaFin, r.FechaFinReal,
                   r.MontoPorDia, r.PorcentajeSena, r.Estado, r.FechaCreacion,
                   r.UsuarioCreadoId, r.UsuarioFinalizaId, r.FechaFinalizacion, r.MultaAplicada,
                   iq.NombreCompleto AS InquilinoNombre, iq.DNI AS InquilinoDni,
                   im.Direccion AS InmuebleDireccion, im.MonedaPrecio,
                   p.Nombre AS PropietarioNombre,
                   uc.Nombre AS UsuarioCreadorNombre,
                   uf.Nombre AS UsuarioFinalizaNombre,
                   COALESCE((SELECT SUM(Importe) FROM Pagos WHERE ReservaId = r.Id AND Estado != 'Anulado'), 0) AS TotalPagado
            FROM Reservas r
            INNER JOIN Inquilinos iq ON iq.Id = r.InquilinoId
            INNER JOIN Inmueble im ON im.Id = r.InmuebleId
            INNER JOIN Propietarios p ON p.Id = im.PropietarioId
            LEFT JOIN Usuarios uc ON uc.Id = r.UsuarioCreadoId
            LEFT JOIN Usuarios uf ON uf.Id = r.UsuarioFinalizaId
            WHERE r.Id = @id
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        connection.Open();
        using var reader = command.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public bool InmuebleEstaOcupado(int inmuebleId, DateTime inicio, DateTime fin, int? reservaIdExcluir = null)
    {
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT COUNT(*)
            FROM Reservas
            WHERE InmuebleId = @inmuebleId
              AND Estado NOT IN ('Cancelada', 'Anulada')
              AND (@reservaIdExcluir IS NULL OR Id != @reservaIdExcluir)
              AND (FechaInicio <= @fin AND FechaFin >= @inicio)
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@inmuebleId", inmuebleId);
        command.Parameters.AddWithValue("@inicio", inicio.Date);
        command.Parameters.AddWithValue("@fin", fin.Date);
        command.Parameters.AddWithValue("@reservaIdExcluir", (object?)reservaIdExcluir ?? DBNull.Value);

        connection.Open();
        var total = Convert.ToInt32(command.ExecuteScalar());
        return total > 0;
    }

    public IList<Inmueble> ObtenerInmueblesDisponibles(
        DateTime inicio,
        DateTime fin,
        int? tipoId = null,
        int? cupoMinimo = null,
        decimal? precioMaximo = null,
        string? moneda = null)
    {
        var inmuebles = new List<Inmueble>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT i.Id, i.PropietarioId, i.TipoId, i.Direccion, i.CupoMaximo, i.Coordenadas,
                   i.PrecioPorDia, i.MonedaPrecio, i.PorcentajeReserva, i.ImagenPortada, i.Disponible, i.FechaRegistro,
                   p.Nombre AS PropietarioNombre, t.Nombre AS TipoNombre
            FROM Inmueble i
            INNER JOIN Propietarios p ON p.Id = i.PropietarioId
            INNER JOIN TiposInmueble t ON t.Id = i.TipoId
            WHERE i.Disponible = TRUE
              AND p.Activo = TRUE
              AND (@tipoId IS NULL OR i.TipoId = @tipoId)
              AND (@cupoMinimo IS NULL OR i.CupoMaximo >= @cupoMinimo)
              AND (@precioMaximo IS NULL OR i.PrecioPorDia <= @precioMaximo)
              AND (@moneda IS NULL OR i.MonedaPrecio = @moneda)
              AND NOT EXISTS (
                  SELECT 1
                  FROM Reservas r
                  WHERE r.InmuebleId = i.Id
                    AND r.Estado NOT IN ('Cancelada', 'Anulada')
                    AND (r.FechaInicio <= @fin AND r.FechaFin >= @inicio)
              )
            ORDER BY i.PrecioPorDia ASC
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@inicio", inicio.Date);
        command.Parameters.AddWithValue("@fin", fin.Date);
        command.Parameters.AddWithValue("@tipoId", (object?)tipoId ?? DBNull.Value);
        command.Parameters.AddWithValue("@cupoMinimo", (object?)cupoMinimo ?? DBNull.Value);
        command.Parameters.AddWithValue("@precioMaximo", (object?)precioMaximo ?? DBNull.Value);
        command.Parameters.AddWithValue("@moneda", string.IsNullOrWhiteSpace(moneda) ? DBNull.Value : moneda.Trim());

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            inmuebles.Add(new Inmueble
            {
                Id = reader.GetInt32("Id"),
                PropietarioId = reader.GetInt32("PropietarioId"),
                TipoId = reader.GetInt32("TipoId"),
                Direccion = reader.GetString("Direccion"),
                CupoMaximo = reader.IsDBNull(reader.GetOrdinal("CupoMaximo")) ? null : reader.GetInt32("CupoMaximo"),
                Coordenadas = reader.IsDBNull(reader.GetOrdinal("Coordenadas")) ? null : reader.GetString("Coordenadas"),
                PrecioPorDia = reader.GetDecimal("PrecioPorDia"),
                MonedaPrecio = reader.GetString("MonedaPrecio"),
                PorcentajeReserva = reader.GetDecimal("PorcentajeReserva"),
                ImagenPortada = reader.IsDBNull(reader.GetOrdinal("ImagenPortada")) ? null : reader.GetString("ImagenPortada"),
                Disponible = reader.GetBoolean("Disponible"),
                FechaRegistro = reader.GetDateTime("FechaRegistro"),
                PropietarioNombre = reader.GetString("PropietarioNombre"),
                TipoNombre = reader.GetString("TipoNombre")
            });
        }
        return inmuebles;
    }

    public IList<OpcionSelect> ObtenerInquilinosSelect(string? busqueda = null, int limite = 25)
    {
        var opciones = new List<OpcionSelect>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT Id, CONCAT(NombreCompleto, ' (DNI: ', DNI, ')') AS Texto
            FROM Inquilinos
            WHERE (@busqueda IS NULL OR NombreCompleto LIKE CONCAT('%', @busqueda, '%') OR DNI LIKE CONCAT('%', @busqueda, '%'))
            ORDER BY NombreCompleto
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

    private static void CargarParametros(MySqlCommand command, Reserva r)
    {
        command.Parameters.AddWithValue("@inquilinoId", r.InquilinoId);
        command.Parameters.AddWithValue("@inmuebleId", r.InmuebleId);
        command.Parameters.AddWithValue("@fechaInicio", r.FechaInicio.Date);
        command.Parameters.AddWithValue("@fechaFin", r.FechaFin.Date);
        command.Parameters.AddWithValue("@fechaFinReal", (object?)r.FechaFinReal?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@montoPorDia", r.MontoPorDia);
        command.Parameters.AddWithValue("@porcentajeSena", (object?)r.PorcentajeSena ?? DBNull.Value);
        command.Parameters.AddWithValue("@estado", r.Estado);
        command.Parameters.AddWithValue("@usuarioCreadoId", r.UsuarioCreadoId);
        command.Parameters.AddWithValue("@usuarioFinalizaId", (object?)r.UsuarioFinalizaId ?? DBNull.Value);
        command.Parameters.AddWithValue("@fechaFinalizacion", (object?)r.FechaFinalizacion ?? DBNull.Value);
        command.Parameters.AddWithValue("@multaAplicada", (object?)r.MultaAplicada ?? DBNull.Value);
    }

    private static Reserva Mapear(MySqlDataReader reader)
    {
        return new Reserva
        {
            Id = reader.GetInt32("Id"),
            InquilinoId = reader.GetInt32("InquilinoId"),
            InmuebleId = reader.GetInt32("InmuebleId"),
            FechaInicio = reader.GetDateTime("FechaInicio"),
            FechaFin = reader.GetDateTime("FechaFin"),
            FechaFinReal = reader.IsDBNull(reader.GetOrdinal("FechaFinReal")) ? null : reader.GetDateTime("FechaFinReal"),
            MontoPorDia = reader.GetDecimal("MontoPorDia"),
            PorcentajeSena = reader.IsDBNull(reader.GetOrdinal("PorcentajeSena")) ? null : reader.GetDecimal("PorcentajeSena"),
            Estado = reader.GetString("Estado"),
            FechaCreacion = reader.GetDateTime("FechaCreacion"),
            UsuarioCreadoId = reader.GetInt32("UsuarioCreadoId"),
            UsuarioFinalizaId = reader.IsDBNull(reader.GetOrdinal("UsuarioFinalizaId")) ? null : reader.GetInt32("UsuarioFinalizaId"),
            FechaFinalizacion = reader.IsDBNull(reader.GetOrdinal("FechaFinalizacion")) ? null : reader.GetDateTime("FechaFinalizacion"),
            MultaAplicada = reader.IsDBNull(reader.GetOrdinal("MultaAplicada")) ? null : reader.GetDecimal("MultaAplicada"),
            InquilinoNombre = reader.GetString("InquilinoNombre"),
            InquilinoDni = reader.GetString("InquilinoDni"),
            InmuebleDireccion = reader.GetString("InmuebleDireccion"),
            MonedaPrecio = reader.GetString("MonedaPrecio"),
            PropietarioNombre = reader.GetString("PropietarioNombre"),
            UsuarioCreadorNombre = reader.IsDBNull(reader.GetOrdinal("UsuarioCreadorNombre")) ? null : reader.GetString("UsuarioCreadorNombre"),
            UsuarioFinalizaNombre = reader.IsDBNull(reader.GetOrdinal("UsuarioFinalizaNombre")) ? null : reader.GetString("UsuarioFinalizaNombre"),
            TotalPagado = reader.GetDecimal("TotalPagado")
        };
    }
}
