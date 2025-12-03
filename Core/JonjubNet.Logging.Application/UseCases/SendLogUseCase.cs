using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Logging;

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
            var configuration = _configurationManager.Current;
            
            if (!configuration.Enabled)
                return;

            try
            {
                // 1. Aplicar filtrado
                if (_logFilter != null && !_logFilter.ShouldLog(logEntry))
                {
                    return; // Log filtrado, no enviar
                }

                // 2. Aplicar sampling y rate limiting
                if (_samplingService != null && !_samplingService.ShouldLog(logEntry))
                {
                    return; // Log descartado por sampling/rate limiting
                }

                // 3. Sanitizar datos sensibles (solo si está habilitado)
                var sanitizedEntry = _sanitizationService != null 
                    ? _sanitizationService.Sanitize(logEntry) 
                    : logEntry;

                // Serializar JSON solo una vez si se necesita para Kafka
                string? json = null;
                if (_kafkaProducer != null && _kafkaProducer.IsEnabled && configuration.KafkaProducer.Enabled)
                {
                    json = sanitizedEntry.ToJson();
                }

                // Enviar a Kafka si está habilitado
                if (_kafkaProducer != null && _kafkaProducer.IsEnabled && configuration.KafkaProducer.Enabled && json != null)
                {
                    try
                    {
                        await _kafkaProducer.SendAsync(json);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al enviar log a Kafka");
                    }
                }

                // Enviar a todos los sinks habilitados en paralelo (usar entrada sanitizada)
                var enabledSinks = _sinks.Where(s => s.IsEnabled).ToList();
                if (enabledSinks.Count > 0)
                {
                    // Procesar sinks en paralelo para mejor throughput
                    var sinkTasks = enabledSinks.Select(async sink =>
                    {
                        await SendToSinkWithResilienceAsync(sink, sanitizedEntry);
                    });

                    await Task.WhenAll(sinkTasks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar log");
            }
        }

        /// <summary>
        /// Envía un log a un sink con resiliencia (Retry -> Circuit Breaker -> DLQ)
        /// </summary>
        private async Task SendToSinkWithResilienceAsync(ILogSink sink, StructuredLogEntry logEntry)
        {
            var retryPolicy = _retryPolicyManager?.GetPolicy(sink.Name);
            var circuitBreaker = _circuitBreakerManager?.GetBreaker(sink.Name);

            try
            {
                // 1. Ejecutar con retry policy (si está disponible)
                if (retryPolicy != null)
                {
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        // 2. Ejecutar con circuit breaker (si está disponible)
                        if (circuitBreaker != null)
                        {
                            await circuitBreaker.ExecuteAsync(async () =>
                            {
                                await sink.SendAsync(logEntry);
                                return Task.CompletedTask;
                            });
                        }
                        else
                        {
                            // Sin circuit breaker, ejecutar directamente
                            await sink.SendAsync(logEntry);
                        }
                        return Task.CompletedTask;
                    });
                }
                else if (circuitBreaker != null)
                {
                    // Solo circuit breaker, sin retry
                    await circuitBreaker.ExecuteAsync(async () =>
                    {
                        await sink.SendAsync(logEntry);
                        return Task.CompletedTask;
                    });
                }
                else
                {
                    // Sin resiliencia, ejecutar directamente
                    await sink.SendAsync(logEntry);
                }
            }
            catch (CircuitBreakerOpenException ex)
            {
                // Circuit breaker está abierto - sink está caído
                _logger.LogWarning("Sink {SinkName} está caído, circuit breaker abierto", sink.Name);
                
                // Enviar a DLQ si está disponible
                if (_deadLetterQueue != null)
                {
                    await _deadLetterQueue.EnqueueAsync(logEntry, sink.Name, "CircuitBreakerOpen", ex);
                }
            }
            catch (RetryExhaustedException ex)
            {
                // Se agotaron los reintentos
                _logger.LogError(ex, "Se agotaron los reintentos para sink {SinkName}", sink.Name);
                
                // Enviar a DLQ si está disponible
                if (_deadLetterQueue != null)
                {
                    await _deadLetterQueue.EnqueueAsync(logEntry, sink.Name, "RetryExhausted", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar log al sink {SinkName}", sink.Name);
                
                // Registrar fallo en circuit breaker
                circuitBreaker?.RecordFailure();
                
                // Enviar a DLQ si está disponible
                if (_deadLetterQueue != null)
                {
                    await _deadLetterQueue.EnqueueAsync(logEntry, sink.Name, ex.GetType().Name, ex);
                }
            }
        }
    }
}

