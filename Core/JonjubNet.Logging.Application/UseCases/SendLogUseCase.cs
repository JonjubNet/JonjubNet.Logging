using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace JonjubNet.Logging.Application.UseCases
{
    /// <summary>
    /// Caso de uso para enviar una entrada de log a los sinks configurados
    /// </summary>
    public class SendLogUseCase
    {
        private readonly ILogger<SendLogUseCase> _logger;
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly IKafkaProducer? _kafkaProducer;
        private readonly IEnumerable<ILogSink> _sinks;
        private readonly ILogFilter? _logFilter;
        private readonly ILogSamplingService? _samplingService;
        private readonly IDataSanitizationService? _sanitizationService;
        private readonly ICircuitBreakerManager? _circuitBreakerManager;
        private readonly IRetryPolicyManager? _retryPolicyManager;
        private readonly IDeadLetterQueue? _deadLetterQueue;

        public SendLogUseCase(
            ILogger<SendLogUseCase> logger,
            ILoggingConfigurationManager configurationManager,
            IEnumerable<ILogSink> sinks,
            IKafkaProducer? kafkaProducer = null,
            ILogFilter? logFilter = null,
            ILogSamplingService? samplingService = null,
            IDataSanitizationService? sanitizationService = null,
            ICircuitBreakerManager? circuitBreakerManager = null,
            IRetryPolicyManager? retryPolicyManager = null,
            IDeadLetterQueue? deadLetterQueue = null)
        {
            _logger = logger;
            _configurationManager = configurationManager;
            _sinks = sinks;
            _kafkaProducer = kafkaProducer;
            _logFilter = logFilter;
            _samplingService = samplingService;
            _sanitizationService = sanitizationService;
            _circuitBreakerManager = circuitBreakerManager;
            _retryPolicyManager = retryPolicyManager;
            _deadLetterQueue = deadLetterQueue;
        }

        /// <summary>
        /// Envía una entrada de log a los sinks configurados
        /// </summary>
        public async Task ExecuteAsync(StructuredLogEntry logEntry)
        {
            // 🔍 LOGGING TEMPORAL DE DIAGNÓSTICO
            _logger.LogInformation("🔵 [DIAG] SendLogUseCase.ExecuteAsync() llamado - Message: {Message}, Timestamp: {Timestamp}", 
                logEntry.Message, logEntry.Timestamp);

            var configuration = _configurationManager.Current;
            
            if (!configuration.Enabled)
            {
                _logger.LogWarning("❌ [DIAG] Logging DESHABILITADO en SendLogUseCase - Mensaje descartado");
                return;
            }

            _logger.LogInformation("✅ [DIAG] Logging habilitado, procesando logEntry...");

            try
            {
                // 1. Aplicar filtrado
                if (_logFilter != null)
                {
                    var shouldLog = _logFilter.ShouldLog(logEntry);
                    _logger.LogInformation("🔵 [DIAG] LogFilter.ShouldLog() = {ShouldLog}", shouldLog);
                    if (!shouldLog)
                    {
                        _logger.LogWarning("❌ [DIAG] Log FILTRADO por LogFilter - Mensaje descartado");
                        return; // Log filtrado, no enviar
                    }
                }
                else
                {
                    _logger.LogInformation("✅ [DIAG] No hay LogFilter configurado");
                }

                // 2. Aplicar sampling y rate limiting
                if (_samplingService != null)
                {
                    var shouldLog = _samplingService.ShouldLog(logEntry);
                    _logger.LogInformation("🔵 [DIAG] SamplingService.ShouldLog() = {ShouldLog}", shouldLog);
                    if (!shouldLog)
                    {
                        _logger.LogWarning("❌ [DIAG] Log DESCARTADO por SamplingService - Mensaje descartado");
                        return; // Log descartado por sampling/rate limiting
                    }
                }
                else
                {
                    _logger.LogInformation("✅ [DIAG] No hay SamplingService configurado");
                }

                // 3. Sanitizar datos sensibles (solo si está habilitado)
                _logger.LogInformation("🔵 [DIAG] Sanitizando logEntry...");
                var sanitizedEntry = _sanitizationService != null 
                    ? _sanitizationService.Sanitize(logEntry) 
                    : logEntry;
                _logger.LogInformation("✅ [DIAG] LogEntry sanitizado");

                // OPTIMIZACIÓN: Detectar qué sinks necesitan JSON y serializar una sola vez
                // Esto evita múltiples serializaciones del mismo logEntry
                var needsJson = _kafkaProducer != null && _kafkaProducer.IsEnabled && configuration.KafkaProducer.Enabled;
                var jsonSinks = new List<string>();
                
                // Verificar qué sinks necesitan JSON (Console, etc.)
                _logger.LogInformation("🔵 [DIAG] Verificando sinks disponibles...");
                int totalSinks = 0;
                int enabledSinks = 0;
                foreach (var sink in _sinks)
                {
                    totalSinks++;
                    _logger.LogInformation("  [DIAG] Sink: {Name}, Enabled: {Enabled}", sink.Name, sink.IsEnabled);
                    if (sink.IsEnabled)
                    {
                        enabledSinks++;
                        if (sink.Name == "Console")
                        {
                            needsJson = true;
                            jsonSinks.Add(sink.Name);
                        }
                    }
                }
                _logger.LogInformation("✅ [DIAG] Total sinks: {Total}, Habilitados: {Enabled}", totalSinks, enabledSinks);

                // Serializar JSON una sola vez si se necesita (Kafka o sinks que lo requieren)
                string? sharedJson = null;
                if (needsJson)
                {
                    _logger.LogInformation("🔵 [DIAG] Serializando JSON (necesario para Kafka o Console)...");
                    // Usar serialización optimizada con ArrayPool
                    sharedJson = JonjubNet.Logging.Domain.Common.JsonSerializationHelper.SerializeToJson(sanitizedEntry);
                    _logger.LogInformation("✅ [DIAG] JSON serializado, longitud: {Length}", sharedJson?.Length ?? 0);
                }
                else
                {
                    _logger.LogInformation("✅ [DIAG] No se necesita JSON serializado");
                }

                // Enviar a Kafka si está habilitado (usar JSON compartido)
                if (_kafkaProducer != null && _kafkaProducer.IsEnabled && configuration.KafkaProducer.Enabled && sharedJson != null)
                {
                    _logger.LogInformation("🔵 [DIAG] Enviando a Kafka...");
                    try
                    {
                        await _kafkaProducer.SendAsync(sharedJson);
                        _logger.LogInformation("✅ [DIAG] Mensaje enviado a Kafka exitosamente");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ [DIAG] Error al enviar log a Kafka");
                    }
                }
                else
                {
                    _logger.LogInformation("⚠️ [DIAG] Kafka no disponible o deshabilitado - Producer: {HasProducer}, Enabled: {Enabled}, ConfigEnabled: {ConfigEnabled}, HasJson: {HasJson}",
                        _kafkaProducer != null, _kafkaProducer?.IsEnabled ?? false, configuration.KafkaProducer.Enabled, sharedJson != null);
                }

                // OPTIMIZACIÓN: Asignar JSON pre-serializado al logEntry para que los sinks puedan reutilizarlo
                if (sharedJson != null)
                {
                    sanitizedEntry.PreSerializedJson = sharedJson;
                    _logger.LogInformation("✅ [DIAG] PreSerializedJson asignado al logEntry");
                }

                // Enviar a todos los sinks habilitados en paralelo (usar entrada sanitizada con JSON compartido)
                // OPTIMIZACIÓN: Usar pool de listas para evitar allocations
                _logger.LogInformation("🔵 [DIAG] Preparando envío a {Count} sinks habilitados...", enabledSinks);
                var sinkTasks = JonjubNet.Logging.Domain.Common.GCOptimizationHelpers.RentTaskList();
                try
                {
                    // Primero contar cuántos sinks están habilitados para pre-allocar capacidad
                    int enabledSinkCount = 0;
                    foreach (var sink in _sinks)
                    {
                        if (sink.IsEnabled)
                            enabledSinkCount++;
                    }

                    // Pre-allocar capacidad para evitar redimensionamientos
                    if (sinkTasks.Capacity < enabledSinkCount)
                    {
                        sinkTasks.EnsureCapacity(enabledSinkCount);
                    }

                    // Agregar tareas de sinks habilitados
                    foreach (var sink in _sinks)
                    {
                        if (sink.IsEnabled)
                        {
                            _logger.LogInformation("🔵 [DIAG] Agregando tarea para sink: {Name}", sink.Name);
                            sinkTasks.Add(SendToSinkWithResilienceAsync(sink, sanitizedEntry));
                        }
                    }

                    if (sinkTasks.Count > 0)
                    {
                        _logger.LogInformation("🔵 [DIAG] Ejecutando {Count} tareas de sinks en paralelo...", sinkTasks.Count);
                        // Procesar sinks en paralelo para mejor throughput
                        await Task.WhenAll(sinkTasks);
                        _logger.LogInformation("✅ [DIAG] Todas las tareas de sinks completadas");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ [DIAG] No hay sinks habilitados para enviar");
                    }
                }
                finally
                {
                    // Devolver lista al pool
                    JonjubNet.Logging.Domain.Common.GCOptimizationHelpers.ReturnTaskList(sinkTasks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DIAG] Error al enviar log en SendLogUseCase.ExecuteAsync()");
            }
        }

        /// <summary>
        /// Envía un log a un sink con resiliencia (Retry -> Circuit Breaker -> DLQ)
        /// </summary>
        /// <param name="sink">Sink destino</param>
        /// <param name="logEntry">Entrada de log (puede contener PreSerializedJson para optimización)</param>
        private async Task SendToSinkWithResilienceAsync(ILogSink sink, StructuredLogEntry logEntry)
        {
            // 🔍 LOGGING TEMPORAL DE DIAGNÓSTICO
            _logger.LogInformation("🔵 [DIAG] SendToSinkWithResilienceAsync() llamado - Sink: {SinkName}, Message: {Message}", 
                sink.Name, logEntry.Message);

            var retryPolicy = _retryPolicyManager?.GetPolicy(sink.Name);
            var circuitBreaker = _circuitBreakerManager?.GetBreaker(sink.Name);

            _logger.LogInformation("🔵 [DIAG] Resiliencia - RetryPolicy: {HasRetry}, CircuitBreaker: {HasBreaker}", 
                retryPolicy != null, circuitBreaker != null);

            try
            {
                // 1. Ejecutar con retry policy (si está disponible)
                if (retryPolicy != null)
                {
                    _logger.LogInformation("🔵 [DIAG] Ejecutando con RetryPolicy...");
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        // 2. Ejecutar con circuit breaker (si está disponible)
                        if (circuitBreaker != null)
                        {
                            _logger.LogInformation("🔵 [DIAG] Ejecutando con CircuitBreaker...");
                            await circuitBreaker.ExecuteAsync(async () =>
                            {
                                _logger.LogInformation("🔵 [DIAG] Llamando sink.SendAsync() - Sink: {SinkName}", sink.Name);
                                await sink.SendAsync(logEntry);
                                _logger.LogInformation("✅ [DIAG] sink.SendAsync() completado - Sink: {SinkName}", sink.Name);
                                return Task.CompletedTask;
                            });
                        }
                        else
                        {
                            // Sin circuit breaker, ejecutar directamente
                            _logger.LogInformation("🔵 [DIAG] Llamando sink.SendAsync() directamente (sin CircuitBreaker) - Sink: {SinkName}", sink.Name);
                            await sink.SendAsync(logEntry);
                            _logger.LogInformation("✅ [DIAG] sink.SendAsync() completado - Sink: {SinkName}", sink.Name);
                        }
                        return Task.CompletedTask;
                    });
                }
                else if (circuitBreaker != null)
                {
                    // Solo circuit breaker, sin retry
                    _logger.LogInformation("🔵 [DIAG] Ejecutando solo con CircuitBreaker (sin RetryPolicy)...");
                    await circuitBreaker.ExecuteAsync(async () =>
                    {
                        _logger.LogInformation("🔵 [DIAG] Llamando sink.SendAsync() - Sink: {SinkName}", sink.Name);
                        await sink.SendAsync(logEntry);
                        _logger.LogInformation("✅ [DIAG] sink.SendAsync() completado - Sink: {SinkName}", sink.Name);
                        return Task.CompletedTask;
                    });
                }
                else
                {
                    // Sin resiliencia, ejecutar directamente
                    _logger.LogInformation("🔵 [DIAG] Llamando sink.SendAsync() directamente (sin resiliencia) - Sink: {SinkName}", sink.Name);
                    await sink.SendAsync(logEntry);
                    _logger.LogInformation("✅ [DIAG] sink.SendAsync() completado exitosamente - Sink: {SinkName}", sink.Name);
                }
            }
            catch (CircuitBreakerOpenException ex)
            {
                // Circuit breaker está abierto - sink está caído
                _logger.LogWarning("❌ [DIAG] Sink {SinkName} está caído, circuit breaker abierto", sink.Name);
                
                // Enviar a DLQ si está disponible
                if (_deadLetterQueue != null)
                {
                    _logger.LogInformation("🔵 [DIAG] Enviando a DeadLetterQueue...");
                    await _deadLetterQueue.EnqueueAsync(logEntry, sink.Name, "CircuitBreakerOpen", ex);
                }
            }
            catch (RetryExhaustedException ex)
            {
                // Se agotaron los reintentos
                _logger.LogError(ex, "❌ [DIAG] Se agotaron los reintentos para sink {SinkName}", sink.Name);
                
                // Enviar a DLQ si está disponible
                if (_deadLetterQueue != null)
                {
                    _logger.LogInformation("🔵 [DIAG] Enviando a DeadLetterQueue...");
                    await _deadLetterQueue.EnqueueAsync(logEntry, sink.Name, "RetryExhausted", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [DIAG] Error al enviar log al sink {SinkName}", sink.Name);
                
                // Registrar fallo en circuit breaker
                circuitBreaker?.RecordFailure();
                
                // Enviar a DLQ si está disponible
                if (_deadLetterQueue != null)
                {
                    _logger.LogInformation("🔵 [DIAG] Enviando a DeadLetterQueue...");
                    await _deadLetterQueue.EnqueueAsync(logEntry, sink.Name, ex.GetType().Name, ex);
                }
            }
        }
    }
}

