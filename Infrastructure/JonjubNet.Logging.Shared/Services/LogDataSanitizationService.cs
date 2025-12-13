using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Domain.Common;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Servicio para sanitizar datos sensibles en logs (PII, PCI, etc.)
    /// </summary>
    public class LogDataSanitizationService : ILogDataSanitizationService
    {
        private readonly LoggingDataSanitizationConfiguration _configuration;
        private readonly List<Regex> _sensitivePatterns;

        public LogDataSanitizationService(IOptions<LoggingConfiguration> configuration)
        {
            _configuration = configuration.Value.DataSanitization;
            // OPTIMIZACIÓN: Eliminar ToList() - usar lista directamente sin LINQ intermedio
            _sensitivePatterns = new List<Regex>(_configuration.SensitivePatterns.Count);
            foreach (var pattern in _configuration.SensitivePatterns)
            {
                _sensitivePatterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
        }

        public StructuredLogEntry Sanitize(StructuredLogEntry logEntry)
        {
            if (!_configuration.Enabled)
                return logEntry;

            // Crear una copia para no modificar el original
            var sanitized = CloneLogEntry(logEntry);

            // Sanitizar propiedades usando DictionaryPool para reducir allocations
            if (sanitized.Properties != null && sanitized.Properties.Count > 0)
            {
                var sanitizedProperties = DictionaryPool.Rent();
                try
                {
                    // Pre-allocar capacidad para evitar redimensionamientos
                    if (sanitizedProperties.Capacity < sanitized.Properties.Count)
                    {
                        sanitizedProperties.EnsureCapacity(sanitized.Properties.Count);
                    }

                    foreach (var prop in sanitized.Properties)
                    {
                        var sanitizedKey = prop.Key;
                        var sanitizedValue = SanitizeValue(prop.Key, prop.Value);
                        sanitizedProperties[sanitizedKey] = sanitizedValue ?? (object)"***NULL***";
                    }
                    
                    // Crear nuevo diccionario para asignar a la entidad (no devolver el del pool)
                    sanitized.Properties = new Dictionary<string, object>(sanitizedProperties);
                }
                finally
                {
                    DictionaryPool.Return(sanitizedProperties);
                }
            }

            // Sanitizar contexto usando DictionaryPool para reducir allocations
            if (sanitized.Context != null && sanitized.Context.Count > 0)
            {
                var sanitizedContext = DictionaryPool.Rent();
                try
                {
                    // Pre-allocar capacidad para evitar redimensionamientos
                    if (sanitizedContext.Capacity < sanitized.Context.Count)
                    {
                        sanitizedContext.EnsureCapacity(sanitized.Context.Count);
                    }

                    foreach (var ctx in sanitized.Context)
                    {
                        var sanitizedKey = ctx.Key;
                        var sanitizedValue = SanitizeValue(ctx.Key, ctx.Value);
                        sanitizedContext[sanitizedKey] = sanitizedValue ?? (object)"***NULL***";
                    }
                    
                    // Crear nuevo diccionario para asignar a la entidad (no devolver el del pool)
                    sanitized.Context = new Dictionary<string, object>(sanitizedContext);
                }
                finally
                {
                    DictionaryPool.Return(sanitizedContext);
                }
            }

            // Sanitizar headers
            if (sanitized.RequestHeaders != null)
            {
                var sanitizedHeaders = new Dictionary<string, string>();
                foreach (var header in sanitized.RequestHeaders)
                {
                    var sanitizedValue = SanitizeValue(header.Key, header.Value);
                    sanitizedHeaders[header.Key] = sanitizedValue?.ToString() ?? "***REDACTED***";
                }
                sanitized.RequestHeaders = sanitizedHeaders;
            }

            if (sanitized.ResponseHeaders != null)
            {
                var sanitizedHeaders = new Dictionary<string, string>();
                foreach (var header in sanitized.ResponseHeaders)
                {
                    var sanitizedValue = SanitizeValue(header.Key, header.Value);
                    sanitizedHeaders[header.Key] = sanitizedValue?.ToString() ?? "***REDACTED***";
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

        private object? SanitizeValue(string key, object? value)
        {
            if (value == null)
                return value;

            // Verificar si el nombre de la propiedad es sensible
            if (IsSensitivePropertyName(key))
            {
                return MaskValue(value);
            }

            // Convertir a string para verificar patrones
            var stringValue = value.ToString();
            if (string.IsNullOrEmpty(stringValue))
                return value;

            // Verificar patrones regex
            if (MatchesSensitivePattern(stringValue))
            {
                return MaskValue(value);
            }

            return value;
        }

        private string SanitizeString(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? string.Empty;

            // Verificar patrones regex
            if (MatchesSensitivePattern(value))
            {
                return MaskString(value);
            }

            return value;
        }

        private bool IsSensitivePropertyName(string propertyName)
        {
            return _configuration.SensitivePropertyNames.Any(sensitiveName =>
                propertyName.Contains(sensitiveName, StringComparison.OrdinalIgnoreCase));
        }

        private bool MatchesSensitivePattern(string value)
        {
            return _sensitivePatterns.Any(pattern => pattern.IsMatch(value));
        }

        private object MaskValue(object value)
        {
            if (value is string strValue)
            {
                return MaskString(strValue);
            }

            return _configuration.MaskValue;
        }

        private string MaskString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return _configuration.MaskValue;

            if (_configuration.MaskPartial && value.Length > _configuration.PartialMaskLength)
            {
                var visiblePart = value.Substring(value.Length - _configuration.PartialMaskLength);
                return $"{_configuration.MaskValue}{visiblePart}";
            }

            return _configuration.MaskValue;
        }

        /// <summary>
        /// Crea una copia profunda de StructuredLogEntry sin serialización JSON.
        /// Optimizado para mejor rendimiento: clonado manual es ~10x más rápido que serialización.
        /// </summary>
        private StructuredLogEntry CloneLogEntry(StructuredLogEntry original)
        {
            // Clonado manual para mejor rendimiento (evita serialización/deserialización completa)
            var cloned = new StructuredLogEntry
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
                Exception = original.Exception, // Exception es inmutable en este contexto
                StackTrace = original.StackTrace,
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
                ResponseBody = original.ResponseBody
            };

            // Copiar diccionarios (copia superficial de referencias - se reemplazarán en Sanitize)
            if (original.Properties != null && original.Properties.Count > 0)
            {
                cloned.Properties = new Dictionary<string, object>(original.Properties);
            }
            else
            {
                cloned.Properties = new Dictionary<string, object>();
            }

            if (original.Context != null && original.Context.Count > 0)
            {
                cloned.Context = new Dictionary<string, object>(original.Context);
            }
            else
            {
                cloned.Context = new Dictionary<string, object>();
            }

            if (original.RequestHeaders != null && original.RequestHeaders.Count > 0)
            {
                cloned.RequestHeaders = new Dictionary<string, string>(original.RequestHeaders);
            }

            if (original.ResponseHeaders != null && original.ResponseHeaders.Count > 0)
            {
                cloned.ResponseHeaders = new Dictionary<string, string>(original.ResponseHeaders);
            }

            return cloned;
        }
    }
}

