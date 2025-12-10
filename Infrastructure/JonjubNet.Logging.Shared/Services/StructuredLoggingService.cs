using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Implementación del servicio de logging estructurado
    /// Esta implementación está en Infrastructure siguiendo Clean Architecture
    /// </summary>
    /// <summary>
    /// Implementación mejorada del servicio de logging estructurado.
    /// Optimizado para .NET 10: usa ILoggerFactory en lugar de ILogger&lt;T&gt; para Singletons.
    /// </summary>
    public class StructuredLoggingService : IStructuredLoggingService
    {
        private readonly ILogger _logger;
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly CreateLogEntryUseCase _createLogEntryUseCase;
        private readonly EnrichLogEntryUseCase _enrichLogEntryUseCase;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IEnumerable<ILogSink> _sinks;
        private readonly IKafkaProducer? _kafkaProducer;
        private readonly ILogScopeManager _scopeManager;
        private readonly ILogQueue? _logQueue;
        private readonly IPriorityLogQueue? _priorityQueue;

        /// <summary>
        /// Inicializa una nueva instancia de StructuredLoggingService.
        /// </summary>
        /// <param name="loggerFactory">Factory para crear loggers (mejor para Singletons).</param>
        /// <param name="configurationManager">Gestor de configuración de logging.</param>
        /// <param name="createLogEntryUseCase">Caso de uso para crear entradas de log.</param>
        /// <param name="enrichLogEntryUseCase">Caso de uso para enriquecer entradas de log.</param>
        /// <param name="serviceScopeFactory">Factory para crear scopes (necesario para resolver SendLogUseCase Scoped).</param>
        /// <param name="sinks">Colección de sinks de log.</param>
        /// <param name="scopeManager">Gestor de scopes de log.</param>
        /// <param name="kafkaProducer">Productor de Kafka opcional.</param>
        /// <param name="logQueue">Cola de logs opcional.</param>
        /// <param name="priorityQueue">Cola de logs prioritaria opcional.</param>
        public StructuredLoggingService(
            ILoggerFactory loggerFactory,
            ILoggingConfigurationManager configurationManager,
            CreateLogEntryUseCase createLogEntryUseCase,
            EnrichLogEntryUseCase enrichLogEntryUseCase,
            IServiceScopeFactory serviceScopeFactory,
            IEnumerable<ILogSink> sinks,
            ILogScopeManager scopeManager,
            IKafkaProducer? kafkaProducer = null,
            ILogQueue? logQueue = null,
            IPriorityLogQueue? priorityQueue = null)
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);
            _logger = loggerFactory.CreateLogger("JonjubNet.Logging.StructuredLoggingService");
            _configurationManager = configurationManager;
            _createLogEntryUseCase = createLogEntryUseCase;
            _enrichLogEntryUseCase = enrichLogEntryUseCase;
            _serviceScopeFactory = serviceScopeFactory;
            _sinks = sinks;
            _scopeManager = scopeManager;
            _kafkaProducer = kafkaProducer;
            _logQueue = logQueue;
            _priorityQueue = priorityQueue;
        }

        public void LogInformation(string message, string operation = "", string category = "", Dictionary<string, object>? properties = default, Dictionary<string, object>? context = default)
        {
            Log(LogLevelValue.Information, message, operation, category, properties, context);
        }

        public void LogWarning(string message, string operation = "", string category = "", Dictionary<string, object>? properties = default, Dictionary<string, object>? context = default, Exception? exception = null)
        {
            Log(LogLevelValue.Warning, message, operation, category, properties, context, exception);
        }

        public void LogError(string message, string operation = "", string category = "", Dictionary<string, object>? properties = default, Dictionary<string, object>? context = default, Exception? exception = null)
        {
            Log(LogLevelValue.Error, message, operation, category, properties, context, exception);
        }

        public void LogCritical(string message, string operation = "", string category = "", Dictionary<string, object>? properties = default, Dictionary<string, object>? context = default, Exception? exception = null)
        {
            Log(LogLevelValue.Critical, message, operation, category, properties, context, exception);
        }

        public void LogDebug(string message, string operation = "", string category = "", Dictionary<string, object>? properties = default, Dictionary<string, object>? context = default)
        {
            Log(LogLevelValue.Debug, message, operation, category, properties, context);
        }

        public void LogTrace(string message, string operation = "", string category = "", Dictionary<string, object>? properties = default, Dictionary<string, object>? context = default)
        {
            Log(LogLevelValue.Trace, message, operation, category, properties, context);
        }

        public void LogCustom(StructuredLogEntry logEntry)
        {
            // 🔍 LOGGING TEMPORAL DE DIAGNÓSTICO
            _logger.LogInformation("🔵 [DIAG] LogCustom() llamado - Message: {Message}, Timestamp: {Timestamp}", 
                logEntry.Message, logEntry.Timestamp);

            var config = _configurationManager.Current;
            if (!config.Enabled)
            {
                _logger.LogWarning("❌ [DIAG] Logging DESHABILITADO en LogCustom - Mensaje descartado");
                return;
            }

            _logger.LogInformation("✅ [DIAG] Logging habilitado, enriqueciendo logEntry...");

            try
            {
                // OPTIMIZACIÓN: Enriquecer solo lo esencial (rápido, ~0.1ms)
                // El enriquecimiento completo (HTTP context, body) se hace en background
                var minimalEnrichedEntry = _enrichLogEntryUseCase.ExecuteMinimal(logEntry);
                _logger.LogInformation("✅ [DIAG] LogEntry enriquecido mínimamente");

                // Usar cola prioritaria si está disponible, sino usar cola estándar
                if (_priorityQueue != null && config.Batching.EnablePriorityQueues)
                {
                    _logger.LogInformation("🔵 [DIAG] Intentando encolar en PriorityQueue...");
                    // TryEnqueue es síncrono y no bloqueante - overhead mínimo (~0.01ms)
                    if (!_priorityQueue.TryEnqueue(minimalEnrichedEntry))
                    {
                        // Cola llena - log crítico pero no bloqueamos la aplicación
                        _logger.LogWarning("❌ [DIAG] Cola de logs prioritaria LLENA - Mensaje descartado. Considera aumentar capacidad o reducir volumen.");
                    }
                    else
                    {
                        _logger.LogInformation("✅ [DIAG] Log encolado exitosamente en PriorityQueue");
                    }
                }
                else if (_logQueue != null)
                {
                    _logger.LogInformation("🔵 [DIAG] Intentando encolar en LogQueue estándar...");
                    // TryEnqueue es síncrono y no bloqueante - overhead mínimo (~0.01ms)
                    if (!_logQueue.TryEnqueue(minimalEnrichedEntry))
                    {
                        // Cola llena - log crítico pero no bloqueamos la aplicación
                        _logger.LogWarning("❌ [DIAG] Cola de logs estándar LLENA - Mensaje descartado. Considera aumentar capacidad o reducir volumen.");
                    }
                    else
                    {
                        _logger.LogInformation("✅ [DIAG] Log encolado exitosamente en LogQueue estándar");
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ [DIAG] NO HAY COLAS DISPONIBLES - Usando fallback directo (Task.Run)");
                    // Fallback: procesamiento directo (para compatibilidad)
                    // Completar enriquecimiento antes de enviar
                    var fullyEnrichedEntry = _enrichLogEntryUseCase.Execute(minimalEnrichedEntry);
                    _logger.LogInformation("✅ [DIAG] LogEntry enriquecido completamente, enviando directamente...");
                    
                    // Usar Task.Run con manejo de errores mejorado
                    // Crear un scope para resolver SendLogUseCase (Scoped)
                    var task = Task.Run(async () =>
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        try
                        {
                            _logger.LogInformation("🔵 [DIAG] Task.Run ejecutando SendLogUseCase.ExecuteAsync()...");
                            var sendLogUseCase = scope.ServiceProvider.GetRequiredService<SendLogUseCase>();
                            await sendLogUseCase.ExecuteAsync(fullyEnrichedEntry);
                            _logger.LogInformation("✅ [DIAG] SendLogUseCase.ExecuteAsync() completado");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ [DIAG] Error al procesar log en Task.Run");
                        }
                    }, CancellationToken.None);

                    // Registrar la tarea para evitar excepciones no observadas
                    _ = task.ContinueWith(t =>
                    {
                        if (t.IsFaulted && t.Exception != null)
                        {
                            _logger.LogError(t.Exception, "❌ [DIAG] Error no manejado en procesamiento de log (Task.Run)");
                        }
                    }, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DIAG] Error al crear o enriquecer log en LogCustom()");
            }
        }

        public void LogOperationStart(string operation, string category = "", Dictionary<string, object>? properties = default)
        {
            var logEntry = _createLogEntryUseCase.Execute(
                $"Operación iniciada: {operation}",
                LogLevelValue.Information,
                operation,
                string.IsNullOrEmpty(category) ? LogCategoryValue.General : LogCategoryValue.FromString(category),
                EventTypeValue.OperationStart,
                properties
            );

            LogCustom(logEntry);
        }

        public void LogOperationEnd(string operation, string category = "", long executionTimeMs = 0, Dictionary<string, object>? properties = default, bool success = true, Exception? exception = null)
        {
            var message = success 
                ? $"Operación completada: {operation} (Tiempo: {executionTimeMs}ms)"
                : $"Operación fallida: {operation} (Tiempo: {executionTimeMs}ms)";

            var logLevel = success ? LogLevelValue.Information : LogLevelValue.Error;
            // Usar collection expression de C# 13 si properties es null
            var props = properties ?? [];
            props["ExecutionTimeMs"] = executionTimeMs;
            props["Success"] = success;

            var logEntry = _createLogEntryUseCase.Execute(
                message,
                logLevel,
                operation,
                string.IsNullOrEmpty(category) ? LogCategoryValue.General : LogCategoryValue.FromString(category),
                EventTypeValue.OperationEnd,
                props,
                exception: exception
            );

            LogCustom(logEntry);
        }

        public void LogUserAction(string action, string entityType = "", string entityId = "", Dictionary<string, object>? properties = default)
        {
            // Usar collection expression de C# 13 si properties es null
            var props = properties ?? [];
            if (!string.IsNullOrEmpty(entityType))
                props["EntityType"] = entityType;
            if (!string.IsNullOrEmpty(entityId))
                props["EntityId"] = entityId;

            var logEntry = _createLogEntryUseCase.Execute(
                $"Acción de usuario: {action}",
                LogLevelValue.Information,
                action,
                LogCategoryValue.UserAction,
                EventTypeValue.UserAction,
                props
            );

            LogCustom(logEntry);
        }

        public void LogSecurityEvent(string eventType, string description, Dictionary<string, object>? properties = default, Exception? exception = null)
        {
            var logEntry = _createLogEntryUseCase.Execute(
                description,
                LogLevelValue.Warning,
                eventType,
                LogCategoryValue.Security,
                EventTypeValue.SecurityEvent,
                properties,
                exception: exception
            );

            LogCustom(logEntry);
        }

        public void LogAuditEvent(string eventType, string description, string entityType = "", string entityId = "", Dictionary<string, object>? properties = default)
        {
            // Usar collection expression de C# 13 si properties es null
            var props = properties ?? [];
            if (!string.IsNullOrEmpty(entityType))
                props["EntityType"] = entityType;
            if (!string.IsNullOrEmpty(entityId))
                props["EntityId"] = entityId;

            var logEntry = _createLogEntryUseCase.Execute(
                description,
                LogLevelValue.Information,
                eventType,
                LogCategoryValue.Audit,
                EventTypeValue.AuditEvent,
                props
            );

            LogCustom(logEntry);
        }

        public ILogScope BeginScope(Dictionary<string, object> properties)
        {
            ArgumentNullException.ThrowIfNull(properties);
            return new LogScope(properties);
        }

        public ILogScope BeginScope(string key, object value)
        {
            // Pre-allocar capacidad para evitar redimensionamientos
            var properties = new Dictionary<string, object>(1) { [key] = value };
            return new LogScope(properties);
        }

        private void Log(LogLevelValue logLevel, string message, string operation, string category, Dictionary<string, object>? properties, Dictionary<string, object>? context, Exception? exception = null)
        {
            ArgumentNullException.ThrowIfNull(message);

            // 🔍 LOGGING TEMPORAL DE DIAGNÓSTICO
            _logger.LogInformation("🔵 [DIAG] Log() llamado - Level: {Level}, Message: {Message}, Operation: {Operation}", 
                logLevel.Value, message, operation);

            var config = _configurationManager.Current;
            if (!config.Enabled)
            {
                _logger.LogWarning("❌ [DIAG] Logging DESHABILITADO en configuración - Mensaje descartado");
                return;
            }

            _logger.LogInformation("✅ [DIAG] Logging habilitado, creando logEntry...");

            try
            {
                var logEntry = _createLogEntryUseCase.Execute(
                    message,
                    logLevel,
                    operation,
                    string.IsNullOrEmpty(category) ? LogCategoryValue.General : LogCategoryValue.FromString(category),
                    null,
                    properties,
                    context,
                    exception
                );

                _logger.LogInformation("✅ [DIAG] LogEntry creado - Timestamp: {Timestamp}, Category: {Category}", 
                    logEntry.Timestamp, logEntry.Category);

                LogCustom(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DIAG] Error al crear entrada de log");
            }
        }
    }
}

