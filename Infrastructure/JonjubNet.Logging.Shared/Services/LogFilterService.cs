using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Options;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Servicio para filtrar logs antes de enviarlos a los sinks
    /// </summary>
    public class LogFilterService : ILogFilter
    {
        private readonly ILoggingConfigurationManager _configurationManager;

        public LogFilterService(ILoggingConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
        }

        private LoggingFiltersConfiguration Configuration => _configurationManager.Current.Filters;

        public bool ShouldLog(StructuredLogEntry logEntry)
        {
            var config = Configuration;
            var excludedCategories = new HashSet<string>(
                config.ExcludedCategories ?? new List<string>(),
                StringComparer.OrdinalIgnoreCase);
            var excludedOperations = new HashSet<string>(
                config.ExcludedOperations ?? new List<string>(),
                StringComparer.OrdinalIgnoreCase);
            var excludedUsers = new HashSet<string>(
                config.ExcludedUsers ?? new List<string>(),
                StringComparer.OrdinalIgnoreCase);

            // Filtrar por categoría excluida
            if (!string.IsNullOrEmpty(logEntry.Category) && 
                excludedCategories.Contains(logEntry.Category))
            {
                return false;
            }

            // Filtrar por operación excluida
            if (!string.IsNullOrEmpty(logEntry.Operation) && 
                excludedOperations.Contains(logEntry.Operation))
            {
                return false;
            }

            // Filtrar por usuario excluido
            if (!string.IsNullOrEmpty(logEntry.UserId) && 
                excludedUsers.Contains(logEntry.UserId))
            {
                return false;
            }

            // Filtrar por nivel de log mínimo por categoría/operación
            if (config.FilterByLogLevel)
            {
                string? effectiveMinLevel = null;

                // Verificar override temporal primero (tiene prioridad)
                // Usar reflexión para acceder al método interno si es LoggingConfigurationManager
                var managerType = _configurationManager.GetType();
                if (managerType.Name == "LoggingConfigurationManager")
                {
                    var getOverrideMethod = managerType.GetMethod("GetTemporaryOverride", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (getOverrideMethod != null)
                    {
                        var overrideKey = !string.IsNullOrEmpty(logEntry.Category) ? logEntry.Category : "GLOBAL";
                        var parameters = new object[] { overrideKey, null! };
                        var result = (bool)getOverrideMethod.Invoke(_configurationManager, parameters)!;
                        if (result && parameters[1] != null)
                        {
                            var overrideObj = parameters[1];
                            var levelProperty = overrideObj.GetType().GetProperty("Level");
                            if (levelProperty != null)
                            {
                                effectiveMinLevel = levelProperty.GetValue(overrideObj)?.ToString();
                            }
                        }
                    }
                }

                // Si no hay override temporal, usar configuración normal
                if (effectiveMinLevel == null)
                {
                    // Verificar nivel por operación primero (más específico)
                    if (!string.IsNullOrEmpty(logEntry.Operation) &&
                        config.OperationLogLevels != null &&
                        config.OperationLogLevels.TryGetValue(logEntry.Operation, out var operationLevel))
                    {
                        effectiveMinLevel = operationLevel;
                    }
                    // Luego verificar nivel por categoría
                    else if (!string.IsNullOrEmpty(logEntry.Category) &&
                             config.CategoryLogLevels != null &&
                             config.CategoryLogLevels.TryGetValue(logEntry.Category, out var categoryLevel))
                    {
                        effectiveMinLevel = categoryLevel;
                    }
                }

                // Si hay un nivel mínimo efectivo, verificar si el log cumple
                if (effectiveMinLevel != null)
                {
                    if (!IsLogLevelAboveOrEqual(logEntry.LogLevel, effectiveMinLevel))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsLogLevelAboveOrEqual(string logLevel, string minLevel)
        {
            var levels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "Fatal" };
            var currentIndex = Array.FindIndex(levels, l => l.Equals(logLevel, StringComparison.OrdinalIgnoreCase));
            var minIndex = Array.FindIndex(levels, l => l.Equals(minLevel, StringComparison.OrdinalIgnoreCase));

            if (currentIndex == -1) currentIndex = 2; // Default a Information
            if (minIndex == -1) minIndex = 2; // Default a Information

            return currentIndex >= minIndex;
        }
    }
}
