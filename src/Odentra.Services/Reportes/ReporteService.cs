using System.Globalization;
using Odentra.Data.Entities;
using Odentra.Data.Repositories;
using Odentra.Services.Reportes.DTOs;

namespace Odentra.Services.Reportes;

/// <summary>
/// Lógica del módulo de Reportes.
///
/// Arquitectura:  Pantalla (UI)  →  ReporteService (este archivo)  →  Repository  →  SQL Server
///
/// El servicio no modifica datos: solo consulta, calcula los totales de las tarjetas,
/// prepara la serie del gráfico y deja las filas de la tabla formateadas (fechas,
/// dinero y estados). El mismo resultado lo consumen la pantalla y la exportación PDF.
///
/// Orden de lectura:
///  1) Generar un reporte            → GenerarAsync
///  2) Un método por tipo de reporte → GenerarCitasAsync, GenerarPacientesAsync,
///                                     GenerarTratamientosAsync, GenerarPagosAsync
///  3) Ayudantes                     → ValidarFechas, ParseEnum, Money, Tonos...
/// </summary>
public sealed class ReporteService(IReporteRepository repository) : IReporteService
{
    private static readonly CultureInfo Cultura = new("es-DO");

    public async Task<ReporteResultadoDto> GenerarAsync(
        ReporteTipo tipo,
        ReporteFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        ValidarFechas(filtro);

        return tipo switch
        {
            ReporteTipo.Citas => await GenerarCitasAsync(filtro, cancellationToken),
            ReporteTipo.Pacientes => await GenerarPacientesAsync(filtro, cancellationToken),
            ReporteTipo.Tratamientos => await GenerarTratamientosAsync(filtro, cancellationToken),
            ReporteTipo.Pagos => await GenerarPagosAsync(filtro, cancellationToken),
            _ => throw new InvalidOperationException("El tipo de reporte no es válido.")
        };
    }

    public async Task<IReadOnlyList<ReporteOpcionDto>> GetOdontologosAsync(
        CancellationToken cancellationToken = default)
    {
        var odontologos = await repository.GetOdontologosAsync(cancellationToken);

        return odontologos
            .Select(odontologo => new ReporteOpcionDto(
                odontologo.Id,
                NombreCompleto(odontologo.Nombres, odontologo.Apellidos)))
            .ToList();
    }

    // =========================================================================================
    // 1) UN MÉTODO POR TIPO DE REPORTE
    // =========================================================================================

    private async Task<ReporteResultadoDto> GenerarCitasAsync(
        ReporteFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var estado = ParseEnum<EstadoCita>(filtro.Estado);
        var citas = await repository.GetCitasAsync(
            ToDateOnly(filtro.Desde),
            ToDateOnly(filtro.Hasta),
            filtro.OdontologoId,
            estado,
            cancellationToken);

        var filas = citas
            .Select(cita => new ReporteFilaDto(
            [
                new(cita.Fecha.ToString("dd/MM/yyyy", Cultura)),
                new(cita.HoraInicio.ToString("HH:mm", Cultura)),
                new(NombreCompleto(cita.Paciente.Nombres, cita.Paciente.Apellidos)),
                new(NombreCompleto(cita.Odontologo.Nombres, cita.Odontologo.Apellidos)),
                new(string.IsNullOrWhiteSpace(cita.Motivo) ? "—" : cita.Motivo),
                new(Duracion(cita.HoraInicio, cita.HoraFin)),
                new(TextoCita(cita.Estado), TonoCita(cita.Estado))
            ]))
            .ToList();

        var tarjetas = new List<ReporteTarjetaDto>
        {
            new("Total citas", citas.Count.ToString(Cultura), "blue", "calendar"),
            new("Atendidas", citas.Count(cita => cita.Estado == EstadoCita.Atendida).ToString(Cultura), "green", "check"),
            new("Canceladas", citas.Count(cita => cita.Estado == EstadoCita.Cancelada).ToString(Cultura), "red", "x"),
            new("Pendientes", citas.Count(cita => cita.Estado is EstadoCita.Programada or EstadoCita.Confirmada).ToString(Cultura), "amber", "clock")
        };

        var series = new List<ReporteSerieDto>();
        foreach (var valor in Enum.GetValues<EstadoCita>())
        {
            var cantidad = citas.Count(cita => cita.Estado == valor);
            if (cantidad > 0)
            {
                series.Add(new ReporteSerieDto(TextoCita(valor), cantidad, TonoCita(valor)));
            }
        }

        var odontologo = filtro.OdontologoId.HasValue
            ? await NombreOdontologoAsync(filtro.OdontologoId.Value, cancellationToken)
            : "Todos";

        return new ReporteResultadoDto(
            ReporteTipo.Citas,
            "Reporte de citas",
            "Citas por período, odontólogo y estado",
            Periodo(filtro),
            Filtros(filtro, odontologo, "Estado", estado.HasValue ? TextoCita(estado.Value) : "Todos"),
            tarjetas,
            "Citas por estado",
            series,
            [
                new("Fecha", TipoColumna.Fecha),
                new("Hora"),
                new("Paciente"),
                new("Odontólogo"),
                new("Motivo"),
                new("Duración", TipoColumna.Numero),
                new("Estado", TipoColumna.Estado)
            ],
            filas,
            null,
            null,
            DateTime.Now);
    }

