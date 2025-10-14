# JonjubNet.Logging

Biblioteca de logging estructurado para aplicaciones .NET con soporte para múltiples sinks (Console, File, HTTP, Elasticsearch, Kafka) y funcionalidades avanzadas de correlación y enriquecimiento.

## Características

- ✅ **Logging Estructurado**: Logs en formato JSON con información contextual rica
- ✅ **Múltiples Sinks**: Console, File, HTTP, Elasticsearch, Kafka
- ✅ **Correlación**: Soporte para CorrelationId, RequestId, SessionId
- ✅ **Enriquecimiento**: Información automática de Environment, Process, Thread, Machine
- ✅ **Filtros Avanzados**: Por categoría, operación, usuario y nivel de log
- ✅ **Configuración Flexible**: Configuración completa via appsettings.json
- ✅ **Integración con Serilog**: Basado en Serilog para máximo rendimiento
- ✅ **ASP.NET Core Ready**: Integración nativa con ASP.NET Core
- ✅ **.NET 10.0 Compatible**: Optimizado para .NET 10.0 con las últimas características

## Instalación

```bash
dotnet add package JonjubNet.Logging
```

## Configuración Básica

### 1. Configurar en appsettings.json

```json
{
  "StructuredLogging": {
    "Enabled": true,
    "MinimumLevel": "Information",
    "ServiceName": "MiServicio",
    "Environment": "Development",
    "Version": "1.0.0",
    "Sinks": {
      "EnableConsole": true,
      "EnableFile": true,
      "EnableHttp": false,
      "EnableElasticsearch": false,
      "File": {
        "Path": "logs/log-.txt",
        "RollingInterval": "Day",
        "RetainedFileCountLimit": 30,
        "FileSizeLimitBytes": 104857600,
        "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
      }
    },
    "Filters": {
      "ExcludedCategories": [],
      "ExcludedOperations": [],
      "ExcludedUsers": [],
      "FilterByLogLevel": true,
      "CategoryLogLevels": {}
    },
    "Enrichment": {
      "IncludeEnvironment": true,
      "IncludeProcess": true,
      "IncludeThread": true,
      "IncludeMachineName": true,
      "IncludeServiceInfo": true,
      "StaticProperties": {}
    },
    "Correlation": {
      "EnableCorrelationId": true,
      "EnableRequestId": true,
      "EnableSessionId": true,
      "CorrelationIdHeader": "X-Correlation-ID",
      "RequestIdHeader": "X-Request-ID",
      "SessionIdHeader": "X-Session-ID"
    },
    "KafkaProducer": {
      "Enabled": false,
      "ProducerUrl": "http://localhost:8080/api/logs",
      "Topic": "structured-logs",
      "TimeoutSeconds": 5,
      "BatchSize": 100,
      "LingerMs": 5,
      "RetryCount": 3,
      "EnableCompression": true,
      "CompressionType": "gzip",
      "Headers": {}
    }
  }
}
```

### 2. Registrar en Program.cs

```csharp
using JonjubNet.Logging;

var builder = WebApplication.CreateBuilder(args);

// Agregar logging estructurado
builder.Services.AddStructuredLoggingInfrastructure(builder.Configuration);

var app = builder.Build();

app.Run();
```

## Uso Básico

### Inyección de Dependencias

```csharp
public class MiController : ControllerBase
{
    private readonly IStructuredLoggingService _loggingService;

    public MiController(IStructuredLoggingService loggingService)
    {
        _loggingService = loggingService;
    }
}
```

### Logging Simple

```csharp
// Log de información
_loggingService.LogInformation("Usuario autenticado exitosamente", "Authentication", "Security");

// Log de error
_loggingService.LogError("Error al procesar petición", "ProcessRequest", "General", 
    properties: new Dictionary<string, object> { { "RequestId", "12345" } },
    exception: ex);

// Log de advertencia
_loggingService.LogWarning("Límite de intentos alcanzado", "LoginAttempt", "Security",
    properties: new Dictionary<string, object> { { "Attempts", 5 } });
```

### Logging de Operaciones

