# 📚 Funcionalidades del Componente JonjubNet.Logging v3.0.13

## 📋 Tabla de Contenidos

1. [Introducción](#introducción)
2. [Funcionalidades Principales](#funcionalidades-principales)
3. [Servicios e Interfaces Públicas](#servicios-e-interfaces-públicas)
4. [Sinks de Logging](#sinks-de-logging)
5. [Características Avanzadas](#características-avanzadas)
6. [Configuración](#configuración)
7. [Optimizaciones de Performance](#optimizaciones-de-performance)
8. [Arquitectura](#arquitectura)
9. [Ejemplos de Uso](#ejemplos-de-uso)

---

## 🎯 Introducción

**JonjubNet.Logging** es una biblioteca de logging estructurado para aplicaciones .NET 10 con C# 13, diseñada específicamente para microservicios y aplicaciones enterprise. Proporciona logging estructurado en formato JSON con soporte para múltiples destinos (sinks), enriquecimiento automático de logs, correlación de operaciones, y optimizaciones avanzadas de performance.

### Características Clave

- ✅ **Logging Estructurado**: Logs en formato JSON con propiedades enriquecidas
- ✅ **Múltiples Sinks**: Console, File, HTTP, Elasticsearch, Kafka, Serilog
- ✅ **Enriquecimiento Automático**: Información de usuario, HTTP context, ambiente, versión
- ✅ **Correlación de Logs**: IDs de correlación, request y sesión para rastrear operaciones
- ✅ **Logging Automático MediatR**: Logging automático de todas las peticiones/respuestas sin código manual
- ✅ **Categorización de Errores**: Distinción entre errores funcionales y técnicos
- ✅ **Filtrado Dinámico**: Filtros por categoría, operación, usuario y nivel de log
- ✅ **Log Scopes**: Contexto temporal que se propaga a todos los logs dentro de un scope
- ✅ **Sampling y Rate Limiting**: Reducción de volumen de logs en producción
- ✅ **Data Sanitization**: Enmascaramiento automático de datos sensibles (PII, PCI)
- ✅ **Resiliencia**: Circuit breakers, retry policies, Dead Letter Queue
- ✅ **Hot-Reload**: Cambio de configuración en runtime sin reiniciar
- ✅ **Optimizado para Performance**: 70-85% reducción de allocations

---

## 🚀 Funcionalidades Principales

### 1. Logging Estructurado

El componente genera logs en formato JSON estructurado con propiedades enriquecidas automáticamente:

```json
{
  "serviceName": "MiServicio",
  "operation": "ProcesarPago",
  "logLevel": "Information",
  "message": "Pago procesado exitosamente",
  "category": "Business",
  "timestamp": "2024-12-15T10:30:00Z",
  "userId": "user123",
  "userName": "Juan Pérez",
  "correlationId": "corr-abc-123",
  "requestId": "req-xyz-456",
  "properties": {
    "paymentId": "pay-789",
    "amount": 100.50
  },
  "context": {
    "environment": "Production",
    "version": "1.0.0",
    "machineName": "server-01"
  }
}
```

### 2. Niveles de Log Soportados

- **Trace**: Información muy detallada para debugging profundo
- **Debug**: Información de depuración
- **Information**: Información general de la aplicación
- **Warning**: Advertencias que no detienen la ejecución
- **Error**: Errores que requieren atención
- **Critical**: Errores críticos que pueden causar fallos del sistema

### 3. Tipos de Eventos Especiales

- **Operaciones**: Inicio y fin de operaciones con tiempo de ejecución
- **Acciones de Usuario**: Tracking de acciones realizadas por usuarios
- **Eventos de Seguridad**: Logging de eventos relacionados con seguridad
- **Eventos de Auditoría**: Registro de eventos de auditoría

---

## 🔧 Servicios e Interfaces Públicas

### IStructuredLoggingService

Servicio principal para logging estructurado. Proporciona métodos para todos los niveles de log y tipos de eventos.

#### Métodos de Logging por Nivel

```csharp
// Logging básico
void LogInformation(string message, string operation = "", string category = "", 
                   Dictionary<string, object>? properties = default, 
                   Dictionary<string, object>? context = default);

void LogWarning(string message, string operation = "", string category = "", 
                Dictionary<string, object>? properties = default, 
                Dictionary<string, object>? context = default, 
                Exception? exception = null);

void LogError(string message, string operation = "", string category = "", 
              Dictionary<string, object>? properties = default, 
              Dictionary<string, object>? context = default, 
              Exception? exception = null);

void LogCritical(string message, string operation = "", string category = "", 
                 Dictionary<string, object>? properties = default, 
                 Dictionary<string, object>? context = default, 
                 Exception? exception = null);

void LogDebug(string message, string operation = "", string category = "", 
              Dictionary<string, object>? properties = default, 
              Dictionary<string, object>? context = default);

void LogTrace(string message, string operation = "", string category = "", 
              Dictionary<string, object>? properties = default, 
              Dictionary<string, object>? context = default);
```

#### Métodos de Eventos Especiales

```csharp
// Operaciones
void LogOperationStart(string operation, string category = "", 
                       Dictionary<string, object>? properties = default);

void LogOperationEnd(string operation, string category = "", 
                     long executionTimeMs = 0, 
                     Dictionary<string, object>? properties = default, 
                     bool success = true, Exception? exception = null);

// Acciones de usuario
void LogUserAction(string action, string entityType = "", string entityId = "", 
                   Dictionary<string, object>? properties = default);

// Eventos de seguridad
void LogSecurityEvent(string eventType, string description, 
                      Dictionary<string, object>? properties = default, 
                      Exception? exception = null);

// Eventos de auditoría
void LogAuditEvent(string eventType, string description, 
                   string entityType = "", string entityId = "", 
                   Dictionary<string, object>? properties = default);

// Logging personalizado
void LogCustom(StructuredLogEntry logEntry);
```

#### Log Scopes

```csharp
// Crear un scope que agrega propiedades a todos los logs dentro del scope
ILogScope BeginScope(Dictionary<string, object> properties);

// Crear un scope con una sola propiedad
ILogScope BeginScope(string key, object value);
```

### ILoggingConfigurationManager

Gestión dinámica de configuración con Hot-Reload (cambios en runtime sin reiniciar).

```csharp
// Obtener configuración actual (siempre actualizada)
LoggingConfiguration Current { get; }

// Cambiar nivel mínimo de log
bool SetMinimumLevel(string minimumLevel);

// Habilitar/deshabilitar sinks
bool SetSinkEnabled(string sinkName, bool enabled);

// Cambiar tasa de sampling
bool SetSamplingRate(string logLevel, double samplingRate);

// Habilitar/deshabilitar sampling
bool SetSamplingEnabled(bool enabled);

// Cambiar límite máximo de logs por minuto
bool SetMaxLogsPerMinute(string logLevel, int maxLogsPerMinute);

// Habilitar/deshabilitar logging completo
bool SetLoggingEnabled(bool enabled);

// Establecer nivel mínimo por categoría
bool SetCategoryLogLevel(string category, string level);

// Establecer nivel mínimo por operación
bool SetOperationLogLevel(string operation, string level);

// Override temporal con expiración automática
bool SetTemporaryOverride(string? category, string level, TimeSpan expiration);

// Remover override temporal
bool RemoveTemporaryOverride(string? category);

// Evento cuando la configuración cambia
event Action<LoggingConfiguration>? ConfigurationChanged;
```

### ICurrentUserService

Obtener información del usuario actual para enriquecimiento automático de logs.

```csharp
string? GetCurrentUserId();
string? GetCurrentUserName();
string? GetCurrentUserEmail();
IEnumerable<string> GetCurrentUserRoles();
bool IsInRole(string role);
bool IsAuthenticated();
```

### IErrorCategorizationService

Categorización de errores para distinguir entre errores funcionales (de negocio) y técnicos (del sistema).

```csharp
bool IsFunctionalError(Exception exception);
string GetErrorCategory(Exception exception);
LogLevel GetLogLevel(Exception exception);
string GetErrorType(Exception exception);
void RegisterFunctionalErrorType(Type exceptionType);
void RegisterTechnicalErrorType(Type exceptionType);
```

### ILogScopeManager

Gestión de scopes de logging para agregar contexto temporal a los logs.

```csharp
ILogScope BeginScope(Dictionary<string, object> properties);
ILogScope BeginScope(string key, object value);
Dictionary<string, object> GetCurrentScopeProperties();
```

### ILogFilter

Filtrado dinámico de logs antes de enviarlos a los sinks.

```csharp
bool ShouldLog(StructuredLogEntry logEntry);
```

### ILogSamplingService

Servicio de sampling y rate limiting para reducir el volumen de logs.

```csharp
bool ShouldSample(StructuredLogEntry logEntry);
```

### IDataSanitizationService

Sanitización de datos sensibles (PII, PCI) antes de enviar a los sinks.

```csharp
StructuredLogEntry Sanitize(StructuredLogEntry logEntry);
Dictionary<string, object> SanitizeDictionary(Dictionary<string, object> dictionary);
string SanitizeString(string value);
```

### Interfaces de Resiliencia

#### ICircuitBreakerManager
Gestión de circuit breakers para proteger los sinks de fallos.

#### IRetryPolicyManager
Gestión de políticas de reintento configurables.

#### IDeadLetterQueue
Cola de mensajes fallidos para procesamiento posterior.

### Interfaces de Batching

#### IIntelligentBatchingService
Batching inteligente de logs con priorización y compresión.

#### IBatchCompressionService
Compresión de batches de logs para reducir ancho de banda.

---

## 📤 Sinks de Logging

Los sinks son los destinos donde se envían los logs. El componente soporta múltiples sinks que pueden funcionar en paralelo.

### ConsoleLogSink

Envía logs a la consola (stdout). Útil para desarrollo y debugging.

**Características:**
- ✅ Formato JSON estructurado
- ✅ Optimizado con JSON pre-serializado
- ✅ Habilitado por defecto

### SerilogSink

Integración con Serilog para aprovechar su ecosistema de sinks.

**Características:**
- ✅ Compatible con todos los sinks de Serilog
- ✅ Configuración flexible
- ✅ Registrado condicionalmente (solo si Serilog está disponible)

### File Sink (Configurable)

Escritura de logs a archivos con rotación automática.

**Configuración:**
- Ruta del archivo
- Intervalo de rotación (Day, Hour, Minute)
- Límite de archivos retenidos
- Límite de tamaño de archivo
- Template de salida

### HTTP Sink (Configurable)

Envío de logs a endpoints HTTP mediante POST.

**Configuración:**
- URL del endpoint
- Headers personalizados (ej: Authorization)
- Batch posting limit
- Periodo de envío
- Content-Type

### Elasticsearch Sink (Configurable)

Envío de logs a Elasticsearch para búsqueda y análisis.

**Configuración:**
- URL de Elasticsearch
- Formato de índice
- Autenticación (usuario/contraseña)
- Configuración de SSL

### Kafka Producer (Configurable)

Envío de logs a Kafka para procesamiento en streaming.

**Configuración:**
- Bootstrap servers
- Topic
- Configuración de producer
- Serialización optimizada (UTF-8 bytes)

---

## 🎨 Características Avanzadas

### 1. Logging Automático de MediatR

El componente incluye `LoggingBehaviour<TRequest, TResponse>` que registra automáticamente todas las peticiones y respuestas de MediatR sin código manual.

**Características:**
- ✅ Logging automático de inicio de petición
- ✅ Logging automático de éxito con tiempo de ejecución
- ✅ Logging automático de errores con excepciones
- ✅ Serialización de request/response
- ✅ RequestId único por petición
- ✅ Optimizado con DictionaryPool y JsonSerializerOptions cacheado

**Registro automático:**
```csharp
// Se registra automáticamente con AddSharedInfrastructure
services.AddStructuredLoggingInfrastructure<YourUserService>(configuration);
```

### 2. Enriquecimiento Automático

El componente enriquece automáticamente los logs con:

- **Información del Usuario**: UserId, UserName, UserEmail, Roles
- **Contexto HTTP**: RequestPath, RequestMethod, StatusCode, ClientIp, UserAgent, QueryString, Headers, Body (configurable)
- **Información del Sistema**: Environment, Version, MachineName, ProcessId, ThreadId
- **Correlación**: CorrelationId, RequestId, SessionId
- **Propiedades Estáticas**: Propiedades configuradas que se agregan a todos los logs
- **Scopes Activos**: Propiedades de scopes activos

### 3. Filtrado Dinámico

Filtros aplicados antes de enviar a los sinks:

- **Por Categoría**: Excluir categorías específicas
- **Por Operación**: Excluir operaciones específicas
- **Por Usuario**: Excluir usuarios específicos
- **Por Nivel de Log**: Filtrado por nivel mínimo global o por categoría
- **Por Operación**: Nivel mínimo específico por operación

### 4. Log Scopes

Contexto temporal que se propaga a todos los logs dentro de un scope:

```csharp
using (var scope = _loggingService.BeginScope("RequestId", requestId))
{
    // Todos los logs dentro de este scope incluirán RequestId
    _loggingService.LogInformation("Procesando petición");
    // ...
}
```

### 5. Sampling y Rate Limiting

Reducción de volumen de logs en producción:

- **Sampling Probabilístico**: Porcentaje de logs a registrar por nivel
- **Rate Limiting**: Máximo de logs por minuto por nivel
- **Configuración por Nivel**: Diferentes tasas para cada nivel de log

### 6. Data Sanitization

Enmascaramiento automático de datos sensibles:

- **Propiedades Sensibles**: Lista configurable de nombres de propiedades a enmascarar
- **Patrones de Datos Sensibles**: Detección por patrones (emails, tarjetas de crédito, etc.)
- **Enmascaramiento Configurable**: Caracteres visibles, caracter de enmascaramiento
- **Recursivo**: Sanitiza diccionarios anidados

### 7. Resiliencia

Protección contra fallos de sinks:

- **Circuit Breakers**: Protección contra fallos repetidos
- **Retry Policies**: Reintentos configurables con backoff exponencial
- **Dead Letter Queue**: Almacenamiento de logs fallidos para procesamiento posterior
- **Transient Error Detection**: Detección automática de errores transitorios

### 8. Batching Inteligente

Agrupación eficiente de logs:

- **Batching por Prioridad**: Logs críticos se procesan primero
- **Compresión**: Compresión de batches para reducir ancho de banda
- **Tamaño de Batch Configurable**: Ajuste según necesidades
- **Procesamiento Asíncrono**: No bloquea el hilo principal

### 9. Hot-Reload de Configuración

Cambio de configuración en runtime sin reiniciar:

- ✅ Cambio de nivel mínimo de log
- ✅ Habilitar/deshabilitar sinks
- ✅ Cambiar tasas de sampling
- ✅ Cambiar límites de rate limiting
- ✅ Override temporal con expiración automática
- ✅ Eventos de cambio de configuración

---

## ⚙️ Configuración

### Configuración Básica (appsettings.json)

```json
{
  "StructuredLogging": {
    "Enabled": true,
    "MinimumLevel": "Information",
    "ServiceName": "MiServicio",
    "Environment": "Production",
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
      },
      "Http": {
        "Url": "https://mi-servidor-logs.com/api/logs",
        "BatchPostingLimit": 100,
        "PeriodSeconds": 2,
        "Headers": {
          "Authorization": "Bearer token123"
        }
      },
      "Elasticsearch": {
        "Url": "http://localhost:9200",
        "IndexFormat": "logs-{0:yyyy.MM.dd}",
        "Username": "elastic",
        "Password": "password",
        "EnableAuthentication": true
      }
    },
    "Filters": {
      "ExcludedCategories": ["Debug", "Trace"],
      "ExcludedOperations": ["HealthCheck"],
      "CategoryLogLevels": {
        "Security": "Warning",
        "Performance": "Information"
      }
    },
    "Enrichment": {
      "IncludeEnvironment": true,
      "IncludeVersion": true,
      "IncludeMachineName": true,
      "IncludeProcess": true,
      "IncludeThread": true,
      "IncludeHttpContext": true,
      "IncludeUserInfo": true,
      "StaticProperties": {
        "Application": "MiApp",
        "Region": "us-east-1"
      }
    },
    "Correlation": {
      "GenerateCorrelationId": true,
      "GenerateRequestId": true,
      "GenerateSessionId": true
    },
    "Sampling": {
      "Enabled": true,
      "Rates": {
        "Trace": 0.1,
        "Debug": 0.2,
        "Information": 1.0,
        "Warning": 1.0,
        "Error": 1.0,
        "Critical": 1.0
      },
      "MaxLogsPerMinute": {
        "Trace": 100,
        "Debug": 500,
        "Information": 1000
      }
    },
    "DataSanitization": {
      "Enabled": true,
      "SensitivePropertyNames": ["password", "token", "creditCard"],
      "MaskCharacter": "*",
      "VisibleChars": 4
    },
    "CircuitBreaker": {
      "Enabled": true,
      "FailureThreshold": 5,
      "TimeoutSeconds": 30
    },
    "RetryPolicy": {
      "MaxRetries": 3,
      "InitialDelayMs": 100,
      "MaxDelayMs": 5000
    },
    "DeadLetterQueue": {
      "Enabled": true,
      "MaxSize": 10000
    },
    "Batching": {
      "Enabled": true,
      "BatchSize": 100,
      "FlushIntervalSeconds": 5
    }
  }
}
```

### Registro en Program.cs

```csharp
using JonjubNet.Logging.Shared;

// Registrar servicios
services.AddStructuredLoggingInfrastructure<YourUserService>(configuration);

// O para aplicaciones sin host:
services.AddStructuredLoggingInfrastructureWithoutHost<YourUserService>(configuration);
```

---

## ⚡ Optimizaciones de Performance

El componente está altamente optimizado para reducir allocations y mejorar throughput:

### Optimizaciones Implementadas (v3.0.13)

1. **DictionaryPool**: Pool de diccionarios reutilizables (reducción 60-70% allocations)
2. **JsonSerializerOptions Cacheado**: Evita allocations repetidas
3. **JsonSerializationHelper**: Serialización optimizada con ArrayBufferWriter y Utf8JsonWriter
4. **Pre-serialización Compartida**: JSON serializado una vez y compartido entre sinks
5. **GCOptimizationHelpers**: 
   - Diccionario vacío reutilizable
   - Pool de listas de Task
   - Cache de ProcessId/ThreadId strings
6. **TryAdd() en lugar de ContainsKey + asignación**: Reducción 50% en operaciones de diccionario
7. **Pre-allocación de capacidad**: Evita redimensionamientos de diccionarios
8. **Eliminación de LINQ innecesario**: Eliminado Select().ToList(), GroupBy().ToList()
9. **LoggingBehaviour optimizado**: DictionaryPool local + JsonSerializerOptions cacheado

### Resultados

- **70-85% reducción de allocations** en hot paths
- **Mejora significativa en throughput** en alta concurrencia
- **Menor presión en GC** (menos colecciones de basura)
- **Mejor latencia** en operaciones de logging

---

## 🏗️ Arquitectura

El componente sigue **Clean Architecture** con separación clara de responsabilidades:

### Capas

1. **Domain**: Entidades, Value Objects, Interfaces comunes
2. **Application**: Use Cases, Interfaces públicas, Configuración
3. **Infrastructure (Shared)**: Implementaciones, Servicios, Sinks
4. **Presentation**: Extensiones de registro, Paquete NuGet

### Principios

- ✅ **Dependency Inversion**: Dependencias apuntan hacia adentro
- ✅ **Separation of Concerns**: Cada capa tiene responsabilidades claras
- ✅ **Single Responsibility**: Cada clase tiene una sola responsabilidad
- ✅ **Open/Closed**: Extensible sin modificar código existente
- ✅ **Interface Segregation**: Interfaces específicas y cohesivas

### Dependencias Garantizadas

- ✅ **MediatR**: Para logging automático de peticiones
- ✅ **Microsoft.Extensions.ObjectPool**: Para object pooling
- ✅ **System.Text.Json**: Para serialización JSON
- ✅ **Serilog** (opcional): Para integración con Serilog

---

## 💡 Ejemplos de Uso

### Ejemplo 1: Logging Básico

```csharp
public class PaymentService
{
    private readonly IStructuredLoggingService _loggingService;
    
    public PaymentService(IStructuredLoggingService loggingService)
    {
        _loggingService = loggingService;
    }
    
    public async Task ProcessPaymentAsync(PaymentRequest request)
    {
        _loggingService.LogInformation(
            "Iniciando procesamiento de pago",
            operation: "ProcessPayment",
            category: "Business",
            properties: new Dictionary<string, object>
            {
                { "PaymentId", request.PaymentId },
                { "Amount", request.Amount }
            }
        );
        
        try
        {
            // Procesar pago...
            
            _loggingService.LogInformation(
                "Pago procesado exitosamente",
                operation: "ProcessPayment",
                category: "Business"
            );
        }
        catch (Exception ex)
        {
            _loggingService.LogError(
                "Error al procesar pago",
                operation: "ProcessPayment",
                category: "Business",
                exception: ex
            );
            throw;
        }
    }
}
```

### Ejemplo 2: Logging de Operaciones

```csharp
public async Task ProcessOrderAsync(Order order)
{
    var stopwatch = Stopwatch.StartNew();
    
    _loggingService.LogOperationStart(
        operation: "ProcessOrder",
        category: "Business",
        properties: new Dictionary<string, object> { { "OrderId", order.Id } }
    );
    
    try
    {
        // Procesar orden...
        
        stopwatch.Stop();
        _loggingService.LogOperationEnd(
            operation: "ProcessOrder",
            category: "Business",
            executionTimeMs: stopwatch.ElapsedMilliseconds,
            success: true
        );
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _loggingService.LogOperationEnd(
            operation: "ProcessOrder",
            category: "Business",
            executionTimeMs: stopwatch.ElapsedMilliseconds,
            success: false,
            exception: ex
        );
        throw;
    }
}
```

### Ejemplo 3: Log Scopes

```csharp
public async Task HandleRequestAsync(HttpRequest request)
{
    var requestId = Guid.NewGuid().ToString();
    
    using (var scope = _loggingService.BeginScope("RequestId", requestId))
    {
        _loggingService.LogInformation("Request recibido");
        // Todos los logs dentro de este scope incluirán RequestId
        
        await ProcessRequestAsync(request);
        
        _loggingService.LogInformation("Request procesado");
    }
}
```

### Ejemplo 4: Eventos de Seguridad

```csharp
public void HandleUnauthorizedAccess(string userId, string resource)
{
    _loggingService.LogSecurityEvent(
        eventType: "UnauthorizedAccess",
        description: $"Usuario {userId} intentó acceder a {resource}",
        properties: new Dictionary<string, object>
        {
            { "UserId", userId },
            { "Resource", resource },
            { "Timestamp", DateTime.UtcNow }
        }
    );
}
```

### Ejemplo 5: Eventos de Auditoría

```csharp
public void UpdateUser(User user)
{
    _loggingService.LogAuditEvent(
        eventType: "UserUpdated",
        description: "Usuario actualizado",
        entityType: "User",
        entityId: user.Id.ToString(),
        properties: new Dictionary<string, object>
        {
            { "ChangedFields", GetChangedFields(user) }
        }
    );
}
```

### Ejemplo 6: Hot-Reload de Configuración

```csharp
public class LoggingController : ControllerBase
{
    private readonly ILoggingConfigurationManager _configManager;
    
    public LoggingController(ILoggingConfigurationManager configManager)
    {
        _configManager = configManager;
    }
    
    [HttpPost("logging/level")]
    public IActionResult SetLogLevel([FromBody] SetLogLevelRequest request)
    {
        var success = _configManager.SetMinimumLevel(request.Level);
        return success ? Ok() : BadRequest();
    }
    
    [HttpPost("logging/sampling")]
    public IActionResult SetSamplingRate([FromBody] SetSamplingRateRequest request)
    {
        var success = _configManager.SetSamplingRate(request.LogLevel, request.Rate);
        return success ? Ok() : BadRequest();
    }
}
```

---

## 📦 Versión

**Versión Actual: 3.0.13**

- ✅ .NET 10 y C# 13
- ✅ Todas las funcionalidades implementadas
- ✅ Optimizaciones de performance completas
- ✅ Clean Architecture validada
- ✅ Listo para producción

---

## 📝 Notas Adicionales

- El componente está diseñado para ser **thread-safe** y puede usarse en aplicaciones de alta concurrencia
- Todos los métodos async usan `ConfigureAwait(false)` para mejor performance
- El componente es **AOT-friendly** gracias al uso de Source Generation JSON
- Compatible con aplicaciones **con y sin ASP.NET Core**
- Soporta **aplicaciones sin host** mediante `SynchronousLogProcessor`

---

**Última actualización:** Diciembre 2024  
**Autor:** Onuar Jimenez  
**Empresa:** JonjubNet