    private async Task<ReporteResultadoDto> GenerarPacientesAsync(
        ReporteFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var estado = ParseEnum<EstadoRegistro>(filtro.Estado);
        var pacientes = await repository.GetPacientesAsync(
            filtro.Desde?.Date,
            filtro.Hasta?.Date,
            estado,
            cancellationToken);

        var filas = pacientes
            .Select(paciente => new ReporteFilaDto(
            [
                new(NombreCompleto(paciente.Nombres, paciente.Apellidos)),
                new(paciente.Identificacion ?? "—"),
                new(CalcularEdad(paciente.FechaNacimiento), null),
                new(paciente.Telefono ?? "—"),
                new(TextoRegistro(paciente.Estado), TonoRegistro(paciente.Estado)),
                new(paciente.FechaRegistro.ToString("dd/MM/yyyy", Cultura))
            ]))
            .ToList();

        var edades = pacientes
            .Where(paciente => paciente.FechaNacimiento.HasValue)
            .Select(paciente => CalcularEdadValor(paciente.FechaNacimiento!.Value))
            .ToList();

        var edadPromedio = edades.Count > 0
            ? $"{edades.Average().ToString("0", Cultura)} años"
            : "—";

        var tarjetas = new List<ReporteTarjetaDto>
        {
            new("Total pacientes", pacientes.Count.ToString(Cultura), "blue", "users"),
            new("Activos", pacientes.Count(paciente => paciente.Estado == EstadoRegistro.Activo).ToString(Cultura), "green", "user-check"),
            new("Inactivos", pacientes.Count(paciente => paciente.Estado == EstadoRegistro.Inactivo).ToString(Cultura), "red", "user-x"),
            new("Edad promedio", edadPromedio, "violet", "gauge")
        };

        var series = new List<ReporteSerieDto>();
        foreach (var valor in Enum.GetValues<EstadoRegistro>())
        {
            var cantidad = pacientes.Count(paciente => paciente.Estado == valor);
            if (cantidad > 0)
            {
                series.Add(new ReporteSerieDto(TextoRegistro(valor), cantidad, TonoRegistro(valor)));
            }
        }

        return new ReporteResultadoDto(
            ReporteTipo.Pacientes,
            "Reporte de pacientes",
            "Pacientes registrados, activos e inactivos",
            Periodo(filtro),
            Filtros(filtro, null, "Estado", estado.HasValue ? TextoRegistro(estado.Value) : "Todos"),
            tarjetas,
            "Pacientes por estado",
            series,
            [
                new("Nombre"),
                new("Identificación"),
                new("Edad", TipoColumna.Numero),
                new("Teléfono"),
                new("Estado", TipoColumna.Estado),
                new("Registro", TipoColumna.Fecha)
            ],
            filas,
            null,
            null,
            DateTime.Now);
    }

