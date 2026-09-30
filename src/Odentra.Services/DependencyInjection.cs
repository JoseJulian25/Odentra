using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Odentra.Data;
using Odentra.Data.Entities;
using Odentra.Data.Repositories;
using Odentra.Services.Pacientes;
using Odentra.Services.Odontologos;
using Odentra.Services.Usuarios;
//using Odentra.Services.Dashboard;
//using Odentra.Services.Citas;

namespace Odentra.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddOdentraServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuro DefaultConnection.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddIdentityCookies(options =>
        {
            options.ApplicationCookie?.Configure(cookieOptions =>
            {
                cookieOptions.LoginPath = "/login";
                cookieOptions.AccessDeniedPath = "/login?error=forbidden";
            });
        });

        services.AddIdentityCore<Usuario>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddRoles<Rol>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IPacienteService, PacienteService>();
        services.AddScoped<IOdontologoRepository, OdontologoRepository>();
        services.AddScoped<IOdontologoService, OdontologoService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        //services.AddScoped<IDashboardService, DashboardService>();
        //services.AddScoped<ICitaRepository, CitaRepository>();
        //services.AddScoped<ICitaService, CitaService>();

        return services;
    }
}