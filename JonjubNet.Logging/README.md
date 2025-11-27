# JonjubNet.Logging

Biblioteca de logging estructurado para aplicaciones .NET con soporte para múltiples sinks (Console, File, HTTP, Elasticsearch) y Kafka Producer con múltiples tipos de conexión.

## Características

- ✅ **Logging Estructurado**: Logs en formato JSON con información contextual rica
- ✅ **Múltiples Sinks**: Console, File, HTTP, Elasticsearch
- ✅ **Kafka Producer**: Soporte para 3 tipos de conexión (Nativa, REST Proxy, Webhook)
- ✅ **Correlación**: Soporte para CorrelationId, RequestId, SessionId
- ✅ **Enriquecimiento Automático**: Información de Environment, Process, Thread, Machine, HTTP Context
- ✅ **Filtros Avanzados**: Por categoría, operación, usuario y nivel de log
- ✅ **ASP.NET Core Ready**: Integración nativa con ASP.NET Core
- ✅ **.NET 10.0 Compatible**: Optimizado para .NET 10.0

## Instalación

```bash
dotnet add package JonjubNet.Logging
```

## Configuración Rápida

### 1. Registrar en Program.cs

```csharp
using JonjubNet.Logging;

var builder = WebApplication.CreateBuilder(args);

// Registrar logging estructurado (registra automáticamente IHttpContextAccessor)
builder.Services.AddStructuredLoggingInfrastructure(builder.Configuration);

var app = builder.Build();
app.Run();
```

**Nota:** `AddStructuredLoggingInfrastructure` registra automáticamente:
- ✅ `IHttpContextAccessor` (para campos HTTP)
- ✅ `ICurrentUserService` (por defecto)
- ✅ `IStructuredLoggingService`

### 2. Configurar en appsettings.json

```json
{
  "StructuredLogging": {
    "Enabled": true,
    "ServiceName": "MiServicio",
    "Environment": "Development",
    "Version": "1.0.0",
    "Sinks": {
      "EnableConsole": true,
      "EnableFile": true,
      "File": {
        "Path": "logs/log-.txt",
        "RollingInterval": "Day",
        "RetainedFileCountLimit": 30
      }
    },
    "Correlation": {
      "EnableCorrelationId": true,
      "EnableRequestId": true,
      "EnableSessionId": true
    },
    "Enrichment": {
      "HttpCapture": {
        "IncludeRequestHeaders": true,
        "IncludeResponseHeaders": false,
        "IncludeQueryString": true,
        "IncludeRequestBody": false,
        "IncludeResponseBody": false,
        "MaxBodySizeBytes": 10240,
        "SensitiveHeaders": [
          "Authorization",
          "Cookie",
          "X-API-Key",
          "X-Auth-Token"
        ]
      }
    }
  }
}
```

## Configuración de Kafka Producer

El componente soporta **3 tipos de conexión a Kafka**, con prioridad según la configuración:

### Opción 1: Conexión Nativa (BootstrapServers) - ⚡ Recomendado

Conexión directa usando el protocolo binario nativo de Kafka. **Tiene prioridad** si está configurado.

```json
{
  "StructuredLogging": {
    "KafkaProducer": {
      "Enabled": true,
      "BootstrapServers": "localhost:9092",
      "Topic": "structured-logs",
      "TimeoutSeconds": 5,
      "BatchSize": 100,
      "LingerMs": 5,
      "RetryCount": 3,
      "EnableCompression": true,
      "CompressionType": "gzip"
    }
  }
}
```

**Ventajas:**
- ⚡ Mayor rendimiento (protocolo binario)
- ✅ Menor latencia
- ✅ Soporte completo de características de Kafka

**Cuándo usar:**
- Cuando tienes acceso directo a los brokers de Kafka
- Para máximo rendimiento
- En entornos de producción

### Opción 2: Kafka REST Proxy (HTTP/HTTPS)

Conexión a través de Kafka REST Proxy usando HTTP/HTTPS.

