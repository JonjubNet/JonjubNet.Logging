using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para servicio de batching inteligente
    /// </summary>
    public interface IIntelligentBatchingService
    {
        /// <summary>
        /// Agrupa logs en batches según tiempo y volumen
        /// </summary>
        Task<List<LogBatch>> CreateBatchesAsync(
            IEnumerable<StructuredLogEntry> logEntries,
            string sinkName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene el tamaño de batch óptimo para un sink específico
        /// </summary>
        int GetOptimalBatchSize(string sinkName);

        /// <summary>
        /// Obtiene el intervalo máximo de batch para un sink específico
        /// </summary>
        TimeSpan GetMaxBatchInterval(string sinkName);
    }

    /// <summary>
    /// Batch de logs agrupados
    /// </summary>
    public class LogBatch
    {
        public List<StructuredLogEntry> LogEntries { get; set; } = new();
        public string SinkName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Size => LogEntries.Count;
    }
}

