using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para una cola de logs que permite procesamiento asíncrono con backpressure
    /// </summary>
    public interface ILogQueue
    {
        /// <summary>
        /// Intenta agregar un log a la cola de forma síncrona (no bloqueante)
        /// </summary>
        /// <param name="logEntry">Entrada de log a encolar</param>
        /// <returns>True si se agregó exitosamente, False si la cola está llena</returns>
        bool TryEnqueue(StructuredLogEntry logEntry);

        /// <summary>
        /// Obtiene el número de logs en la cola
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Obtiene la capacidad máxima de la cola
        /// </summary>
        int Capacity { get; }
    }
}

