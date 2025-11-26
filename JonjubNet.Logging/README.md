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
