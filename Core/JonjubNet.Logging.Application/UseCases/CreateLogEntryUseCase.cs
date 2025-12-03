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
            var logEntry = new StructuredLogEntry
            {
                Message = message ?? string.Empty,
                LogLevel = logLevel.Value,
                Operation = operation ?? string.Empty,
                Category = category?.Value ?? LogCategoryValue.General.Value,
                EventType = eventType?.Value,
                Properties = properties ?? new Dictionary<string, object>(),
                Context = context ?? new Dictionary<string, object>(),
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

