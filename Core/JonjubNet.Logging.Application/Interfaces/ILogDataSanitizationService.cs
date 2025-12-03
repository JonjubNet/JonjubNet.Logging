using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Servicio para sanitizar datos sensibles en logs (PII, PCI, etc.)
    /// </summary>
    public interface ILogDataSanitizationService
    {
        /// <summary>
        /// Sanitiza una entrada de log enmascarando datos sensibles
        /// </summary>
        /// <param name="logEntry">Entrada de log a sanitizar</param>
        /// <returns>Entrada de log con datos sensibles enmascarados</returns>
        StructuredLogEntry Sanitize(StructuredLogEntry logEntry);
    }
}

