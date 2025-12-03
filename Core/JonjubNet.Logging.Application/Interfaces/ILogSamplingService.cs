using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Servicio para determinar si un log debe ser muestreado (sampling) o limitado por rate
    /// </summary>
    public interface ILogSamplingService
    {
        /// <summary>
        /// Determina si un log debe ser registrado basado en sampling y rate limiting
        /// </summary>
        /// <param name="logEntry">Entrada de log a evaluar</param>
        /// <returns>True si el log debe ser registrado, False si debe ser descartado</returns>
        bool ShouldLog(StructuredLogEntry logEntry);
    }
}
