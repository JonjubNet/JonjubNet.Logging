# Implementación en ASP.NET Core - JonjubNet.Logging

## Implementación Básica

### Paso 1: Instalar el Paquete

```bash
dotnet add package JonjubNet.Logging
```

### Paso 2: Configurar appsettings.json

Agrega la configuración en `appsettings.json`:

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

### Paso 3: Registrar Servicios en Program.cs

```csharp
using JonjubNet.Logging;

var builder = WebApplication.CreateBuilder(args);

// Habilitar hot-reload (opcional pero recomendado)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Registrar servicios de logging estructurado
builder.Services.AddStructuredLoggingInfrastructure(builder.Configuration);

// ... resto de tu configuración

var app = builder.Build();

// ... resto de tu aplicación

app.Run();
```

### Paso 4: Usar el Servicio

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

## Implementación con Servicio de Usuario Personalizado

Si necesitas obtener información del usuario actual desde tu sistema de autenticación:

```csharp
// Implementar ICurrentUserService
public class CustomUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public string? GetCurrentUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    }
}

// Registrar con servicio personalizado
builder.Services.AddHttpContextAccessor();
builder.Services.AddStructuredLoggingInfrastructure<CustomUserService>(builder.Configuration);
```

## Implementación con MediatR

Para logging automático de todas las peticiones MediatR:

```csharp
// Registrar MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Registrar LoggingBehaviour
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));

// Ahora todas las peticiones MediatR se registran automáticamente
public class CreateOrderCommand : IRequest<Order>
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Order>
{
    // El logging se hace automáticamente, no necesitas código adicional
    public async Task<Order> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Tu lógica aquí
        return new Order();
    }
}
```

## Configuración por Ambiente

Puedes tener diferentes configuraciones por ambiente:

**appsettings.Development.json:**
```json
{
  "StructuredLogging": {
    "MinimumLevel": "Debug",
    "Sinks": {
      "EnableConsole": true
    }
  }
}
```

**appsettings.Production.json:**
```json
{
  "StructuredLogging": {
    "MinimumLevel": "Information",
    "Sinks": {
      "EnableConsole": false,
      "EnableFile": true,
      "EnableElasticsearch": true
    }
  }
}
```

---

**Siguiente:** [Aplicaciones Sin Host](without-host.md)

