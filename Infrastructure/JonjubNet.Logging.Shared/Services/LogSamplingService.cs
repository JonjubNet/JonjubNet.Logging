using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Servicio para determinar si un log debe ser muestreado (sampling) o limitado por rate
    /// Optimizado para performance: usa ThreadLocal Random y limpieza periódica de contadores
    /// </summary>
    public class LogSamplingService : ILogSamplingService, IDisposable
    {
        private readonly ILoggingConfigurationManager _configurationManager;
        // ThreadLocal Random para evitar contention en alta concurrencia
        private static readonly ThreadLocal<Random> _random = new(() => new Random(Environment.TickCount + Thread.CurrentThread.ManagedThreadId));
        
        // Rate limiting: contador de logs por nivel por minuto
        private readonly ConcurrentDictionary<string, RateLimitCounter> _rateLimitCounters = new();
        
        // Limpiar contadores antiguos periódicamente (cada 5 minutos)
        private DateTime _lastCleanup = DateTime.UtcNow;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

        public LogSamplingService(ILoggingConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
            
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
            var now = DateTime.UtcNow;
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

            var counter = _rateLimitCounters.GetOrAdd(logLevel, _ => new RateLimitCounter());
            var now = DateTime.UtcNow;

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

            // Sampling probabilístico: usar ThreadLocal Random para evitar contention
            var randomValue = _random.Value!.NextDouble();
            return randomValue <= samplingRate;
        }

        public void Dispose()
        {
            _configurationManager.ConfigurationChanged -= OnConfigurationChanged;
        }

        private class RateLimitCounter
        {
            public int Count { get; set; }
            public DateTime LastReset { get; set; } = DateTime.UtcNow;
        }
    }
}
