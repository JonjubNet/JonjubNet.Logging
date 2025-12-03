using JonjubNet.Logging.Domain.Entities;
using System.Threading.Channels;

namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para cola de logs con priorización
    /// </summary>
    public interface IPriorityLogQueue
    {
        /// <summary>
        /// Intenta agregar un log a la cola según su prioridad
        /// </summary>
        bool TryEnqueue(StructuredLogEntry logEntry);

        /// <summary>
        /// Obtiene el reader de una cola de prioridad específica
        /// </summary>
        ChannelReader<StructuredLogEntry> GetReader(string priority);

        /// <summary>
        /// Obtiene todas las prioridades disponibles ordenadas de mayor a menor
        /// </summary>
        IEnumerable<string> GetPriorities();

        /// <summary>
        /// Obtiene el conteo de logs en una cola de prioridad específica
        /// </summary>
        int GetCount(string priority);

        /// <summary>
        /// Obtiene la capacidad de una cola de prioridad específica
        /// </summary>
        int GetCapacity(string priority);
    }
}

