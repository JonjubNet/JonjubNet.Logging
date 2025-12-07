using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Application.Interfaces
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
        void LogInformation(string message, string operation = "", string category = "", Dictionary<string, object>? properties = default, Dictionary<string, object>? context = default);

        /// <summary>
        /// Registra un log de advertencia
        /// </summary>
        /// <param name="message">Mensaje del log</param>
        /// <param name="operation">Operación que se está ejecutando</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="context">Contexto adicional</param>
        /// <param name="exception">Excepción asociada</param>
        void LogWarning(string message, string operation = "", string category = "", Dictionary<string, object>? properties = default, Dictionary<string, object>? context = default, Exception? exception = null);

        /// <summary>
        /// Registra un log de error
        /// </summary>
        /// <param name="message">Mensaje del log</param>
        /// <param name="operation">Operación que se está ejecutando</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="context">Contexto adicional</param>
        /// <param name="exception">Excepción asociada</param>
        void LogError(string message, string operation = "", string category = "", Dictionary<string, object>? properties = default, Dictionary<string, object>? context = default, Exception? exception = null);

        /// <summary>
        /// Registra un log crítico
        /// </summary>
        /// <param name="message">Mensaje del log</param>
        /// <param name="operation">Operación que se está ejecutando</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="context">Contexto adicional</param>
        /// <param name="exception">Excepción asociada</param>
        void LogCritical(string message, string operation = "", string category = "", Dictionary<string, object>? properties = default, Dictionary<string, object>? context = default, Exception? exception = null);

        /// <summary>
        /// Registra un log de debug
        /// </summary>
        /// <param name="message">Mensaje del log</param>
        /// <param name="operation">Operación que se está ejecutando</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="context">Contexto adicional</param>
        void LogDebug(string message, string operation = "", string category = "", Dictionary<string, object>? properties = default, Dictionary<string, object>? context = default);

        /// <summary>
        /// Registra un log de trace
        /// </summary>
        /// <param name="message">Mensaje del log</param>
        /// <param name="operation">Operación que se está ejecutando</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="context">Contexto adicional</param>
        void LogTrace(string message, string operation = "", string category = "", Dictionary<string, object>? properties = default, Dictionary<string, object>? context = default);

        /// <summary>
        /// Registra una entrada de log personalizada
        /// </summary>
        /// <param name="logEntry">Entrada de log estructurado</param>
        void LogCustom(StructuredLogEntry logEntry);

        /// <summary>
        /// Crea un scope de logging que agrega propiedades a todos los logs dentro del scope
        /// </summary>
        /// <param name="properties">Propiedades que se agregarán a todos los logs dentro del scope</param>
        /// <returns>Scope de logging que debe ser descartado cuando termine</returns>
        ILogScope BeginScope(Dictionary<string, object> properties);

        /// <summary>
        /// Crea un scope de logging con una sola propiedad
        /// </summary>
        /// <param name="key">Clave de la propiedad</param>
        /// <param name="value">Valor de la propiedad</param>
        /// <returns>Scope de logging que debe ser descartado cuando termine</returns>
        ILogScope BeginScope(string key, object value);

        /// <summary>
        /// Registra el inicio de una operación
        /// </summary>
        /// <param name="operation">Nombre de la operación</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="properties">Propiedades adicionales</param>
        void LogOperationStart(string operation, string category = "", Dictionary<string, object>? properties = default);

        /// <summary>
        /// Registra el final de una operación
        /// </summary>
        /// <param name="operation">Nombre de la operación</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="executionTimeMs">Tiempo de ejecución en milisegundos</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="success">Indica si la operación fue exitosa</param>
        /// <param name="exception">Excepción si la operación falló</param>
        void LogOperationEnd(string operation, string category = "", long executionTimeMs = 0, Dictionary<string, object>? properties = default, bool success = true, Exception? exception = null);

        /// <summary>
        /// Registra una acción del usuario
        /// </summary>
        /// <param name="action">Acción realizada</param>
        /// <param name="entityType">Tipo de entidad afectada</param>
        /// <param name="entityId">ID de la entidad afectada</param>
        /// <param name="properties">Propiedades adicionales</param>
        void LogUserAction(string action, string entityType = "", string entityId = "", Dictionary<string, object>? properties = default);

        /// <summary>
        /// Registra un evento de seguridad
        /// </summary>
        /// <param name="eventType">Tipo de evento de seguridad</param>
        /// <param name="description">Descripción del evento</param>
        /// <param name="properties">Propiedades adicionales</param>
        /// <param name="exception">Excepción asociada</param>
        void LogSecurityEvent(string eventType, string description, Dictionary<string, object>? properties = default, Exception? exception = null);

        /// <summary>
        /// Registra un evento de auditoría
        /// </summary>
        /// <param name="eventType">Tipo de evento de auditoría</param>
        /// <param name="description">Descripción del evento</param>
        /// <param name="entityType">Tipo de entidad afectada</param>
        /// <param name="entityId">ID de la entidad afectada</param>
        /// <param name="properties">Propiedades adicionales</param>
        void LogAuditEvent(string eventType, string description, string entityType = "", string entityId = "", Dictionary<string, object>? properties = default);
    }
}

