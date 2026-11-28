using System.ComponentModel.DataAnnotations;
using Odentra.Data.Entities;
using Odentra.Data.Repositories;
using Odentra.Services.Tratamientos.DTOs;

namespace Odentra.Services.Tratamientos;

/// <summary>
/// Lógica de negocio del módulo de Tratamientos.
/// Recibe el repositorio por inyección de dependencias (constructor primario).
/// Su trabajo es validar reglas, convertir entidades a DTOs y coordinar guardados.
/// </summary>
public sealed class TratamientoService(ITratamientoRepository repository) : ITratamientoService
{
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
        // Calcula página válida, tamaño permitido y cuántos registros saltar.
        var (paginaNormalizada, tamano, skip) = NormalizarPaginacion(pagina, tamanoPagina);

        var (items, total) = await repository.SearchAsync(
            searchTerm,
            estado,
            skip,
            tamano,
            cancellationToken);

        // Convierte entidades a DTOs: la UI nunca recibe entidades de EF.
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
        // Valida atributos del DTO ([Required], [StringLength], [Range]).
        Validar(model);

        var nombre = model.Nombre.Trim();
        EnsureNombreValido(nombre);

        // El índice único de la BD protege igualmente, pero aquí damos un mensaje amigable.
        if (await repository.ExistsNombreAsync(nombre, null, cancellationToken))
        {
            throw new InvalidOperationException("Ya existe un tratamiento con ese nombre.");
        }

        var tratamiento = new Tratamiento
        {
            Nombre = nombre,
            // Descripción vacía se guarda como null para no almacenar espacios.
            Descripcion = Normalizar(model.Descripcion),
            PrecioBase = model.PrecioBase,
            Estado = model.Estado
        };

