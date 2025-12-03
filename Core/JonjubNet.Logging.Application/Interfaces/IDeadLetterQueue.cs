using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para Dead Letter Queue (cola de logs fallidos)
    /// </summary>
    public interface IDeadLetterQueue
    {
        /// <summary>
        /// Agrega un log fallido a la cola
        /// </summary>
        Task EnqueueAsync(
            StructuredLogEntry logEntry,
            string sinkName,
            string failureReason,
            Exception? exception = null);

        /// <summary>
        /// Obtiene logs fallidos
        /// </summary>
        Task<IEnumerable<DeadLetterQueueItem>> GetFailedLogsAsync(
            int maxCount = 100,
            DateTime? since = null);

        /// <summary>
        /// Reintenta enviar un log específico
        /// </summary>
        Task<bool> RetryAsync(Guid itemId);

        /// <summary>
        /// Reintenta enviar todos los logs (opcionalmente filtrado por sink)
        /// </summary>
        Task<bool> RetryAllAsync(string? sinkName = null);

        /// <summary>
        /// Elimina un log de la cola
        /// </summary>
        Task<bool> DeleteAsync(Guid itemId);

        /// <summary>
        /// Obtiene el conteo de logs en la cola
        /// </summary>
        Task<int> GetCountAsync(string? sinkName = null);

        /// <summary>
        /// Obtiene métricas de la cola
        /// </summary>
        DeadLetterQueueMetrics GetMetrics();
    }

    /// <summary>
    /// Item en la Dead Letter Queue
    /// </summary>
    public class DeadLetterQueueItem
    {
        public Guid Id { get; set; }
        public StructuredLogEntry LogEntry { get; set; } = null!;
        public string SinkName { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public DateTime EnqueuedAt { get; set; }
        public int RetryCount { get; set; }
        public DateTime? LastRetryAt { get; set; }
    }

    /// <summary>
    /// Métricas de la Dead Letter Queue
    /// </summary>
    public class DeadLetterQueueMetrics
    {
        public int TotalItems { get; set; }
        public int ItemsBySink { get; set; }
        public DateTime? OldestItemDate { get; set; }
        public DateTime? NewestItemDate { get; set; }
        public Dictionary<string, int> ItemsBySinkName { get; set; } = new();
    }

    /// <summary>
    /// Tipo de almacenamiento para DLQ
    /// </summary>
    public enum DeadLetterQueueStorage
    {
        /// <summary>
        /// Solo en memoria (se pierde al reiniciar)
        /// </summary>
        InMemory,

        /// <summary>
        /// Persistido en archivo
        /// </summary>
        File,

        /// <summary>
        /// Persistido en base de datos (futuro)
        /// </summary>
        Database
    }
}

