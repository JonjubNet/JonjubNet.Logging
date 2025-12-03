using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Options;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Filtro para determinar si un log debe ser enviado a los sinks basado en la configuración
    /// </summary>
    public class LogFilter : ILogFilter
    {
        private readonly LoggingFiltersConfiguration _configuration;

        public LogFilter(IOptions<LoggingConfiguration> configuration)
        {
            _configuration = configuration.Value.Filters;
        }

        public bool ShouldLog(StructuredLogEntry logEntry)
        {
            // Filtrar por categoría excluida
            if (_configuration.ExcludedCategories.Contains(logEntry.Category, StringComparer.OrdinalIgnoreCase))
                return false;

            // Filtrar por operación excluida
            if (_configuration.ExcludedOperations.Contains(logEntry.Operation, StringComparer.OrdinalIgnoreCase))
                return false;

            // Filtrar por usuario excluido
            if (!string.IsNullOrEmpty(logEntry.UserId) && 
                _configuration.ExcludedUsers.Contains(logEntry.UserId, StringComparer.OrdinalIgnoreCase))
                return false;

            // Filtrar por nivel de log por categoría
            if (_configuration.FilterByLogLevel && 
                _configuration.CategoryLogLevels.TryGetValue(logEntry.Category, out var minLevel))
            {
                if (!IsLogLevelAboveOrEqual(logEntry.LogLevel, minLevel))
                    return false;
            }

            return true;
        }

        private bool IsLogLevelAboveOrEqual(string logLevel, string minLevel)
        {
            var levels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "Fatal" };
            var logLevelIndex = Array.FindIndex(levels, l => l.Equals(logLevel, StringComparison.OrdinalIgnoreCase));
            var minLevelIndex = Array.FindIndex(levels, l => l.Equals(minLevel, StringComparison.OrdinalIgnoreCase));

            if (logLevelIndex == -1) logLevelIndex = 2; // Default to Information
            if (minLevelIndex == -1) minLevelIndex = 2; // Default to Information

            return logLevelIndex >= minLevelIndex;
        }
    }
}
