using Odentra.Data.Entities;
using Odentra.Data.Repositories;
using Odentra.Services.Auditoria.DTOs;
using Odentra.Services.Autenticacion;

namespace Odentra.Services.Auditoria;

public sealed class AuditoriaService(
    IAuditoriaRepository repository,
    ICurrentUserService currentUserService) : IAuditoriaService
{
    private const int MaximoRegistrosExportacion = 50000;

    public async Task RegistrarAsync(
        string accion,
        string modulo,
        string entidad,
        int? entidadId,
        string descripcion,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = currentUserService.UsuarioId;
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            // Sin usuario identificado no se puede registrar (la FK UsuarioId es obligatoria).
            return;
        }

        await repository.AddAsync(new RegistroAuditoria
        {
            UsuarioId = usuarioId,
            FechaHora = DateTime.UtcNow,
            Accion = accion,
            Modulo = modulo,
            Entidad = entidad,
            EntidadId = entidadId,
            Descripcion = descripcion
        }, cancellationToken);
    }

    public async Task RegistrarEventoSesionAsync(
        string usuarioId,
        string accion,
        string descripcion,
        string? direccionIp = null,
        string? agenteUsuario = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return;
        }

        await repository.AddAsync(new RegistroAuditoria
        {
            UsuarioId = usuarioId,
            FechaHora = DateTime.UtcNow,
            Accion = accion,
            Modulo = "Sistema",
            Entidad = "Sesión",
            EntidadId = null,
            Descripcion = descripcion,
            DireccionIp = Truncar(direccionIp, 45),
            AgenteUsuario = Truncar(agenteUsuario, 512)
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<ActividadRecienteDto>> GetRecientesAsync(
        int cantidad = 20,
        CancellationToken cancellationToken = default)
    {
        var registros = await repository.GetRecientesAsync(cantidad, cancellationToken);

        return registros
            .Select(registro => new ActividadRecienteDto(
                registro.Id,
                ObtenerNombreActor(registro.Usuario),
                registro.Accion,
                registro.Modulo,
                registro.Entidad,
                registro.EntidadId,
                registro.Descripcion,
                registro.FechaHora))
            .ToList();
    }

    public async Task<AuditoriaPaginaDto> BuscarAsync(
        AuditoriaFiltroDto filtro,
        int pagina = 1,
        int tamanoPagina = 12,
        CancellationToken cancellationToken = default)
    {
        var query = ConstruirQuery(filtro);
        var paginaNormalizada = Math.Max(1, pagina);
        var tamanoNormalizado = Math.Clamp(tamanoPagina, 1, 100);

        var data = await repository.SearchAsync(
            query,
            paginaNormalizada,
            tamanoNormalizado,
            cancellationToken);

        return new AuditoriaPaginaDto(
            data.Items.Select(ToDto).ToList(),
            data.Total,
            data.Pagina,
            data.TamanoPagina);
    }

    public async Task<AuditoriaRegistroDto?> GetDetalleAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var registro = await repository.GetByIdAsync(id, cancellationToken);
        return registro is null ? null : ToDto(registro);
    }

    public async Task<AuditoriaFiltrosDto> GetFiltrosAsync(
        CancellationToken cancellationToken = default)
    {
        var usuarios = await repository.GetUsuariosConActividadAsync(cancellationToken);
        var modulos = await repository.GetModulosAsync(cancellationToken);
        var entidades = await repository.GetEntidadesAsync(cancellationToken);

        var usuariosDto = usuarios
            .Select(usuario => new AuditoriaOpcionDto(usuario.Id, ObtenerNombreActor(usuario)))
            .OrderBy(opcion => opcion.Texto, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var accionesDto = AuditoriaAcciones.Conocidas
            .Select(accion => new AuditoriaOpcionDto(accion, accion))
            .ToList();

        var modulosDto = AuditoriaModulos.Conocidos
            .Concat(modulos.Except(AuditoriaModulos.Conocidos, StringComparer.Ordinal))
            .Select(modulo => new AuditoriaOpcionDto(modulo, modulo))
            .ToList();

        var entidadesDto = AuditoriaEtiquetas.Conocidas
            .Concat(entidades.Except(AuditoriaEtiquetas.Conocidas, StringComparer.Ordinal))
            .Select(entidad => new AuditoriaOpcionDto(entidad, AuditoriaEtiquetas.Entidad(entidad)))
            .ToList();

        return new AuditoriaFiltrosDto(usuariosDto, accionesDto, modulosDto, entidadesDto);
    }

    public async Task<AuditoriaResumenDto> GetResumenAsync(
        CancellationToken cancellationToken = default)
    {
        // "Hoy" y "últimos 7 días" son días de calendario locales. Se convierten
        // a UTC porque la columna FechaHora guarda la hora universal.
        var hoyLocal = DateTime.Today;
        var hoyInicioUtc = hoyLocal.ToUniversalTime();
        var mananaInicioUtc = hoyLocal.AddDays(1).ToUniversalTime();
        var haceSieteDiasUtc = hoyLocal.AddDays(-6).ToUniversalTime();

        var resumen = await repository.GetResumenAsync(
            hoyInicioUtc,
            mananaInicioUtc,
            haceSieteDiasUtc,
            cancellationToken);

        return new AuditoriaResumenDto(
            resumen.Total,
            resumen.Hoy,
            resumen.UltimosSieteDias,
            resumen.UsuariosDistintos,
            resumen.ModuloMasActivo,
            resumen.EventosModuloMasActivo);
    }

    public async Task<IReadOnlyList<AuditoriaRegistroDto>> GetParaExportarAsync(
        AuditoriaFiltroDto filtro,
        int maximo = 10000,
        CancellationToken cancellationToken = default)
    {
        var query = ConstruirQuery(filtro);
        var limite = Math.Clamp(maximo, 1, MaximoRegistrosExportacion);

        var data = await repository.SearchAsync(query, 1, limite, cancellationToken);
        return data.Items.Select(ToDto).ToList();
    }

    private static AuditoriaQuery ConstruirQuery(AuditoriaFiltroDto filtro)
    {
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue &&
            filtro.Desde.Value.Date > filtro.Hasta.Value.Date)
        {
            throw new InvalidOperationException("La fecha inicial no puede ser posterior a la fecha final.");
        }

        var desdeUtc = filtro.Desde?.Date.ToUniversalTime();
        var hastaUtc = filtro.Hasta?.Date.AddDays(1).ToUniversalTime();

        return new AuditoriaQuery(
            Normalizar(filtro.Texto),
            Normalizar(filtro.UsuarioId),
            Normalizar(filtro.Accion),
            Normalizar(filtro.Modulo),
            Normalizar(filtro.Entidad),
            desdeUtc,
            hastaUtc);
    }

    private static AuditoriaRegistroDto ToDto(RegistroAuditoria registro) => new(
        registro.Id,
        registro.FechaHora,
        ObtenerNombreActor(registro.Usuario),
        registro.Usuario?.Email,
        registro.Accion,
        registro.Modulo,
        registro.Entidad,
        AuditoriaEtiquetas.Entidad(registro.Entidad),
        registro.EntidadId,
        registro.Descripcion,
        registro.DireccionIp,
        registro.AgenteUsuario,
        AuditoriaAcciones.ObtenerNivel(registro.Accion));

    private static string ObtenerNombreActor(Usuario? usuario)
    {
        if (usuario is null)
        {
            return "Sistema";
        }

        if (usuario.Odontologo is not null)
        {
            var nombreCompleto = $"{usuario.Odontologo.Nombres} {usuario.Odontologo.Apellidos}".Trim();
            if (!string.IsNullOrWhiteSpace(nombreCompleto))
            {
                return nombreCompleto;
            }
        }

        if (!string.IsNullOrWhiteSpace(usuario.Nombre))
        {
            return usuario.Nombre;
        }

        return string.IsNullOrWhiteSpace(usuario.Email) ? "Sistema" : usuario.Email;
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? Truncar(string? valor, int maximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var limpio = valor.Trim();
        return limpio.Length <= maximo ? limpio : limpio[..maximo];
    }
}