        await repository.AddAsync(tratamiento, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        // EF rellena el Id después del INSERT.
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

        // Excluye el propio Id para permitir guardar sin cambiar el nombre.
        if (await repository.ExistsNombreAsync(nombre, model.Id, cancellationToken))
        {
            throw new InvalidOperationException("Ya existe un tratamiento con ese nombre.");
        }

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

        await repository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Busca tratamientos realizados con paginación y los proyecta a DTOs.
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
    /// Prepara las opciones de los selectores del formulario de registro.
    /// Solo incluye pacientes y odontólogos activos, y tratamientos activos.
    /// </summary>
    public async Task<CatalogosRealizadoDto> GetCatalogosAsync(CancellationToken cancellationToken = default)
    {
        var pacientes = await repository.GetPacientesActivosAsync(cancellationToken);
        var odontologos = await repository.GetOdontologosActivosAsync(cancellationToken);
        var piezas = await repository.GetPiezasAsync(cancellationToken);
        var tratamientos = await repository.GetActivosAsync(cancellationToken);

        // Cada lista se convierte en opciones simples (Id + texto) para la UI.
        return new CatalogosRealizadoDto(
            pacientes
                .Select(paciente => new OpcionTratamientoDto(paciente.Id, $"{paciente.Nombres} {paciente.Apellidos}"))
                .ToList(),
            odontologos
                .Select(odontologo => new OpcionTratamientoDto(odontologo.Id, $"{odontologo.Nombres} {odontologo.Apellidos}"))
                .ToList(),
            piezas
                .Select(pieza => new OpcionTratamientoDto(pieza.Id, $"{pieza.NumeroFDI} · {pieza.Nombre}"))
                .ToList(),
            tratamientos
                // El precio viaja en la opción para autocompletar el formulario.
                .Select(tratamiento => new OpcionTratamientoDto(tratamiento.Id, tratamiento.Nombre, tratamiento.PrecioBase))
                .ToList());
    }

    /// <summary>
    /// Registra un tratamiento realizado para un paciente.
    /// Antes de insertar valida todas las reglas de negocio.
    /// </summary>
    public async Task<int> CreateRealizadoAsync(
        TratamientoRealizadoFormDto model,
        CancellationToken cancellationToken = default)
    {
        Validar(model);

        // Una fecha sin seleccionar llega como DateTime.MinValue.
        if (model.Fecha == default)
        {
            throw new InvalidOperationException("La fecha del tratamiento es obligatoria.");
        }

        // Regla del módulo: no se permiten fechas futuras.
        if (model.Fecha.Date > DateTime.Today)
        {
            throw new InvalidOperationException("La fecha del tratamiento no puede ser futura.");
        }

        if (!await repository.PacienteActivoExisteAsync(model.PacienteId, cancellationToken))
        {
            throw new InvalidOperationException("Seleccione un paciente activo.");
        }

        // Trae el tratamiento para comprobar que exista y siga activo.
        var tratamiento = await repository.GetByIdAsync(model.TratamientoId, cancellationToken)
            ?? throw new InvalidOperationException("El tratamiento seleccionado no existe.");

        if (tratamiento.Estado != EstadoRegistro.Activo)
        {
            throw new InvalidOperationException("El tratamiento seleccionado está inactivo y no puede usarse en nuevos registros.");
        }

        if (!await repository.OdontologoActivoExisteAsync(model.OdontologoId, cancellationToken))
        {
            throw new InvalidOperationException("Seleccione un odontólogo activo.");
        }

        // La pieza es opcional (tratamientos generales), pero si viene debe existir.
        if (model.PiezaDentalId.HasValue &&
            !await repository.PiezaExisteAsync(model.PiezaDentalId.Value, cancellationToken))
        {
            throw new InvalidOperationException("La pieza dental seleccionada no existe.");
        }

        var realizado = new TratamientoRealizado
        {
            PacienteId = model.PacienteId,
            TratamientoId = model.TratamientoId,
            OdontologoId = model.OdontologoId,
            PiezaDentalId = model.PiezaDentalId,
            // La fecha se guarda en UTC; la UI la vuelve a mostrar en hora local.
            Fecha = ToUtcDate(model.Fecha),
            Precio = model.Precio,
            Estado = model.Estado,
            Observaciones = Normalizar(model.Observaciones)
        };

        await repository.AddRealizadoAsync(realizado, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return realizado.Id;
    }

    /// <summary>
    /// Construye el DTO del detalle con los datos de las relaciones y los pagos.
    /// Los totales (pagado, saldo, estado financiero) se calculan en el DTO.
    /// </summary>
    public async Task<TratamientoDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default)
    {
        var realizado = await repository.GetRealizadoAsync(id, cancellationToken);
        if (realizado is null)
        {
            return null;
        }

        return new TratamientoDetalleDto(
            realizado.Id,
            $"{realizado.Paciente.Nombres} {realizado.Paciente.Apellidos}",
            realizado.Tratamiento.Nombre,
            $"{realizado.Odontologo.Nombres} {realizado.Odontologo.Apellidos}",
            realizado.Fecha,
            realizado.PiezaDental?.NumeroFDI,
            realizado.Precio,
            realizado.Estado,
            realizado.Observaciones,
            // Los pagos se muestran del más antiguo al más reciente.
            realizado.Pagos
                .OrderBy(pago => pago.Fecha)
                .Select(pago => new PagoTratamientoDto(pago.Fecha, pago.Monto, pago.MetodoPago))
                .ToList());
    }

    /// <summary>
    /// Cambia el estado clínico de un tratamiento realizado.
    /// Regla financiera: si ya hay pagos, no se permite cancelarlo.
    /// </summary>
    public async Task SetEstadoRealizadoAsync(
        int id,
        EstadoTratamiento estado,
        CancellationToken cancellationToken = default)
    {
        // Se carga con pagos incluidos para poder aplicar la regla de cancelación.
        var realizado = await repository.GetRealizadoAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("El tratamiento realizado no existe.");

        if (estado == EstadoTratamiento.Cancelado && realizado.Pagos.Count > 0)
        {
            throw new InvalidOperationException("No se puede cancelar un tratamiento que tiene pagos registrados.");
        }

        // El repositorio modifica la instancia rastreada y devuelve false si ya no existe.
        var actualizado = await repository.SetEstadoRealizadoAsync(id, estado, cancellationToken);

        if (!actualizado)
        {
            throw new InvalidOperationException("El tratamiento realizado no existe.");
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Asegura valores de paginación seguros:
    /// página mínima 1, tamaño entre 1 y 100, y calcula el desplazamiento (skip).
    /// </summary>
    private static (int Pagina, int Tamano, int Skip) NormalizarPaginacion(int pagina, int tamanoPagina)
    {
        var paginaNormalizada = Math.Max(1, pagina);
        var tamano = Math.Clamp(tamanoPagina, 1, 100);
        return (paginaNormalizada, tamano, (paginaNormalizada - 1) * tamano);
    }

    /// <summary>
    /// Ejecuta las validaciones declaradas con DataAnnotations en el DTO.
    /// Se hace también aquí (no solo en el formulario) para proteger llamadas directas al servicio.
    /// </summary>
    private static void Validar(object model)
    {
        var resultados = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                resultados,
                validateAllProperties: true))
        {
            // Une todos los mensajes de error en uno solo para mostrarlo en la UI.
            var mensaje = string.Join(" ", resultados.Select(resultado => resultado.ErrorMessage));
            throw new InvalidOperationException(mensaje);
        }
    }

    /// <summary>Valida la longitud del nombre ya recortado (sin espacios extremos).</summary>
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

    /// <summary>Convierte texto vacío o solo espacios en null y recorta el resto.</summary>
    private static string? Normalizar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Convierte una fecha elegida por el usuario (hora local, sin hora)
    /// a UTC para almacenarla de forma consistente.
    /// </summary>
    private static DateTime ToUtcDate(DateTime fecha) =>
        DateTime.SpecifyKind(fecha.Date, DateTimeKind.Local).ToUniversalTime();

    /// <summary>Proyección de entidad a DTO de la tabla del catálogo.</summary>
    private static TratamientoListItemDto ToListItem(Tratamiento tratamiento) =>
        new(
            tratamiento.Id,
            tratamiento.Nombre,
            tratamiento.Descripcion,
            tratamiento.PrecioBase,
            tratamiento.Estado);

    /// <summary>Proyección de entidad a DTO de la tabla de realizados.</summary>
    private static TratamientoRealizadoListItemDto ToRealizadoListItem(TratamientoRealizado realizado) =>
        new(
            realizado.Id,
            $"{realizado.Paciente.Nombres} {realizado.Paciente.Apellidos}",
            realizado.Tratamiento.Nombre,
            $"{realizado.Odontologo.Nombres} {realizado.Odontologo.Apellidos}",
            realizado.Fecha,
            realizado.PiezaDental?.NumeroFDI,
            realizado.Precio,
            realizado.Estado);
}
