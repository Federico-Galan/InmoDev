using InmoDev.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IRepositorio<Propietario>, RepositorioPropietario>();
builder.Services.AddScoped<IRepositorio<Inquilino>, RepositorioInquilino>();
builder.Services.AddScoped<IRepositorio<TipoInmueble>, RepositorioTipoInmueble>();
builder.Services.AddScoped<RepositorioTipoInmueble>();
builder.Services.AddScoped<RepositorioInmueble>();
builder.Services.AddScoped<RepositorioImagen>();
builder.Services.AddScoped<RepositorioUsuario>();
builder.Services.AddScoped<RepositorioReserva>();
builder.Services.AddScoped<RepositorioPago>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Usuarios/Login";
        options.LogoutPath = "/Usuarios/Logout";
        options.AccessDeniedPath = "/Usuarios/Denegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Sembrado automático inicial de usuarios
try
{
    using var scope = app.Services.CreateScope();
    var repoUsuario = scope.ServiceProvider.GetRequiredService<RepositorioUsuario>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

    var admin = repoUsuario.ObtenerPorEmail("admin@inmodev.local");
    if (admin == null)
    {
        var nuevoAdmin = new Usuario
        {
            Nombre = "Administrador Principal",
            Email = "admin@inmodev.local",
            Rol = "Administrador",
            Activo = true
        };
        nuevoAdmin.PasswordHash = hasher.HashPassword(nuevoAdmin, "admin123");
        repoUsuario.Alta(nuevoAdmin);
    }
    else if (admin.PasswordHash == "pendiente-definir-hash")
    {
        repoUsuario.ActualizarClave(admin.Id, hasher.HashPassword(admin, "admin123"));
    }

    var empleado = repoUsuario.ObtenerPorEmail("empleado@inmodev.local");
    if (empleado == null)
    {
        var nuevoEmp = new Usuario
        {
            Nombre = "Empleado InmoDev",
            Email = "empleado@inmodev.local",
            Rol = "Empleado",
            Activo = true
        };
        nuevoEmp.PasswordHash = hasher.HashPassword(nuevoEmp, "empleado123");
        repoUsuario.Alta(nuevoEmp);
    }
    else if (empleado.PasswordHash == "pendiente-definir-hash")
    {
        repoUsuario.ActualizarClave(empleado.Id, hasher.HashPassword(empleado, "empleado123"));
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
    logger.LogWarning(ex, "No se pudo sembrar usuarios iniciales automaticamente (la BD puede requerir conexion previa).");
}

app.Run();

