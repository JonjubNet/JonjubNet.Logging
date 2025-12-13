using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para servicio de audit logging
    /// </summary>
    public interface IAuditLoggingService
    {
        /// <summary>
        /// Registra un cambio de configuración
        /// </summary>
        /// <param name="changeType">Tipo de cambio (Created, Updated, Deleted)</param>
        /// <param name="configurationSection">Sección de configuración afectada</param>
        /// <param name="oldValue">Valor anterior (opcional)</param>
        /// <param name="newValue">Valor nuevo (opcional)</param>
        /// <param name="changedBy">Usuario que realizó el cambio (opcional)</param>
        Task LogConfigurationChangeAsync(
            string changeType,
            string configurationSection,
            object? oldValue = null,
            object? newValue = null,
            string? changedBy = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra un acceso a logs sensibles
        /// </summary>
        /// <param name="logEntry">Entrada de log accedida</param>
        /// <param name="accessedBy">Usuario que accedió</param>
        /// <param name="accessMethod">Método de acceso (Query, Export, View, etc.)</param>
        Task LogSensitiveAccessAsync(
            StructuredLogEntry logEntry,
            string? accessedBy = null,
            string accessMethod = "View",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra un evento de cumplimiento (compliance)
        /// </summary>
        /// <param name="standard">Estándar de cumplimiento (GDPR, HIPAA, PCI-DSS, etc.)</param>
        /// <param name="eventType">Tipo de evento</param>
        /// <param name="description">Descripción del evento</param>
        /// <param name="metadata">Metadatos adicionales</param>
        Task LogComplianceEventAsync(
            string standard,
            string eventType,
            string description,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si una entrada de log es sensible y debe ser auditada
        /// </summary>
        /// <param name="logEntry">Entrada de log a verificar</param>
        /// <returns>True si es sensible y debe ser auditada</returns>
        bool IsSensitive(StructuredLogEntry logEntry);
    }
}

