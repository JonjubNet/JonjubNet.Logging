using System.Text.Json;
using JonjubNet.Logging.Domain.Common;

namespace JonjubNet.Logging.Domain.Entities
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
        /// Query string de la petición HTTP
        /// </summary>
        public string? QueryString { get; set; }

        /// <summary>
        /// Headers HTTP de la petición (excluyendo headers sensibles como Authorization)
        /// </summary>
        public Dictionary<string, string>? RequestHeaders { get; set; }

        /// <summary>
        /// Headers HTTP de la respuesta
        /// </summary>
        public Dictionary<string, string>? ResponseHeaders { get; set; }

        /// <summary>
        /// Body de la petición HTTP (solo si está habilitado en configuración)
        /// </summary>
        public string? RequestBody { get; set; }

        /// <summary>
        /// Body de la respuesta HTTP (solo si está habilitado en configuración)
        /// </summary>
        public string? ResponseBody { get; set; }

        /// <summary>
        /// Convierte la entrada de log a JSON
        /// Maneja errores de serialización sin afectar la aplicación
        /// </summary>
        /// <returns>JSON string válido y bien formateado</returns>
        public string ToJson()
        {
            try
            {
                // Usar opciones cacheadas para evitar allocations
                var options = JsonSerializerOptionsCache.Default;

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
                    SessionId,
                    
                    // Información HTTP adicional
                    QueryString,
                    RequestHeaders,
                    ResponseHeaders,
                    RequestBody,
                    ResponseBody
                };

                return JsonSerializer.Serialize(logObject, options);
            }
            catch (Exception ex)
            {
                // Error crítico interno del componente al serializar - retornar JSON mínimo
                // Esto nunca debe ocurrir, pero por seguridad retornamos un JSON válido
                return JsonSerializer.Serialize(new
                {
                    ServiceName = ServiceName ?? "Unknown",
                    Operation = Operation ?? "Unknown",
                    LogLevel = LogLevel ?? "Error",
                    Message = Message ?? $"Error interno del componente al serializar log: {ex.Message}",
                    Category = Category ?? "System",
                    EventType = EventType ?? "Custom",
                    Timestamp = Timestamp,
                    ComponentError = ex.Message
                }, JsonSerializerOptionsCache.Default);
            }
        }
    }
}

