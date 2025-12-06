using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
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
            _sensitivePatterns = _configuration.SensitivePatterns
                .Select(pattern => new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase))
                .ToList();
        }

        public StructuredLogEntry Sanitize(StructuredLogEntry logEntry)
        {
            if (!_configuration.Enabled)
                return logEntry;

            // Crear una copia para no modificar el original
            var sanitized = CloneLogEntry(logEntry);

            // Sanitizar propiedades
            if (sanitized.Properties != null && sanitized.Properties.Count > 0)
            {
                var sanitizedProperties = new Dictionary<string, object>();
                foreach (var prop in sanitized.Properties)
                {
                    var sanitizedKey = prop.Key;
                    var sanitizedValue = SanitizeValue(prop.Key, prop.Value);
                    sanitizedProperties[sanitizedKey] = sanitizedValue;
                }
                sanitized.Properties = sanitizedProperties;
            }

            // Sanitizar contexto
            if (sanitized.Context != null && sanitized.Context.Count > 0)
            {
                var sanitizedContext = new Dictionary<string, object>();
                foreach (var ctx in sanitized.Context)
                {
                    var sanitizedKey = ctx.Key;
                    var sanitizedValue = SanitizeValue(ctx.Key, ctx.Value);
                    sanitizedContext[sanitizedKey] = sanitizedValue;
                }
                sanitized.Context = sanitizedContext;
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

        private StructuredLogEntry CloneLogEntry(StructuredLogEntry original)
        {
            // Serializar y deserializar para crear una copia profunda
            var json = JsonSerializer.Serialize(original);
            return JsonSerializer.Deserialize<StructuredLogEntry>(json) ?? original;
        }
    }
}

