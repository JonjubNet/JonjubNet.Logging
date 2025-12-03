# JonjubNet.Logging

![.NET Version](https://img.shields.io/badge/.NET-8.0-blue)
![NuGet Version](https://img.shields.io/badge/nuget-v1.0.24-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Build Status](https://img.shields.io/badge/build-passing-brightgreen)

Biblioteca de logging estructurado para aplicaciones .NET con soporte para múltiples sinks (Console, File, HTTP, Elasticsearch, Kafka), funcionalidades avanzadas de correlación y enriquecimiento, y logging automático para operaciones MediatR sin código manual.

## 📋 Tabla de Contenidos

- [Características](#-características)
- [Instalación](#-instalación)
- [Quick Start](#-quick-start)
- [Configuración Paso a Paso](#-configuración-paso-a-paso)
- [Uso Básico](#-uso-básico)
- [Ejemplos Avanzados](#-ejemplos-avanzados)
- [Personalización](#-personalización)
- [Arquitectura](#-arquitectura)
- [Troubleshooting](#-troubleshooting)
- [Documentación Adicional](#-documentación-adicional)

## ✨ Características

### Funcionalidades Principales

- **Logging Estructurado**: Logs en formato JSON estructurado con propiedades enriquecidas
- **Múltiples Sinks**: Soporte para Console, File, HTTP, Elasticsearch y Kafka
- **Correlación de Logs**: IDs de correlación, request y sesión para rastrear operaciones
- **Enriquecimiento Automático**: Información de usuario, HTTP context, ambiente, versión, etc.
- **Categorización de Errores**: Distinción entre errores funcionales y técnicos
- **Filtrado Dinámico**: Filtros por categoría, operación, usuario y nivel de log aplicados antes de enviar a sinks
- **Log Scopes**: Contexto temporal que se propaga a todos los logs dentro de un scope
- **Log Sampling / Rate Limiting**: Reducción de volumen de logs en producción mediante sampling probabilístico y límites por minuto
- **Data Sanitization**: Enmascaramiento automático de datos sensibles (PII, PCI) para cumplimiento y seguridad
- **Captura HTTP**: Headers, query string, body de request/response (configurable)
- **Clean Architecture**: Implementado siguiendo principios de Clean Architecture

### Niveles de Log Soportados

- `Trace` - Información muy detallada
- `Debug` - Información de depuración
- `Information` - Información general
- `Warning` - Advertencias
- `Error` - Errores
- `Critical` - Errores críticos

### Tipos de Eventos Especiales

- **Operaciones**: Inicio y fin de operaciones con tiempo de ejecución
- **Acciones de Usuario**: Tracking de acciones realizadas por usuarios
- **Eventos de Seguridad**: Logging de eventos relacionados con seguridad
- **Eventos de Auditoría**: Registro de eventos de auditoría

## 📦 Instalación

### Paso 1: Instalar el Paquete NuGet

```bash
dotnet add package JonjubNet.Logging
```

O desde el Package Manager Console:

```powershell
Install-Package JonjubNet.Logging
```

### Paso 2: Verificar Dependencias

El paquete incluye todas las dependencias necesarias:
- Serilog y sus sinks
- Microsoft.Extensions.*
- Confluent.Kafka (para Kafka)

## 🚀 Quick Start

El ejemplo más simple para empezar:

```csharp
// 1. Instalar el paquete
dotnet add package JonjubNet.Logging

// 2. Configurar appsettings.json
{
  "StructuredLogging": {
    "Enabled": true,
    "MinimumLevel": "Information",
    "ServiceName": "MiServicio"
  }
}

// 3. Registrar en Program.cs
using JonjubNet.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddStructuredLoggingInfrastructure(builder.Configuration);

// 4. Usar en tu código
_loggingService.LogInformation("Hello World!");
```

**¡Listo!** Ya tienes logging estructurado funcionando. Para más opciones, consulta la [Configuración Completa](#-configuración-paso-a-paso) más abajo.

## ⚙️ Configuración Paso a Paso

### 📋 **Cómo Funciona la Configuración en Microservicios**

Cuando agregas el componente a un microservicio, **la configuración se lee automáticamente desde el `appsettings.json` del microservicio**. El componente busca la sección `"StructuredLogging"` y mapea todos los parámetros automáticamente.

**Características:**
- ✅ **Lectura automática** desde `appsettings.json` del microservicio
- ✅ **Hot-reload** - Cambios se detectan automáticamente (si `reloadOnChange: true`)
- ✅ **Valores por defecto** - Si falta un parámetro, usa valores por defecto
- ✅ **Sin código adicional** - Solo configuración JSON

### Paso 1: Configurar appsettings.json en tu Microservicio

Crea o actualiza el archivo `appsettings.json` de tu microservicio con la sección `StructuredLogging`:

```json
{
  "StructuredLogging": {
    "Enabled": true,
    "MinimumLevel": "Information",
    "ServiceName": "MiMicroservicio",
    "Environment": "Production",
    "Version": "1.0.0",
    
    "Sinks": {
      "EnableConsole": true,
      "EnableFile": true,
      "EnableHttp": false,
      "EnableElasticsearch": true,
      "File": {
        "Path": "logs/log-.txt",
        "RollingInterval": "Day",
        "RetainedFileCountLimit": 30,
        "FileSizeLimitBytes": 104857600,
        "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
      },
      "Http": {
        "Url": "https://mi-servidor-logs.com/api/logs",
        "ContentType": "application/json",
        "BatchPostingLimit": 100,
        "PeriodSeconds": 2,
        "Headers": {
          "Authorization": "Bearer token123"
        }
      },
      "Elasticsearch": {
        "Url": "http://elasticsearch:9200",
        "IndexFormat": "logs-{0:yyyy.MM.dd}",
        "Username": "elastic",
        "Password": "password",
        "EnableAuthentication": true
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
      },
      "OperationLogLevels": {
        "GetUser": "Debug",
        "CreateOrder": "Information"
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
      "Enabled": false,
      "BootstrapServers": "localhost:9092",
      "ProducerUrl": "http://localhost:8080/api/logs",
      "Topic": "structured-logs",
      "TimeoutSeconds": 5,
      "BatchSize": 100,
      "LingerMs": 5,
      "RetryCount": 3,
      "EnableCompression": true,
      "CompressionType": "gzip",
      "Headers": {
        "Content-Type": "application/json"
      }
    },
    
    "Sampling": {
      "Enabled": false,
      "SamplingRates": {
        "Debug": 0.01,
        "Information": 0.1,
        "Trace": 0.001
      },
      "MaxLogsPerMinute": {
        "Information": 1000,
        "Debug": 500
      },
      "NeverSampleCategories": ["Security", "Audit", "Error", "Critical"],
      "NeverSampleLevels": ["Error", "Critical"]
    },
    
    "DataSanitization": {
      "Enabled": true,
      "SensitivePropertyNames": [
        "Password",
        "Passwd",
        "Pwd",
        "CreditCard",
        "CardNumber",
        "CCNumber",
        "SSN",
        "SocialSecurityNumber",
        "Email",
        "EmailAddress",
        "Phone",
        "PhoneNumber",
        "Mobile",
        "Token",
        "AccessToken",
        "RefreshToken",
        "ApiKey",
        "Secret",
        "SecretKey",
        "Authorization",
        "AuthToken"
      ],
      "SensitivePatterns": [
        "\\b\\d{4}[\\s-]?\\d{4}[\\s-]?\\d{4}[\\s-]?\\d{4}\\b",
        "\\b\\d{3}-\\d{2}-\\d{4}\\b",
        "\\b\\d{3}\\.\\d{3}\\.\\d{4}\\b"
      ],
      "MaskValue": "***REDACTED***",
      "MaskPartial": true,
      "PartialMaskLength": 4
    },
    
    "CircuitBreaker": {
      "Enabled": true,
      "Default": {
        "FailureThreshold": 5,
        "OpenTimeout": "00:01:00",
        "HalfOpenTestCount": 3
      },
      "PerSink": {
        "Http": {
          "Enabled": true,
          "FailureThreshold": 3,
          "OpenTimeout": "00:00:30"
        },
        "Elasticsearch": {
          "Enabled": true,
          "FailureThreshold": 5,
          "OpenTimeout": "00:02:00"
        }
      }
    },
    
    "RetryPolicy": {
      "Enabled": true,
      "Default": {
        "Strategy": "ExponentialBackoff",
        "MaxRetries": 3,
        "InitialDelay": "00:00:01",
        "MaxDelay": "00:00:30",
        "BackoffMultiplier": 2.0
      },
      "PerSink": {
        "Http": {
          "Enabled": true,
          "Strategy": "FixedDelay",
          "MaxRetries": 5,
          "InitialDelay": "00:00:02"
        },
        "Elasticsearch": {
          "Enabled": true,
          "Strategy": "ExponentialBackoff",
          "MaxRetries": 3,
          "InitialDelay": "00:00:01"
        }
      },
      "NonRetryableExceptions": [
        "System.ArgumentException",
        "System.UnauthorizedAccessException",
        "System.ArgumentNullException"
      ]
    },
    
    "DeadLetterQueue": {
      "Enabled": true,
      "MaxSize": 10000,
      "RetryInterval": "00:05:00",
      "MaxRetriesPerItem": 10,
      "ItemRetentionPeriod": "7.00:00:00",
      "AutoRetry": true,
      "Storage": "InMemory",
      "PersistencePath": null
    },
    
    "Batching": {
      "Enabled": true,
      "DefaultBatchSize": 100,
      "MaxBatchIntervalMs": 1000,
      "BatchSizeBySink": {
        "Http": 200,
        "Elasticsearch": 500,
        "Kafka": 1000
      },
      "EnableCompression": false,
      "CompressionLevel": "Optimal",
      "EnablePriorityQueues": false,
      "QueueCapacityByPriority": {
        "Critical": 10000,
        "Error": 5000,
        "Warning": 3000,
        "Information": 2000,
        "Debug": 1000,
        "Trace": 500
      },
      "EnablePriorityProcessing": false,
      "CriticalProcessingIntervalMs": 100,
      "NormalProcessingIntervalMs": 1000
    }
  }
}
```

### Paso 2: Registrar Servicios en Program.cs (o Startup.cs)

El componente **automáticamente lee la configuración** desde `appsettings.json` cuando pasas `builder.Configuration` o `Configuration`.

#### Opción A: Configuración Simple (Todo en appsettings.json)

**Recomendado para:** Microservicios pequeños/medianos con configuración <500 líneas

#### Para ASP.NET Core 6.0+ (Program.cs)

```csharp
using JonjubNet.Logging;

var builder = WebApplication.CreateBuilder(args);

// Opcional: Habilitar hot-reload para cambios automáticos en appsettings.json
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Registrar servicios de logging estructurado
// ⚠️ IMPORTANTE: Pasa builder.Configuration para que lea automáticamente appsettings.json
builder.Services.AddStructuredLoggingInfrastructure(builder.Configuration);

// ... resto de tu configuración

var app = builder.Build();

// ... resto de tu aplicación

app.Run();
```

#### Opción B: Configuración Modular (Archivos Separados)

**Recomendado para:** Microservicios con múltiples componentes y configuración extensa (>500 líneas)

Cuando tienes múltiples componentes con configuración extensa, puedes separar la configuración en archivos individuales:

**Estructura de archivos:**
```
Microservicio/
├── appsettings.json                    (base - mínimo)
│   {
│     "Logging": { "LogLevel": { "Default": "Information" } },
│     "ConnectionStrings": { ... }
│   }
│
├── appsettings.Production.json         (overrides por entorno)
│   {
│     "StructuredLogging": {
│       "MinimumLevel": "Warning"
│     }
│   }
│
└── config/
    ├── structured-logging.json         (configuración completa del componente)
    ├── monitoring.json                  (otro componente)
    └── caching.json                     (otro componente)
```

**Program.cs con carga automática:**
```csharp
using JonjubNet.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configuración base
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddComponentConfigurations(builder.Environment.ContentRootPath); // Carga automática de todos los .json en config/

// Registrar servicios
builder.Services.AddStructuredLoggingInfrastructure(builder.Configuration);

var app = builder.Build();
app.Run();
```

**O cargar componentes específicos:**
```csharp
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddComponentConfigurations(builder.Environment.ContentRootPath, "structured-logging", "monitoring"); // Solo estos componentes
```
<｜tool▁calls▁begin｜><｜tool▁call▁begin｜>
run_terminal_cmd

**O cargar un archivo específico:**
```csharp
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddComponentConfiguration("config/structured-logging.json", optional: true);
```

**Ventajas de la configuración modular:**
- ✅ Separa configuración de cada componente
- ✅ `appsettings.json` más limpio y fácil de leer
- ✅ Archivos pueden compartirse entre microservicios
- ✅ Fácil de mantener y versionar
- ✅ Hot-reload funciona automáticamente

**Nota:** El componente busca la sección `"StructuredLogging"` en la configuración combinada, por lo que funciona igual con archivos separados.

#### Para ASP.NET Core 5.0 o anterior (Startup.cs)

```csharp
using JonjubNet.Logging;

public class Startup
{
    public IConfiguration Configuration { get; }
    
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    public void ConfigureServices(IServiceCollection services)
    {
        // Registrar servicios de logging estructurado
        // ⚠️ IMPORTANTE: Pasa Configuration para que lea automáticamente appsettings.json
        services.AddStructuredLoggingInfrastructure(Configuration);
        
        // ... resto de tus servicios
    }
}
```

#### Para Aplicaciones sin Host (Console Apps, etc.)

```csharp
using JonjubNet.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();

// Usar método sin host (no requiere BackgroundService)
services.AddStructuredLoggingInfrastructureWithoutHost<DefaultCurrentUserService>(configuration);

var serviceProvider = services.BuildServiceProvider();
```

**Nota:** El componente detecta automáticamente si hay un `IHost` disponible y registra los servicios apropiados (con o sin `BackgroundService`).

### Paso 3: Inyectar y Usar el Servicio

En cualquier clase donde necesites logging:

```csharp
using JonjubNet.Logging.Application.Interfaces;

public class MiServicio
{
    private readonly IStructuredLoggingService _loggingService;

    public MiServicio(IStructuredLoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public void MiMetodo()
    {
        _loggingService.LogInformation("Operación completada", "MiMetodo", "Business");
    }
}
```

## 🚀 Uso Básico

### Logging Simple

```csharp
// Log de información básico
_loggingService.LogInformation("Aplicación iniciada correctamente");

// Log con operación y categoría
_loggingService.LogInformation("Usuario autenticado", "Authentication", "Security");

// Log con propiedades adicionales
_loggingService.LogInformation("Producto creado", "CreateProduct", "Business",
    properties: new Dictionary<string, object>
    {
        { "ProductId", "PROD-12345" },
        { "ProductName", "Laptop Gaming" },
        { "Price", 1299.99 }
    });
```

### Logging de Errores

```csharp
try
{
    // Tu código aquí
}
catch (Exception ex)
{
    _loggingService.LogError("Error al procesar solicitud", "ProcessRequest", "General",
        properties: new Dictionary<string, object> { { "RequestId", "REQ-12345" } },
        exception: ex);
}
```

### Logging de Operaciones

```csharp
var operationName = "ProcessOrder";
var category = "Business";

// Iniciar operación
_loggingService.LogOperationStart(operationName, category,
    properties: new Dictionary<string, object> { { "OrderId", "ORD-12345" } });

var stopwatch = System.Diagnostics.Stopwatch.StartNew();

try
{
    // Tu lógica aquí
    await ProcessOrderAsync();
    
    // Operación exitosa
    stopwatch.Stop();
    _loggingService.LogOperationEnd(operationName, category, 
        executionTimeMs: stopwatch.ElapsedMilliseconds,
        properties: new Dictionary<string, object> 
        { 
            { "OrderId", "ORD-12345" },
            { "Status", "Completed" }
        });
}
catch (Exception ex)
{
    // Operación fallida
    stopwatch.Stop();
    _loggingService.LogOperationEnd(operationName, category,
        executionTimeMs: stopwatch.ElapsedMilliseconds,
        success: false,
        exception: ex);
    throw;
}
```

### Todos los Niveles de Log

```csharp
// Trace - Información muy detallada
_loggingService.LogTrace("Iniciando validación de datos", "ValidateData", "Debug");

// Debug - Información de depuración
_loggingService.LogDebug("Datos validados correctamente", "ValidateData", "Debug");

// Information - Información general
_loggingService.LogInformation("Proceso completado exitosamente", "ProcessData", "General");

// Warning - Advertencias
_loggingService.LogWarning("Límite de memoria alcanzado", "MemoryCheck", "System",
    properties: new Dictionary<string, object> { { "MemoryUsage", "85%" } });

// Error - Errores
_loggingService.LogError("Error de conexión a base de datos", "DatabaseConnection", "Database");

// Critical - Errores críticos
_loggingService.LogCritical("Sistema no disponible", "SystemCheck", "System");
```

## 📚 Ejemplos Avanzados

### Eventos de Usuario

```csharp
_loggingService.LogUserAction("UpdateProfile", "User", "USER-12345",
    properties: new Dictionary<string, object>
    {
        { "FieldUpdated", "Email" },
        { "OldValue", "old@email.com" },
        { "NewValue", "new@email.com" }
    });
```

### Eventos de Seguridad

```csharp
_loggingService.LogSecurityEvent("FailedLogin", "Intento de login fallido",
    properties: new Dictionary<string, object>
    {
        { "IP", "192.168.1.100" },
        { "UserAgent", "Mozilla/5.0..." },
        { "Attempts", 3 }
    });
```

### Eventos de Auditoría

```csharp
_loggingService.LogAuditEvent("DataAccess", "Consulta de datos sensibles", 
    "User", "USER-12345",
    properties: new Dictionary<string, object>
    {
        { "DataAccessed", "PersonalInformation" },
        { "AccessReason", "ProfileUpdate" }
    });
```

### Logging Personalizado

```csharp
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Domain.ValueObjects;

var customLogEntry = new StructuredLogEntry
{
    ServiceName = "MiServicio",
    Operation = "CustomOperation",
    LogLevel = LogLevelValue.Information.Value,
    Message = "Operación personalizada ejecutada",
    Category = LogCategoryValue.Custom.Value,
    Properties = new Dictionary<string, object>
    {
        { "CustomProperty1", "Valor1" },
        { "CustomProperty2", 42 }
    }
};

_loggingService.LogCustom(customLogEntry);
```

## 🆕 Nuevas Funcionalidades

### Log Scopes (Contexto Temporal)

Los scopes permiten agregar propiedades a todos los logs dentro de un contexto específico, sin necesidad de pasarlas en cada llamada:

```csharp
using (_loggingService.BeginScope(new Dictionary<string, object> 
{ 
    { "OrderId", "ORD-123" },
    { "CustomerId", "CUST-456" }
}))
{
    // Todos estos logs incluirán automáticamente OrderId y CustomerId
    _loggingService.LogInformation("Procesando orden"); 
    _loggingService.LogInformation("Validando pago");
    _loggingService.LogInformation("Enviando confirmación");
}

// También puedes usar una sola propiedad
using (_loggingService.BeginScope("RequestId", "REQ-789"))
{
    _loggingService.LogInformation("Operación iniciada");
}
```

### Log Sampling / Rate Limiting

Reduce el volumen de logs en producción mediante sampling probabilístico y límites por minuto:

```json
{
  "StructuredLogging": {
    "Sampling": {
      "Enabled": true,
      "SamplingRates": {
        "Debug": 0.01,      // Solo 1% de logs Debug
        "Information": 0.1, // Solo 10% de logs Information
        "Trace": 0.001      // Solo 0.1% de logs Trace
      },
      "MaxLogsPerMinute": {
        "Information": 1000,
        "Debug": 500
      },
      "NeverSampleCategories": ["Security", "Audit", "Error", "Critical"],
      "NeverSampleLevels": ["Error", "Critical"]
    }
  }
}
```

**Beneficios:**
- Reduce costos de almacenamiento
- Mejora performance en alta carga
- Mantiene logs representativos sin saturar
- Los logs críticos (Error, Critical, Security, Audit) nunca se muestrean

### Data Sanitization (Enmascaramiento de Datos Sensibles)

Enmascara automáticamente datos sensibles (PII, PCI) para cumplimiento y seguridad:

```json
{
  "StructuredLogging": {
    "DataSanitization": {
      "Enabled": true,
      "SensitivePropertyNames": [
        "Password", "CreditCard", "SSN", "Email", "Phone", "Token"
      ],
      "SensitivePatterns": [
        "\\b\\d{4}[\\s-]?\\d{4}[\\s-]?\\d{4}[\\s-]?\\d{4}\\b", // Tarjetas de crédito
        "\\b\\d{3}-\\d{2}-\\d{4}\\b" // SSN
      ],
      "MaskValue": "***REDACTED***",
      "MaskPartial": true,
      "PartialMaskLength": 4
    }
  }
}
```

**Ejemplo:**
```csharp
_loggingService.LogInformation("Usuario autenticado", "Login", "Security",
    properties: new Dictionary<string, object>
    {
        { "Email", "user@example.com" },        // Se enmascara
        { "Password", "secret123" },            // Se enmascara completamente
        { "CreditCard", "1234-5678-9012-3456" } // Se muestra como: ***REDACTED***3456
    });
```

**Beneficios:**
- Cumplimiento con GDPR, PCI-DSS, HIPAA
- Seguridad mejorada
- Prevención de exposición accidental de datos

### Filtrado Dinámico Pre-Sink

Los filtros se aplican automáticamente antes de enviar logs a los sinks:

```json
{
  "StructuredLogging": {
    "Filters": {
      "ExcludedCategories": ["Debug", "Trace"],
      "ExcludedOperations": ["HealthCheck"],
      "ExcludedUsers": ["System"],
      "FilterByLogLevel": true,
      "CategoryLogLevels": {
        "Security": "Warning",
        "Performance": "Information"
      },
      "OperationLogLevels": {
        "GetUser": "Debug",
        "CreateOrder": "Information"
      }
    }
  }
}
```

**Beneficios:**
- Reduce logs innecesarios
- Mejora performance
- Ahorra costos de almacenamiento

### Configuración Dinámica Avanzada (Hot-Reload)

El componente soporta cambios dinámicos de configuración sin reiniciar:

#### Cambio de Nivel por Categoría/Operación

```csharp
using JonjubNet.Logging.Application.Interfaces;

// En un controlador o servicio
public class LoggingController : ControllerBase
{
    private readonly ILoggingConfigurationManager _configManager;
    
    public LoggingController(ILoggingConfigurationManager configManager)
    {
        _configManager = configManager;
    }
    
    [HttpPost("logging/level")]
    public IActionResult ChangeLogLevel([FromBody] ChangeLevelRequest request)
    {
        // Cambiar nivel global
        _configManager.SetMinimumLevel(request.Level);
        
        // Cambiar nivel por categoría
        _configManager.SetCategoryLogLevel(request.Category, request.Level);
        
        // Cambiar nivel por operación
        _configManager.SetOperationLogLevel(request.Operation, request.Level);
        
        return Ok();
    }
    
    [HttpPost("logging/override")]
    public IActionResult SetTemporaryOverride([FromBody] OverrideRequest request)
    {
        // Override temporal con expiración automática
        _configManager.SetTemporaryLogLevelOverride(
            request.Category, 
            request.Level, 
            TimeSpan.FromMinutes(request.DurationMinutes));
        
        return Ok();
    }
}
```

#### Hot-Reload desde appsettings.json

Si tienes `reloadOnChange: true` en la configuración, los cambios en `appsettings.json` se detectan automáticamente:

```csharp
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
```

**Beneficios:**
- Cambios sin reiniciar la aplicación
- Overrides temporales para debugging
- Configuración por categoría/operación específica

### Resiliencia y Circuit Breakers

El componente incluye circuit breakers, retry policies y dead letter queue para garantizar la entrega de logs:

```json
{
  "StructuredLogging": {
    "CircuitBreaker": {
      "Enabled": true,
      "Default": {
        "FailureThreshold": 5,
        "OpenTimeout": "00:01:00",
        "HalfOpenTestCount": 3
      },
      "PerSink": {
        "Http": {
          "Enabled": true,
          "FailureThreshold": 3
        }
      }
    },
    "RetryPolicy": {
      "Enabled": true,
      "Default": {
        "Strategy": "ExponentialBackoff",
        "MaxRetries": 3,
        "InitialDelay": "00:00:01",
        "MaxDelay": "00:00:30"
      }
    },
    "DeadLetterQueue": {
      "Enabled": true,
      "MaxSize": 10000,
      "AutoRetry": true,
      "RetryInterval": "00:05:00"
    }
  }
}
```

**Estrategias de Retry disponibles:**
- `NoRetry` - Sin reintentos
- `FixedDelay` - Delay fijo entre intentos
- `ExponentialBackoff` - Delay exponencial creciente
- `JitteredExponentialBackoff` - Exponential backoff con jitter

**Beneficios:**
- Protección contra fallos de sinks
- Reintentos automáticos con backoff
- Dead letter queue para logs fallidos
- Recuperación automática

### Batch Processing Avanzado

El componente agrupa logs en batches para mejorar el rendimiento:

```json
{
  "StructuredLogging": {
    "Batching": {
      "Enabled": true,
      "DefaultBatchSize": 100,
      "MaxBatchIntervalMs": 1000,
      "BatchSizeBySink": {
        "Http": 200,
        "Elasticsearch": 500
      },
      "EnableCompression": false,
      "CompressionLevel": "Optimal",
      "EnablePriorityQueues": false,
      "EnablePriorityProcessing": false
    }
  }
}
```

**Características:**
- **Batching Inteligente**: Agrupa logs por tiempo/volumen
- **Compresión**: GZip para batches (opcional, deshabilitado por defecto)
- **Priorización**: Colas separadas por nivel de log (opcional, deshabilitado por defecto)
- **Procesamiento Prioritario**: Errores críticos se procesan primero (opcional)

**Beneficios:**
- Mejora significativa de rendimiento
- Reduce llamadas a sinks
- Optimización de ancho de banda (con compresión)
- Priorización de logs críticos (si está habilitado)

## 🔧 Personalización

### Servicio de Usuario Personalizado

Si necesitas obtener información del usuario desde JWT, sesiones o headers personalizados:

#### Paso 1: Crear tu Implementación

```csharp
using JonjubNet.Logging.Application.Interfaces;
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
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var subClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier) ?? 
                          httpContext.User.FindFirst("sub");
            return subClaim?.Value;
        }
        return null;
    }

    public string? GetCurrentUserName()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var nameClaim = httpContext.User.FindFirst(ClaimTypes.Name) ?? 
                           httpContext.User.FindFirst("name");
            return nameClaim?.Value;
        }
        return null;
    }

    public string? GetCurrentUserEmail()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var emailClaim = httpContext.User.FindFirst(ClaimTypes.Email) ?? 
                            httpContext.User.FindFirst("email");
            return emailClaim?.Value;
        }
        return null;
    }

    public IEnumerable<string> GetCurrentUserRoles()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            return httpContext.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct();
        }
        return new List<string>();
    }

    public bool IsInRole(string role)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.IsInRole(role) ?? false;
    }

    public bool IsAuthenticated()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.Identity?.IsAuthenticated == true;
    }
}
```

#### Paso 2: Registrar tu Servicio Personalizado

```csharp
// En Program.cs o Startup.cs
builder.Services.AddStructuredLoggingInfrastructure<CustomUserService>(builder.Configuration);
```

### Configuración de Kafka

Para habilitar el envío de logs a Kafka:

```json
{
  "StructuredLogging": {
    "KafkaProducer": {
      "Enabled": true,
      "BootstrapServers": "localhost:9092",
      "Topic": "structured-logs",
      "TimeoutSeconds": 5,
      "BatchSize": 100,
      "EnableCompression": true,
      "CompressionType": "gzip"
    }
  }
}
```

**Opciones de Conexión a Kafka:**

1. **Conexión Nativa (BootstrapServers)**: Conexión directa usando el protocolo binario de Kafka
   ```json
   "BootstrapServers": "localhost:9092"
   ```

2. **REST Proxy (ProducerUrl)**: A través de Kafka REST Proxy
   ```json
   "ProducerUrl": "http://kafka-rest:8082",
   "UseWebhook": false
   ```

3. **Webhook HTTP (ProducerUrl + UseWebhook)**: Envío directo a endpoint webhook
   ```json
   "ProducerUrl": "https://webhook-url/api/logs",
   "UseWebhook": true
   ```

### Configuración de Elasticsearch

```json
{
  "StructuredLogging": {
    "Sinks": {
      "EnableElasticsearch": true,
      "Elasticsearch": {
        "Url": "http://localhost:9200",
        "IndexFormat": "logs-{0:yyyy.MM.dd}",
        "EnableAuthentication": true,
        "Username": "elastic",
        "Password": "password"
      }
    }
  }
}
```

### Configuración de Filtros

```json
{
  "StructuredLogging": {
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
}
```

## 🏗️ Arquitectura

Este componente está implementado siguiendo **Clean Architecture** con las siguientes capas:

```
JonjubNet.Logging/
├── Core/
│   ├── Domain/              # Entidades y Value Objects
│   └── Application/         # Interfaces y Casos de Uso
├── Infrastructure/
│   ├── Shared/              # Implementaciones compartidas
│   └── Persistence/         # Persistencia (si aplica)
└── Presentation/
    └── JonjubNet.Logging/   # Punto de entrada y extensión
```

### Principios Aplicados

- **Dependency Rule**: Las dependencias apuntan hacia adentro (Infrastructure → Application → Domain)
- **Independencia de Frameworks**: La capa Application no depende de ASP.NET Core
- **Testabilidad**: Todas las dependencias están abstraídas mediante interfaces
- **Separación de Responsabilidades**: Cada capa tiene una responsabilidad clara

## 📝 Notas Importantes

### Configuración en Microservicios

1. **Lectura Automática**: El componente lee automáticamente la sección `"StructuredLogging"` del `appsettings.json` del microservicio cuando pasas `IConfiguration` al método `AddStructuredLoggingInfrastructure()`.

2. **Hot-Reload**: Si configuras `reloadOnChange: true` en `AddJsonFile()`, los cambios en `appsettings.json` se detectan automáticamente sin reiniciar.

3. **Valores por Defecto**: Si falta un parámetro en la configuración, el componente usa valores por defecto sensatos.

4. **Múltiples Entornos**: Puedes tener diferentes configuraciones en `appsettings.Development.json`, `appsettings.Production.json`, etc.

### Seguridad y Performance

1. **Headers Sensibles**: Por defecto, los headers sensibles (Authorization, Cookie, etc.) no se capturan en los logs por seguridad.

2. **Tamaño de Body**: El tamaño máximo del body HTTP capturado es configurable (por defecto 10KB). Si el body es mayor, se trunca.

3. **Performance**: El logging se realiza de forma asíncrona para no bloquear el hilo principal.

4. **Batching**: Los logs se agrupan en batches para mejorar el rendimiento (habilitado por defecto).

5. **Resiliencia**: Circuit breakers y retry policies protegen contra fallos de sinks.

### Compatibilidad

1. **ASP.NET Core**: Soporta ASP.NET Core 5.0+ con `IHost` y `BackgroundService`.

2. **Aplicaciones sin Host**: Soporta aplicaciones sin host (Console Apps) usando `AddStructuredLoggingInfrastructureWithoutHost()` y procesamiento síncrono.

3. **Registros Condicionales**: El componente detecta automáticamente si `IHttpContextAccessor` y `BackgroundService` están disponibles y se adapta.

4. **Serilog**: El componente usa Serilog base, sin depender de `Serilog.AspNetCore`.

## 🔧 Troubleshooting

### Problema: Los logs no aparecen

**Solución:**
1. Verificar que `Enabled: true` en la configuración de `appsettings.json`
2. Verificar el nivel mínimo de log (`MinimumLevel`)
3. Revisar los filtros configurados (`Filters`)
4. Verificar que los sinks estén habilitados (`EnableConsole`, `EnableFile`, etc.)

### Problema: Error de compilación al instalar

**Solución:**
1. Verificar versión de .NET (requiere .NET 8.0 o superior)
2. Verificar que todas las dependencias estén instaladas correctamente
3. Limpiar y reconstruir la solución:
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```

### Problema: Los logs no se envían a Elasticsearch/HTTP

**Solución:**
1. Verificar que el sink esté habilitado (`EnableElasticsearch: true` o `EnableHttp: true`)
2. Verificar la URL y credenciales en la configuración
3. Revisar el estado del circuit breaker (puede estar abierto por fallos previos)
4. Verificar la configuración de retry policies
5. Revisar la Dead Letter Queue para logs fallidos

### Problema: Hot-reload no funciona

**Solución:**
1. Verificar que `reloadOnChange: true` esté configurado:
   ```csharp
   builder.Configuration
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
   ```
2. Verificar que el archivo `appsettings.json` tenga permisos de lectura/escritura
3. Reiniciar la aplicación si es necesario

### Problema: Performance degradada

**Solución:**
1. Habilitar batching si no está habilitado (`Batching.Enabled: true`)
2. Ajustar el tamaño de batch según tu volumen de logs
3. Habilitar sampling para reducir volumen (`Sampling.Enabled: true`)
4. Revisar filtros para excluir logs innecesarios
5. Verificar que los sinks no estén bloqueando (revisar circuit breakers)

### Problema: Datos sensibles aparecen en logs

**Solución:**
1. Verificar que `DataSanitization.Enabled: true`
2. Agregar nombres de propiedades sensibles a `SensitivePropertyNames`
3. Agregar patrones regex a `SensitivePatterns` si es necesario
4. Verificar que los headers sensibles estén en `HttpCapture.SensitiveHeaders`

## 📖 Documentación Adicional

- [Mejores Prácticas de Documentación](MEJORES_PRACTICAS_DOCUMENTACION.md) - Guía de mejores prácticas
- [Evaluación de Producción](EVALUACION_PRODUCCION.md) - Análisis técnico y roadmap

## 🤝 Contribuir

Las contribuciones son bienvenidas. Por favor, abre un issue o pull request.

## 📄 Licencia

Este proyecto está licenciado bajo la licencia MIT.

---

**Versión**: 1.0.24

**Autor**: JonjubNet

