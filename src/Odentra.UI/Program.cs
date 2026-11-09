using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Odentra.UI.Components;
using Odentra.Data.Entities;
using Odentra.Services;
using Odentra.Services.Auditoria;
using Odentra.Services.Auditoria.DTOs;
using Odentra.Services.Autenticacion;
using Odentra.Services.Odontograma;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOdentraServices(builder.Configuration);
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
    try
    {
        await IdentitySeeder.SeedAsync(scope.ServiceProvider, builder.Configuration, logger);
        await OdontogramaCatalogSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<Odentra.Data.ApplicationDbContext>());
    }
    catch (Exception exception)
    {
        logger.LogWarning(exception, "No se pudo preparar Identity. Verifica que la base Odentra exista y tenga aplicada la migracion.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

// Si alguien pide una direccion que no existe, el servidor responde 404 pero
// SIN cuerpo: el navegador mostraba una pantalla en blanco.
// UseStatusCodePagesWithReExecute vuelve a ejecutar el pipeline apuntando a /404,
// asi el usuario ve la pantalla "No encontramos esta pagina" y el navegador
// recibe igual el codigo 404 (que es lo correcto).
app.UseStatusCodePagesWithReExecute("/404");

// MapStaticAssets sirve wwwroot y los assets generados por el build
// (Odentra.UI.styles.css, _framework/*.js) desde el manifiesto de endpoints,
// por lo que funcionan igual en Development y en Production.
// Con UseStaticFiles() esos archivos solo se servian en Development y el resto
// de entornos la pagina quedaba sin estilos y sin Blazor.
app.MapStaticAssets();
app.UseAntiforgery();

app.MapPost("/account/login", async (
    HttpContext context,
    IAntiforgery antiforgery,
    SignInManager<Usuario> signInManager,
    UserManager<Usuario> userManager,
    IAuditoriaService auditoriaService,
    ILogger<Program> logger) =>
{
    await antiforgery.ValidateRequestAsync(context);
    var form = await context.Request.ReadFormAsync();
    var email = form["Email"].ToString().Trim();
    var password = form["Password"].ToString();
    var returnUrl = form["ReturnUrl"].ToString();
    var user = await userManager.FindByEmailAsync(email);

    if (user is null)
    {
        return Results.Redirect("/login?error=invalid");
    }

    var direccionIp = ObtenerDireccionIp(context);
    var agenteUsuario = ObtenerAgenteUsuario(context);

    if (user.Estado != EstadoRegistro.Activo)
    {
        await RegistrarEventoAsync(
            auditoriaService,
            logger,
            user.Id,
            AuditoriaAcciones.InicioSesionFallido,
            DescribirEvento("Intento de inicio de sesión con una cuenta inactiva", direccionIp),
            direccionIp,
            agenteUsuario);

        return Results.Redirect("/login?error=invalid");
    }

    var result = await signInManager.PasswordSignInAsync(user, password, false, true);
    if (result.Succeeded)
    {
        await RegistrarEventoAsync(
            auditoriaService,
            logger,
            user.Id,
            AuditoriaAcciones.InicioSesion,
            DescribirEvento("Inicio de sesión exitoso", direccionIp),
            direccionIp,
            agenteUsuario);

        return Results.Redirect(IsLocalUrl(returnUrl) ? returnUrl : "/");
    }

    var motivo = result.IsLockedOut
        ? "Intento de inicio de sesión con una cuenta bloqueada temporalmente"
        : "Intento de inicio de sesión con credenciales incorrectas";

    await RegistrarEventoAsync(
        auditoriaService,
        logger,
        user.Id,
        AuditoriaAcciones.InicioSesionFallido,
        DescribirEvento(motivo, direccionIp),
        direccionIp,
        agenteUsuario);

    return Results.Redirect(result.IsLockedOut ? "/login?error=locked" : "/login?error=invalid");
});

app.MapGet("/account/logout", async (
    HttpContext context,
    SignInManager<Usuario> signInManager,
    IAuditoriaService auditoriaService,
    ILogger<Program> logger) =>
{
    var usuarioId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!string.IsNullOrWhiteSpace(usuarioId))
    {
        var direccionIp = ObtenerDireccionIp(context);
        await RegistrarEventoAsync(
            auditoriaService,
            logger,
            usuarioId,
            AuditoriaAcciones.CierreSesion,
            DescribirEvento("Cierre de sesión", direccionIp),
            direccionIp,
            ObtenerAgenteUsuario(context));
    }

    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

app.MapGet("/auditoria/exportar", async (
    string? texto,
    string? usuarioId,
    string? accion,
    string? modulo,
    string? entidad,
    DateTime? desde,
    DateTime? hasta,
    IAuditoriaService auditoriaService) =>
{
    var filtro = new AuditoriaFiltroDto
    {
        Texto = texto,
        UsuarioId = usuarioId,
        Accion = accion,
        Modulo = modulo,
        Entidad = entidad,
        Desde = desde,
        Hasta = hasta
    };

    var registros = await auditoriaService.GetParaExportarAsync(filtro);
    var contenido = ConstruirCsv(registros);
    var nombreArchivo = $"auditoria-{DateTime.Now:yyyyMMdd-HHmm}.csv";

    return Results.File(
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(contenido),
        "text/csv; charset=utf-8",
        nombreArchivo);
}).RequireAuthorization("Auditoria.Ver");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool IsLocalUrl(string? url) =>
    !string.IsNullOrWhiteSpace(url) && url.StartsWith('/') && !url.StartsWith("//") && !url.Contains('\\');

static string? ObtenerDireccionIp(HttpContext context)
{
    var direccion = context.Connection.RemoteIpAddress;
    if (direccion is null)
    {
        return null;
    }

    if (direccion.IsIPv4MappedToIPv6)
    {
        direccion = direccion.MapToIPv4();
    }

    return direccion.ToString();
}

static string? ObtenerAgenteUsuario(HttpContext context)
{
    var agente = context.Request.Headers.UserAgent.ToString();
    if (string.IsNullOrWhiteSpace(agente))
    {
        return null;
    }

    return agente.Length <= 512 ? agente : agente[..512];
}

static string DescribirEvento(string descripcion, string? direccionIp) =>
    string.IsNullOrWhiteSpace(direccionIp)
        ? $"{descripcion}."
        : $"{descripcion} desde {direccionIp}.";

static async Task RegistrarEventoAsync(
    IAuditoriaService auditoriaService,
    ILogger logger,
    string usuarioId,
    string accion,
    string descripcion,
    string? direccionIp,
    string? agenteUsuario)
{
    try
    {
        await auditoriaService.RegistrarEventoSesionAsync(
            usuarioId,
            accion,
            descripcion,
            direccionIp,
            agenteUsuario);
    }
    catch (Exception exception)
    {
        // La auditoría no debe impedir que el usuario inicie o cierre sesión.
        logger.LogWarning(exception, "No se pudo registrar el evento de auditoría {Accion}.", accion);
    }
}

static string ConstruirCsv(IReadOnlyList<AuditoriaRegistroDto> registros)
{
    var builder = new StringBuilder();
    builder.AppendLine("Fecha y hora,Usuario,Correo,Acción,Módulo,Entidad,ID entidad,Descripción,Dirección IP,Agente de usuario");

    foreach (var registro in registros)
    {
        builder
            .Append(EscaparCsv(registro.FechaHora.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"))).Append(',')
            .Append(EscaparCsv(registro.Usuario)).Append(',')
            .Append(EscaparCsv(registro.UsuarioEmail)).Append(',')
            .Append(EscaparCsv(registro.Accion)).Append(',')
            .Append(EscaparCsv(registro.Modulo)).Append(',')
            .Append(EscaparCsv(registro.EntidadLegible)).Append(',')
            .Append(EscaparCsv(registro.EntidadId?.ToString())).Append(',')
            .Append(EscaparCsv(registro.Descripcion)).Append(',')
            .Append(EscaparCsv(registro.DireccionIp)).Append(',')
            .Append(EscaparCsv(registro.AgenteUsuario))
            .AppendLine();
    }

    return builder.ToString();
}

static string EscaparCsv(string? valor)
{
    var texto = valor ?? string.Empty;
    return $"\"{texto.Replace("\"", "\"\"")}\"";
}
