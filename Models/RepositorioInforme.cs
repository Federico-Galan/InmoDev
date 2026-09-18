using MySqlConnector;

namespace InmoDev.Models;

public class RepositorioInforme : RepositorioBase
{
    public RepositorioInforme(IConfiguration configuration) : base(configuration)
    {
    }

    // =========================================================================
    // REPORTE 1: Inmuebles y sus respectivos dueños (filtro por disponibilidad)
    // =========================================================================
    public IList<Inmueble> InmueblesConDuenios(bool? disponible = null)
    {
        var lista = new List<Inmueble>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT i.Id, i.PropietarioId, i.TipoId, i.Direccion, i.CupoMaximo, i.Coordenadas,
                   i.PrecioPorDia, i.MonedaPrecio, i.PorcentajeReserva, i.ImagenPortada, i.Disponible, i.FechaRegistro,
                   p.Nombre AS PropietarioNombre, p.Telefono AS PropietarioTelefono, p.Email AS PropietarioEmail,
                   t.Nombre AS TipoNombre
            FROM Inmueble i
            INNER JOIN Propietarios p ON p.Id = i.PropietarioId
            INNER JOIN TiposInmueble t ON t.Id = i.TipoId
            WHERE (@disponible IS NULL OR i.Disponible = @disponible)
            ORDER BY p.Nombre ASC, i.Direccion ASC
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@disponible", (object?)disponible ?? DBNull.Value);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(MapearInmueble(reader));
        }
        return lista;
    }

    // =========================================================================
    // REPORTE 2: Inmuebles pertenecientes a un propietario específico
    // =========================================================================
    public IList<Inmueble> InmueblesPorPropietario(int propietarioId)
    {
        var lista = new List<Inmueble>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT i.Id, i.PropietarioId, i.TipoId, i.Direccion, i.CupoMaximo, i.Coordenadas,
                   i.PrecioPorDia, i.MonedaPrecio, i.PorcentajeReserva, i.ImagenPortada, i.Disponible, i.FechaRegistro,
                   p.Nombre AS PropietarioNombre, p.Telefono AS PropietarioTelefono, p.Email AS PropietarioEmail,
                   t.Nombre AS TipoNombre
            FROM Inmueble i
            INNER JOIN Propietarios p ON p.Id = i.PropietarioId
            INNER JOIN TiposInmueble t ON t.Id = i.TipoId
            WHERE i.PropietarioId = @propietarioId
            ORDER BY i.Direccion ASC
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@propietarioId", propietarioId);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(MapearInmueble(reader));
        }
        return lista;
    }

    // =========================================================================
    // REPORTE 3: Ranking de inmuebles más reservados en los últimos 365 días
    // =========================================================================
    public IList<InmuebleRanking> RankingInmueblesMasReservados(int limite = 25)
    {
        var lista = new List<InmuebleRanking>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT i.Id, i.Direccion, i.PrecioPorDia, i.MonedaPrecio,
                   t.Nombre AS TipoNombre, p.Nombre AS PropietarioNombre,
                   COUNT(r.Id) AS CantidadReservas,
                   COALESCE(SUM(DATEDIFF(COALESCE(r.FechaFinReal, r.FechaFin), r.FechaInicio) + 1), 0) AS TotalDiasReservados,
                   COALESCE(SUM((DATEDIFF(COALESCE(r.FechaFinReal, r.FechaFin), r.FechaInicio) + 1) * r.MontoPorDia), 0) AS TotalRecaudado
            FROM Inmueble i
            INNER JOIN Propietarios p ON p.Id = i.PropietarioId
            INNER JOIN TiposInmueble t ON t.Id = i.TipoId
            LEFT JOIN Reservas r ON r.InmuebleId = i.Id
                AND r.Estado NOT IN ('Cancelada', 'Anulada')
                AND r.FechaInicio >= DATE_SUB(CURDATE(), INTERVAL 365 DAY)
            GROUP BY i.Id, i.Direccion, i.PrecioPorDia, i.MonedaPrecio, t.Nombre, p.Nombre
            ORDER BY CantidadReservas DESC, TotalDiasReservados DESC, TotalRecaudado DESC
            LIMIT @limite
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@limite", limite);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new InmuebleRanking
            {
                Id = reader.GetInt32("Id"),
                Direccion = reader.GetString("Direccion"),
                TipoNombre = reader.GetString("TipoNombre"),
                PropietarioNombre = reader.GetString("PropietarioNombre"),
                PrecioPorDia = reader.GetDecimal("PrecioPorDia"),
                MonedaPrecio = reader.GetString("MonedaPrecio"),
                CantidadReservas = Convert.ToInt32(reader["CantidadReservas"]),
                TotalDiasReservados = Convert.ToInt32(reader["TotalDiasReservados"]),
                TotalRecaudado = Convert.ToDecimal(reader["TotalRecaudado"])
            });
        }
        return lista;
    }

    // =========================================================================
    // REPORTE 4: Inmuebles sin reservas en los últimos X días
    // =========================================================================
    public IList<Inmueble> InmueblesSinReservas(int dias = 30)
    {
        var lista = new List<Inmueble>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT i.Id, i.PropietarioId, i.TipoId, i.Direccion, i.CupoMaximo, i.Coordenadas,
                   i.PrecioPorDia, i.MonedaPrecio, i.PorcentajeReserva, i.ImagenPortada, i.Disponible, i.FechaRegistro,
                   p.Nombre AS PropietarioNombre, p.Telefono AS PropietarioTelefono, p.Email AS PropietarioEmail,
                   t.Nombre AS TipoNombre
            FROM Inmueble i
            INNER JOIN Propietarios p ON p.Id = i.PropietarioId
            INNER JOIN TiposInmueble t ON t.Id = i.TipoId
            WHERE NOT EXISTS (
                SELECT 1 FROM Reservas r
                WHERE r.InmuebleId = i.Id
                  AND r.Estado NOT IN ('Cancelada', 'Anulada')
                  AND r.FechaInicio >= DATE_SUB(CURDATE(), INTERVAL @dias DAY)
            )
            ORDER BY i.Direccion ASC
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@dias", dias);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(MapearInmueble(reader));
        }
        return lista;
    }

    // =========================================================================
    // REPORTE 5: Listado de todas las reservas de alquiler vigentes (en rango)
    // =========================================================================
    public IList<Reserva> ReservasVigentes(DateTime? desde = null, DateTime? hasta = null)
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
            WHERE r.Estado = 'Vigente'
              AND (@desde IS NULL OR r.FechaFin >= @desde)
              AND (@hasta IS NULL OR r.FechaInicio <= @hasta)
            ORDER BY r.FechaInicio ASC
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@desde", (object?)desde?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@hasta", (object?)hasta?.Date ?? DBNull.Value);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(MapearReserva(reader));
        }
        return lista;
    }

    // =========================================================================
    // REPORTE 6: Reservas que terminan dentro de los próximos X días
    // =========================================================================
    public IList<Reserva> ReservasPorVencer(int dias = 30)
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
            WHERE r.Estado = 'Vigente'
              AND r.FechaFin >= CURDATE()
              AND r.FechaFin <= DATE_ADD(CURDATE(), INTERVAL @dias DAY)
            ORDER BY r.FechaFin ASC
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@dias", dias);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(MapearReserva(reader));
        }
        return lista;
    }

    // =========================================================================
    // REPORTE 7: Detalle de pagos realizados para una reserva en particular
    // =========================================================================
    public IList<Pago> PagosPorReserva(int reservaId)
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
            lista.Add(new Pago
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
            });
        }
        return lista;
    }

    // =========================================================================
    // REPORTE 8: Consulta de disponibilidad de inmuebles entre dos fechas
    // =========================================================================
    public IList<Inmueble> InmueblesDisponiblesEnRango(DateTime inicio, DateTime fin)
    {
        var lista = new List<Inmueble>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT i.Id, i.PropietarioId, i.TipoId, i.Direccion, i.CupoMaximo, i.Coordenadas,
                   i.PrecioPorDia, i.MonedaPrecio, i.PorcentajeReserva, i.ImagenPortada, i.Disponible, i.FechaRegistro,
                   p.Nombre AS PropietarioNombre, p.Telefono AS PropietarioTelefono, p.Email AS PropietarioEmail,
                   t.Nombre AS TipoNombre
            FROM Inmueble i
            INNER JOIN Propietarios p ON p.Id = i.PropietarioId
            INNER JOIN TiposInmueble t ON t.Id = i.TipoId
            WHERE i.Disponible = TRUE
              AND p.Activo = TRUE
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

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(MapearInmueble(reader));
        }
        return lista;
    }

    // =========================================================================
    // MÉTODOS AUXILIARES PARA SELECTS
    // =========================================================================
    public IList<OpcionSelect> ObtenerPropietariosSelect()
    {
        var lista = new List<OpcionSelect>();
        using var connection = new MySqlConnection(connectionString);
        const string sql = "SELECT Id, Nombre AS Texto FROM Propietarios WHERE Activo = TRUE ORDER BY Nombre ASC";
        using var command = new MySqlCommand(sql, connection);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new OpcionSelect
            {
                Id = reader.GetInt32("Id"),
                Texto = reader.GetString("Texto")
            });
        }
        return lista;
    }

    public IList<OpcionSelect> ObtenerReservasSelect(int limite = 100)
    {
        var lista = new List<OpcionSelect>();
        using var connection = new MySqlConnection(connectionString);
        var sql = """
            SELECT r.Id,
                   CONCAT('Reserva #', r.Id, ' — ', iq.NombreCompleto, ' (', im.Direccion, ') [', r.Estado, ']') AS Texto
            FROM Reservas r
            INNER JOIN Inquilinos iq ON iq.Id = r.InquilinoId
            INNER JOIN Inmueble im ON im.Id = r.InmuebleId
            ORDER BY r.Id DESC
            LIMIT @limite
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@limite", limite);

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new OpcionSelect
            {
                Id = reader.GetInt32("Id"),
                Texto = reader.GetString("Texto")
            });
        }
        return lista;
    }

    public Reserva? ObtenerReservaDetallada(int id)
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
        return reader.Read() ? MapearReserva(reader) : null;
    }

    private static Inmueble MapearInmueble(MySqlDataReader reader)
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
            PorcentajeReserva = reader.GetDecimal("PorcentajeReserva"),
            ImagenPortada = reader.IsDBNull(reader.GetOrdinal("ImagenPortada")) ? null : reader.GetString("ImagenPortada"),
            Disponible = reader.GetBoolean("Disponible"),
            FechaRegistro = reader.GetDateTime("FechaRegistro"),
            PropietarioNombre = reader.GetString("PropietarioNombre"),
            PropietarioTelefono = reader.IsDBNull(reader.GetOrdinal("PropietarioTelefono")) ? null : reader.GetString("PropietarioTelefono"),
            PropietarioEmail = reader.IsDBNull(reader.GetOrdinal("PropietarioEmail")) ? null : reader.GetString("PropietarioEmail"),
            TipoNombre = reader.GetString("TipoNombre")
        };
    }

    private static Reserva MapearReserva(MySqlDataReader reader)
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
