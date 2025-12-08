using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Domain.ValueObjects;

namespace JonjubNet.Logging.Application.UseCases
{
    /// <summary>
    /// Caso de uso para crear una entrada de log estructurado
    /// </summary>
    public class CreateLogEntryUseCase
    {
        /// <summary>
        /// Crea una entrada de log estructurado con los parámetros proporcionados
        /// </summary>
        public StructuredLogEntry Execute(
            string message,
            LogLevelValue logLevel,
            string operation = "",
            LogCategoryValue? category = null,
            EventTypeValue? eventType = null,
            Dictionary<string, object>? properties = null,
            Dictionary<string, object>? context = null,
            Exception? exception = null)
        {
            // OPTIMIZACIÓN: Pre-allocar capacidad estimada para diccionarios si vienen null
            var estimatedPropertiesCapacity = properties?.Count ?? 4; // Estimación conservadora
            var estimatedContextCapacity = context?.Count ?? 2; // Estimación conservadora
            
            var logEntry = new StructuredLogEntry
            {
                Message = message ?? string.Empty,
                LogLevel = logLevel.Value,
                Operation = operation ?? string.Empty,
                Category = category?.Value ?? LogCategoryValue.General.Value,
                EventType = eventType?.Value,
                Properties = properties ?? new Dictionary<string, object>(estimatedPropertiesCapacity),
                Context = context ?? new Dictionary<string, object>(estimatedContextCapacity),
                Exception = exception,
                Timestamp = DateTime.UtcNow
            };

            if (exception != null)
            {
                logEntry.StackTrace = exception.StackTrace;
            }

            return logEntry;
        }
    }
}

