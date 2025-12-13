# Inicio Rápido - JonjubNet.Logging

Esta guía te llevará desde la instalación hasta el primer log en menos de 5 minutos.

## Paso 1: Instalar el Paquete

```bash
dotnet add package JonjubNet.Logging
```

## Paso 2: Configurar appsettings.json

Agrega la configuración mínima en `appsettings.json`:

```json
{
  "StructuredLogging": {
    "Enabled": true,
    "MinimumLevel": "Information",
    "ServiceName": "MiServicio",
    "Environment": "Development",
    "Sinks": {
      "EnableConsole": true
    }
  }
}
```

## Paso 3: Registrar Servicios (ASP.NET Core)

En `Program.cs`:

```csharp
using JonjubNet.Logging;

var builder = WebApplication.CreateBuilder(args);

// Registrar servicios de logging estructurado
builder.Services.AddStructuredLoggingInfrastructure(builder.Configuration);

var app = builder.Build();

app.Run();
```

## Paso 4: Usar el Servicio

En tu controlador o servicio:

```csharp
public class MiController : ControllerBase
{
    private readonly IStructuredLoggingService _loggingService;

    public MiController(IStructuredLoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _loggingService.LogInformation(
            "Obteniendo datos",
            operation: "GetData",
            category: "API"
        );
        
        return Ok();
    }
}
```

## Paso 5: Ver los Logs

Ejecuta tu aplicación y verás los logs en la consola:

```json
{
  "Timestamp": "2024-12-01T10:30:00Z",
  "Level": "Information",
  "Message": "Obteniendo datos",
  "Operation": "GetData",
  "Category": "API",
  "ServiceName": "MiServicio",
  "Environment": "Development"
}
```

## ¡Listo!

Ya tienes logging estructurado funcionando. Ahora puedes:

1. [Configurar más opciones](../configuration/main.md)
2. [Ver ejemplos de uso](../examples/basic-usage.md)
3. [Aprender sobre la arquitectura](../architecture/overview.md)

## Para Aplicaciones Sin Host

Si estás usando una aplicación Console o sin BackgroundService:

```csharp
using JonjubNet.Logging;

var services = new ServiceCollection();
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Registrar servicios SIN BackgroundService
services.AddStructuredLoggingInfrastructureWithoutHost(configuration);

var serviceProvider = services.BuildServiceProvider();
var loggingService = serviceProvider.GetRequiredService<IStructuredLoggingService>();

// Usar el servicio
loggingService.LogInformation("Aplicación iniciada");
```

---

**Anterior:** [Instalación](installation.md)  
**Siguiente:** [Configuración Principal](../configuration/main.md)

