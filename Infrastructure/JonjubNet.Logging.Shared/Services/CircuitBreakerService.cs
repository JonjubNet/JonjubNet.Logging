using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Implementación mejorada de Circuit Breaker para proteger contra llamadas repetidas a servicios fallidos.
    /// Optimizado para .NET 10: usa SemaphoreSlim en lugar de lock para mejor rendimiento en alta concurrencia.
    /// </summary>
    public class CircuitBreakerService : ICircuitBreaker
    {
        private readonly string _sinkName;
        private readonly CircuitBreakerConfiguration _config;
        private readonly ILogger<CircuitBreakerService>? _logger;
        private readonly TimeProvider _timeProvider;
        
        // Usar SemaphoreSlim en lugar de lock para mejor rendimiento en async
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        
        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private int _failureCount = 0;
        private int _halfOpenSuccessCount = 0;
        private DateTimeOffset _lastFailureTime = DateTimeOffset.MinValue;
        private DateTimeOffset _openedAt = DateTimeOffset.MinValue;

        public CircuitBreakerState State
        {
            get
            {
                // Lectura rápida sin lock para el caso común (estado cerrado)
                // Solo usar semaphore si necesitamos actualizar estado
                return _state;
            }
        }

        public string SinkName => _sinkName;

        /// <summary>
        /// Inicializa una nueva instancia de CircuitBreakerService.
        /// </summary>
        /// <param name="sinkName">Nombre del sink asociado a este circuit breaker.</param>
        /// <param name="config">Configuración del circuit breaker.</param>
        /// <param name="logger">Logger opcional para registrar eventos del circuit breaker.</param>
        /// <param name="timeProvider">TimeProvider para obtener la hora actual (permite testing y time mocking).</param>
        public CircuitBreakerService(
            string sinkName,
            CircuitBreakerConfiguration config,
            ILogger<CircuitBreakerService>? logger = null,
            TimeProvider? timeProvider = null)
        {
            _sinkName = sinkName;
            _config = config;
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <summary>
        /// Ejecuta una operación protegida por el circuit breaker.
        /// </summary>
        /// <typeparam name="T">Tipo de retorno de la operación.</typeparam>
        /// <param name="operation">Operación a ejecutar.</param>
        /// <returns>Resultado de la operación.</returns>
        /// <exception cref="CircuitBreakerOpenException">Se lanza cuando el circuit breaker está abierto.</exception>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            if (!_config.Enabled)
            {
                // Si está deshabilitado, ejecutar directamente
                return await operation().ConfigureAwait(false);
            }

            // Actualizar estado si es necesario (async-safe)
            await UpdateStateAsync().ConfigureAwait(false);

            var currentState = _state;

            if (currentState == CircuitBreakerState.Open)
            {
                // Circuit breaker está abierto - lanzar excepción inmediatamente
                _logger?.LogWarning("Circuit breaker abierto para sink {SinkName}, rechazando operación", _sinkName);
                throw new CircuitBreakerOpenException(_sinkName);
            }

            try
            {
                var result = await operation().ConfigureAwait(false);

                // Éxito - registrar y actualizar estado
                await RecordSuccessAsync().ConfigureAwait(false);

                return result;
            }
            catch
            {
                // Fallo - registrar y actualizar estado
                await RecordFailureAsync().ConfigureAwait(false);

                throw;
            }
        }

        /// <summary>
        /// Registra un éxito en el circuit breaker (método síncrono para compatibilidad).
        /// </summary>
        public void RecordSuccess()
        {
            _semaphore.Wait();
            try
            {
                RecordSuccessInternal();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Registra un éxito en el circuit breaker.
        /// </summary>
        public async Task RecordSuccessAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                RecordSuccessInternal();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Registra un fallo en el circuit breaker (método síncrono para compatibilidad).
        /// </summary>
        public void RecordFailure()
        {
            _semaphore.Wait();
            try
            {
                RecordFailureInternal();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Registra un fallo en el circuit breaker.
        /// </summary>
        public async Task RecordFailureAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                RecordFailureInternal();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Resetea el circuit breaker a su estado inicial (método síncrono para compatibilidad).
        /// </summary>
        public void Reset()
        {
            _semaphore.Wait();
            try
            {
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
                _halfOpenSuccessCount = 0;
                _lastFailureTime = DateTimeOffset.MinValue;
                _openedAt = DateTimeOffset.MinValue;
                _logger?.LogInformation("Circuit breaker reseteado manualmente para sink {SinkName}", _sinkName);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Resetea el circuit breaker a su estado inicial.
        /// </summary>
        public async Task ResetAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
                _halfOpenSuccessCount = 0;
                _lastFailureTime = DateTimeOffset.MinValue;
                _openedAt = DateTimeOffset.MinValue;
                _logger?.LogInformation("Circuit breaker reseteado manualmente para sink {SinkName}", _sinkName);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void RecordSuccessInternal()
        {
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _halfOpenSuccessCount++;
                if (_halfOpenSuccessCount >= _config.HalfOpenTestCount)
                {
                    // Suficientes éxitos en HalfOpen - cerrar circuit breaker
                    _state = CircuitBreakerState.Closed;
                    _failureCount = 0;
                    _halfOpenSuccessCount = 0;
                    _logger?.LogInformation("Circuit breaker cerrado para sink {SinkName} después de recuperación", _sinkName);
                }
            }
            else if (_state == CircuitBreakerState.Closed)
            {
                // Resetear contador de fallos en estado cerrado
                _failureCount = 0;
            }
        }

        private void RecordFailureInternal()
        {
            _failureCount++;
            _lastFailureTime = _timeProvider.GetUtcNow();

            if (_state == CircuitBreakerState.HalfOpen)
            {
                // Fallo en HalfOpen - volver a abrir
                _state = CircuitBreakerState.Open;
                _openedAt = _timeProvider.GetUtcNow();
                _halfOpenSuccessCount = 0;
                _logger?.LogWarning("Circuit breaker reabierto para sink {SinkName} después de fallo en HalfOpen", _sinkName);
            }
            else if (_state == CircuitBreakerState.Closed && _failureCount >= _config.FailureThreshold)
            {
                // Demasiados fallos - abrir circuit breaker
                _state = CircuitBreakerState.Open;
                _openedAt = _timeProvider.GetUtcNow();
                _logger?.LogWarning("Circuit breaker abierto para sink {SinkName} después de {FailureCount} fallos", _sinkName, _failureCount);
            }
        }

        private async Task UpdateStateAsync()
        {
            if (_state == CircuitBreakerState.Open)
            {
                // Verificar si ha pasado suficiente tiempo para intentar HalfOpen
                var now = _timeProvider.GetUtcNow();
                var timeSinceOpened = now - _openedAt;
                if (timeSinceOpened >= _config.OpenTimeout)
                {
                    await _semaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        // Verificar nuevamente dentro del lock (double-check)
                        if (_state == CircuitBreakerState.Open)
                        {
                            var timeSinceOpenedLocked = _timeProvider.GetUtcNow() - _openedAt;
                            if (timeSinceOpenedLocked >= _config.OpenTimeout)
                            {
                                _state = CircuitBreakerState.HalfOpen;
                                _halfOpenSuccessCount = 0;
                                _logger?.LogInformation("Circuit breaker cambiado a HalfOpen para sink {SinkName}", _sinkName);
                            }
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
            }
        }

        /// <summary>
        /// Configuración interna del circuit breaker.
        /// </summary>
        public class CircuitBreakerConfiguration
        {
            /// <summary>
            /// Indica si el circuit breaker está habilitado.
            /// </summary>
            public bool Enabled { get; set; } = true;

            /// <summary>
            /// Número de fallos antes de abrir el circuit breaker.
            /// </summary>
            public int FailureThreshold { get; set; } = 5;

            /// <summary>
            /// Tiempo antes de probar de nuevo cuando está abierto.
            /// </summary>
            public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMinutes(1);

            /// <summary>
            /// Número de intentos en estado HalfOpen.
            /// </summary>
            public int HalfOpenTestCount { get; set; } = 3;
        }
    }
}

