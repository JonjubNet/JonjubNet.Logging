using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Servicio para sanitizar datos sensibles en logs
    /// </summary>
    public interface IDataSanitizationService
    {
        /// <summary>
        /// Sanitiza una entrada de log enmascarando datos sensibles
        /// </summary>
        /// <param name="logEntry">Entrada de log a sanitizar</param>
        /// <returns>Entrada de log sanitizada</returns>
        StructuredLogEntry Sanitize(StructuredLogEntry logEntry);
    }
}
