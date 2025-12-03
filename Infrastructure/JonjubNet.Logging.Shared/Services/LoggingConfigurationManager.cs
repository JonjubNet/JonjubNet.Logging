using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Implementación de ILoggingConfigurationManager con soporte para Hot-Reload
    /// Usa IOptionsMonitor para detectar cambios automáticamente desde appsettings.json
    /// También permite cambios manuales en runtime
    /// </summary>
    public class LoggingConfigurationManager : ILoggingConfigurationManager, IDisposable
    {
        private readonly IOptionsMonitor<LoggingConfiguration> _optionsMonitor;
        private readonly ILogger<LoggingConfigurationManager> _logger;
        private LoggingConfiguration _currentConfiguration;
        private readonly IDisposable? _changeListener;
        private readonly object _lock = new();
        private readonly ConcurrentDictionary<string, TemporaryLogLevelOverride> _temporaryOverrides = new();
        private Timer? _expirationTimer;

        /// <summary>
        /// Obtiene un override temporal si existe (método interno para uso por LogFilterService)
        /// </summary>
        internal bool GetTemporaryOverride(string key, out TemporaryLogLevelOverride? overrideObj)
        {
            if (_temporaryOverrides.TryGetValue(key, out overrideObj) && !overrideObj.IsExpired)
            {
                return true;
            }
            overrideObj = null;
            return false;
        }

        public LoggingConfiguration Current
        {
            get
            {
                lock (_lock)
                {
                    return _currentConfiguration;
                }
            }
        }

        public event Action<LoggingConfiguration>? ConfigurationChanged;

        public LoggingConfigurationManager(
            IOptionsMonitor<LoggingConfiguration> optionsMonitor,
            ILogger<LoggingConfigurationManager> logger)
        {
            _optionsMonitor = optionsMonitor;
            _logger = logger;
            _currentConfiguration = _optionsMonitor.CurrentValue;

            // Suscribirse a cambios automáticos desde appsettings.json
            _changeListener = _optionsMonitor.OnChange(config =>
            {
                lock (_lock)
                {
                    _currentConfiguration = config;
                    _logger.LogInformation("Configuración de logging actualizada automáticamente desde appsettings.json");
                    OnConfigurationChanged(config);
                }
            });

            // Iniciar timer para limpiar overrides expirados cada minuto
            _expirationTimer = new Timer(CheckExpiredOverrides, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public bool SetMinimumLevel(string minimumLevel)
        {
            if (string.IsNullOrWhiteSpace(minimumLevel))
                return false;

            var validLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "Fatal" };
            if (!validLevels.Contains(minimumLevel, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Nivel de log inválido: {Level}", minimumLevel);
                return false;
            }

            lock (_lock)
            {
                _currentConfiguration.MinimumLevel = minimumLevel;
                _logger.LogInformation("Nivel mínimo de log cambiado a: {Level}", minimumLevel);
                OnConfigurationChanged(_currentConfiguration);
                return true;
            }
        }

        public bool SetSinkEnabled(string sinkName, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(sinkName))
                return false;

            lock (_lock)
            {
                var sinkNameLower = sinkName.ToLowerInvariant();
                switch (sinkNameLower)
                {
                    case "console":
                        _currentConfiguration.Sinks.EnableConsole = enabled;
                        break;
                    case "file":
                        _currentConfiguration.Sinks.EnableFile = enabled;
                        break;
                    case "http":
                        _currentConfiguration.Sinks.EnableHttp = enabled;
                        break;
                    case "elasticsearch":
                        _currentConfiguration.Sinks.EnableElasticsearch = enabled;
                        break;
                    default:
                        _logger.LogWarning("Sink desconocido: {SinkName}", sinkName);
                        return false;
                }

                _logger.LogInformation("Sink {SinkName} {Status}", sinkName, enabled ? "habilitado" : "deshabilitado");
                OnConfigurationChanged(_currentConfiguration);
                return true;
            }
        }

        public bool SetSamplingRate(string logLevel, double samplingRate)
        {
            if (string.IsNullOrWhiteSpace(logLevel))
                return false;

            if (samplingRate < 0.0 || samplingRate > 1.0)
            {
                _logger.LogWarning("Tasa de sampling debe estar entre 0.0 y 1.0: {Rate}", samplingRate);
                return false;
            }

            lock (_lock)
            {
                if (_currentConfiguration.Sampling.SamplingRates == null)
                {
                    _currentConfiguration.Sampling.SamplingRates = new Dictionary<string, double>();
                }

                _currentConfiguration.Sampling.SamplingRates[logLevel] = samplingRate;
                _logger.LogInformation("Tasa de sampling para nivel {Level} cambiada a: {Rate:P2}", logLevel, samplingRate);
                OnConfigurationChanged(_currentConfiguration);
                return true;
            }
        }

        public bool SetSamplingEnabled(bool enabled)
        {
            lock (_lock)
            {
                _currentConfiguration.Sampling.Enabled = enabled;
                _logger.LogInformation("Sampling {Status}", enabled ? "habilitado" : "deshabilitado");
                OnConfigurationChanged(_currentConfiguration);
                return true;
            }
        }

        public bool SetMaxLogsPerMinute(string logLevel, int maxLogsPerMinute)
        {
            if (string.IsNullOrWhiteSpace(logLevel))
                return false;

            if (maxLogsPerMinute < 0)
            {
                _logger.LogWarning("Máximo de logs por minuto no puede ser negativo: {Max}", maxLogsPerMinute);
                return false;
            }

            lock (_lock)
            {
                if (_currentConfiguration.Sampling.MaxLogsPerMinute == null)
                {
                    _currentConfiguration.Sampling.MaxLogsPerMinute = new Dictionary<string, int>();
                }

                if (maxLogsPerMinute == 0)
                {
                    _currentConfiguration.Sampling.MaxLogsPerMinute.Remove(logLevel);
                    _logger.LogInformation("Límite de logs por minuto removido para nivel {Level}", logLevel);
                }
                else
                {
                    _currentConfiguration.Sampling.MaxLogsPerMinute[logLevel] = maxLogsPerMinute;
                    _logger.LogInformation("Límite de logs por minuto para nivel {Level} cambiado a: {Max}", logLevel, maxLogsPerMinute);
                }

                OnConfigurationChanged(_currentConfiguration);
                return true;
            }
        }

        public bool SetLoggingEnabled(bool enabled)
        {
            lock (_lock)
            {
                _currentConfiguration.Enabled = enabled;
                _logger.LogInformation("Logging {Status}", enabled ? "habilitado" : "deshabilitado");
                OnConfigurationChanged(_currentConfiguration);
                return true;
            }
        }

        public bool SetCategoryLogLevel(string category, string level)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(level))
                return false;

            var validLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "Fatal" };
            if (!validLevels.Contains(level, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Nivel de log inválido: {Level}", level);
                return false;
            }

            lock (_lock)
            {
                if (_currentConfiguration.Filters.CategoryLogLevels == null)
                {
                    _currentConfiguration.Filters.CategoryLogLevels = new Dictionary<string, string>();
                }

                _currentConfiguration.Filters.CategoryLogLevels[category] = level;
                _logger.LogInformation("Nivel de log para categoría {Category} cambiado a: {Level}", category, level);
                OnConfigurationChanged(_currentConfiguration);
                return true;
            }
        }

        public bool SetOperationLogLevel(string operation, string level)
        {
            if (string.IsNullOrWhiteSpace(operation) || string.IsNullOrWhiteSpace(level))
                return false;

            var validLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "Fatal" };
            if (!validLevels.Contains(level, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Nivel de log inválido: {Level}", level);
                return false;
            }

            lock (_lock)
            {
                if (_currentConfiguration.Filters.OperationLogLevels == null)
                {
                    _currentConfiguration.Filters.OperationLogLevels = new Dictionary<string, string>();
                }

                _currentConfiguration.Filters.OperationLogLevels[operation] = level;
                _logger.LogInformation("Nivel de log para operación {Operation} cambiado a: {Level}", operation, level);
                OnConfigurationChanged(_currentConfiguration);
                return true;
            }
        }

        public bool SetTemporaryOverride(string? category, string level, TimeSpan expiration)
        {
            if (string.IsNullOrWhiteSpace(level))
                return false;

            var validLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "Fatal" };
            if (!validLevels.Contains(level, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Nivel de log inválido: {Level}", level);
                return false;
            }

            if (expiration <= TimeSpan.Zero)
            {
                _logger.LogWarning("Tiempo de expiración debe ser mayor que cero");
                return false;
            }

            lock (_lock)
            {
                var key = category ?? "GLOBAL";
                var originalLevel = category != null && _currentConfiguration.Filters.CategoryLogLevels != null &&
                    _currentConfiguration.Filters.CategoryLogLevels.TryGetValue(category, out var orig) 
                    ? orig 
                    : _currentConfiguration.MinimumLevel;

                var overrideObj = new TemporaryLogLevelOverride
                {
                    Category = category,
                    Level = level,
                    ExpiresAt = DateTime.UtcNow.Add(expiration),
                    OriginalLevel = originalLevel
                };

                _temporaryOverrides.AddOrUpdate(key, overrideObj, (k, v) => overrideObj);

                // Aplicar el override inmediatamente
                if (category != null)
                {
                    SetCategoryLogLevel(category, level);
                }
                else
                {
                    SetMinimumLevel(level);
                }

                _logger.LogInformation("Override temporal establecido para {Category} con nivel {Level}, expira en {Expiration}", 
                    category ?? "GLOBAL", level, expiration);
                return true;
            }
        }

        public bool RemoveTemporaryOverride(string? category)
        {
            lock (_lock)
            {
                var key = category ?? "GLOBAL";
                if (!_temporaryOverrides.TryRemove(key, out var overrideObj))
                {
                    return false;
                }

                // Restaurar nivel original
                if (category != null)
                {
                    if (overrideObj.OriginalLevel != null)
                    {
                        SetCategoryLogLevel(category, overrideObj.OriginalLevel);
                    }
                    else if (_currentConfiguration.Filters.CategoryLogLevels != null)
                    {
                        _currentConfiguration.Filters.CategoryLogLevels.Remove(category);
                    }
                }
                else
                {
                    if (overrideObj.OriginalLevel != null)
                    {
                        SetMinimumLevel(overrideObj.OriginalLevel);
                    }
                }

                _logger.LogInformation("Override temporal removido para {Category}", category ?? "GLOBAL");
                return true;
            }
        }

        private void CheckExpiredOverrides(object? state)
        {
            var expiredKeys = new List<string>();
            
            foreach (var kvp in _temporaryOverrides)
            {
                if (kvp.Value.IsExpired)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                var category = key == "GLOBAL" ? null : key;
                RemoveTemporaryOverride(category);
                _logger.LogInformation("Override temporal expirado y removido automáticamente para {Category}", category ?? "GLOBAL");
            }
        }

        private void OnConfigurationChanged(LoggingConfiguration configuration)
        {
            try
            {
                ConfigurationChanged?.Invoke(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al notificar cambio de configuración");
            }
        }

        public void Dispose()
        {
            _changeListener?.Dispose();
            _expirationTimer?.Dispose();
        }
    }
}

