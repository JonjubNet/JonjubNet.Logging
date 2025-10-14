namespace JonjubNet.Logging.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de logging estructurado
    /// </summary>
    public interface IStructuredLoggingService
    {
        /// <summary>
        /// Registra un log de información
        /// </summary>
        /// <param name="message">Mensaje del log</param>
        /// <param name="operation">Operación que se está ejecutando</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="context">Contexto adicional</param>
        void LogInformation(string message, string operation = "", string category = "", Dictionary<string, object>? properties = null, Dictionary<string, object>? context = null);

        /// <summary>
        /// Registra un log de advertencia
        /// </summary>
        /// <param name="message">Mensaje del log</param>
        /// <param name="operation">Operación que se está ejecutando</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="context">Contexto adicional</param>
        /// <param name="exception">Excepción asociada</param>
        void LogWarning(string message, string operation = "", string category = "", Dictionary<string, object>? properties = null, Dictionary<string, object>? context = null, Exception? exception = null);

        /// <summary>
        /// Registra un log de error
        /// </summary>
        /// <param name="message">Mensaje del log</param>
        /// <param name="operation">Operación que se está ejecutando</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="context">Contexto adicional</param>
        /// <param name="exception">Excepción asociada</param>
        void LogError(string message, string operation = "", string category = "", Dictionary<string, object>? properties = null, Dictionary<string, object>? context = null, Exception? exception = null);

        /// <summary>
        /// Registra un log crítico
        /// </summary>
        /// <param name="message">Mensaje del log</param>
        /// <param name="operation">Operación que se está ejecutando</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="context">Contexto adicional</param>
        /// <param name="exception">Excepción asociada</param>
        void LogCritical(string message, string operation = "", string category = "", Dictionary<string, object>? properties = null, Dictionary<string, object>? context = null, Exception? exception = null);

        /// <summary>
        /// Registra un log de debug
        /// </summary>
        /// <param name="message">Mensaje del log</param>
        /// <param name="operation">Operación que se está ejecutando</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="context">Contexto adicional</param>
        void LogDebug(string message, string operation = "", string category = "", Dictionary<string, object>? properties = null, Dictionary<string, object>? context = null);

        /// <summary>
        /// Registra un log de trace
        /// </summary>
        /// <param name="message">Mensaje del log</param>
        /// <param name="operation">Operación que se está ejecutando</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="context">Contexto adicional</param>
        void LogTrace(string message, string operation = "", string category = "", Dictionary<string, object>? properties = null, Dictionary<string, object>? context = null);

        /// <summary>
        /// Registra una entrada de log personalizada
        /// </summary>
        /// <param name="logEntry">Entrada de log estructurado</param>
        void LogCustom(Models.StructuredLogEntry logEntry);

        /// <summary>
        /// Registra el inicio de una operación
        /// </summary>
        /// <param name="operation">Nombre de la operación</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        void LogOperationStart(string operation, string category = "", Dictionary<string, object>? properties = null);

        /// <summary>
        /// Registra el final de una operación
        /// </summary>
        /// <param name="operation">Nombre de la operación</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="executionTimeMs">Tiempo de ejecución en milisegundos</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="success">Indica si la operación fue exitosa</param>
        /// <param name="exception">Excepción si la operación falló</param>
        void LogOperationEnd(string operation, string category = "", long executionTimeMs = 0, Dictionary<string, object>? properties = null, bool success = true, Exception? exception = null);

        /// <summary>
        /// Registra una acción del usuario
        /// </summary>
        /// <param name="action">Acción realizada</param>
        /// <param name="entityType">Tipo de entidad afectada</param>
        /// <param name="entityId">ID de la entidad afectada</param>
        /// <param name="properties">Propiedades adicionales</param>
        void LogUserAction(string action, string entityType = "", string entityId = "", Dictionary<string, object>? properties = null);

        /// <summary>
        /// Registra un evento de seguridad
        /// </summary>
        /// <param name="eventType">Tipo de evento de seguridad</param>
        /// <param name="description">Descripción del evento</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="exception">Excepción asociada</param>
        void LogSecurityEvent(string eventType, string description, Dictionary<string, object>? properties = null, Exception? exception = null);

        /// <summary>
        /// Registra un evento de auditoría
        /// </summary>
        /// <param name="eventType">Tipo de evento de auditoría</param>
        /// <param name="description">Descripción del evento</param>
        /// <param name="entityType">Tipo de entidad afectada</param>
        /// <param name="entityId">ID de la entidad afectada</param>
        /// <param name="properties">Propiedades adicionales</param>
        void LogAuditEvent(string eventType, string description, string entityType = "", string entityId = "", Dictionary<string, object>? properties = null);
    }
}
