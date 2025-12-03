using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Servicio para sanitizar datos sensibles en logs
    /// </summary>
    public class DataSanitizationService : IDataSanitizationService
    {
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly Dictionary<string, Regex> _compiledPatterns = new();
        private readonly object _patternsLock = new object();

        public DataSanitizationService(ILoggingConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
            var configuration = _configurationManager.Current.DataSanitization;

            // Compilar y cachear patrones regex una vez (optimización de rendimiento)
            CompileAndCachePatterns(configuration.SensitivePatterns);

            // Suscribirse a cambios de configuración para actualizar patrones
            _configurationManager.ConfigurationChanged += OnConfigurationChanged;
        }

        private void CompileAndCachePatterns(List<string> patterns)
        {
            lock (_patternsLock)
            {
                _compiledPatterns.Clear();
                foreach (var pattern in patterns)
                {
                    try
                    {
                        // Cachear por patrón para evitar recompilación
                        if (!_compiledPatterns.ContainsKey(pattern))
                        {
                            _compiledPatterns[pattern] = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        }
                    }
                    catch
                    {
                        // Si el patrón es inválido, lo ignoramos
                    }
                }
            }
        }

        private void OnConfigurationChanged(Application.Configuration.LoggingConfiguration config)
        {
            // Actualizar patrones compilados cuando cambia la configuración
            CompileAndCachePatterns(config.DataSanitization.SensitivePatterns);
        }

        public StructuredLogEntry Sanitize(StructuredLogEntry logEntry)
        {
            var configuration = _configurationManager.Current.DataSanitization;
            if (!configuration.Enabled)
                return logEntry;

            // Crear una copia para no modificar el original
            var sanitized = CloneLogEntry(logEntry);

            // Sanitizar propiedades
            sanitized.Properties = SanitizeDictionary(sanitized.Properties);

            // Sanitizar contexto
            sanitized.Context = SanitizeDictionary(sanitized.Context);

            // Sanitizar headers
            if (sanitized.RequestHeaders != null)
            {
                var sanitizedHeaders = new Dictionary<string, string>();
                foreach (var header in sanitized.RequestHeaders)
                {
                    var sanitizedValue = SanitizeString(header.Value);
                    sanitizedHeaders[header.Key] = sanitizedValue;
                }
                sanitized.RequestHeaders = sanitizedHeaders;
            }

            if (sanitized.ResponseHeaders != null)
            {
                var sanitizedHeaders = new Dictionary<string, string>();
                foreach (var header in sanitized.ResponseHeaders)
                {
                    var sanitizedValue = SanitizeString(header.Value);
                    sanitizedHeaders[header.Key] = sanitizedValue;
                }
                sanitized.ResponseHeaders = sanitizedHeaders;
            }

            // Sanitizar body
            if (!string.IsNullOrEmpty(sanitized.RequestBody))
            {
                sanitized.RequestBody = SanitizeString(sanitized.RequestBody);
            }

            if (!string.IsNullOrEmpty(sanitized.ResponseBody))
            {
                sanitized.ResponseBody = SanitizeString(sanitized.ResponseBody);
            }

            // Sanitizar query string
            if (!string.IsNullOrEmpty(sanitized.QueryString))
            {
                sanitized.QueryString = SanitizeString(sanitized.QueryString);
            }

            return sanitized;
        }

        private Dictionary<string, object> SanitizeDictionary(Dictionary<string, object> dictionary)
        {
            if (dictionary == null || dictionary.Count == 0)
                return dictionary ?? new Dictionary<string, object>();

            var configuration = _configurationManager.Current.DataSanitization;
            var sanitized = new Dictionary<string, object>();

            foreach (var kvp in dictionary)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                // Verificar si el nombre de la propiedad es sensible
                if (IsSensitivePropertyName(key, configuration))
                {
                    sanitized[key] = MaskValue(value?.ToString(), configuration);
                }
                else if (value is string stringValue)
                {
                    // Verificar si el valor contiene datos sensibles por patrón
                    sanitized[key] = SanitizeString(stringValue);
                }
                else if (value is Dictionary<string, object> nestedDict)
                {
                    // Recursivamente sanitizar diccionarios anidados
                    sanitized[key] = SanitizeDictionary(nestedDict);
                }
                else
                {
                    // Para otros tipos, convertir a string y sanitizar
                    var stringified = value?.ToString();
                    if (!string.IsNullOrEmpty(stringified))
                    {
                        sanitized[key] = SanitizeString(stringified);
                    }
                    else
                    {
                        sanitized[key] = value;
                    }
                }
            }

            return sanitized;
        }

        private string SanitizeString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Verificar patrones regex compilados (optimizado)
            // Usar lock solo para lectura (patrones no cambian frecuentemente)
            List<Regex> patterns;
            lock (_patternsLock)
            {
                patterns = _compiledPatterns.Values.ToList();
            }

            foreach (var pattern in patterns)
            {
                if (pattern.IsMatch(value))
                {
                    var configuration = _configurationManager.Current.DataSanitization;
                    return MaskValue(value, configuration);
                }
            }

            return value;
        }

        private bool IsSensitivePropertyName(string propertyName, LoggingDataSanitizationConfiguration configuration)
        {
            if (string.IsNullOrEmpty(propertyName))
                return false;

            return configuration.SensitivePropertyNames.Any(sensitiveName =>
                propertyName.Contains(sensitiveName, StringComparison.OrdinalIgnoreCase));
        }

        private string MaskValue(string? value, LoggingDataSanitizationConfiguration configuration)
        {
            if (string.IsNullOrEmpty(value))
                return configuration.MaskValue;

            if (configuration.MaskPartial && value.Length > configuration.PartialMaskLength)
            {
                var visiblePart = value.Substring(value.Length - configuration.PartialMaskLength);
                return $"{configuration.MaskValue}{visiblePart}";
            }

            return configuration.MaskValue;
        }

        private StructuredLogEntry CloneLogEntry(StructuredLogEntry original)
        {
            // Crear una copia profunda del log entry
            // Optimizado: solo clonar lo necesario, evitar serialización/deserialización costosa
            var clone = new StructuredLogEntry
            {
                ServiceName = original.ServiceName,
                Operation = original.Operation,
                LogLevel = original.LogLevel,
                Message = original.Message,
                Category = original.Category,
                EventType = original.EventType,
                UserId = original.UserId,
                UserName = original.UserName,
                Environment = original.Environment,
                Version = original.Version,
                MachineName = original.MachineName,
                ProcessId = original.ProcessId,
                ThreadId = original.ThreadId,
                Timestamp = original.Timestamp,
                RequestPath = original.RequestPath,
                RequestMethod = original.RequestMethod,
                StatusCode = original.StatusCode,
                ClientIp = original.ClientIp,
                UserAgent = original.UserAgent,
                CorrelationId = original.CorrelationId,
                RequestId = original.RequestId,
                SessionId = original.SessionId,
                QueryString = original.QueryString,
                RequestBody = original.RequestBody,
                ResponseBody = original.ResponseBody,
                StackTrace = original.StackTrace,
                Exception = original.Exception,
                // Pre-allocar capacidad para evitar re-allocations
                Properties = new Dictionary<string, object>(original.Properties.Count),
                Context = new Dictionary<string, object>(original.Context.Count),
                RequestHeaders = original.RequestHeaders != null 
                    ? new Dictionary<string, string>(original.RequestHeaders.Count)
                    : null,
                ResponseHeaders = original.ResponseHeaders != null
                    ? new Dictionary<string, string>(original.ResponseHeaders.Count)
                    : null
            };

            // Copiar propiedades y contexto (más eficiente que constructor)
            foreach (var kvp in original.Properties)
            {
                clone.Properties[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in original.Context)
            {
                clone.Context[kvp.Key] = kvp.Value;
            }

            if (original.RequestHeaders != null)
            {
                foreach (var kvp in original.RequestHeaders)
                {
                    clone.RequestHeaders![kvp.Key] = kvp.Value;
                }
            }

            if (original.ResponseHeaders != null)
            {
                foreach (var kvp in original.ResponseHeaders)
                {
                    clone.ResponseHeaders![kvp.Key] = kvp.Value;
                }
            }

            return clone;
        }
    }
}