```json
{
  "StructuredLogging": {
    "KafkaProducer": {
      "Enabled": true,
      "ProducerUrl": "http://kafka-rest:8082",
      "UseWebhook": false,
      "Topic": "structured-logs",
      "TimeoutSeconds": 5,
      "Headers": {}
    }
  }
}
```

**Para HTTPS:**
```json
{
  "KafkaProducer": {
    "Enabled": true,
    "ProducerUrl": "https://kafka-rest:8443",
    "UseWebhook": false,
    "Topic": "structured-logs"
  }
}
```

**Ventajas:**
- ✅ Fácil de configurar
- ✅ Funciona a través de firewalls
- ✅ No requiere librerías nativas de Kafka en algunos entornos

**Cuándo usar:**
- Cuando usas Confluent REST Proxy
- En entornos donde no puedes acceder directamente a los brokers
- Para integración con sistemas que solo soportan HTTP

**Formato del mensaje:** El componente envía el mensaje en formato de REST Proxy:
```json
{
  "records": [
    { "value": "{\"serviceName\":\"...\",\"message\":\"...\"}" }
  ]
}
```

### Opción 3: Webhook HTTP/HTTPS

Envío directo a un endpoint webhook (no es Kafka, pero útil para integraciones).

```json
{
  "StructuredLogging": {
    "KafkaProducer": {
      "Enabled": true,
      "ProducerUrl": "https://mi-webhook.com/api/logs",
      "UseWebhook": true,
      "Topic": "structured-logs",
      "TimeoutSeconds": 5,
      "Headers": {
        "Authorization": "Bearer token123",
        "X-Custom-Header": "valor"
      }
    }
  }
}
```

**Ventajas:**
- ✅ Envío directo a cualquier endpoint HTTP/HTTPS
- ✅ Headers personalizados
- ✅ Útil para integraciones con sistemas externos

**Cuándo usar:**
- Para enviar logs a sistemas que no son Kafka
- Para integraciones con APIs externas
- Cuando necesitas headers personalizados

**Formato del mensaje:** El componente envía el JSON del log directamente:
```json
{
  "serviceName": "...",
  "message": "...",
  "timestamp": "..."
}
```

### Prioridad de Configuración

1. **BootstrapServers** (si está configurado) → Conexión Nativa
2. **ProducerUrl + UseWebhook = false** → REST Proxy
3. **ProducerUrl + UseWebhook = true** → Webhook

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
_loggingService.LogInformation("Usuario autenticado", "Authentication", "Security");

// Log de error
_loggingService.LogError("Error al procesar", "ProcessRequest", "General", 
    properties: new Dictionary<string, object> { { "RequestId", "12345" } },
    exception: ex);

// Log de advertencia
_loggingService.LogWarning("Límite alcanzado", "LoginAttempt", "Security",
    properties: new Dictionary<string, object> { { "Attempts", 5 } });
```

### Logging de Operaciones

```csharp
// Inicio de operación
_loggingService.LogOperationStart("ProcessOrder", "Business");

