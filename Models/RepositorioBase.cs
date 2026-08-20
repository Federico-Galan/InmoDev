using Microsoft.Extensions.Configuration;

namespace InmoDev.Models;

public abstract class RepositorioBase
{
    protected readonly string connectionString;

    protected RepositorioBase(IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta configurar ConnectionStrings:DefaultConnection.");
    }
}
