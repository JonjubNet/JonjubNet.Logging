using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Servicio mejorado para determinar si un log debe ser muestreado (sampling) o limitado por rate.
    /// Optimizado para .NET 10: usa Random.Shared en lugar de ThreadLocal para mejor rendimiento.
    /// </summary>
    public class LogSamplingService : ILogSamplingService, IDisposable
    {
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly TimeProvider _timeProvider;
        
        // Rate limiting: contador de logs por nivel por minuto
        private readonly ConcurrentDictionary<string, RateLimitCounter> _rateLimitCounters = new();
        
        // Limpiar contadores antiguos periódicamente (cada 5 minutos)
        private DateTimeOffset _lastCleanup;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Inicializa una nueva instancia de LogSamplingService.
        /// </summary>
        /// <param name="configurationManager">Gestor de configuración de logging.</param>
        /// <param name="timeProvider">TimeProvider para obtener la hora actual (permite testing y time mocking).</param>
        public LogSamplingService(
            ILoggingConfigurationManager configurationManager,
            TimeProvider? timeProvider = null)
        {
            _configurationManager = configurationManager;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _lastCleanup = _timeProvider.GetUtcNow();
            
            // Suscribirse a cambios de configuración para limpiar contadores si es necesario
            _configurationManager.ConfigurationChanged += OnConfigurationChanged;
        }

        private void OnConfigurationChanged(LoggingConfiguration configuration)
        {
            // Si se deshabilita el sampling o se cambian los límites, limpiar contadores
            if (!configuration.Sampling.Enabled)
            {
                _rateLimitCounters.Clear();
            }
        }

        private LoggingSamplingConfiguration Configuration => _configurationManager.Current.Sampling;

        public bool ShouldLog(StructuredLogEntry logEntry)
        {
            var config = Configuration;
            
            if (!config.Enabled)
                return true;

            // Nunca muestrear niveles críticos
            if (config.NeverSampleLevels.Contains(logEntry.LogLevel, StringComparer.OrdinalIgnoreCase))
                return true;

            // Nunca muestrear categorías críticas
            if (config.NeverSampleCategories.Contains(logEntry.Category, StringComparer.OrdinalIgnoreCase))
                return true;

            // Verificar rate limiting
            if (!CheckRateLimit(logEntry.LogLevel, config))
                return false;

            // Limpiar contadores antiguos periódicamente
            CleanupOldCountersIfNeeded();

            // Verificar sampling probabilístico
            if (!CheckSamplingRate(logEntry.LogLevel, config))
                return false;

            return true;
        }

        private void CleanupOldCountersIfNeeded()
        {
            var now = _timeProvider.GetUtcNow();
            if (now - _lastCleanup < _cleanupInterval)
                return;

            _lastCleanup = now;
            var cutoff = now - TimeSpan.FromMinutes(2); // Eliminar contadores de hace más de 2 minutos

            var keysToRemove = new List<string>();
            foreach (var kvp in _rateLimitCounters)
            {
                if (kvp.Value.LastReset < cutoff)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _rateLimitCounters.TryRemove(key, out _);
            }
        }

        private bool CheckRateLimit(string logLevel, LoggingSamplingConfiguration config)
        {
            if (config.MaxLogsPerMinute == null || !config.MaxLogsPerMinute.TryGetValue(logLevel, out var maxPerMinute))
                return true; // Sin límite configurado

            var counter = _rateLimitCounters.GetOrAdd(logLevel, _ => new RateLimitCounter { LastReset = _timeProvider.GetUtcNow() });
            var now = _timeProvider.GetUtcNow();

            // Limpiar contador si pasó un minuto
            if (now - counter.LastReset > TimeSpan.FromMinutes(1))
            {
                counter.Count = 0;
                counter.LastReset = now;
            }

            if (counter.Count >= maxPerMinute)
                return false;

            counter.Count++;
            return true;
        }

        private bool CheckSamplingRate(string logLevel, LoggingSamplingConfiguration config)
        {
            if (config.SamplingRates == null || !config.SamplingRates.TryGetValue(logLevel, out var samplingRate))
                return true; // Sin sampling configurado para este nivel

            // Sampling probabilístico: usar Random.Shared (thread-safe en .NET 6+)
            var randomValue = Random.Shared.NextDouble();
            return randomValue <= samplingRate;
        }

        public void Dispose()
        {
            _configurationManager.ConfigurationChanged -= OnConfigurationChanged;
        }

        private class RateLimitCounter
        {
            public int Count { get; set; }
            public DateTimeOffset LastReset { get; set; }
        }
    }
}
