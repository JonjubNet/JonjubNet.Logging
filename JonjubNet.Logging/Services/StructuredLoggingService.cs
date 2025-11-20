using JonjubNet.Logging.Configuration;
using JonjubNet.Logging.Interfaces;
using JonjubNet.Logging.Models;
using Microsoft.AspNetCore.Http;
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
        private readonly ICurrentUserService? _currentUserService;
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
            ICurrentUserService? currentUserService = null,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _logger = logger;
            _configuration = configuration.Value;
            _currentUserService = currentUserService;
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
                        LingerMs = kafkaConfig.LingerMs,
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

        public void LogCustom(Models.StructuredLogEntry logEntry)
        {
            if (!_configuration.Enabled)
                return;

            // Aplicar filtros
            if (ShouldFilterLog(logEntry))
                return;

            // Enriquecer con información del contexto
            EnrichLogEntry(logEntry);

            // Enviar a Kafka de forma asíncrona (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendToKafkaAsync(logEntry);
                }
                catch (Exception ex)
                {
                    // Fallback: log local si Kafka falla
                    var logLevel = GetLogLevel(logEntry.LogLevel);
                    var structuredMessage = logEntry.ToJson();
                    _logger.LogError(ex, "Error sending log to Kafka. Log: {StructuredLog}", structuredMessage);
                }
            });

            // También mantener logging local como fallback
            var localLogLevel = GetLogLevel(logEntry.LogLevel);
            var localMessage = logEntry.ToJson();
            _logger.Log(localLogLevel, "{StructuredLog}", localMessage);
        }

        public void LogOperationStart(string operation, string category = "", Dictionary<string, object>? properties = null)
        {
            var context = new Dictionary<string, object>
            {
                { "OperationStart", DateTime.UtcNow },
                { "OperationStatus", "Started" }
            };

            LogInformation($"Operation started: {operation}", operation, category, properties, context);
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

            if (success)
            {
                LogInformation($"Operation completed: {operation}", operation, category, properties, context);
            }
            else
            {
                LogError($"Operation failed: {operation}", operation, category, properties, context, exception);
            }
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

            LogInformation($"User action: {action}", action, Models.LogCategory.UserAction, properties, context);
        }

        public void LogSecurityEvent(string eventType, string description, Dictionary<string, object>? properties = null, Exception? exception = null)
        {
            var context = new Dictionary<string, object>
            {
                { "SecurityEventType", eventType },
                { "EventTimestamp", DateTime.UtcNow }
            };

            LogWarning($"Security event: {description}", eventType, Models.LogCategory.Security, properties, context, exception);
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

            LogInformation($"Audit event: {description}", eventType, Models.LogCategory.Audit, properties, context);
        }

        private Models.StructuredLogEntry CreateLogEntry(string logLevel, string message, string operation, string category, Dictionary<string, object>? properties, Dictionary<string, object>? context, Exception? exception = null)
        {
            return new Models.StructuredLogEntry
            {
                ServiceName = _configuration.ServiceName,
                Operation = operation,
                LogLevel = logLevel,
                Message = message,
                Category = category,
                UserId = _currentUserService?.GetCurrentUserId() ?? "Anonymous",
                UserName = _currentUserService?.GetCurrentUserName() ?? "Anonymous",
                Environment = _configuration.Environment,
                Version = _configuration.Version,
                MachineName = Environment.MachineName,
                ProcessId = Environment.ProcessId.ToString(),
                ThreadId = Environment.CurrentManagedThreadId.ToString(),
                Properties = properties ?? new Dictionary<string, object>(),
                Context = context ?? new Dictionary<string, object>(),
                Exception = exception,
                StackTrace = exception?.StackTrace,
                Timestamp = DateTime.UtcNow
            };
        }

        private void EnrichLogEntry(Models.StructuredLogEntry logEntry)
        {
            // Enriquecer con información del HTTP Context
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                logEntry.RequestPath = httpContext.Request.Path;
                logEntry.RequestMethod = httpContext.Request.Method;
                logEntry.StatusCode = httpContext.Response.StatusCode;
                logEntry.ClientIp = GetClientIpAddress(httpContext);
                logEntry.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();

                // Agregar IDs de correlación si están configurados
                if (_configuration.Correlation.EnableCorrelationId)
                {
                    logEntry.CorrelationId = httpContext.Request.Headers[_configuration.Correlation.CorrelationIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();
                }

                if (_configuration.Correlation.EnableRequestId)
                {
                    logEntry.RequestId = httpContext.Request.Headers[_configuration.Correlation.RequestIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();
                }

                if (_configuration.Correlation.EnableSessionId)
                {
                    logEntry.SessionId = httpContext.Request.Headers[_configuration.Correlation.SessionIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();
                }
            }

            // Agregar propiedades estáticas configuradas
            foreach (var property in _configuration.Enrichment.StaticProperties)
            {
                logEntry.Properties[property.Key] = property.Value;
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
    }
}