try
{
    var result = await ProcessOrderAsync();
    
    // Fin exitoso
    _loggingService.LogOperationEnd("ProcessOrder", "Business", 
        executionTimeMs: 1500, 
        properties: new Dictionary<string, object> { { "OrderId", result.Id } });
}
catch (Exception ex)
{
    // Fin con error
    _loggingService.LogOperationEnd("ProcessOrder", "Business", 
        executionTimeMs: 500, 
        success: false, 
        exception: ex);
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

## Servicio de Usuario Personalizado

Para que `UserId` y `UserName` se llenen con datos reales en lugar de "Anonymous":

### Implementación

```csharp
using JonjubNet.Logging.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

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
            .FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? "Anonymous";
    }

    public string? GetCurrentUserName()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.Name)?.Value 
            ?? "Anonymous";
    }

    public string? GetCurrentUserEmail()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.Email)?.Value;
    }

    public IEnumerable<string> GetCurrentUserRoles()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value) 
            ?? Enumerable.Empty<string>();
    }

    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User?
            .IsInRole(role) ?? false;
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?
            .Identity?.IsAuthenticated ?? false;
    }
}
```

### Registro

```csharp
builder.Services.AddStructuredLoggingInfrastructure<CustomUserService>(builder.Configuration);
```

## Campos que se Llenan Automáticamente

### Campos Siempre Disponibles

| Campo | Fuente | Valor por Defecto |
|-------|--------|-------------------|
| `ServiceName` | Configuración | De `appsettings.json` |
| `Environment` | Configuración | De `appsettings.json` |
| `Version` | Configuración | De `appsettings.json` |
| `MachineName` | Sistema | Nombre de la máquina |
| `ProcessId` | Sistema | ID del proceso |
| `ThreadId` | Sistema | ID del hilo |
| `Timestamp` | Sistema | Fecha/hora UTC |

### Campos HTTP (si hay HttpContext)

| Campo | Fuente | Valor sin HttpContext | Configuración |
|-------|--------|----------------------|---------------|
| `RequestPath` | HttpContext | `"N/A"` | Siempre disponible |
| `RequestMethod` | HttpContext | `"N/A"` | Siempre disponible |
| `StatusCode` | HttpContext | `0` | Siempre disponible |
| `ClientIp` | Headers HTTP | `"N/A"` | Siempre disponible |
| `UserAgent` | Headers HTTP | `"N/A"` | Siempre disponible |
| `QueryString` | HttpContext.Request.QueryString | `null` | `IncludeQueryString = true` (por defecto) |
| `RequestHeaders` | HttpContext.Request.Headers | `null` | `IncludeRequestHeaders = true` (por defecto) |
| `ResponseHeaders` | HttpContext.Response.Headers | `null` | `IncludeResponseHeaders = false` (por defecto) |
| `RequestBody` | HttpContext.Items | `null` | `IncludeRequestBody = false` (requiere middleware) |
| `ResponseBody` | HttpContext.Items | `null` | `IncludeResponseBody = false` (requiere middleware) |

### Campos de Correlación (si están habilitados)

| Campo | Fuente | Comportamiento |
|-------|--------|----------------|
| `CorrelationId` | Header HTTP o generado | Se genera automáticamente (GUID) si `EnableCorrelationId = true` |
| `RequestId` | Header HTTP o generado | Se genera automáticamente (GUID) si `EnableRequestId = true` |
| `SessionId` | Header HTTP o generado | Se genera automáticamente (GUID) si `EnableSessionId = true` |

**Nota:** Los IDs de correlación se generan automáticamente incluso sin HttpContext si están habilitados en la configuración.

### Campos de Usuario (si implementas ICurrentUserService)

| Campo | Fuente | Valor por Defecto |
|-------|--------|-------------------|
| `UserId` | ICurrentUserService | `"Anonymous"` |
| `UserName` | ICurrentUserService | `"Anonymous"` |

## Captura de Datos HTTP (Headers, Query String, Body)

El componente puede capturar información adicional de las peticiones HTTP para análisis y debugging.

### Configuración

```json
{
  "StructuredLogging": {
    "Enrichment": {
      "HttpCapture": {
        "IncludeRequestHeaders": true,
        "IncludeResponseHeaders": false,
        "IncludeQueryString": true,
        "IncludeRequestBody": false,
        "IncludeResponseBody": false,
        "MaxBodySizeBytes": 10240,
        "SensitiveHeaders": [
          "Authorization",
          "Cookie",
          "X-API-Key",
          "X-Auth-Token"
        ]
      }
    }
  }
}
```

### Opciones de Configuración

| Opción | Descripción | Valor por Defecto |
|--------|-------------|-------------------|
| `IncludeRequestHeaders` | Capturar headers HTTP de la petición | `true` |
| `IncludeResponseHeaders` | Capturar headers HTTP de la respuesta | `false` |
| `IncludeQueryString` | Capturar query string de la URL | `true` |
| `IncludeRequestBody` | Capturar body de la petición | `false` |
| `IncludeResponseBody` | Capturar body de la respuesta | `false` |
| `MaxBodySizeBytes` | Tamaño máximo del body a capturar (en bytes) | `10240` (10KB) |
| `SensitiveHeaders` | Lista de headers sensibles que se ocultan | `["Authorization", "Cookie", "X-API-Key", "X-Auth-Token"]` |

### Seguridad: Headers Sensibles

Los headers configurados en `SensitiveHeaders` se muestran como `"[REDACTED]"` en los logs para proteger información sensible como tokens y credenciales.

**Ejemplo:**
```json
{
  "requestHeaders": {
    "Accept": "application/json",
    "Content-Type": "application/json",
    "Authorization": "[REDACTED]",
    "X-API-Key": "[REDACTED]"
  }
}
```

### Captura de Request Body y Response Body

Para capturar los bodies de las peticiones y respuestas, necesitas un middleware que los lea y los almacene en `HttpContext.Items`:

```csharp
// Ejemplo de middleware para capturar Request Body
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    context.Request.Body.Position = 0;
    context.Items["JonjubNet.Logging.RequestBody"] = body;
    await next();
});
```

**Nota:** Los bodies se capturan desde `HttpContext.Items["JonjubNet.Logging.RequestBody"]` y `HttpContext.Items["JonjubNet.Logging.ResponseBody"]`.

## Ejemplo de Log Generado

```json
{
  "serviceName": "MiServicio",
  "operation": "ProcessOrder",
  "logLevel": "Information",
  "message": "Orden procesada exitosamente",
  "category": "Business",
  "eventType": "OperationEnd",
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
    "operationEnd": "2024-01-15T10:30:01.500Z",
    "operationStatus": "Completed",
    "executionTimeMs": 1500,
    "success": true
  },
  "exception": null,
  "stackTrace": null,
  "timestamp": "2024-01-15T10:30:01.500Z",
  "requestPath": "/api/orders",
  "requestMethod": "POST",
  "statusCode": 200,
  "clientIp": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "queryString": "?page=1&limit=10",
  "requestHeaders": {
    "Accept": "application/json",
    "Content-Type": "application/json",
    "Authorization": "[REDACTED]",
    "X-Requested-With": "XMLHttpRequest"
  },
  "responseHeaders": null,
  "requestBody": null,
  "responseBody": null,
  "correlationId": "d8284def-e59c-4d9c-a814-b05ea8319b03",
  "requestId": "27473e7d-6afc-4f50-8165-3ec89ccffcc2",
  "sessionId": "62ca01d0-bd52-468d-b597-2a790b12f124"
}
```

## Configuración Completa de Ejemplo

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
        "FileSizeLimitBytes": 104857600
      }
    },
    "Filters": {
      "ExcludedCategories": ["Debug", "Trace"],
      "ExcludedOperations": ["HealthCheck"],
      "ExcludedUsers": ["System"],
      "FilterByLogLevel": true,
      "CategoryLogLevels": {
        "Security": "Warning",
        "Performance": "Information"
      }
    },
    "Enrichment": {
      "IncludeEnvironment": true,
      "IncludeProcess": true,
      "IncludeThread": true,
      "IncludeMachineName": true,
      "IncludeServiceInfo": true,
      "StaticProperties": {
        "Application": "MiAplicacion",
        "Region": "us-east-1"
      },
      "HttpCapture": {
        "IncludeRequestHeaders": true,
        "IncludeResponseHeaders": false,
        "IncludeQueryString": true,
        "IncludeRequestBody": false,
        "IncludeResponseBody": false,
        "MaxBodySizeBytes": 10240,
        "SensitiveHeaders": [
          "Authorization",
          "Cookie",
          "X-API-Key",
          "X-Auth-Token"
        ]
      }
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
      "Enabled": true,
      "BootstrapServers": "localhost:9092",
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

## Mejores Prácticas: Registro de Errores en APIs

### ¿Dónde Registrar Errores?

Las mejores prácticas recomiendan registrar errores en **múltiples capas** de la aplicación:

#### 1. **Middleware Global de Manejo de Excepciones** (Recomendado - Nivel más alto)

Un middleware global captura **todos los errores no manejados** de la aplicación. Es el lugar ideal para registrar errores críticos y de infraestructura.

```csharp
// Program.cs o Startup.cs
using JonjubNet.Logging.Interfaces;

var app = builder.Build();

// Middleware de manejo global de excepciones
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandler?.Error;
        var loggingService = context.RequestServices.GetRequiredService<IStructuredLoggingService>();

        if (exception != null)
        {
            // Registrar error con información completa del contexto HTTP
            loggingService.LogError(
                message: $"Error no manejado en la aplicación: {exception.Message}",
                operation: context.Request.Path,
                category: "System",
                properties: new Dictionary<string, object>
                {
                    { "HttpMethod", context.Request.Method },
                    { "StatusCode", context.Response.StatusCode },
                    { "Path", context.Request.Path.ToString() },
                    { "QueryString", context.Request.QueryString.ToString() }
                },
                exception: exception
            );

            // Responder con error estructurado
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = new
                {
                    message = "Ha ocurrido un error interno del servidor",
                    correlationId = context.Items["JonjubNet.Logging.CorrelationId"]?.ToString()
                }
            }));
        }
    });
});

app.Run();
```

#### 2. **Controllers / Endpoints** (Errores de negocio y validación)

En los controllers, registra errores específicos de la lógica de negocio y validaciones.

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IStructuredLoggingService _loggingService;
    private readonly IOrderService _orderService;

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _loggingService.LogOperationStart("CreateOrder", "Business");
            
            // Validación de negocio
            if (request.Amount <= 0)
            {
                _loggingService.LogWarning(
                    message: "Intento de crear orden con monto inválido",
                    operation: "CreateOrder",
                    category: "Business",
                    properties: new Dictionary<string, object>
                    {
                        { "RequestedAmount", request.Amount },
                        { "UserId", request.UserId }
                    }
                );
                
                return BadRequest(new { error = "El monto debe ser mayor a cero" });
            }

            var order = await _orderService.CreateOrderAsync(request);
            stopwatch.Stop();

            _loggingService.LogOperationEnd(
                operation: "CreateOrder",
                category: "Business",
                executionTimeMs: stopwatch.ElapsedMilliseconds,
                properties: new Dictionary<string, object>
                {
                    { "OrderId", order.Id },
                    { "Amount", order.Amount }
                },
                success: true
            );

            return Ok(order);
        }
        catch (BusinessException ex)
        {
            stopwatch.Stop();
            
            // Error de negocio - registrar como Warning o Error según la gravedad
            _loggingService.LogError(
                message: $"Error de negocio al crear orden: {ex.Message}",
                operation: "CreateOrder",
                category: "Business",
                properties: new Dictionary<string, object>
                {
                    { "RequestedAmount", request.Amount },
                    { "UserId", request.UserId },
                    { "BusinessRule", ex.BusinessRule }
                },
                exception: ex
            );

            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Error inesperado - registrar como Error crítico
            _loggingService.LogError(
                message: $"Error inesperado al crear orden: {ex.Message}",
                operation: "CreateOrder",
                category: "System",
                properties: new Dictionary<string, object>
                {
                    { "RequestedAmount", request.Amount },
                    { "UserId", request.UserId }
                },
                exception: ex
            );

            // Re-lanzar para que el middleware global lo maneje
            throw;
        }
    }
}
```

#### 3. **Servicios / Lógica de Negocio** (Errores específicos del dominio)

En los servicios, registra errores relacionados con la lógica de negocio y operaciones de datos.

```csharp
public class OrderService : IOrderService
{
    private readonly IStructuredLoggingService _loggingService;
    private readonly IOrderRepository _repository;

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        try
        {
            _loggingService.LogInformation(
                message: "Iniciando creación de orden",
                operation: "CreateOrder",
                category: "Business",
                properties: new Dictionary<string, object>
                {
                    { "UserId", request.UserId },
                    { "Amount", request.Amount }
                }
            );

            // Validar stock
            var hasStock = await _repository.CheckStockAsync(request.ProductId);
            if (!hasStock)
            {
                _loggingService.LogWarning(
                    message: "Intento de crear orden sin stock disponible",
                    operation: "CreateOrder",
                    category: "Business",
                    properties: new Dictionary<string, object>
                    {
                        { "ProductId", request.ProductId },
                        { "RequestedQuantity", request.Quantity }
                    }
                );
                
                throw new BusinessException("No hay stock disponible", "INSUFFICIENT_STOCK");
            }

            var order = await _repository.CreateAsync(request);
            
            _loggingService.LogInformation(
                message: "Orden creada exitosamente",
                operation: "CreateOrder",
                category: "Business",
                properties: new Dictionary<string, object>
                {
                    { "OrderId", order.Id }
                }
            );

            return order;
        }
        catch (BusinessException)
        {
            // Re-lanzar excepciones de negocio sin modificar
            throw;
        }
        catch (Exception ex)
        {
            // Error inesperado en la capa de servicio
            _loggingService.LogError(
                message: $"Error al crear orden en el servicio: {ex.Message}",
                operation: "CreateOrder",
                category: "Database",
                properties: new Dictionary<string, object>
                {
                    { "ProductId", request.ProductId },
                    { "UserId", request.UserId }
                },
                exception: ex
            );
            
            throw;
        }
    }
}
```

### ¿Qué Información Registrar?

#### ✅ **Información Obligatoria en Errores:**

1. **Mensaje descriptivo**: Descripción clara del error
2. **Operación**: Nombre de la operación que falló
3. **Categoría**: Tipo de error (System, Business, Database, etc.)
4. **Excepción completa**: Objeto `Exception` con stack trace
5. **Contexto HTTP**: Path, método, query string (capturado automáticamente)
6. **IDs de correlación**: CorrelationId, RequestId, SessionId (generados automáticamente)
7. **Propiedades relevantes**: Datos del request que causaron el error

#### ✅ **Información Recomendada:**

```csharp
_loggingService.LogError(
    message: "Error al procesar pago",
    operation: "ProcessPayment",
    category: "Business",
    properties: new Dictionary<string, object>
    {
        // Datos del request
        { "PaymentId", paymentId },
        { "Amount", amount },
        { "PaymentMethod", paymentMethod },
        
        // Contexto adicional
        { "UserId", userId },
        { "OrderId", orderId },
        
        // Información de diagnóstico
        { "RetryCount", retryCount },
        { "GatewayResponse", gatewayResponse }
    },
    context: new Dictionary<string, object>
    {
        { "PaymentAttempt", attemptNumber },
        { "PaymentTimestamp", DateTime.UtcNow }
    },
    exception: ex
);
```

### Niveles de Log según Tipo de Error

| Tipo de Error | Nivel de Log | Ejemplo |
|---------------|--------------|---------|
| **Error de validación** | `Warning` | Datos inválidos del usuario |
| **Error de negocio** | `Warning` o `Error` | Regla de negocio violada |
| **Error de infraestructura** | `Error` | Base de datos no disponible |
| **Error crítico** | `Critical` | Error que requiere atención inmediata |
| **Error de seguridad** | `Error` + `Security` | Intento de acceso no autorizado |

### Ejemplo Completo: Middleware de Manejo de Errores

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IStructuredLoggingService _loggingService;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        IStructuredLoggingService loggingService,
        IWebHostEnvironment environment)
    {
        _next = next;
        _loggingService = loggingService;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            NotFoundException => StatusCodes.Status404NotFound,
            BusinessException => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

        // Determinar categoría según el tipo de excepción
        var category = exception switch
        {
            BusinessException => "Business",
            UnauthorizedAccessException => "Security",
            TimeoutException => "Performance",
            _ => "System"
        };

        // Registrar error con información completa
        _loggingService.LogError(
            message: $"Error procesando petición: {exception.Message}",
            operation: context.Request.Path,
            category: category,
            properties: new Dictionary<string, object>
            {
                { "HttpMethod", context.Request.Method },
                { "StatusCode", statusCode },
                { "Path", context.Request.Path.ToString() },
                { "QueryString", context.Request.QueryString.ToString() },
                { "ExceptionType", exception.GetType().Name }
            },
            exception: exception
        );

        // Responder con error estructurado
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = new
            {
                message = _environment.IsDevelopment() ? exception.Message : "Ha ocurrido un error",
                correlationId = context.Items["JonjubNet.Logging.CorrelationId"]?.ToString(),
                timestamp = DateTime.UtcNow
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

// Registrar en Program.cs
app.UseMiddleware<GlobalExceptionMiddleware>();
```