    private async Task<ReporteResultadoDto> GenerarTratamientosAsync(
        ReporteFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var estado = ParseEnum<EstadoTratamiento>(filtro.Estado);
        var tratamientos = await repository.GetTratamientosAsync(
            filtro.Desde?.Date,
            filtro.Hasta?.Date,
            filtro.OdontologoId,
            estado,
            cancellationToken);

        var filas = tratamientos
            .Select(tratamiento => new ReporteFilaDto(
            [
                new(NombreCompleto(tratamiento.Paciente.Nombres, tratamiento.Paciente.Apellidos)),
                new(tratamiento.Tratamiento.Nombre),
                new(NombreCompleto(tratamiento.Odontologo.Nombres, tratamiento.Odontologo.Apellidos)),
                new(tratamiento.Fecha.ToString("dd/MM/yyyy", Cultura)),
                new(tratamiento.PiezaDental?.NumeroFDI ?? "—"),
                new(Money(tratamiento.Precio)),
                new(TextoTratamiento(tratamiento.Estado), TonoTratamiento(tratamiento.Estado))
            ]))
            .ToList();

        var tarjetas = new List<ReporteTarjetaDto>
        {
            new("Total tratamientos", tratamientos.Count.ToString(Cultura), "blue", "activity"),
            new("Completados", tratamientos.Count(tratamiento => tratamiento.Estado == EstadoTratamiento.Completado).ToString(Cultura), "green", "check"),
            new("Pendientes", tratamientos.Count(tratamiento => tratamiento.Estado is EstadoTratamiento.Pendiente or EstadoTratamiento.EnProceso).ToString(Cultura), "amber", "clock"),
            new("Total facturado", Money(tratamientos.Sum(tratamiento => tratamiento.Precio)), "violet", "money")
        };

        var series = new List<ReporteSerieDto>();
        foreach (var valor in Enum.GetValues<EstadoTratamiento>())
        {
            var cantidad = tratamientos.Count(tratamiento => tratamiento.Estado == valor);
            if (cantidad > 0)
            {
                series.Add(new ReporteSerieDto(TextoTratamiento(valor), cantidad, TonoTratamiento(valor)));
            }
        }

        var odontologo = filtro.OdontologoId.HasValue
            ? await NombreOdontologoAsync(filtro.OdontologoId.Value, cancellationToken)
            : "Todos";

        return new ReporteResultadoDto(
            ReporteTipo.Tratamientos,
            "Reporte de tratamientos",
            "Tratamientos realizados por período y estado",
            Periodo(filtro),
            Filtros(filtro, odontologo, "Estado", estado.HasValue ? TextoTratamiento(estado.Value) : "Todos"),
            tarjetas,
            "Tratamientos por estado",
            series,
            [
                new("Paciente"),
                new("Tratamiento"),
                new("Odontólogo"),
                new("Fecha", TipoColumna.Fecha),
                new("Pieza"),
                new("Precio", TipoColumna.Dinero),
                new("Estado", TipoColumna.Estado)
            ],
            filas,
            "Total facturado",
            Money(tratamientos.Sum(tratamiento => tratamiento.Precio)),
            DateTime.Now);
    }

