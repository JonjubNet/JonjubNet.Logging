using JonjubNet.Logging.Configuration;
using JonjubNet.Logging.Interfaces;
using JonjubNet.Logging.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace JonjubNet.Logging.Services
{
    /// <summary>
    /// Servicio genérico de logging estructurado
    /// Soporta múltiples tipos de conexión a Kafka:
    /// 1. Conexión directa nativa (BootstrapServers) - Protocolo binario nativo de Kafka
    /// 2. Conexión HTTP/HTTPS (ProducerUrl) - A través de Kafka REST Proxy
    /// 3. Webhook HTTP/HTTPS (ProducerUrl + UseWebhook) - Envío directo a endpoint webhook
    /// </summary>
    public class StructuredLoggingService : IStructuredLoggingService, IDisposable
    {
        private readonly ILogger<StructuredLoggingService> _logger;
        private readonly LoggingConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IProducer<Null, string>? _kafkaProducer;
        private readonly KafkaConnectionType _connectionType;

        /// <summary>
        /// Tipo de conexión a Kafka
        /// </summary>
        private enum KafkaConnectionType
        {
            None,
            Native,      // Conexión directa usando protocolo binario nativo (BootstrapServers)
            Http,        // Conexión HTTP a través de REST Proxy
            Https,       // Conexión HTTPS a través de REST Proxy
            WebhookHttp, // Webhook HTTP - envío directo a endpoint
            WebhookHttps // Webhook HTTPS - envío directo a endpoint seguro
        }

        public StructuredLoggingService(
            ILogger<StructuredLoggingService> logger,
            IOptions<LoggingConfiguration> configuration,
            IServiceProvider serviceProvider,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _logger = logger;
            _configuration = configuration.Value;
            _serviceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;

            // Inicializar conexión a Kafka si está habilitado
            if (_configuration.KafkaProducer.Enabled)
            {
                var result = InitializeKafkaConnection();
                _connectionType = result.ConnectionType;
                _kafkaProducer = result.Producer;
                _logger.LogInformation("Kafka Producer inicializado. ConnectionType: {ConnectionType}, Topic: {Topic}, BootstrapServers: {BootstrapServers}",
                    _connectionType, _configuration.KafkaProducer.Topic, _configuration.KafkaProducer.BootstrapServers ?? "N/A");
            }
            else
            {
                _connectionType = KafkaConnectionType.None;
                _kafkaProducer = null;
                _logger.LogWarning("Kafka Producer está deshabilitado en la configuración");
            }
        }

        /// <summary>
        /// Resuelve ICurrentUserService dinámicamente desde el ServiceProvider
        /// Esto permite mantener el scope correcto por request aunque el servicio principal sea Singleton
        /// </summary>
        private ICurrentUserService? GetCurrentUserService()
        {
            try
            {
                // Intentar resolver desde el ServiceProvider actual
                // Si estamos en un scope HTTP, obtendrá el servicio correcto para ese request
                return _serviceProvider.GetService<ICurrentUserService>();
            }
            catch (Exception ex)
            {
                // Si no hay scope disponible o hay algún error, retornar null
                // Esto es seguro porque el código maneja null correctamente
                _logger.LogDebug(ex, "No se pudo resolver ICurrentUserService. Puede ser normal si no hay scope HTTP activo.");
                return null;
            }
        }

        /// <summary>
        /// Resultado de la inicialización de Kafka
        /// </summary>
        private class KafkaInitializationResult
        {
            public KafkaConnectionType ConnectionType { get; set; }
            public IProducer<Null, string>? Producer { get; set; }
        }

        /// <summary>
        /// Inicializa la conexión a Kafka según la configuración
        /// </summary>
        private KafkaInitializationResult InitializeKafkaConnection()
        {
            var kafkaConfig = _configuration.KafkaProducer;
            var result = new KafkaInitializationResult();

            // Prioridad 1: Conexión directa nativa (BootstrapServers)
            if (!string.IsNullOrEmpty(kafkaConfig.BootstrapServers))
            {
                try
                {
                    var producerConfig = new ProducerConfig
                    {
                        BootstrapServers = kafkaConfig.BootstrapServers,
                        Acks = Acks.All,
                        MessageSendMaxRetries = kafkaConfig.RetryCount,
                        CompressionType = kafkaConfig.EnableCompression
                            ? (kafkaConfig.CompressionType?.ToLower() == "gzip" ? CompressionType.Gzip : CompressionType.None)
                            : CompressionType.None,
                        // LingerMs: Tiempo de espera antes de enviar el batch (0 = enviar inmediatamente)
                        // Si es 0, cada mensaje se envía individualmente sin batching
                        LingerMs = kafkaConfig.LingerMs,
                        // BatchSize: Tamaño del batch en bytes (solo aplica si LingerMs > 0)
                        BatchSize = kafkaConfig.BatchSize
                    };

                    result.Producer = new ProducerBuilder<Null, string>(producerConfig).Build();
                    result.ConnectionType = KafkaConnectionType.Native;
                    _logger.LogInformation("Kafka Producer inicializado - Tipo: Conexión Directa Nativa | Tópico: {Topic} | BootstrapServers: {BootstrapServers}",
                        kafkaConfig.Topic, kafkaConfig.BootstrapServers);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al inicializar Kafka Producer con conexión directa");
                    result.ConnectionType = KafkaConnectionType.None;
                    result.Producer = null;
                    return result;
                }
            }
            // Prioridad 2: Conexión HTTP/HTTPS (ProducerUrl)
            else if (!string.IsNullOrEmpty(kafkaConfig.ProducerUrl))
            {
                result.Producer = null; // No se usa producer para HTTP/HTTPS/Webhook
                
                // Determinar si es webhook o REST Proxy
                if (kafkaConfig.UseWebhook)
                {
                    // Webhook: envío directo a endpoint
                    if (kafkaConfig.ProducerUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Kafka Producer configurado - Tipo: Webhook HTTPS | Tópico: {Topic} | URL: {Url}",
                            kafkaConfig.Topic, kafkaConfig.ProducerUrl);
                        result.ConnectionType = KafkaConnectionType.WebhookHttps;
                        return result;
                    }
                    else if (kafkaConfig.ProducerUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Kafka Producer configurado - Tipo: Webhook HTTP | Tópico: {Topic} | URL: {Url}",
                            kafkaConfig.Topic, kafkaConfig.ProducerUrl);
                        result.ConnectionType = KafkaConnectionType.WebhookHttp;
                        return result;
                    }
                    else
                    {
                        _logger.LogWarning("ProducerUrl no tiene un protocolo válido (http:// o https://). Se asume Webhook HTTPS.");
                        result.ConnectionType = KafkaConnectionType.WebhookHttps;
                        return result;
                    }
                }
                else
                {
                    // REST Proxy: formato específico de Kafka REST Proxy
                    if (kafkaConfig.ProducerUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Kafka Producer configurado - Tipo: HTTPS (REST Proxy) | Tópico: {Topic} | URL: {Url}",
                            kafkaConfig.Topic, kafkaConfig.ProducerUrl);
                        result.ConnectionType = KafkaConnectionType.Https;
                        return result;
                    }
                    else if (kafkaConfig.ProducerUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Kafka Producer configurado - Tipo: HTTP (REST Proxy) | Tópico: {Topic} | URL: {Url}",
                            kafkaConfig.Topic, kafkaConfig.ProducerUrl);
                        result.ConnectionType = KafkaConnectionType.Http;
                        return result;
                    }
                    else
                    {
                        _logger.LogWarning("ProducerUrl no tiene un protocolo válido (http:// o https://). Se asume HTTPS.");
                        result.ConnectionType = KafkaConnectionType.Https;
                        return result;
                    }
                }
            }
            else
            {
                _logger.LogWarning("Kafka está habilitado pero no se ha configurado BootstrapServers ni ProducerUrl. Los mensajes no se enviarán a Kafka.");
                result.ConnectionType = KafkaConnectionType.None;
                result.Producer = null;
                return result;
            }
        }

        public void LogInformation(string message, string operation = "", string category = "", Dictionary<string, object>? properties = null, Dictionary<string, object>? context = null)
        {
            LogCustom(CreateLogEntry(Models.LogLevel.Information, message, operation, category, properties, context));
        }

        public void LogWarning(string message, string operation = "", string category = "", Dictionary<string, object>? properties = null, Dictionary<string, object>? context = null, Exception? exception = null)
        {
            LogCustom(CreateLogEntry(Models.LogLevel.Warning, message, operation, category, properties, context, exception));
        }

        public void LogError(string message, string operation = "", string category = "", Dictionary<string, object>? properties = null, Dictionary<string, object>? context = null, Exception? exception = null)
        {
            LogCustom(CreateLogEntry(Models.LogLevel.Error, message, operation, category, properties, context, exception));
        }

        public void LogCritical(string message, string operation = "", string category = "", Dictionary<string, object>? properties = null, Dictionary<string, object>? context = null, Exception? exception = null)
        {
            LogCustom(CreateLogEntry(Models.LogLevel.Critical, message, operation, category, properties, context, exception));
        }

        public void LogDebug(string message, string operation = "", string category = "", Dictionary<string, object>? properties = null, Dictionary<string, object>? context = null)
        {
            LogCustom(CreateLogEntry(Models.LogLevel.Debug, message, operation, category, properties, context));
        }

        public void LogTrace(string message, string operation = "", string category = "", Dictionary<string, object>? properties = null, Dictionary<string, object>? context = null)
        {
            LogCustom(CreateLogEntry(Models.LogLevel.Trace, message, operation, category, properties, context));
        }

        /// <summary>
        /// Registra un log personalizado
        /// IMPORTANTE: Este método NUNCA lanza excepciones para no afectar la aplicación
        /// Todos los errores del componente se manejan internamente
        /// </summary>
        public void LogCustom(Models.StructuredLogEntry logEntry)
        {
            // Este método nunca debe lanzar excepciones - todos los errores se manejan internamente
            try
            {
                if (!_configuration.Enabled)
                    return;

                // Aplicar filtros
                if (ShouldFilterLog(logEntry))
                    return;

                // También mantener logging local como fallback (siempre se ejecuta)
                // El JSON completo se envía tal cual, manteniendo todos los campos necesarios
                try
                {
                    var localLogLevel = GetLogLevel(logEntry.LogLevel);
                    var localMessage = logEntry.ToJson();
                    _logger.Log(localLogLevel, "{StructuredLog}", localMessage);
                }
                catch (Exception ex)
                {
                    // Error crítico interno del componente al serializar/logear localmente
                    // Registrar error mínimo sin afectar la aplicación
                    _logger.LogError(ex, "Error crítico interno del componente al registrar log localmente. Mensaje: {Message}", 
                        logEntry.Message ?? "N/A");
                }

                // Enriquecer con información del contexto y enviar a Kafka (de forma asíncrona, fire-and-forget)
                // Nota: El JSON es completo y autocontenido, no necesita LogContext
                // El LogContext solo se usa para logs escritos directamente con _logger.Log()
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await EnrichLogEntryAsync(logEntry);
                        
                        // Enviar a Kafka de forma asíncrona (fire-and-forget)
                        await SendToKafkaAsync(logEntry);
                    }
                    catch (Exception ex)
                    {
                        // Error interno del componente al enriquecer o enviar a Kafka
                        // Registrar error del componente sin afectar la aplicación
                        await HandleComponentErrorAsync(ex, logEntry, "Error interno del componente al procesar log");
                    }
                });
            }
            catch (Exception ex)
            {
                // Error crítico interno del componente en LogCustom - nunca debe llegar aquí
                // Pero por seguridad, capturamos cualquier error para no afectar la aplicación
                _logger.LogError(ex, "Error crítico interno del componente en LogCustom. Este error no debería ocurrir.");
            }
        }

        public void LogOperationStart(string operation, string category = "", Dictionary<string, object>? properties = null)
        {
            var context = new Dictionary<string, object>
            {
                { "OperationStart", DateTime.UtcNow },
                { "OperationStatus", "Started" }
            };

            var logEntry = CreateLogEntry(Models.LogLevel.Information, $"Operation started: {operation}", operation, category, properties, context);
            logEntry.EventType = Models.EventType.OperationStart;
            LogCustom(logEntry);
        }

        public void LogOperationEnd(string operation, string category = "", long executionTimeMs = 0, Dictionary<string, object>? properties = null, bool success = true, Exception? exception = null)
        {
            var context = new Dictionary<string, object>
            {
                { "OperationEnd", DateTime.UtcNow },
                { "OperationStatus", success ? "Completed" : "Failed" },
                { "ExecutionTimeMs", executionTimeMs },
                { "Success", success }
            };

            var logLevel = success ? Models.LogLevel.Information : Models.LogLevel.Error;
            var message = success ? $"Operation completed: {operation}" : $"Operation failed: {operation}";
            
            var logEntry = CreateLogEntry(logLevel, message, operation, category, properties, context, exception);
            logEntry.EventType = Models.EventType.OperationEnd;
            LogCustom(logEntry);
        }

        public void LogUserAction(string action, string entityType = "", string entityId = "", Dictionary<string, object>? properties = null)
        {
            var context = new Dictionary<string, object>
            {
                { "Action", action },
                { "EntityType", entityType },
                { "EntityId", entityId },
                { "ActionTimestamp", DateTime.UtcNow }
            };

            var logEntry = CreateLogEntry(Models.LogLevel.Information, $"User action: {action}", action, Models.LogCategory.UserAction, properties, context);
            logEntry.EventType = Models.EventType.UserAction;
            LogCustom(logEntry);
        }

        public void LogSecurityEvent(string eventType, string description, Dictionary<string, object>? properties = null, Exception? exception = null)
        {
            var context = new Dictionary<string, object>
            {
                { "SecurityEventType", eventType },
                { "EventTimestamp", DateTime.UtcNow }
            };

            var logEntry = CreateLogEntry(Models.LogLevel.Warning, $"Security event: {description}", eventType, Models.LogCategory.Security, properties, context, exception);
            logEntry.EventType = Models.EventType.SecurityEvent;
            LogCustom(logEntry);
        }

        public void LogAuditEvent(string eventType, string description, string entityType = "", string entityId = "", Dictionary<string, object>? properties = null)
        {
            var context = new Dictionary<string, object>
            {
                { "AuditEventType", eventType },
                { "EntityType", entityType },
                { "EntityId", entityId },
                { "AuditTimestamp", DateTime.UtcNow }
            };

            var logEntry = CreateLogEntry(Models.LogLevel.Information, $"Audit event: {description}", eventType, Models.LogCategory.Audit, properties, context);
            logEntry.EventType = Models.EventType.AuditEvent;
            LogCustom(logEntry);
        }

        /// <summary>
        /// Crea una entrada de log estructurado
        /// Maneja errores internos del componente sin afectar la aplicación
        /// </summary>
        private Models.StructuredLogEntry CreateLogEntry(string logLevel, string message, string operation, string category, Dictionary<string, object>? properties, Dictionary<string, object>? context, Exception? exception = null)
        {
            try
            {
                // Filtrar campos duplicados de properties que ya existen en el nivel raíz
                // Esto evita duplicación de datos según mejores prácticas de logging estructurado
                var filteredProperties = FilterDuplicateProperties(properties);

                string userId = "Anonymous";
                string userName = "Anonymous";
                
                try
                {
                    // Resolver ICurrentUserService dinámicamente para mantener el scope correcto por request
                    // Esto es necesario porque el servicio es Singleton pero ICurrentUserService es Scoped
                    var currentUserService = GetCurrentUserService();
                    userId = currentUserService?.GetCurrentUserId() ?? "Anonymous";
                    userName = currentUserService?.GetCurrentUserName() ?? "Anonymous";
                }
                catch (Exception ex)
                {
                    // Error interno del componente al obtener usuario - usar valores por defecto
                    _logger.LogWarning(ex, "Error interno del componente al obtener información del usuario. Usando valores por defecto.");
                }

                return new Models.StructuredLogEntry
                {
                    ServiceName = _configuration.ServiceName ?? "Unknown",
                    Operation = operation ?? string.Empty,
                    LogLevel = logLevel ?? Models.LogLevel.Information,
                    Message = message ?? string.Empty,
                    Category = category ?? Models.LogCategory.General,
                    UserId = userId,
                    UserName = userName,
                    Environment = _configuration.Environment ?? "Unknown",
                    Version = _configuration.Version ?? "Unknown",
                    MachineName = Environment.MachineName,
                    ProcessId = Environment.ProcessId.ToString(),
                    ThreadId = Environment.CurrentManagedThreadId.ToString(),
                    Properties = filteredProperties,
                    Context = context ?? new Dictionary<string, object>(),
                    Exception = exception,
                    StackTrace = exception?.StackTrace,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                // Error crítico interno del componente al crear log - crear log mínimo
                _logger.LogError(ex, "Error crítico interno del componente al crear log. Creando log mínimo.");
                return new Models.StructuredLogEntry
                {
                    ServiceName = "Unknown",
                    Operation = operation ?? "Unknown",
                    LogLevel = Models.LogLevel.Error,
                    Message = $"Error interno del componente al crear log: {message ?? "N/A"}",
                    Category = Models.LogCategory.System,
                    UserId = "Anonymous",
                    UserName = "Anonymous",
                    Environment = "Unknown",
                    Version = "Unknown",
                    MachineName = Environment.MachineName,
                    ProcessId = Environment.ProcessId.ToString(),
                    ThreadId = Environment.CurrentManagedThreadId.ToString(),
                    Properties = new Dictionary<string, object> { { "ComponentError", ex.Message } },
                    Context = new Dictionary<string, object>(),
                    Exception = exception ?? ex,
                    StackTrace = exception?.StackTrace ?? ex.StackTrace,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Obtiene el conjunto de campos reservados que ya están en el nivel raíz del log
        /// Estos campos no deben duplicarse en el diccionario Properties
        /// </summary>
        private static HashSet<string> GetReservedFields()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ServiceName", "Operation", "LogLevel", "Message", "Category", "EventType",
                "UserId", "UserName", "Environment", "Version", "MachineName", "ProcessId", "ThreadId",
                "RequestPath", "RequestMethod", "StatusCode", "ClientIp", "UserAgent",
                "QueryString", "RequestHeaders", "ResponseHeaders", "RequestBody", "ResponseBody",
                "CorrelationId", "RequestId", "SessionId", "Timestamp", "Exception", "StackTrace"
            };
        }

        /// <summary>
        /// Filtra propiedades duplicadas que ya existen en el nivel raíz del log
        /// Evita duplicación de campos según mejores prácticas de logging estructurado
        /// </summary>
        private Dictionary<string, object> FilterDuplicateProperties(Dictionary<string, object>? properties)
        {
            if (properties == null || properties.Count == 0)
                return new Dictionary<string, object>();

            var reservedFields = GetReservedFields();
            var filtered = new Dictionary<string, object>();
            foreach (var prop in properties)
            {
                // Solo agregar si no es un campo reservado (ya está en nivel raíz)
                if (!reservedFields.Contains(prop.Key))
                {
                    filtered[prop.Key] = prop.Value;
                }
            }

            return filtered;
        }

        /// <summary>
        /// Enriquece la entrada de log con información del contexto HTTP y correlación
        /// Aplica mejores prácticas: siempre llena campos con valores por defecto cuando no hay contexto HTTP
        /// Maneja errores internos del componente sin afectar la aplicación
        /// </summary>
        private async Task EnrichLogEntryAsync(Models.StructuredLogEntry logEntry)
        {
            try
            {
                // Enriquecer con información del HTTP Context
                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    try
                    {
                        // Información HTTP disponible
                        logEntry.RequestPath = httpContext.Request.Path.ToString();
                        logEntry.RequestMethod = httpContext.Request.Method;
                        logEntry.StatusCode = httpContext.Response.StatusCode;
                        logEntry.ClientIp = GetClientIpAddress(httpContext);
                        logEntry.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();
                    }
                    catch (Exception ex)
                    {
                        // Error interno del componente al acceder HttpContext - usar valores por defecto
                        _logger.LogWarning(ex, "Error interno del componente al acceder HttpContext. Usando valores por defecto.");
                        logEntry.RequestPath = "N/A";
                        logEntry.RequestMethod = "N/A";
                        logEntry.StatusCode = 0;
                        logEntry.ClientIp = "N/A";
                        logEntry.UserAgent = "N/A";
                    }

                    // Capturar Query String si está habilitado
                    if (_configuration.Enrichment.HttpCapture.IncludeQueryString)
                    {
                        try
                        {
                            logEntry.QueryString = httpContext.Request.QueryString.ToString();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error interno del componente al capturar QueryString.");
                            logEntry.QueryString = null;
                        }
                    }

                    // Capturar Headers HTTP de la petición si está habilitado
                    if (_configuration.Enrichment.HttpCapture.IncludeRequestHeaders)
                    {
                        try
                        {
                            logEntry.RequestHeaders = CaptureRequestHeaders(httpContext);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error interno del componente al capturar RequestHeaders.");
                            logEntry.RequestHeaders = null;
                        }
                    }

                    // Capturar Headers HTTP de la respuesta si está habilitado
                    if (_configuration.Enrichment.HttpCapture.IncludeResponseHeaders)
                    {
                        try
                        {
                            logEntry.ResponseHeaders = CaptureResponseHeaders(httpContext);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error interno del componente al capturar ResponseHeaders.");
                            logEntry.ResponseHeaders = null;
                        }
                    }

                    // Capturar Body de la petición si está habilitado
                    // Nota: El body solo se puede capturar si está disponible en HttpContext.Items
                    // (requiere middleware que lo capture antes)
                    if (_configuration.Enrichment.HttpCapture.IncludeRequestBody)
                    {
                        logEntry.RequestBody = httpContext.Items["JonjubNet.Logging.RequestBody"]?.ToString();
                    }

                    // Capturar Body de la respuesta si está habilitado
                    // Nota: El body solo se puede capturar si está disponible en HttpContext.Items
                    // (requiere middleware que lo capture antes)
                    if (_configuration.Enrichment.HttpCapture.IncludeResponseBody)
                    {
                        logEntry.ResponseBody = httpContext.Items["JonjubNet.Logging.ResponseBody"]?.ToString();
                    }

                    // Agregar IDs de correlación si están configurados
                    // Usar HttpContext.Items para almacenar y reutilizar los IDs en todo el request
                    try
                    {
                        if (_configuration.Correlation.EnableCorrelationId)
                        {
                            const string correlationIdKey = "JonjubNet.Logging.CorrelationId";
                            if (!httpContext.Items.ContainsKey(correlationIdKey))
                            {
                                // Intentar obtener del header, si no existe generar uno nuevo
                                var headerValue = httpContext.Request.Headers[_configuration.Correlation.CorrelationIdHeader].FirstOrDefault();
                                var correlationId = !string.IsNullOrEmpty(headerValue) 
                                    ? headerValue 
                                    : Guid.NewGuid().ToString();
                                
                                // Almacenar en HttpContext.Items para reutilizar
                                httpContext.Items[correlationIdKey] = correlationId;
                                
                                // También establecer en el header de respuesta para que el cliente lo reciba
                                if (string.IsNullOrEmpty(headerValue))
                                {
                                    httpContext.Response.Headers[_configuration.Correlation.CorrelationIdHeader] = correlationId;
                                }
                            }
                            logEntry.CorrelationId = httpContext.Items[correlationIdKey]?.ToString();
                        }

                        if (_configuration.Correlation.EnableRequestId)
                        {
                            const string requestIdKey = "JonjubNet.Logging.RequestId";
                            if (!httpContext.Items.ContainsKey(requestIdKey))
                            {
                                var headerValue = httpContext.Request.Headers[_configuration.Correlation.RequestIdHeader].FirstOrDefault();
                                var requestId = !string.IsNullOrEmpty(headerValue) 
                                    ? headerValue 
                                    : Guid.NewGuid().ToString();
                                
                                httpContext.Items[requestIdKey] = requestId;
                                
                                if (string.IsNullOrEmpty(headerValue))
                                {
                                    httpContext.Response.Headers[_configuration.Correlation.RequestIdHeader] = requestId;
                                }
                            }
                            logEntry.RequestId = httpContext.Items[requestIdKey]?.ToString();
                        }

                        if (_configuration.Correlation.EnableSessionId)
                        {
                            const string sessionIdKey = "JonjubNet.Logging.SessionId";
                            if (!httpContext.Items.ContainsKey(sessionIdKey))
                            {
                                var headerValue = httpContext.Request.Headers[_configuration.Correlation.SessionIdHeader].FirstOrDefault();
                                var sessionId = !string.IsNullOrEmpty(headerValue) 
                                    ? headerValue 
                                    : Guid.NewGuid().ToString();
                                
                                httpContext.Items[sessionIdKey] = sessionId;
                                
                                if (string.IsNullOrEmpty(headerValue))
                                {
                                    httpContext.Response.Headers[_configuration.Correlation.SessionIdHeader] = sessionId;
                                }
                            }
                            logEntry.SessionId = httpContext.Items[sessionIdKey]?.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Error interno del componente al manejar correlación - generar IDs sin HttpContext
                        _logger.LogWarning(ex, "Error interno del componente al manejar IDs de correlación. Generando IDs sin HttpContext.");
                        if (_configuration.Correlation.EnableCorrelationId && string.IsNullOrEmpty(logEntry.CorrelationId))
                        {
                            logEntry.CorrelationId = Guid.NewGuid().ToString();
                        }
                        if (_configuration.Correlation.EnableRequestId && string.IsNullOrEmpty(logEntry.RequestId))
                        {
                            logEntry.RequestId = Guid.NewGuid().ToString();
                        }
                        if (_configuration.Correlation.EnableSessionId && string.IsNullOrEmpty(logEntry.SessionId))
                        {
                            logEntry.SessionId = Guid.NewGuid().ToString();
                        }
                    }
                }
                else
                {
                    // No hay contexto HTTP - aplicar mejores prácticas: llenar con valores por defecto
                    // Esto asegura estructura consistente incluso para logs fuera de contexto HTTP
                    logEntry.RequestPath = "N/A";
                    logEntry.RequestMethod = "N/A";
                    logEntry.StatusCode = 0; // 0 indica que no hay contexto HTTP
                    logEntry.ClientIp = "N/A";
                    logEntry.UserAgent = "N/A";
                    
                    // Campos HTTP adicionales cuando no hay HttpContext
                    if (_configuration.Enrichment.HttpCapture.IncludeQueryString)
                    {
                        logEntry.QueryString = null;
                    }
                    
                    if (_configuration.Enrichment.HttpCapture.IncludeRequestHeaders)
                    {
                        logEntry.RequestHeaders = null;
                    }
                    
                    if (_configuration.Enrichment.HttpCapture.IncludeResponseHeaders)
                    {
                        logEntry.ResponseHeaders = null;
                    }
                    
                    if (_configuration.Enrichment.HttpCapture.IncludeRequestBody)
                    {
                        logEntry.RequestBody = null;
                    }
                    
                    if (_configuration.Enrichment.HttpCapture.IncludeResponseBody)
                    {
                        logEntry.ResponseBody = null;
                    }

                    // Generar IDs de correlación incluso sin HttpContext si están habilitados
                    // Esto permite rastrear logs fuera de contexto HTTP
                    if (_configuration.Correlation.EnableCorrelationId && string.IsNullOrEmpty(logEntry.CorrelationId))
                    {
                        logEntry.CorrelationId = Guid.NewGuid().ToString();
                    }

                    if (_configuration.Correlation.EnableRequestId && string.IsNullOrEmpty(logEntry.RequestId))
                    {
                        logEntry.RequestId = Guid.NewGuid().ToString();
                    }

                    if (_configuration.Correlation.EnableSessionId && string.IsNullOrEmpty(logEntry.SessionId))
                    {
                        logEntry.SessionId = Guid.NewGuid().ToString();
                    }
                }

                // Asegurar que los IDs de correlación se generen si están habilitados pero aún son null
                // (por si acaso no se generaron en los bloques anteriores)
                if (_configuration.Correlation.EnableCorrelationId && string.IsNullOrEmpty(logEntry.CorrelationId))
                {
                    logEntry.CorrelationId = Guid.NewGuid().ToString();
                }

                if (_configuration.Correlation.EnableRequestId && string.IsNullOrEmpty(logEntry.RequestId))
                {
                    logEntry.RequestId = Guid.NewGuid().ToString();
                }

                if (_configuration.Correlation.EnableSessionId && string.IsNullOrEmpty(logEntry.SessionId))
                {
                    logEntry.SessionId = Guid.NewGuid().ToString();
                }

                // Agregar propiedades estáticas configuradas (filtradas para evitar duplicación)
                try
                {
                    var reservedFields = GetReservedFields();
                    foreach (var property in _configuration.Enrichment.StaticProperties)
                    {
                        // Solo agregar si no es un campo reservado (ya está en nivel raíz)
                        if (!reservedFields.Contains(property.Key))
                        {
                            logEntry.Properties[property.Key] = property.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Error interno del componente al agregar propiedades estáticas - continuar sin ellas
                    _logger.LogWarning(ex, "Error interno del componente al agregar propiedades estáticas.");
                }
            }
            catch (Exception ex)
            {
                // Error crítico interno del componente en EnrichLogEntryAsync - registrar y continuar
                // No lanzar excepción para no afectar la aplicación
                _logger.LogError(ex, "Error crítico interno del componente en EnrichLogEntryAsync. El log se procesará sin enriquecimiento completo.");
            }
        }

        private bool ShouldFilterLog(Models.StructuredLogEntry logEntry)
        {
            // Filtrar por categoría excluida
            if (_configuration.Filters.ExcludedCategories.Contains(logEntry.Category))
                return true;

            // Filtrar por operación excluida
            if (_configuration.Filters.ExcludedOperations.Contains(logEntry.Operation))
                return true;

            // Filtrar por usuario excluido
            if (_configuration.Filters.ExcludedUsers.Contains(logEntry.UserId))
                return true;

            // Filtrar por nivel de log por categoría
            if (_configuration.Filters.FilterByLogLevel && 
                _configuration.Filters.CategoryLogLevels.TryGetValue(logEntry.Category, out var categoryLevel))
            {
                if (!IsLogLevelEnabled(logEntry.LogLevel, categoryLevel))
                    return true;
            }

            return false;
        }

        private bool IsLogLevelEnabled(string logLevel, string minimumLevel)
        {
            var levels = new[] { Models.LogLevel.Trace, Models.LogLevel.Debug, Models.LogLevel.Information, Models.LogLevel.Warning, Models.LogLevel.Error, Models.LogLevel.Critical, Models.LogLevel.Fatal };
            var logLevelIndex = Array.IndexOf(levels, logLevel);
            var minimumLevelIndex = Array.IndexOf(levels, minimumLevel);
            return logLevelIndex >= minimumLevelIndex;
        }

        private Microsoft.Extensions.Logging.LogLevel GetLogLevel(string logLevel)
        {
            return logLevel switch
            {
                Models.LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
                Models.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
                Models.LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
                Models.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
                Models.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
                Models.LogLevel.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
                Models.LogLevel.Fatal => Microsoft.Extensions.Logging.LogLevel.Critical,
                _ => Microsoft.Extensions.Logging.LogLevel.Information
            };
        }

        private async Task SendToKafkaAsync(Models.StructuredLogEntry logEntry)
        {
            // Verificar si Kafka está habilitado
            if (!_configuration.KafkaProducer.Enabled)
            {
                _logger.LogDebug("Kafka Producer deshabilitado. Mensaje no enviado.");
                return;
            }

            if (_connectionType == KafkaConnectionType.None)
            {
                _logger.LogWarning("Kafka Producer habilitado pero ConnectionType es None. Mensaje no enviado.");
                return;
            }

            try
            {
                var jsonMessage = logEntry.ToJson();
                _logger.LogDebug("Enviando a Kafka. Tipo: {ConnectionType}, Topic: {Topic}", _connectionType, _configuration.KafkaProducer.Topic);

                // Enviar según el tipo de conexión configurado
                switch (_connectionType)
                {
                    case KafkaConnectionType.Native:
                        await SendToKafkaNativeAsync(jsonMessage);
                        _logger.LogDebug("Mensaje enviado exitosamente a Kafka (Native)");
                        break;
                    case KafkaConnectionType.Http:
                    case KafkaConnectionType.Https:
                        await SendToKafkaHttpAsync(jsonMessage);
                        _logger.LogDebug("Mensaje enviado exitosamente a Kafka ({Type})", _connectionType);
                        break;
                    case KafkaConnectionType.WebhookHttp:
                    case KafkaConnectionType.WebhookHttps:
                        await SendToWebhookAsync(jsonMessage);
                        _logger.LogDebug("Mensaje enviado exitosamente a Webhook ({Type})", _connectionType);
                        break;
                    case KafkaConnectionType.None:
                    default:
                        _logger.LogWarning("Tipo de conexión Kafka no válido o no configurado: {ConnectionType}", _connectionType);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log del error pero no re-lanzar para no afectar la aplicación
                _logger.LogError(ex, "Error al enviar log a Kafka. Tipo: {ConnectionType}, Topic: {Topic}", 
                    _connectionType, _configuration.KafkaProducer.Topic);
                throw; // Re-lanzar para que el catch en LogCustom lo maneje
            }
        }

        /// <summary>
        /// Envía mensaje usando conexión directa nativa de Kafka (protocolo binario)
        /// </summary>
        private async Task SendToKafkaNativeAsync(string jsonMessage)
        {
            if (_kafkaProducer == null)
            {
                _logger.LogError("Kafka Producer es null. No se puede enviar el mensaje.");
                throw new InvalidOperationException("Kafka Producer no está inicializado");
            }

            try
            {
                var result = await _kafkaProducer.ProduceAsync(_configuration.KafkaProducer.Topic, new Message<Null, string> { Value = jsonMessage });
                _logger.LogInformation("Mensaje enviado a Kafka (Conexión Nativa). Topic: {Topic}, Offset: {Offset}, Partition: {Partition}", 
                    result.Topic, result.Offset, result.Partition);
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex, "Error al enviar mensaje a Kafka (Conexión Nativa). Topic: {Topic}, Error: {Error}", 
                    _configuration.KafkaProducer.Topic, ex.Error?.Reason ?? ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al enviar mensaje a Kafka (Conexión Nativa). Topic: {Topic}", 
                    _configuration.KafkaProducer.Topic);
                throw;
            }
        }

        /// <summary>
        /// Envía mensaje usando conexión HTTP/HTTPS a través de Kafka REST Proxy
        /// </summary>
        private async Task SendToKafkaHttpAsync(string jsonMessage)
        {
            var kafkaConfig = _configuration.KafkaProducer;
            if (string.IsNullOrEmpty(kafkaConfig.ProducerUrl)) return;

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(kafkaConfig.TimeoutSeconds);

                // Preparar el payload para Kafka REST Proxy
                // El formato esperado por Kafka REST Proxy es: { "records": [{ "value": "mensaje" }] }
                var restProxyPayload = new
                {
                    records = new[]
                    {
                        new { value = jsonMessage }
                    }
                };

                var payloadJson = JsonSerializer.Serialize(restProxyPayload, new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                // Construir la URL completa con el tópico
                var url = $"{kafkaConfig.ProducerUrl.TrimEnd('/')}/topics/{kafkaConfig.Topic}";
                var content = new StringContent(payloadJson, Encoding.UTF8, "application/vnd.kafka.json.v2+json");

                var response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Mensaje enviado a Kafka ({ConnectionType}). Topic: {Topic}",
                        _connectionType == KafkaConnectionType.Https ? "HTTPS" : "HTTP", kafkaConfig.Topic);
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Error al enviar mensaje a Kafka ({ConnectionType}). Status: {StatusCode}, Response: {Response}",
                        _connectionType == KafkaConnectionType.Https ? "HTTPS" : "HTTP", response.StatusCode, responseContent);
                    throw new HttpRequestException($"Kafka REST Proxy responded with {response.StatusCode}: {responseContent}");
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout al enviar mensaje a Kafka ({ConnectionType}). Url: {Url}",
                    _connectionType == KafkaConnectionType.Https ? "HTTPS" : "HTTP", kafkaConfig.ProducerUrl);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar mensaje a Kafka ({ConnectionType}). Url: {Url}",
                    _connectionType == KafkaConnectionType.Https ? "HTTPS" : "HTTP", kafkaConfig.ProducerUrl);
                throw;
            }
        }

        /// <summary>
        /// Envía mensaje a un webhook HTTP/HTTPS (formato JSON directo)
        /// </summary>
        private async Task SendToWebhookAsync(string jsonMessage)
        {
            var kafkaConfig = _configuration.KafkaProducer;
            if (string.IsNullOrEmpty(kafkaConfig.ProducerUrl)) return;

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(kafkaConfig.TimeoutSeconds);

                // Leer headers personalizados si están configurados
                if (kafkaConfig.Headers != null && kafkaConfig.Headers.Count > 0)
                {
                    foreach (var header in kafkaConfig.Headers)
                    {
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // Para webhook, enviamos el JSON directamente sin formato de REST Proxy
                var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(kafkaConfig.ProducerUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Mensaje enviado a Webhook ({ConnectionType}). URL: {Url}",
                        _connectionType == KafkaConnectionType.WebhookHttps ? "HTTPS" : "HTTP", kafkaConfig.ProducerUrl);
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Error al enviar mensaje a Webhook ({ConnectionType}). Status: {StatusCode}, Response: {Response}",
                        _connectionType == KafkaConnectionType.WebhookHttps ? "HTTPS" : "HTTP", response.StatusCode, responseContent);
                    throw new HttpRequestException($"Webhook responded with {response.StatusCode}: {responseContent}");
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout al enviar mensaje a Webhook ({ConnectionType}). Url: {Url}",
                    _connectionType == KafkaConnectionType.WebhookHttps ? "HTTPS" : "HTTP", kafkaConfig.ProducerUrl);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar mensaje a Webhook ({ConnectionType}). Url: {Url}",
                    _connectionType == KafkaConnectionType.WebhookHttps ? "HTTPS" : "HTTP", kafkaConfig.ProducerUrl);
                throw;
            }
        }

        public void Dispose()
        {
            _kafkaProducer?.Dispose();
        }

        /// <summary>
        /// Maneja errores internos del componente
        /// Registra el error localmente e intenta enviarlo a Kafka si es posible
        /// NUNCA lanza excepciones para no afectar la aplicación
        /// </summary>
        private async Task HandleComponentErrorAsync(Exception error, Models.StructuredLogEntry? originalLogEntry, string errorContext)
        {
            try
            {
                // Registrar error del componente localmente
                var originalLogJson = originalLogEntry?.ToJson() ?? "N/A";
                _logger.LogError(error, "Error interno del componente: {ErrorContext}. Tipo: {ConnectionType}, Topic: {Topic}, Log: {StructuredLog}", 
                    errorContext, _connectionType, _configuration.KafkaProducer.Topic, originalLogJson);
                
                // Intentar enviar el error a Kafka (solo errores del componente, no de la aplicación)
                // Solo si el error NO es de tipo de conexión None (para evitar loops)
                if (_connectionType != KafkaConnectionType.None && _configuration.KafkaProducer.Enabled)
                {
                    try
                    {
                        await TrySendComponentErrorToKafkaAsync(error, originalLogEntry, originalLogJson, errorContext);
                    }
                    catch (Exception ex)
                    {
                        // Si falla el envío del error, solo registrar localmente (evitar loop infinito)
                        _logger.LogWarning(ex, "No se pudo enviar el error del componente a Kafka. Error registrado solo localmente.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Error crítico al manejar errores - solo registrar localmente
                _logger.LogError(ex, "Error crítico interno del componente al manejar errores. Error original: {OriginalError}", 
                    error.Message);
            }
        }

        /// <summary>
        /// Intenta enviar un error del componente a Kafka (para registro de errores del componente)
        /// Solo se usa para errores internos del componente, NO para errores de la aplicación
        /// </summary>
        private async Task TrySendComponentErrorToKafkaAsync(Exception error, Models.StructuredLogEntry? originalLogEntry, string originalLogJson, string errorContext)
        {
            try
            {
                // Crear un log de error específico para el error del componente
                var errorLogEntry = new Models.StructuredLogEntry
                {
                    ServiceName = _configuration.ServiceName,
                    Operation = "ComponentError",
                    LogLevel = Models.LogLevel.Error,
                    Message = $"Error interno del componente: {errorContext} - {error.Message}",
                    Category = Models.LogCategory.System,
                    EventType = Models.EventType.Custom,
                    UserId = originalLogEntry?.UserId ?? "Anonymous",
                    UserName = originalLogEntry?.UserName ?? "Anonymous",
                    Environment = _configuration.Environment,
                    Version = _configuration.Version,
                    MachineName = Environment.MachineName,
                    ProcessId = Environment.ProcessId.ToString(),
                    ThreadId = Environment.CurrentManagedThreadId.ToString(),
                    Properties = new Dictionary<string, object>
                    {
                        { "ErrorType", error.GetType().Name },
                        { "ErrorMessage", error.Message },
                        { "ConnectionType", _connectionType.ToString() },
                        { "Topic", _configuration.KafkaProducer.Topic },
                        { "OriginalLog", originalLogJson }
                    },
                    Context = new Dictionary<string, object>
                    {
                        { "ErrorTimestamp", DateTime.UtcNow },
                        { "StackTrace", error.StackTrace ?? "N/A" }
                    },
                    Exception = error,
                    StackTrace = error.StackTrace,
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = originalLogEntry?.CorrelationId,
                    RequestId = originalLogEntry?.RequestId,
                    SessionId = originalLogEntry?.SessionId
                };

                // Enriquecer el log de error
                await EnrichLogEntryAsync(errorLogEntry);

                // Intentar enviar el error a Kafka (usando el mismo método pero con el log de error)
                var errorJson = errorLogEntry.ToJson();
                
                // Enviar según el tipo de conexión (evitar recursión usando los métodos internos directamente)
                switch (_connectionType)
                {
                    case KafkaConnectionType.Native:
                        if (_kafkaProducer != null)
                        {
                            await _kafkaProducer.ProduceAsync(_configuration.KafkaProducer.Topic, 
                                new Message<Null, string> { Value = errorJson });
                        }
                        break;
                    case KafkaConnectionType.Http:
                    case KafkaConnectionType.Https:
                        await SendToKafkaHttpAsync(errorJson);
                        break;
                    case KafkaConnectionType.WebhookHttp:
                    case KafkaConnectionType.WebhookHttps:
                        await SendToWebhookAsync(errorJson);
                        break;
                    case KafkaConnectionType.None:
                    default:
                        _logger.LogWarning("Tipo de conexión Kafka no válido o no configurado: {ConnectionType}", _connectionType);
                        break;
                }
                
                _logger.LogDebug("Error de envío a Kafka registrado exitosamente en Kafka");
            }
            catch (Exception ex)
            {
                // Si falla el envío del error, solo registrar localmente (evitar loop infinito)
                _logger.LogWarning(ex, "No se pudo enviar el error de Kafka a Kafka. Error registrado solo localmente.");
                throw; // Re-lanzar para que el catch externo lo maneje
            }
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                return xForwardedFor.Split(',')[0].Trim();
            }

            var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Captura los headers HTTP de la petición, excluyendo headers sensibles
        /// Maneja errores internos del componente sin afectar la aplicación
        /// </summary>
        private Dictionary<string, string> CaptureRequestHeaders(HttpContext context)
        {
            try
            {
                var headers = new Dictionary<string, string>();
                var sensitiveHeaders = _configuration.Enrichment.HttpCapture.SensitiveHeaders
                    .Select(h => h.ToLowerInvariant())
                    .ToHashSet();

                foreach (var header in context.Request.Headers)
                {
                    var headerName = header.Key;
                    // Excluir headers sensibles
                    if (!sensitiveHeaders.Contains(headerName.ToLowerInvariant()))
                    {
                        headers[headerName] = string.Join(", ", header.Value.ToArray());
                    }
                    else
                    {
                        // Mostrar que el header existe pero no su valor (por seguridad)
                        headers[headerName] = "[REDACTED]";
                    }
                }

                return headers;
            }
            catch (Exception ex)
            {
                // Error interno del componente al capturar headers - retornar diccionario vacío
                _logger.LogWarning(ex, "Error interno del componente al capturar RequestHeaders.");
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Captura los headers HTTP de la respuesta
        /// Maneja errores internos del componente sin afectar la aplicación
        /// </summary>
        private Dictionary<string, string> CaptureResponseHeaders(HttpContext context)
        {
            try
            {
                var headers = new Dictionary<string, string>();

                foreach (var header in context.Response.Headers)
                {
                    headers[header.Key] = string.Join(", ", header.Value.ToArray());
                }

                return headers;
            }
            catch (Exception ex)
            {
                // Error interno del componente al capturar headers - retornar diccionario vacío
                _logger.LogWarning(ex, "Error interno del componente al capturar ResponseHeaders.");
                return new Dictionary<string, string>();
            }
        }

    }
}
