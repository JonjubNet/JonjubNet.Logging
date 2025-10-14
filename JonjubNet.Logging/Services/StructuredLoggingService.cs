using JonjubNet.Logging.Configuration;
using JonjubNet.Logging.Interfaces;
using JonjubNet.Logging.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace JonjubNet.Logging.Services
{
    /// <summary>
    /// Servicio genérico de logging estructurado
    /// </summary>
    public class StructuredLoggingService : IStructuredLoggingService
    {
        private readonly ILogger<StructuredLoggingService> _logger;
        private readonly LoggingConfiguration _configuration;
        private readonly ICurrentUserService? _currentUserService;
        private readonly IHttpContextAccessor? _httpContextAccessor;

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
                return;

            try
            {
                // Crear mensaje para Kafka
                var kafkaMessage = new
                {
                    Topic = _configuration.KafkaProducer.Topic,
                    Key = logEntry.CorrelationId ?? Guid.NewGuid().ToString(),
                    Value = logEntry.ToJson(),
                    Headers = new Dictionary<string, string>
                    {
                        { "ServiceName", logEntry.ServiceName },
                        { "LogLevel", logEntry.LogLevel },
                        { "Category", logEntry.Category },
                        { "Timestamp", logEntry.Timestamp.ToString("O") }
                    }
                };

                // Enviar a Kafka (implementación con HttpClient por simplicidad)
                // En producción usarías Confluent.Kafka
                var json = JsonSerializer.Serialize(kafkaMessage);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(_configuration.KafkaProducer.TimeoutSeconds);

                var response = await httpClient.PostAsync(_configuration.KafkaProducer.ProducerUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Kafka producer responded with {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Log del error pero no re-lanzar para no afectar la aplicación
                _logger.LogWarning(ex, "Failed to send log to Kafka producer");
                throw; // Re-lanzar para que el catch en LogCustom lo maneje
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
    }
}