    private async Task<ReporteResultadoDto> GenerarPagosAsync(
        ReporteFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var metodo = ParseEnum<MetodoPago>(filtro.Estado);
        var pagos = await repository.GetPagosAsync(
            filtro.Desde?.Date,
            filtro.Hasta?.Date,
            metodo,
            cancellationToken);

        var filas = pagos
            .Select(pago => new ReporteFilaDto(
            [
                new(pago.Fecha.ToString("dd/MM/yyyy", Cultura)),
                new(NombreCompleto(
                    pago.TratamientoRealizado.Paciente.Nombres,
                    pago.TratamientoRealizado.Paciente.Apellidos)),
                new(pago.TratamientoRealizado.Tratamiento.Nombre),
                new(Money(pago.Monto)),
                new(TextoMetodo(pago.MetodoPago), TonoMetodo(pago.MetodoPago)),
                new(pago.Usuario?.Nombre ?? "Sistema")
            ]))
            .ToList();

        var efectivo = pagos
            .Where(pago => pago.MetodoPago == MetodoPago.Efectivo)
            .Sum(pago => pago.Monto);

        var tarjetaTransferencia = pagos
            .Where(pago => pago.MetodoPago is MetodoPago.Tarjeta or MetodoPago.Transferencia)
            .Sum(pago => pago.Monto);

        var tarjetas = new List<ReporteTarjetaDto>
        {
            new("Total cobrado", Money(pagos.Sum(pago => pago.Monto)), "green", "money"),
            new("Pagos registrados", pagos.Count.ToString(Cultura), "blue", "receipt"),
            new("Efectivo", Money(efectivo), "amber", "cash"),
            new("Tarjeta/Transferencia", Money(tarjetaTransferencia), "violet", "card")
        };

        var series = new List<ReporteSerieDto>();
        foreach (var valor in Enum.GetValues<MetodoPago>())
        {
            var cantidad = pagos.Count(pago => pago.MetodoPago == valor);
            if (cantidad > 0)
            {
                series.Add(new ReporteSerieDto(TextoMetodo(valor), cantidad, TonoMetodo(valor)));
            }
        }

        return new ReporteResultadoDto(
            ReporteTipo.Pagos,
            "Reporte de pagos",
            "Ingresos y pagos por período y método",
            Periodo(filtro),
            Filtros(filtro, null, "Método de pago", metodo.HasValue ? TextoMetodo(metodo.Value) : "Todos"),
            tarjetas,
            "Pagos por método",
            series,
            [
                new("Fecha", TipoColumna.Fecha),
                new("Paciente"),
                new("Tratamiento"),
                new("Monto", TipoColumna.Dinero),
                new("Método", TipoColumna.Estado),
                new("Registrado por")
            ],
            filas,
            "Total cobrado",
            Money(pagos.Sum(pago => pago.Monto)),
            DateTime.Now);
    }

    // =========================================================================================
    // 2) AYUDANTES
    // =========================================================================================

    private async Task<string> NombreOdontologoAsync(
        int odontologoId,
        CancellationToken cancellationToken)
    {
        var odontologos = await repository.GetOdontologosAsync(cancellationToken);
        var odontologo = odontologos.FirstOrDefault(item => item.Id == odontologoId);

        return odontologo is null
            ? "Todos"
            : NombreCompleto(odontologo.Nombres, odontologo.Apellidos);
    }

    private static IReadOnlyList<ReporteFiltroAplicadoDto> Filtros(
        ReporteFiltroDto filtro,
        string? odontologo,
        string etiquetaEstado,
        string valorEstado)
    {
        var lista = new List<ReporteFiltroAplicadoDto>
        {
            new("Período", Periodo(filtro))
        };

        if (odontologo is not null)
        {
            lista.Add(new("Odontólogo", odontologo));
        }

        lista.Add(new(etiquetaEstado, valorEstado));

        return lista;
    }

    private static string Periodo(ReporteFiltroDto filtro)
    {
        var desde = filtro.Desde?.ToString("dd/MM/yyyy", Cultura);
        var hasta = filtro.Hasta?.ToString("dd/MM/yyyy", Cultura);

        return (desde, hasta) switch
        {
            (not null, not null) when desde == hasta => desde,
            (not null, not null) => $"{desde} — {hasta}",
            (not null, null) => $"Desde {desde}",
            (null, not null) => $"Hasta {hasta}",
            _ => "Todo el historial"
        };
    }

