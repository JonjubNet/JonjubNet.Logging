using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Manager para obtener políticas de retry por sink
    /// </summary>
    public class RetryPolicyManager : IRetryPolicyManager
    {
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly ILogger<RetryPolicyService>? _logger;
        private readonly ConcurrentDictionary<string, IRetryPolicy> _policies = new();

        public RetryPolicyManager(
            ILoggingConfigurationManager configurationManager,
            ILogger<RetryPolicyService>? logger = null)
        {
            _configurationManager = configurationManager;
            _logger = logger;
        }

        public IRetryPolicy GetPolicy(string sinkName)
        {
            return _policies.GetOrAdd(sinkName, name =>
            {
                var config = _configurationManager.Current.RetryPolicy;
                if (!config.Enabled)
                {
                    return new NoRetryPolicy();
                }

                // Obtener configuración específica del sink o usar la por defecto
                var sinkConfig = config.PerSink.TryGetValue(name, out var specific) ? specific : null;
                var defaultConfig = config.Default;

                var policyConfig = new RetryPolicyConfiguration
                {
                    Enabled = sinkConfig?.Enabled ?? true,
                    Strategy = ParseStrategy(sinkConfig?.Strategy ?? defaultConfig.Strategy),
                    MaxRetries = sinkConfig?.MaxRetries ?? defaultConfig.MaxRetries,
                    InitialDelay = sinkConfig?.InitialDelay ?? defaultConfig.InitialDelay,
                    MaxDelay = sinkConfig?.MaxDelay ?? defaultConfig.MaxDelay,
                    BackoffMultiplier = sinkConfig?.BackoffMultiplier ?? defaultConfig.BackoffMultiplier,
                    // OPTIMIZACIÓN: Eliminar ToList() - usar lista directamente sin LINQ intermedio
                    NonRetryableExceptions = ParseNonRetryableExceptions(config.NonRetryableExceptions)
                };

                if (!policyConfig.Enabled || policyConfig.Strategy == RetryStrategy.NoRetry)
                {
                    return new NoRetryPolicy();
                }

                return policyConfig.Strategy switch
                {
                    RetryStrategy.FixedDelay => new FixedDelayRetryPolicy(policyConfig, _logger),
                    RetryStrategy.ExponentialBackoff => new ExponentialBackoffRetryPolicy(policyConfig, _logger),
                    RetryStrategy.JitteredExponentialBackoff => new JitteredExponentialBackoffRetryPolicy(policyConfig, _logger),
                    _ => new NoRetryPolicy()
                };
            });
        }

        private static RetryStrategy ParseStrategy(string strategy)
        {
            return strategy switch
            {
                "NoRetry" => RetryStrategy.NoRetry,
                "FixedDelay" => RetryStrategy.FixedDelay,
                "ExponentialBackoff" => RetryStrategy.ExponentialBackoff,
                "JitteredExponentialBackoff" => RetryStrategy.JitteredExponentialBackoff,
                _ => RetryStrategy.ExponentialBackoff
            };
        }

        private static List<Type> ParseNonRetryableExceptions(List<string> typeNames)
        {
            // OPTIMIZACIÓN: Eliminar LINQ Select().Where().Cast().ToList() - usar foreach directo
            var result = new List<Type>(typeNames.Count);
            foreach (var typeName in typeNames)
            {
                var type = Type.GetType(typeName);
                if (type != null)
                {
                    result.Add(type);
                }
            }
            return result;
        }

        /// <summary>
        /// Política sin reintentos
        /// </summary>
        private class NoRetryPolicy : IRetryPolicy
        {
            public int MaxRetries => 0;

            public Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
            {
                return operation();
            }

            public bool ShouldRetry(Exception exception, int attemptNumber) => false;
            public TimeSpan GetDelay(int attemptNumber) => TimeSpan.Zero;
        }

        /// <summary>
        /// Política con delay fijo
        /// </summary>
        private class FixedDelayRetryPolicy : RetryPolicyService
        {
            public FixedDelayRetryPolicy(RetryPolicyConfiguration config, ILogger<RetryPolicyService>? logger)
                : base(config, logger)
            {
            }

            protected override TimeSpan CalculateDelay(int attemptNumber)
            {
                return _config.InitialDelay;
            }
        }

        /// <summary>
        /// Política con exponential backoff
        /// </summary>
        private class ExponentialBackoffRetryPolicy : RetryPolicyService
        {
            public ExponentialBackoffRetryPolicy(RetryPolicyConfiguration config, ILogger<RetryPolicyService>? logger)
                : base(config, logger)
            {
            }

            protected override TimeSpan CalculateDelay(int attemptNumber)
            {
                var delayMs = _config.InitialDelay.TotalMilliseconds * Math.Pow(_config.BackoffMultiplier, attemptNumber - 1);
                var delay = TimeSpan.FromMilliseconds(delayMs);
                return delay > _config.MaxDelay ? _config.MaxDelay : delay;
            }
        }

        /// <summary>
        /// Política con exponential backoff y jitter.
        /// Optimizado para .NET 10: usa Random.Shared en lugar de ThreadLocal.
        /// </summary>
        private class JitteredExponentialBackoffRetryPolicy : RetryPolicyService
        {
            public JitteredExponentialBackoffRetryPolicy(RetryPolicyConfiguration config, ILogger<RetryPolicyService>? logger)
                : base(config, logger)
            {
            }

            protected override TimeSpan CalculateDelay(int attemptNumber)
            {
                var baseDelay = _config.InitialDelay.TotalMilliseconds * Math.Pow(_config.BackoffMultiplier, attemptNumber - 1);
                // Jitter: ±25% de variación usando Random.Shared (thread-safe en .NET 6+)
                var jitter = baseDelay * 0.25 * (2 * Random.Shared.NextDouble() - 1);
                var delayMs = baseDelay + jitter;
                var delay = TimeSpan.FromMilliseconds(delayMs);
                return delay > _config.MaxDelay ? _config.MaxDelay : delay;
            }
        }
    }

    /// <summary>
    /// Configuración interna de retry policy
    /// </summary>
    public class RetryPolicyConfiguration
    {
        public bool Enabled { get; set; } = true;
        public RetryStrategy Strategy { get; set; } = RetryStrategy.ExponentialBackoff;
        public int MaxRetries { get; set; } = 3;
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
        public double BackoffMultiplier { get; set; } = 2.0;
        public List<Type> NonRetryableExceptions { get; set; } = new();
    }

    /// <summary>
    /// Implementación base de política de retry
    /// </summary>
    public abstract class RetryPolicyService : IRetryPolicy
    {
        protected readonly RetryPolicyConfiguration _config;
        protected readonly ILogger<RetryPolicyService>? _logger;

        protected RetryPolicyService(RetryPolicyConfiguration config, ILogger<RetryPolicyService>? logger)
        {
            _config = config;
            _logger = logger;
        }

        public int MaxRetries => _config.MaxRetries;

        /// <summary>
        /// Ejecuta una operación con la política de retry configurada.
        /// </summary>
        /// <typeparam name="T">Tipo de retorno de la operación.</typeparam>
        /// <param name="operation">Operación a ejecutar.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Resultado de la operación.</returns>
        /// <exception cref="RetryExhaustedException">Se lanza cuando se agotan los reintentos.</exception>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt <= _config.MaxRetries)
            {
                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    // Verificar si es retryable
                    if (!ShouldRetry(ex, attempt))
                    {
                        throw; // No retryable, lanzar inmediatamente
                    }

                    attempt++;

                    if (attempt > _config.MaxRetries)
                    {
                        break; // Se agotaron los reintentos
                    }

                    // Calcular delay
                    var delay = GetDelay(attempt);
                    _logger?.LogDebug("Reintentando operación después de {Delay}ms (intento {Attempt}/{MaxRetries})", 
                        delay.TotalMilliseconds, attempt, _config.MaxRetries);

                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            // Se agotaron los reintentos
            throw new RetryExhaustedException(
                $"Se agotaron los {_config.MaxRetries} reintentos",
                lastException);
        }

        public bool ShouldRetry(Exception exception, int attemptNumber)
        {
            if (attemptNumber > _config.MaxRetries)
                return false;

            // Verificar si el tipo de excepción está en la lista de no retryable
            var exceptionType = exception.GetType();
            if (_config.NonRetryableExceptions.Any(nonRetryable => 
                nonRetryable.IsAssignableFrom(exceptionType)))
            {
                return false;
            }

            return true;
        }

        public TimeSpan GetDelay(int attemptNumber)
        {
            if (attemptNumber <= 0)
                return TimeSpan.Zero;

            return CalculateDelay(attemptNumber);
        }

        protected abstract TimeSpan CalculateDelay(int attemptNumber);
    }
}

