using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Manager para obtener circuit breakers por sink
    /// </summary>
    public class CircuitBreakerManager : ICircuitBreakerManager
    {
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly ILogger<CircuitBreakerService>? _logger;
        private readonly ConcurrentDictionary<string, ICircuitBreaker> _breakers = new();

        public CircuitBreakerManager(
            ILoggingConfigurationManager configurationManager,
            ILogger<CircuitBreakerService>? logger = null)
        {
            _configurationManager = configurationManager;
            _logger = logger;
        }

        public ICircuitBreaker GetBreaker(string sinkName)
        {
            return _breakers.GetOrAdd(sinkName, name =>
            {
                var config = _configurationManager.Current.CircuitBreaker;
                if (!config.Enabled)
                {
                    // Retornar un circuit breaker deshabilitado (no-op)
                    return new DisabledCircuitBreaker(name);
                }

                // Obtener configuración específica del sink o usar la por defecto
                var sinkConfig = config.PerSink.TryGetValue(name, out var specific) ? specific : null;
                var breakerConfig = new CircuitBreakerService.CircuitBreakerConfiguration
                {
                    Enabled = sinkConfig?.Enabled ?? true,
                    FailureThreshold = sinkConfig?.FailureThreshold ?? config.Default.FailureThreshold,
                    OpenTimeout = sinkConfig?.OpenTimeout ?? config.Default.OpenTimeout,
                    HalfOpenTestCount = sinkConfig?.HalfOpenTestCount ?? config.Default.HalfOpenTestCount
                };

                return new CircuitBreakerService(name, breakerConfig, _logger);
            });
        }

        /// <summary>
        /// Circuit breaker deshabilitado que no hace nada
        /// </summary>
        private class DisabledCircuitBreaker : ICircuitBreaker
        {
            private readonly string _sinkName;

            public DisabledCircuitBreaker(string sinkName)
            {
                _sinkName = sinkName;
            }

            public CircuitBreakerState State => CircuitBreakerState.Closed;
            public string SinkName => _sinkName;

            public Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
            {
                return operation();
            }

            public void RecordSuccess() { }
            public void RecordFailure() { }
            public void Reset() { }
        }
    }
}