### Mejores Prácticas Resumidas

1. ✅ **Usa middleware global** para errores no manejados
2. ✅ **Registra en cada capa** (Controllers, Services, Repositories)
3. ✅ **Incluye contexto completo**: Request, usuario, operación
4. ✅ **Usa niveles apropiados**: Warning para validaciones, Error para fallos, Critical para errores críticos
5. ✅ **No registres información sensible**: Passwords, tokens completos, datos personales
6. ✅ **Incluye la excepción completa**: El componente captura automáticamente el stack trace
7. ✅ **Usa CorrelationId**: Permite rastrear errores relacionados en múltiples servicios
8. ✅ **Registra antes de re-lanzar**: Asegúrate de registrar antes de `throw`
9. ✅ **No registres en loops**: Evita registrar el mismo error múltiples veces
10. ✅ **Usa categorías consistentes**: System, Business, Security, Database, etc.

### Ejemplo de Log de Error Generado

```json
{
  "serviceName": "OrderService",
  "operation": "CreateOrder",
  "logLevel": "Error",
  "message": "Error al procesar pago: Insufficient funds",
  "category": "Business",
  "eventType": "Custom",
  "userId": "user123",
  "userName": "john.doe",
  "environment": "Production",
  "version": "1.0.0",
  "properties": {
    "PaymentId": "PAY-12345",
    "Amount": 1500.00,
    "PaymentMethod": "CreditCard",
    "ExceptionType": "BusinessException"
  },
  "exception": "BusinessException: Insufficient funds\n   at OrderService.ProcessPayment()...",
  "stackTrace": "   at OrderService.ProcessPayment()...",
  "timestamp": "2025-01-15T10:30:00Z",
  "requestPath": "/api/orders",
  "requestMethod": "POST",
  "statusCode": 422,
  "correlationId": "d8284def-e59c-4d9c-a814-b05ea8319b03",
  "requestId": "27473e7d-6afc-4f50-8165-3ec89ccffcc2"
}
```

