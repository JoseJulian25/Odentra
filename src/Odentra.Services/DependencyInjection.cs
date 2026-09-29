using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Odentra.Data;
using Odentra.Data.Repositories;
using Odentra.Services.Pacientes;
using Odentra.Services.Odontologos;
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

        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IPacienteService, PacienteService>();
        services.AddScoped<IOdontologoRepository, OdontologoRepository>();
        services.AddScoped<IOdontologoService, OdontologoService>();
        //services.AddScoped<IDashboardService, DashboardService>();
        //services.AddScoped<ICitaRepository, CitaRepository>();
        //services.AddScoped<ICitaService, CitaService>();

        return services;
    }
}