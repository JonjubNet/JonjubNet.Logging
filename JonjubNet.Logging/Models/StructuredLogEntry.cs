using System.Text.Json;

namespace JonjubNet.Logging.Models
{
    /// <summary>
    /// Entrada de log estructurado
    /// </summary>
    public class StructuredLogEntry
    {
        /// <summary>
        /// Nombre del servicio
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Operación que se está ejecutando
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Nivel del log
        /// </summary>
        public string LogLevel { get; set; } = string.Empty;

        /// <summary>
        /// Mensaje del log
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Categoría del log
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de evento (OperationStart, OperationEnd, UserAction, SecurityEvent, AuditEvent, etc.)
        /// </summary>
        public string? EventType { get; set; }

        /// <summary>
        /// ID del usuario
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del usuario
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Entorno de ejecución
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Versión del servicio
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de la máquina
        /// </summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>
        /// ID del proceso
        /// </summary>
        public string ProcessId { get; set; } = string.Empty;

        /// <summary>
        /// ID del hilo
        /// </summary>
        public string ThreadId { get; set; } = string.Empty;

        /// <summary>
        /// Propiedades adicionales
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// Contexto adicional
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>
        /// Excepción asociada
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Stack trace de la excepción
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Timestamp del log
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Ruta de la petición HTTP
        /// </summary>
        public string? RequestPath { get; set; }

        /// <summary>
        /// Método HTTP
        /// </summary>
        public string? RequestMethod { get; set; }

        /// <summary>
        /// Código de estado HTTP
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// IP del cliente
        /// </summary>
        public string? ClientIp { get; set; }

        /// <summary>
        /// User Agent del cliente
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// ID de correlación
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// ID de la petición
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// ID de la sesión
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Convierte la entrada de log a JSON
        /// </summary>
        /// <returns>JSON string válido y bien formateado</returns>
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                // Nota: No omitimos nulls para mantener estructura consistente
                // Esto facilita queries y análisis en sistemas como Elasticsearch
            };

            // Crear un objeto anónimo con orden lógico
            // System.Text.Json garantiza JSON válido sin comas finales
            // Los campos null se incluyen explícitamente para mantener estructura consistente
            var logObject = new
            {
                // Identificación y contexto básico
                ServiceName,
                Operation,
                LogLevel,
                Message,
                Category,
                EventType,
                
                // Información de usuario
                UserId,
                UserName,
                
                // Información del sistema
                Environment,
                Version,
                MachineName,
                ProcessId,
                ThreadId,
                
                // Datos específicos del evento (siempre incluidos, incluso si están vacíos)
                Properties = Properties.Count > 0 ? Properties : new Dictionary<string, object>(),
                Context = Context.Count > 0 ? Context : new Dictionary<string, object>(),
                
                // Información de excepción
                Exception = Exception?.ToString(),
                StackTrace,
                
                // Timestamp
                Timestamp,
                
                // Información HTTP
                RequestPath,
                RequestMethod,
                StatusCode,
                ClientIp,
                UserAgent,
                
                // IDs de correlación
                CorrelationId,
                RequestId,
                SessionId
            };

            return JsonSerializer.Serialize(logObject, options);
        }
    }

    /// <summary>
    /// Niveles de log disponibles
    /// </summary>
    public static class LogLevel
    {
        public const string Trace = "Trace";
        public const string Debug = "Debug";
        public const string Information = "Information";
        public const string Warning = "Warning";
        public const string Error = "Error";
        public const string Critical = "Critical";
        public const string Fatal = "Fatal";
    }

    /// <summary>
    /// Categorías de log predefinidas
    /// </summary>
    public static class LogCategory
    {
        public const string General = "General";
        public const string Security = "Security";
        public const string Audit = "Audit";
        public const string Performance = "Performance";
        public const string UserAction = "UserAction";
        public const string System = "System";
        public const string Business = "Business";
        public const string Integration = "Integration";
        public const string Database = "Database";
        public const string External = "External";
        public const string BusinessLogic = "BusinessLogic";
    }

    /// <summary>
    /// Tipos de eventos predefinidos
    /// </summary>
    public static class EventType
    {
        public const string OperationStart = "OperationStart";
        public const string OperationEnd = "OperationEnd";
        public const string UserAction = "UserAction";
        public const string SecurityEvent = "SecurityEvent";
        public const string AuditEvent = "AuditEvent";
        public const string Custom = "Custom";
    }
}