    private static void ValidarFechas(ReporteFiltroDto filtro)
    {
        if (filtro.Desde.HasValue &&
            filtro.Hasta.HasValue &&
            filtro.Desde.Value.Date > filtro.Hasta.Value.Date)
        {
            throw new InvalidOperationException("La fecha inicial no puede ser mayor que la fecha final.");
        }
    }

    private static DateOnly? ToDateOnly(DateTime? fecha) =>
        fecha.HasValue ? DateOnly.FromDateTime(fecha.Value) : null;

    private static TEnum? ParseEnum<TEnum>(string? valor)
        where TEnum : struct, Enum =>
        !string.IsNullOrWhiteSpace(valor) &&
        Enum.TryParse<TEnum>(valor, ignoreCase: true, out var resultado)
            ? resultado
            : null;

    private static string NombreCompleto(string nombres, string apellidos) =>
        $"{nombres} {apellidos}".Trim();

    private static string Money(decimal monto) =>
        $"RD$ {monto.ToString("N2", Cultura)}";

    private static string Duracion(TimeOnly inicio, TimeOnly fin)
    {
        var minutos = (int)Math.Round((fin - inicio).TotalMinutes);

        return minutos <= 0 ? "—" : $"{minutos} min";
    }

    private static string CalcularEdad(DateOnly? fechaNacimiento) =>
        fechaNacimiento.HasValue
            ? CalcularEdadValor(fechaNacimiento.Value).ToString(Cultura)
            : "—";

    private static int CalcularEdadValor(DateOnly fechaNacimiento)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var edad = hoy.Year - fechaNacimiento.Year;

        if (fechaNacimiento.AddYears(edad) > hoy)
        {
            edad--;
        }

        return Math.Max(edad, 0);
    }

    private static string TextoCita(EstadoCita estado) => estado switch
    {
        EstadoCita.Programada => "Programada",
        EstadoCita.Confirmada => "Confirmada",
        EstadoCita.Atendida => "Atendida",
        EstadoCita.Cancelada => "Cancelada",
        EstadoCita.NoAsistio => "No asistió",
        _ => estado.ToString()
    };

    private static string TextoRegistro(EstadoRegistro estado) => estado switch
    {
        EstadoRegistro.Activo => "Activo",
        EstadoRegistro.Inactivo => "Inactivo",
        _ => estado.ToString()
    };

    private static string TextoTratamiento(EstadoTratamiento estado) => estado switch
    {
        EstadoTratamiento.Pendiente => "Pendiente",
        EstadoTratamiento.EnProceso => "En proceso",
        EstadoTratamiento.Completado => "Completado",
        EstadoTratamiento.Cancelado => "Cancelado",
        _ => estado.ToString()
    };

    private static string TextoMetodo(MetodoPago metodo) => metodo switch
    {
        MetodoPago.Efectivo => "Efectivo",
        MetodoPago.Tarjeta => "Tarjeta",
        MetodoPago.Transferencia => "Transferencia",
        _ => metodo.ToString()
    };

    private static string TonoCita(EstadoCita estado) => estado switch
    {
        EstadoCita.Atendida => "green",
        EstadoCita.Confirmada => "blue",
        EstadoCita.Programada => "amber",
        EstadoCita.Cancelada => "red",
        EstadoCita.NoAsistio => "slate",
        _ => "slate"
    };

    private static string TonoRegistro(EstadoRegistro estado) =>
        estado == EstadoRegistro.Activo ? "green" : "slate";

    private static string TonoTratamiento(EstadoTratamiento estado) => estado switch
    {
        EstadoTratamiento.Completado => "green",
        EstadoTratamiento.EnProceso => "blue",
        EstadoTratamiento.Pendiente => "amber",
        EstadoTratamiento.Cancelado => "red",
        _ => "slate"
    };

    private static string TonoMetodo(MetodoPago metodo) => metodo switch
    {
        MetodoPago.Efectivo => "amber",
        MetodoPago.Tarjeta => "blue",
        MetodoPago.Transferencia => "violet",
        _ => "slate"
    };
}
