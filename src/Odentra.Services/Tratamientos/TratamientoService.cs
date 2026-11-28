using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Odentra.Data;
using Odentra.Data.Entities;
using Odentra.Data.Repositories;
using Odentra.Services.Auditoria;
using Odentra.Services.Autenticacion;
using Odentra.Services.Autorizacion;
using Odentra.Services.Pagos;
using Odentra.Services.Tratamientos.DTOs;

namespace Odentra.Services.Tratamientos;

/// <summary>
/// Lógica de negocio del módulo de Tratamientos.
/// Recibe el repositorio y el DbContext por inyección de dependencias (constructor primario).
/// Su trabajo es validar reglas, convertir entidades a DTOs y coordinar guardados y auditoría.
/// </summary>
public sealed class TratamientoService(
    ITratamientoRepository repository,
    ApplicationDbContext dbContext,
    IAuditoriaService auditoriaService,
    ICurrentUserService currentUserService,
    UserManager<Usuario> userManager) : ITratamientoService
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    // =====================================================================
    // Catálogo
    // =====================================================================

    /// <summary>
    /// Busca en el catálogo con paginación.
    /// Normaliza la página para que nunca llegue un valor inválido al repositorio.
    /// </summary>
    public async Task<PaginaResultadoDto<TratamientoListItemDto>> SearchCatalogoAsync(
        string? searchTerm,
        EstadoRegistro? estado,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken = default)
    {
        var (paginaNormalizada, tamano, skip) = NormalizarPaginacion(pagina, tamanoPagina);

        var (items, total) = await repository.SearchAsync(
            searchTerm,
            estado,
            skip,
            tamano,
            cancellationToken);

        return new PaginaResultadoDto<TratamientoListItemDto>(
            items.Select(ToListItem).ToList(),
            total,
            paginaNormalizada,
            tamano);
    }

    /// <summary>
    /// Carga un tratamiento en el DTO editable del formulario.
    /// Si el Id no existe devuelve null para que la UI muestre el mensaje adecuado.
    /// </summary>
    public async Task<TratamientoFormDto?> GetFormAsync(int id, CancellationToken cancellationToken = default)
    {
        var tratamiento = await repository.GetByIdAsync(id, cancellationToken);
        return tratamiento is null
            ? null
            : new TratamientoFormDto
            {
                Id = tratamiento.Id,
                Nombre = tratamiento.Nombre,
                Descripcion = tratamiento.Descripcion,
                PrecioBase = tratamiento.PrecioBase,
                Estado = tratamiento.Estado
            };
    }

    /// <summary>
    /// Crea un tratamiento del catálogo.
    /// Reglas: datos válidos, nombre sin espacios extremos, mínimo 3 caracteres y no duplicado.
    /// </summary>
    public async Task<int> CreateAsync(TratamientoFormDto model, CancellationToken cancellationToken = default)
    {
        Validar(model);

        var nombre = model.Nombre.Trim();
        EnsureNombreValido(nombre);

        if (await repository.ExistsNombreAsync(nombre, null, cancellationToken))
        {
            throw new InvalidOperationException("Ya existe un tratamiento con ese nombre.");
        }

        var tratamiento = new Tratamiento
        {
            Nombre = nombre,
            Descripcion = Normalizar(model.Descripcion),
            PrecioBase = model.PrecioBase,
            Estado = model.Estado
        };

        await ExecuteInTransactionAsync(async () =>
        {
            await repository.AddAsync(tratamiento, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            await auditoriaService.RegistrarAsync(
                "Creó",
                "Tratamientos",
                nameof(Tratamiento),
                tratamiento.Id,
                $"Creó el tratamiento '{tratamiento.Nombre}' con precio base {Money(tratamiento.PrecioBase)}.",
                cancellationToken);
        }, cancellationToken);

        return tratamiento.Id;
    }

    /// <summary>
    /// Actualiza un tratamiento existente.
    /// El repositorio copia los campos sobre la instancia rastreada y avisa si no existe.
    /// </summary>
    public async Task UpdateAsync(TratamientoFormDto model, CancellationToken cancellationToken = default)
    {
        Validar(model);

        var nombre = model.Nombre.Trim();
        EnsureNombreValido(nombre);

        if (await repository.ExistsNombreAsync(nombre, model.Id, cancellationToken))
        {
            throw new InvalidOperationException("Ya existe un tratamiento con ese nombre.");
        }

        var anterior = await repository.GetByIdAsync(model.Id, cancellationToken)
            ?? throw new InvalidOperationException("El tratamiento no existe.");

        var estadoAnterior = anterior.Estado;

        var actualizado = await repository.UpdateAsync(
            new Tratamiento
            {
                Id = model.Id,
                Nombre = nombre,
                Descripcion = Normalizar(model.Descripcion),
                PrecioBase = model.PrecioBase,
                Estado = model.Estado
            },
            cancellationToken);

        if (!actualizado)
        {
            throw new InvalidOperationException("El tratamiento no existe.");
        }

        var verbo = estadoAnterior == model.Estado
            ? "Modificó"
            : model.Estado == EstadoRegistro.Activo ? "Activó" : "Desactivó";

        var descripcion = verbo == "Modificó"
            ? $"Modificó el tratamiento '{nombre}' (precio base {Money(model.PrecioBase)})."
            : $"{verbo} el tratamiento '{nombre}'.";

        await ExecuteInTransactionAsync(async () =>
        {
            await repository.SaveChangesAsync(cancellationToken);

            await auditoriaService.RegistrarAsync(
                verbo,
                "Tratamientos",
                nameof(Tratamiento),
                model.Id,
                descripcion,
                cancellationToken);
        }, cancellationToken);
    }

    // =====================================================================
    // Tratamientos realizados
    // =====================================================================

    /// <summary>
    /// Busca tratamientos realizados con paginación y los proyecta a DTOs,
    /// incluyendo el estado financiero calculado a partir de los pagos.
    /// </summary>
    public async Task<PaginaResultadoDto<TratamientoRealizadoListItemDto>> SearchRealizadosAsync(
        string? searchTerm,
        EstadoTratamiento? estado,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken = default)
    {
        var (paginaNormalizada, tamano, skip) = NormalizarPaginacion(pagina, tamanoPagina);

        var (items, total) = await repository.SearchRealizadosAsync(
            searchTerm,
            estado,
            skip,
            tamano,
            cancellationToken);

        return new PaginaResultadoDto<TratamientoRealizadoListItemDto>(
            items.Select(ToRealizadoListItem).ToList(),
            total,
            paginaNormalizada,
            tamano);
    }

    /// <summary>
    /// Detalle completo de un tratamiento realizado, con los totales financieros.
    /// Devuelve null si no existe.
    /// </summary>
    public async Task<TratamientoRealizadoDetalleDto?> GetRealizadoAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        var realizado = await repository.GetRealizadoAsync(id, cancellationToken);
        if (realizado is null)
        {
            return null;
        }

        var pagado = realizado.Pagos.Sum(pago => pago.Monto);
        var saldo = realizado.Precio - pagado;

        return new TratamientoRealizadoDetalleDto(
            realizado.Id,
            realizado.PacienteId,
            NombreCompleto(realizado.Paciente),
            realizado.Tratamiento.Nombre,
            NombreCompleto(realizado.Odontologo),
            realizado.Fecha,
            realizado.PiezaDental is null
                ? null
                : $"{realizado.PiezaDental.NumeroFDI} — {realizado.PiezaDental.Nombre}",
            realizado.Precio,
            realizado.Estado,
            realizado.Observaciones,
            pagado,
            saldo,
            CalcularEstadoPago(pagado, saldo),
            realizado.Pagos.Count);
    }

    /// <summary>
    /// Carga un tratamiento realizado en el DTO del formulario de edición.
    /// Devuelve null si no existe.
    /// </summary>
    public async Task<TratamientoRealizadoFormDto?> GetRealizadoFormAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        var realizado = await repository.GetRealizadoAsync(id, cancellationToken);
        if (realizado is null)
        {
            return null;
        }

        return new TratamientoRealizadoFormDto
        {
            Id = realizado.Id,
            PacienteId = realizado.PacienteId,
            TratamientoId = realizado.TratamientoId,
            OdontologoId = realizado.OdontologoId,
            PiezaDentalId = realizado.PiezaDentalId,
            ConsultaId = realizado.ConsultaId,
            Fecha = realizado.Fecha.ToLocalTime().Date,
            Precio = realizado.Precio,
            Estado = realizado.Estado,
            Observaciones = realizado.Observaciones,
            TienePagos = realizado.Pagos.Count > 0
        };
    }

    /// <summary>
    /// Registra un tratamiento realizado para un paciente.
    /// El estado inicial siempre es En proceso; se completa o cancela con acciones explícitas.
    /// </summary>
    public async Task<int> CreateRealizadoAsync(
        TratamientoRealizadoFormDto model,
        CancellationToken cancellationToken = default)
    {
        Validar(model);
        EnsureFechaValida(model.Fecha);

        var paciente = await repository.GetPacienteAsync(model.PacienteId, cancellationToken)
            ?? throw new InvalidOperationException("El paciente seleccionado no existe.");

        if (paciente.Estado != EstadoRegistro.Activo)
        {
            throw new InvalidOperationException("El paciente seleccionado está inactivo.");
        }

        var odontologo = await repository.GetOdontologoAsync(model.OdontologoId, cancellationToken)
            ?? throw new InvalidOperationException("El odontólogo seleccionado no existe.");

        if (odontologo.Estado != EstadoRegistro.Activo)
        {
            throw new InvalidOperationException("El odontólogo seleccionado está inactivo.");
        }

        var tratamiento = await repository.GetByIdAsync(model.TratamientoId, cancellationToken)
            ?? throw new InvalidOperationException("El tratamiento seleccionado no existe.");

        if (tratamiento.Estado != EstadoRegistro.Activo)
        {
            throw new InvalidOperationException("El tratamiento seleccionado está inactivo y no puede usarse en nuevos registros.");
        }

        if (model.PiezaDentalId.HasValue)
        {
            await EnsurePiezaExisteAsync(model.PiezaDentalId.Value, cancellationToken);
        }

        await EnsureConsultaValidaAsync(model.ConsultaId, model.PacienteId, cancellationToken);

        var realizado = new TratamientoRealizado
        {
            PacienteId = model.PacienteId,
            TratamientoId = model.TratamientoId,
            OdontologoId = model.OdontologoId,
            PiezaDentalId = model.PiezaDentalId,
            ConsultaId = model.ConsultaId,
            Fecha = ToUtcDate(model.Fecha),
            Precio = model.Precio,
            Estado = EstadoTratamiento.EnProceso,
            Observaciones = Normalizar(model.Observaciones)
        };

        await ExecuteInTransactionAsync(async () =>
        {
            await repository.AddRealizadoAsync(realizado, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            await auditoriaService.RegistrarAsync(
                "Registró",
                "Tratamientos",
                nameof(TratamientoRealizado),
                realizado.Id,
                $"Registró el tratamiento '{tratamiento.Nombre}' para {NombreCompleto(paciente)} por {Money(realizado.Precio)}.",
                cancellationToken);
        }, cancellationToken);

        return realizado.Id;
    }

    /// <summary>
    /// Actualiza un tratamiento realizado.
    /// No se edita un cancelado, el precio no puede quedar por debajo de lo pagado
    /// y el estado clínico no se cambia desde este formulario.
    /// </summary>
    public async Task UpdateRealizadoAsync(
        TratamientoRealizadoFormDto model,
        CancellationToken cancellationToken = default)
    {
        Validar(model);

        if (model.Id <= 0)
        {
            throw new InvalidOperationException("El tratamiento realizado no existe.");
        }

        EnsureFechaValida(model.Fecha);

        var realizado = await repository.GetRealizadoForUpdateAsync(model.Id, cancellationToken)
            ?? throw new InvalidOperationException("El tratamiento realizado no existe.");

        if (realizado.Estado == EstadoTratamiento.Cancelado)
        {
            throw new InvalidOperationException("Un tratamiento cancelado no se puede editar. Solo se puede consultar.");
        }

        if (realizado.Pagos.Count > 0 && !await EsAdministradorAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "Este tratamiento tiene pagos registrados. Solo el Administrador puede modificarlo.");
        }

        // Los pagos pertenecen al paciente: con pagos registrados el paciente nunca cambia.
        if (realizado.Pagos.Count > 0 && model.PacienteId != realizado.PacienteId)
        {
            throw new InvalidOperationException(
                "No se puede cambiar el paciente de un tratamiento que ya tiene pagos registrados.");
        }

        var paciente = await repository.GetPacienteAsync(model.PacienteId, cancellationToken)
            ?? throw new InvalidOperationException("El paciente seleccionado no existe.");

        var odontologo = await repository.GetOdontologoAsync(model.OdontologoId, cancellationToken)
            ?? throw new InvalidOperationException("El odontólogo seleccionado no existe.");

        var tratamiento = await repository.GetByIdAsync(model.TratamientoId, cancellationToken)
            ?? throw new InvalidOperationException("El tratamiento seleccionado no existe.");

        var cambiaPaciente = realizado.PacienteId != model.PacienteId;
        var cambiaOdontologo = realizado.OdontologoId != model.OdontologoId;
        var cambiaTratamiento = realizado.TratamientoId != model.TratamientoId;

        if (cambiaPaciente && paciente.Estado != EstadoRegistro.Activo)
        {
            throw new InvalidOperationException("El paciente seleccionado está inactivo.");
        }

        if (cambiaOdontologo && odontologo.Estado != EstadoRegistro.Activo)
        {
            throw new InvalidOperationException("El odontólogo seleccionado está inactivo.");
        }

        if (cambiaTratamiento && tratamiento.Estado != EstadoRegistro.Activo)
        {
            throw new InvalidOperationException("El tratamiento seleccionado está inactivo.");
        }

        if (model.PiezaDentalId.HasValue)
        {
            await EnsurePiezaExisteAsync(model.PiezaDentalId.Value, cancellationToken);
        }

        await EnsureConsultaValidaAsync(model.ConsultaId, model.PacienteId, cancellationToken);

        var pagado = realizado.Pagos.Sum(pago => pago.Monto);
        if (model.Precio < pagado)
        {
            throw new InvalidOperationException(
                $"El precio no puede ser menor que lo ya pagado ({Money(pagado)}).");
        }

        realizado.PacienteId = model.PacienteId;
        realizado.TratamientoId = model.TratamientoId;
        realizado.OdontologoId = model.OdontologoId;
        realizado.PiezaDentalId = model.PiezaDentalId;
        realizado.ConsultaId = model.ConsultaId;
        realizado.Fecha = ToUtcDate(model.Fecha);
        realizado.Precio = model.Precio;
        realizado.Observaciones = Normalizar(model.Observaciones);

        await ExecuteInTransactionAsync(async () =>
        {
            await repository.SaveChangesAsync(cancellationToken);

            await auditoriaService.RegistrarAsync(
                "Modificó",
                "Tratamientos",
                nameof(TratamientoRealizado),
                realizado.Id,
                $"Modificó el tratamiento '{tratamiento.Nombre}' de {NombreCompleto(paciente)} (precio {Money(realizado.Precio)}).",
                cancellationToken);
        }, cancellationToken);
    }

    /// <summary>
    /// Cancela un tratamiento realizado. No se permite si está completado o si tiene pagos.
    /// </summary>
    public async Task CancelarRealizadoAsync(int id, CancellationToken cancellationToken = default)
    {
        var realizado = await repository.GetRealizadoForUpdateAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("El tratamiento realizado no existe.");

        if (realizado.Estado == EstadoTratamiento.Cancelado)
        {
            throw new InvalidOperationException("El tratamiento ya está cancelado.");
        }

        if (realizado.Estado == EstadoTratamiento.Completado)
        {
            throw new InvalidOperationException("Un tratamiento completado no se puede cancelar.");
        }

        if (realizado.Pagos.Count > 0)
        {
            throw new InvalidOperationException("No se puede cancelar un tratamiento que tiene pagos registrados.");
        }

        realizado.Estado = EstadoTratamiento.Cancelado;

        await ExecuteInTransactionAsync(async () =>
        {
            await repository.SaveChangesAsync(cancellationToken);

            await auditoriaService.RegistrarAsync(
                "Canceló",
                "Tratamientos",
                nameof(TratamientoRealizado),
                realizado.Id,
                $"Canceló el tratamiento '{realizado.Tratamiento.Nombre}' de {NombreCompleto(realizado.Paciente)}.",
                cancellationToken);
        }, cancellationToken);
    }

    /// <summary>Marca como completado un tratamiento que está en proceso.</summary>
    public async Task MarcarCompletadoAsync(int id, CancellationToken cancellationToken = default)
    {
        var realizado = await repository.GetRealizadoForUpdateAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("El tratamiento realizado no existe.");

        if (realizado.Estado != EstadoTratamiento.EnProceso)
        {
            throw new InvalidOperationException("Solo un tratamiento en proceso se puede marcar como completado.");
        }

        realizado.Estado = EstadoTratamiento.Completado;
        await GuardarCambioEstadoAsync(realizado, "Marcó como completado", cancellationToken);
    }

    /// <summary>Vuelve a poner en proceso un tratamiento completado.</summary>
    public async Task ReabrirRealizadoAsync(int id, CancellationToken cancellationToken = default)
    {
        var realizado = await repository.GetRealizadoForUpdateAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("El tratamiento realizado no existe.");

        if (realizado.Estado != EstadoTratamiento.Completado)
        {
            throw new InvalidOperationException("Solo un tratamiento completado se puede reabrir.");
        }

        realizado.Estado = EstadoTratamiento.EnProceso;
        await GuardarCambioEstadoAsync(realizado, "Reabrió", cancellationToken);
    }

    /// <summary>
    /// Cambia el estado clínico usando la acción correspondiente.
    /// Cualquier estado distinto de Cancelado o Completado equivale a reabrir.
    /// </summary>
    public Task SetEstadoRealizadoAsync(
        int id,
        EstadoTratamiento estado,
        CancellationToken cancellationToken = default) => estado switch
        {
            EstadoTratamiento.Cancelado => CancelarRealizadoAsync(id, cancellationToken),
            EstadoTratamiento.Completado => MarcarCompletadoAsync(id, cancellationToken),
            _ => ReabrirRealizadoAsync(id, cancellationToken)
        };

    // =====================================================================
    // Opciones de los formularios
    // =====================================================================

    /// <summary>Consultas del paciente para asociarlas opcionalmente al tratamiento.</summary>
    public async Task<IReadOnlyList<ConsultaOptionDto>> GetConsultasPacienteAsync(
        int pacienteId,
        CancellationToken cancellationToken = default)
    {
        if (pacienteId <= 0)
        {
            return [];
        }

        var consultas = await repository.GetConsultasPacienteAsync(pacienteId, cancellationToken);

        return consultas
            .Select(consulta => new ConsultaOptionDto(
                consulta.Id,
                consulta.Fecha,
                string.IsNullOrWhiteSpace(consulta.Motivo) ? "Sin motivo" : consulta.Motivo))
            .ToList();
    }

    /// <summary>Tratamientos del catálogo, activos o todos, para el selector del formulario.</summary>
    public async Task<IReadOnlyList<TratamientoOptionDto>> GetTratamientosAsync(
        bool soloActivos,
        CancellationToken cancellationToken = default)
    {
        var tratamientos = await repository.GetTratamientosAsync(soloActivos, cancellationToken);

        return tratamientos
            .Select(tratamiento => new TratamientoOptionDto(
                tratamiento.Id,
                tratamiento.Nombre,
                tratamiento.PrecioBase,
                tratamiento.Estado == EstadoRegistro.Activo))
            .ToList();
    }

    /// <summary>Pacientes para el selector del formulario (incluye inactivos para edición).</summary>
    public async Task<IReadOnlyList<PacienteOptionDto>> GetPacientesAsync(
        CancellationToken cancellationToken = default)
    {
        var pacientes = await repository.GetPacientesAsync(cancellationToken);

        return pacientes
            .Select(paciente => new PacienteOptionDto(
                paciente.Id,
                NombreCompleto(paciente),
                paciente.Estado == EstadoRegistro.Activo))
            .ToList();
    }

    /// <summary>Odontólogos para el selector del formulario (incluye inactivos para edición).</summary>
    public async Task<IReadOnlyList<OdontologoOptionDto>> GetOdontologosAsync(
        CancellationToken cancellationToken = default)
    {
        var odontologos = await repository.GetOdontologosAsync(cancellationToken);

        return odontologos
            .Select(odontologo => new OdontologoOptionDto(
                odontologo.Id,
                NombreCompleto(odontologo),
                odontologo.Estado == EstadoRegistro.Activo))
            .ToList();
    }

    /// <summary>Piezas dentales para el selector del formulario.</summary>
    public async Task<IReadOnlyList<PiezaDentalOptionDto>> GetPiezasAsync(
        CancellationToken cancellationToken = default)
    {
        var piezas = await repository.GetPiezasAsync(cancellationToken);

        return piezas
            .Select(pieza => new PiezaDentalOptionDto(pieza.Id, pieza.NumeroFDI, pieza.Nombre))
            .ToList();
    }

    // =====================================================================
    // Validaciones y ayudantes
    // =====================================================================

    private async Task GuardarCambioEstadoAsync(
        TratamientoRealizado realizado,
        string accion,
        CancellationToken cancellationToken)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            await repository.SaveChangesAsync(cancellationToken);

            await auditoriaService.RegistrarAsync(
                accion,
                "Tratamientos",
                nameof(TratamientoRealizado),
                realizado.Id,
                $"{accion} el tratamiento '{realizado.Tratamiento.Nombre}' de {NombreCompleto(realizado.Paciente)}.",
                cancellationToken);
        }, cancellationToken);
    }

    private async Task<bool> EsAdministradorAsync(CancellationToken cancellationToken)
    {
        var usuarioId = currentUserService.UsuarioId;
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return false;
        }

        var usuario = await userManager.FindByIdAsync(usuarioId);
        return usuario is not null
            && await userManager.IsInRoleAsync(usuario, RolesOdentra.Administrador);
    }

    private async Task EnsureConsultaValidaAsync(
        int? consultaId,
        int pacienteId,
        CancellationToken cancellationToken)
    {
        if (!consultaId.HasValue)
        {
            return;
        }

        var consultas = await repository.GetConsultasPacienteAsync(pacienteId, cancellationToken);
        if (consultas.All(consulta => consulta.Id != consultaId.Value))
        {
            throw new InvalidOperationException("La consulta seleccionada no pertenece al paciente.");
        }
    }

    private async Task EnsurePiezaExisteAsync(int piezaId, CancellationToken cancellationToken)
    {
        if (!await repository.PiezaExisteAsync(piezaId, cancellationToken))
        {
            throw new InvalidOperationException("La pieza dental seleccionada no existe.");
        }
    }

    private static void EnsureFechaValida(DateTime fecha)
    {
        if (fecha == default)
        {
            throw new InvalidOperationException("La fecha del tratamiento es obligatoria.");
        }

        if (fecha.Date > DateTime.Today)
        {
            throw new InvalidOperationException("La fecha del tratamiento no puede ser futura.");
        }
    }

    private async Task ExecuteInTransactionAsync(
        Func<Task> operacion,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await operacion();
        await transaction.CommitAsync(cancellationToken);
    }

    private static (int Pagina, int Tamano, int Skip) NormalizarPaginacion(int pagina, int tamanoPagina)
    {
        var paginaNormalizada = Math.Max(1, pagina);
        var tamano = Math.Clamp(tamanoPagina, 1, 100);
        return (paginaNormalizada, tamano, (paginaNormalizada - 1) * tamano);
    }

    private static void Validar(object model)
    {
        var resultados = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                resultados,
                validateAllProperties: true))
        {
            var mensaje = string.Join(" ", resultados.Select(resultado => resultado.ErrorMessage));
            throw new InvalidOperationException(mensaje);
        }
    }

    private static void EnsureNombreValido(string nombre)
    {
        if (nombre.Length < 3)
        {
            throw new InvalidOperationException("El nombre debe tener al menos 3 caracteres.");
        }

        if (nombre.Length > 150)
        {
            throw new InvalidOperationException("El nombre no puede superar los 150 caracteres.");
        }
    }

    private static string? Normalizar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime ToUtcDate(DateTime fecha) =>
        DateTime.SpecifyKind(fecha.Date, DateTimeKind.Local).ToUniversalTime();

    private static string NombreCompleto(Paciente paciente) =>
        $"{paciente.Nombres} {paciente.Apellidos}".Trim();

    private static string NombreCompleto(Odontologo odontologo) =>
        $"{odontologo.Nombres} {odontologo.Apellidos}".Trim();

    private static string Money(decimal value) =>
        $"RD$ {value.ToString("N2", Cultura)}";

    private static EstadoFinanciero CalcularEstadoPago(decimal pagado, decimal saldo)
    {
        if (saldo <= 0)
        {
            return EstadoFinanciero.Pagado;
        }

        return pagado > 0 ? EstadoFinanciero.PagoParcial : EstadoFinanciero.Pendiente;
    }

    private static TratamientoListItemDto ToListItem(Tratamiento tratamiento) =>
        new(
            tratamiento.Id,
            tratamiento.Nombre,
            tratamiento.Descripcion,
            tratamiento.PrecioBase,
            tratamiento.Estado);

    private static TratamientoRealizadoListItemDto ToRealizadoListItem(TratamientoRealizado realizado)
    {
        var pagado = realizado.Pagos.Sum(pago => pago.Monto);
        var saldo = realizado.Precio - pagado;

        return new(
            realizado.Id,
            realizado.PacienteId,
            NombreCompleto(realizado.Paciente),
            realizado.Tratamiento.Nombre,
            NombreCompleto(realizado.Odontologo),
            realizado.Fecha,
            realizado.PiezaDental?.NumeroFDI,
            realizado.Precio,
            pagado,
            saldo,
            CalcularEstadoPago(pagado, saldo),
            realizado.Estado);
    }
}
