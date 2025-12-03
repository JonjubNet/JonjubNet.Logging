using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Filtro para determinar si un log debe ser enviado a los sinks
    /// </summary>
    public interface ILogFilter
    {
        /// <summary>
        /// Determina si un log debe ser enviado a los sinks
        /// </summary>
        /// <param name="logEntry">Entrada de log a evaluar</param>
        /// <returns>True si el log debe ser enviado, False si debe ser filtrado</returns>
        bool ShouldLog(StructuredLogEntry logEntry);
    }
}
