using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Servicio de batching inteligente que agrupa logs por tiempo y volumen
    /// </summary>
    public class IntelligentBatchingService : IIntelligentBatchingService
    {
        private readonly LoggingBatchingConfiguration _config;
        private readonly ILogger<IntelligentBatchingService>? _logger;

        public IntelligentBatchingService(
            ILoggingConfigurationManager configurationManager,
            ILogger<IntelligentBatchingService>? logger = null)
        {
            _config = configurationManager.Current.Batching;
            _logger = logger;
        }

        public async Task<List<LogBatch>> CreateBatchesAsync(
            IEnumerable<StructuredLogEntry> logEntries,
            string sinkName,
            CancellationToken cancellationToken = default)
        {
            var batches = new List<LogBatch>();
            
            if (!_config.Enabled)
            {
                // Si batching está deshabilitado, crear un batch por log
                // OPTIMIZACIÓN: Eliminar Select().ToList() - usar foreach directo
                foreach (var log in logEntries)
                {
                    batches.Add(new LogBatch
                    {
                        LogEntries = new List<StructuredLogEntry> { log },
                        SinkName = sinkName
                    });
                }
                return batches;
            }
            var batchSize = GetOptimalBatchSize(sinkName);
            var maxInterval = GetMaxBatchInterval(sinkName);
            var currentBatch = new List<StructuredLogEntry>();
            var batchStartTime = DateTime.UtcNow;

            foreach (var logEntry in logEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Si el batch está lleno o ha pasado el intervalo máximo, crear nuevo batch
                if (currentBatch.Count >= batchSize || 
                    (currentBatch.Count > 0 && DateTime.UtcNow - batchStartTime >= maxInterval))
                {
                    if (currentBatch.Count > 0)
                    {
                        batches.Add(new LogBatch
                        {
                            LogEntries = new List<StructuredLogEntry>(currentBatch),
                            SinkName = sinkName
                        });
                        currentBatch.Clear();
                        batchStartTime = DateTime.UtcNow;
                    }
                }

                currentBatch.Add(logEntry);
            }

            // Agregar batch final si tiene elementos
            if (currentBatch.Count > 0)
            {
                batches.Add(new LogBatch
                {
                    LogEntries = new List<StructuredLogEntry>(currentBatch),
                    SinkName = sinkName
                });
            }

            _logger?.LogDebug("Creados {BatchCount} batches para sink {SinkName} con {TotalLogs} logs",
                batches.Count, sinkName, logEntries.Count());

            return batches;
        }

        public int GetOptimalBatchSize(string sinkName)
        {
            // Obtener tamaño específico del sink o usar el default
            if (_config.BatchSizeBySink.TryGetValue(sinkName, out var size))
            {
                return size;
            }

            return _config.DefaultBatchSize;
        }

        public TimeSpan GetMaxBatchInterval(string sinkName)
        {
            return TimeSpan.FromMilliseconds(_config.MaxBatchIntervalMs);
        }
    }
}