## Solución de Problemas

### Los campos HTTP están en "N/A"
- ✅ `IHttpContextAccessor` ya se registra automáticamente
- Verifica que el log se genere dentro del contexto de una petición HTTP

### `UserId` y `UserName` están en "Anonymous"
- Implementa `ICurrentUserService` personalizado
- Regístralo con `AddStructuredLoggingInfrastructure<CustomUserService>`

### `CorrelationId`, `RequestId`, `SessionId` están en `null`
- Habilita en `appsettings.json`: `"EnableCorrelationId": true`, etc.
- Una vez habilitados, se generan automáticamente incluso sin HttpContext

### `QueryString` o `RequestHeaders` no aparecen en los logs
- Verifica que `IncludeQueryString` o `IncludeRequestHeaders` estén en `true` en la configuración
- Por defecto están habilitados (`IncludeQueryString = true`, `IncludeRequestHeaders = true`)
- Si están deshabilitados, habilítalos en `Enrichment.HttpCapture`

### `RequestBody` o `ResponseBody` están siempre en `null`
- Estos campos requieren un middleware que capture los bodies y los almacene en `HttpContext.Items`
- Ver la sección "Captura de Request Body y Response Body" para un ejemplo de middleware
- Los bodies se capturan desde `HttpContext.Items["JonjubNet.Logging.RequestBody"]` y `HttpContext.Items["JonjubNet.Logging.ResponseBody"]`

### Kafka no envía mensajes
- Verifica que `"Enabled": true` en `KafkaProducer`
- Para conexión nativa: verifica que `BootstrapServers` sea accesible
- Para REST Proxy: verifica que `ProducerUrl` apunte al REST Proxy correcto
- Para Webhook: verifica que `UseWebhook: true` y que la URL sea accesible

## Licencia

Este proyecto está licenciado bajo la Licencia MIT.
