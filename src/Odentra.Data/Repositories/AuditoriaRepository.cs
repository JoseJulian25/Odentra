using Microsoft.EntityFrameworkCore;
using Odentra.Data.Entities;

namespace Odentra.Data.Repositories;

public sealed class AuditoriaRepository(ApplicationDbContext dbContext) : IAuditoriaRepository
{
    public async Task AddAsync(RegistroAuditoria registro, CancellationToken cancellationToken = default)
    {
        await dbContext.RegistrosAuditoria.AddAsync(registro, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RegistroAuditoria>> GetRecientesAsync(int cantidad, CancellationToken cancellationToken = default)
    {
        return await dbContext.RegistrosAuditoria
            .AsNoTracking()
            .Include(registro => registro.Usuario)
                .ThenInclude(usuario => usuario!.Odontologo)
            .OrderByDescending(registro => registro.FechaHora)
            .ThenByDescending(registro => registro.Id)
            .Take(cantidad)
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditoriaPaginaData> SearchAsync(
        AuditoriaQuery query,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken = default)
    {
        var consulta = ConstruirConsulta(query);

        var total = await consulta.CountAsync(cancellationToken);
        var totalPaginas = (int)Math.Ceiling(total / (double)tamanoPagina);
        var paginaActual = totalPaginas == 0 ? 1 : Math.Clamp(pagina, 1, totalPaginas);

        var items = await consulta
            .Include(registro => registro.Usuario)
                .ThenInclude(usuario => usuario!.Odontologo)
            .OrderByDescending(registro => registro.FechaHora)
            .ThenByDescending(registro => registro.Id)
            .Skip((paginaActual - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(cancellationToken);

        return new AuditoriaPaginaData(items, total, paginaActual, tamanoPagina);
    }

    public Task<RegistroAuditoria?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        dbContext.RegistrosAuditoria
            .AsNoTracking()
            .Include(registro => registro.Usuario)
                .ThenInclude(usuario => usuario!.Odontologo)
            .FirstOrDefaultAsync(registro => registro.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Usuario>> GetUsuariosConActividadAsync(CancellationToken cancellationToken = default)
    {
        var usuarioIds = await dbContext.RegistrosAuditoria
            .AsNoTracking()
            .Select(registro => registro.UsuarioId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await dbContext.Users
            .AsNoTracking()
            .Include(usuario => usuario.Odontologo)
            .Where(usuario => usuarioIds.Contains(usuario.Id))
            .OrderBy(usuario => usuario.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetModulosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.RegistrosAuditoria
            .AsNoTracking()
            .Select(registro => registro.Modulo)
            .Distinct()
            .OrderBy(modulo => modulo)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetAccionesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.RegistrosAuditoria
            .AsNoTracking()
            .Select(registro => registro.Accion)
            .Distinct()
            .OrderBy(accion => accion)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetEntidadesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.RegistrosAuditoria
            .AsNoTracking()
            .Select(registro => registro.Entidad)
            .Distinct()
            .OrderBy(entidad => entidad)
            .ToListAsync(cancellationToken);

    public async Task<AuditoriaResumenData> GetResumenAsync(
        DateTime hoyInicioUtc,
        DateTime mananaInicioUtc,
        DateTime haceSieteDiasUtc,
        CancellationToken cancellationToken = default)
    {
        var consulta = dbContext.RegistrosAuditoria.AsNoTracking();

        var total = await consulta.CountAsync(cancellationToken);
        var hoy = await consulta.CountAsync(
            registro => registro.FechaHora >= hoyInicioUtc && registro.FechaHora < mananaInicioUtc,
            cancellationToken);
        var ultimosSieteDias = await consulta.CountAsync(
            registro => registro.FechaHora >= haceSieteDiasUtc && registro.FechaHora < mananaInicioUtc,
            cancellationToken);
        var usuariosDistintos = await consulta
            .Select(registro => registro.UsuarioId)
            .Distinct()
            .CountAsync(cancellationToken);

        var moduloMasActivo = await consulta
            .GroupBy(registro => registro.Modulo)
            .Select(grupo => new { Modulo = grupo.Key, Total = grupo.Count() })
            .OrderByDescending(item => item.Total)
            .ThenBy(item => item.Modulo)
            .FirstOrDefaultAsync(cancellationToken);

        return new AuditoriaResumenData(
            total,
            hoy,
            ultimosSieteDias,
            usuariosDistintos,
            moduloMasActivo?.Modulo,
            moduloMasActivo?.Total ?? 0);
    }

    private IQueryable<RegistroAuditoria> ConstruirConsulta(AuditoriaQuery query)
    {
        IQueryable<RegistroAuditoria> consulta = dbContext.RegistrosAuditoria.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Texto))
        {
            var termino = query.Texto.Trim();
            consulta = consulta.Where(registro =>
                (registro.Descripcion != null && registro.Descripcion.Contains(termino)) ||
                registro.Entidad.Contains(termino) ||
                registro.Accion.Contains(termino) ||
                registro.Modulo.Contains(termino) ||
                registro.Usuario.Nombre.Contains(termino) ||
                (registro.Usuario.Email != null && registro.Usuario.Email.Contains(termino)) ||
                (registro.DireccionIp != null && registro.DireccionIp.Contains(termino)));
        }

        if (!string.IsNullOrWhiteSpace(query.UsuarioId))
        {
            consulta = consulta.Where(registro => registro.UsuarioId == query.UsuarioId);
        }

        if (!string.IsNullOrWhiteSpace(query.Accion))
        {
            consulta = consulta.Where(registro => registro.Accion == query.Accion);
        }

        if (!string.IsNullOrWhiteSpace(query.Modulo))
        {
            consulta = consulta.Where(registro => registro.Modulo == query.Modulo);
        }

        if (!string.IsNullOrWhiteSpace(query.Entidad))
        {
            consulta = consulta.Where(registro => registro.Entidad == query.Entidad);
        }

        if (query.DesdeUtc.HasValue)
        {
            consulta = consulta.Where(registro => registro.FechaHora >= query.DesdeUtc.Value);
        }

        if (query.HastaUtc.HasValue)
        {
            consulta = consulta.Where(registro => registro.FechaHora < query.HastaUtc.Value);
        }

        return consulta;
    }
}
