using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para enviar logs a diferentes destinos (sinks)
    /// Abstrae la implementación específica de cada sink
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// Indica si el sink está habilitado
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Nombre del sink para identificación
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Envía una entrada de log al sink
        /// </summary>
        /// <param name="logEntry">Entrada de log estructurado</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        Task SendAsync(StructuredLogEntry logEntry);
    }
}

