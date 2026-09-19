using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Odentra.Data;
using Odentra.Data.Repositories;
using Odentra.Services.Pacientes;

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

        return services;
    }
}