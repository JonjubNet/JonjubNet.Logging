using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Implementación de Circuit Breaker para proteger contra llamadas repetidas a servicios fallidos
    /// </summary>
    public class CircuitBreakerService : ICircuitBreaker
    {
        private readonly string _sinkName;
        private readonly CircuitBreakerConfiguration _config;
        private readonly ILogger<CircuitBreakerService>? _logger;
        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private int _failureCount = 0;
        private int _halfOpenSuccessCount = 0;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private DateTime _openedAt = DateTime.MinValue;
        private readonly object _lock = new();

        public CircuitBreakerState State
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }

        public string SinkName => _sinkName;

        public CircuitBreakerService(
            string sinkName,
            CircuitBreakerConfiguration config,
            ILogger<CircuitBreakerService>? logger = null)
        {
            _sinkName = sinkName;
            _config = config;
            _logger = logger;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            if (!_config.Enabled)
            {
                // Si está deshabilitado, ejecutar directamente
                return await operation();
            }

            lock (_lock)
            {
                // Verificar si debemos cambiar de estado
                UpdateState();
            }

            var currentState = State;

            if (currentState == CircuitBreakerState.Open)
            {
                // Circuit breaker está abierto - lanzar excepción inmediatamente
                _logger?.LogWarning("Circuit breaker abierto para sink {SinkName}, rechazando operación", _sinkName);
                throw new CircuitBreakerOpenException(_sinkName);
            }

            try
            {
                var result = await operation();

                // Éxito - registrar y actualizar estado
                lock (_lock)
                {
                    RecordSuccessInternal();
                }

                return result;
            }
            catch (Exception ex)
            {
                // Fallo - registrar y actualizar estado
                lock (_lock)
                {
                    RecordFailureInternal();
                }

                throw;
            }
        }

        public void RecordSuccess()
        {
            lock (_lock)
            {
                RecordSuccessInternal();
            }
        }

        public void RecordFailure()
        {
            lock (_lock)
            {
                RecordFailureInternal();
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
                _halfOpenSuccessCount = 0;
                _lastFailureTime = DateTime.MinValue;
                _openedAt = DateTime.MinValue;
                _logger?.LogInformation("Circuit breaker reseteado manualmente para sink {SinkName}", _sinkName);
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
            _lastFailureTime = DateTime.UtcNow;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                // Fallo en HalfOpen - volver a abrir
                _state = CircuitBreakerState.Open;
                _openedAt = DateTime.UtcNow;
                _halfOpenSuccessCount = 0;
                _logger?.LogWarning("Circuit breaker reabierto para sink {SinkName} después de fallo en HalfOpen", _sinkName);
            }
            else if (_state == CircuitBreakerState.Closed && _failureCount >= _config.FailureThreshold)
            {
                // Demasiados fallos - abrir circuit breaker
                _state = CircuitBreakerState.Open;
                _openedAt = DateTime.UtcNow;
                _logger?.LogWarning("Circuit breaker abierto para sink {SinkName} después de {FailureCount} fallos", _sinkName, _failureCount);
            }
        }

        private void UpdateState()
        {
            if (_state == CircuitBreakerState.Open)
            {
                // Verificar si ha pasado suficiente tiempo para intentar HalfOpen
                var timeSinceOpened = DateTime.UtcNow - _openedAt;
                if (timeSinceOpened >= _config.OpenTimeout)
                {
                    _state = CircuitBreakerState.HalfOpen;
                    _halfOpenSuccessCount = 0;
                    _logger?.LogInformation("Circuit breaker cambiado a HalfOpen para sink {SinkName}", _sinkName);
                }
            }
        }

        /// <summary>
        /// Configuración interna del circuit breaker
        /// </summary>
        public class CircuitBreakerConfiguration
        {
            public bool Enabled { get; set; } = true;
            public int FailureThreshold { get; set; } = 5;
            public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMinutes(1);
            public int HalfOpenTestCount { get; set; } = 3;
        }
    }
}