```csharp
// Inicio de operación
_loggingService.LogOperationStart("ProcessOrder", "Business");

try
{
    // Lógica de negocio
    var result = await ProcessOrderAsync();
    
    // Fin exitoso
    _loggingService.LogOperationEnd("ProcessOrder", "Business", executionTimeMs: 1500, 
        properties: new Dictionary<string, object> { { "OrderId", result.Id } });
}
catch (Exception ex)
{
    // Fin con error
    _loggingService.LogOperationEnd("ProcessOrder", "Business", executionTimeMs: 500, 
        success: false, exception: ex);
    throw;
}
```

### Logging de Eventos Específicos

```csharp
// Evento de usuario
_loggingService.LogUserAction("CreateOrder", "Order", "12345",
    properties: new Dictionary<string, object> { { "Amount", 99.99 } });

// Evento de seguridad
_loggingService.LogSecurityEvent("FailedLogin", "Intento de login fallido",
    properties: new Dictionary<string, object> { { "IP", "192.168.1.1" } });

// Evento de auditoría
_loggingService.LogAuditEvent("DataAccess", "Consulta de datos sensibles", "User", "12345");
```

## Configuración Avanzada

### Sink HTTP

```json
{
  "Sinks": {
    "EnableHttp": true,
    "Http": {
      "Url": "https://mi-servidor-logs.com/api/logs",
      "ContentType": "application/json",
      "BatchPostingLimit": 100,
      "PeriodSeconds": 2,
      "Headers": {
        "Authorization": "Bearer token123"
      }
    }
  }
}
```

### Sink Elasticsearch

```json
{
  "Sinks": {
    "EnableElasticsearch": true,
    "Elasticsearch": {
      "Url": "http://localhost:9200",
      "IndexFormat": "logs-{0:yyyy.MM.dd}",
      "Username": "elastic",
      "Password": "password",
      "EnableAuthentication": true
    }
  }
}
```

### Sink Kafka

```json
{
  "KafkaProducer": {
    "Enabled": true,
    "ProducerUrl": "http://localhost:8080/api/logs",
    "Topic": "structured-logs",
    "TimeoutSeconds": 5,
    "BatchSize": 100,
    "LingerMs": 5,
    "RetryCount": 3,
    "EnableCompression": true,
    "CompressionType": "gzip"
  }
}
```

### Filtros

```json
{
  "Filters": {
    "ExcludedCategories": ["Debug", "Trace"],
    "ExcludedOperations": ["HealthCheck"],
    "ExcludedUsers": ["System"],
    "FilterByLogLevel": true,
    "CategoryLogLevels": {
      "Security": "Warning",
      "Performance": "Information"
    }
  }
}
```

### Enriquecimiento

```json
{
  "Enrichment": {
    "IncludeEnvironment": true,
    "IncludeProcess": true,
    "IncludeThread": true,
    "IncludeMachineName": true,
    "IncludeServiceInfo": true,
    "StaticProperties": {
      "Application": "MiAplicacion",
      "Region": "us-east-1"
    }
  }
}
```

## Servicio de Usuario Personalizado

Para integrar con tu sistema de autenticación, implementa `ICurrentUserService`:

```csharp
public class CustomUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
    }

    public string? GetCurrentUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    }

    // Implementar otros métodos...
}
```

Y registrarlo:

```csharp
builder.Services.AddStructuredLoggingInfrastructure<CustomUserService>(builder.Configuration);
```

## Ejemplo de Log Generado

```json
{
  "serviceName": "MiServicio",
  "operation": "ProcessOrder",
  "logLevel": "Information",
  "message": "Orden procesada exitosamente",
  "category": "Business",
  "userId": "user123",
  "userName": "john.doe",
  "environment": "Production",
  "version": "1.0.0",
  "machineName": "SERVER-01",
  "processId": "1234",
  "threadId": "5",
  "properties": {
    "orderId": "ORD-12345",
    "amount": 99.99
  },
  "context": {
    "operationStart": "2024-01-15T10:30:00Z",
    "operationStatus": "Completed",
    "executionTimeMs": 1500,
    "success": true
  },
  "timestamp": "2024-01-15T10:30:01.500Z",
  "requestPath": "/api/orders",
  "requestMethod": "POST",
  "statusCode": 200,
  "clientIp": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "correlationId": "corr-12345",
  "requestId": "req-67890",
  "sessionId": "sess-abc123"
}
```

## Contribuir

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## Licencia

Este proyecto está licenciado bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para detalles.

## Soporte

Para soporte y preguntas, por favor abre un issue en el repositorio de GitHub.
