# Odentra
Proyecto universitario sobre un sistema de Clinica Dental

## Base de datos

La base de datos se configura mediante Entity Framework Core y migraciones. No es necesario ejecutar un script SQL manual.

### Requisitos

* SQL Server disponible.
* SDK de .NET 10.
* La herramienta `dotnet ef` instalada.

### Configuracion inicial

1. Crear manualmente una base de datos vacia llamada `Odentra` en SQL Server.
2. Revisar la cadena `DefaultConnection` en `src/Odentra.UI/appsettings.Development.json`.
3. Cambiar unicamente `Server=localhost` si la instancia de SQL Server usa otro servidor o instancia.
4. Abrir una terminal en la raiz del repositorio.
5. Ejecutar:

```bash
dotnet ef database update --project src/Odentra.Data/Odentra.Data.csproj --startup-project src/Odentra.UI/Odentra.UI.csproj
```

El comando aplica la migracion `InitialCreate` y crea las tablas, relaciones, indices y tablas de ASP.NET Core Identity.

### Ejecutar la aplicacion

Despues de aplicar la migracion, iniciar el proyecto `Odentra.UI` desde Visual Studio Code Insiders o ejecutar:

```bash
dotnet run --project src/Odentra.UI/Odentra.UI.csproj
```
