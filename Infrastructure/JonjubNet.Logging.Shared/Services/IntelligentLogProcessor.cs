using System.Threading.Channels;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Procesador inteligente de logs con batching, compresión y priorización
    /// </summary>
    public class IntelligentLogProcessor : BackgroundService
    {
        private readonly ILogger<IntelligentLogProcessor> _logger;
        private readonly IPriorityLogQueue? _priorityQueue;
        private readonly LogQueue? _standardQueue;
        private readonly SendLogUseCase _sendLogUseCase;
        private readonly EnrichLogEntryUseCase _enrichLogEntryUseCase;
        private readonly IIntelligentBatchingService _batchingService;
        private readonly IBatchCompressionService _compressionService;
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly Dictionary<string, Task> _priorityProcessingTasks = new();

        public IntelligentLogProcessor(
            ILogger<IntelligentLogProcessor> logger,
            SendLogUseCase sendLogUseCase,
            EnrichLogEntryUseCase enrichLogEntryUseCase,
            IIntelligentBatchingService batchingService,
            IBatchCompressionService compressionService,
            ILoggingConfigurationManager configurationManager,
            IPriorityLogQueue? priorityQueue = null,
            LogQueue? standardQueue = null)
        {
            _logger = logger;
            _sendLogUseCase = sendLogUseCase;
            _enrichLogEntryUseCase = enrichLogEntryUseCase;
            _batchingService = batchingService;
            _compressionService = compressionService;
            _configurationManager = configurationManager;
            _priorityQueue = priorityQueue;
            _standardQueue = standardQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = _configurationManager.Current.Batching;

            if (config.EnablePriorityQueues && _priorityQueue != null)
            {
                // Procesar colas por prioridad
                await ProcessPriorityQueuesAsync(stoppingToken);
            }
            else if (_standardQueue != null)
            {
                // Procesar cola estándar con batching inteligente
                await ProcessStandardQueueAsync(stoppingToken);
            }
        }

        private async Task ProcessPriorityQueuesAsync(CancellationToken stoppingToken)
        {
            var config = _configurationManager.Current.Batching;
            // OPTIMIZACIÓN: Evitar ToList() innecesario - iterar directamente
            var priorities = _priorityQueue!.GetPriorities();

            // Procesar colas de mayor a menor prioridad
            foreach (var priority in priorities)
            {
                var interval = priority == "Critical" || priority == "Error"
                    ? TimeSpan.FromMilliseconds(config.CriticalProcessingIntervalMs)
                    : TimeSpan.FromMilliseconds(config.NormalProcessingIntervalMs);

                var task = Task.Run(async () =>
                {
                    await ProcessPriorityQueueAsync(priority, interval, stoppingToken);
                }, stoppingToken);

                _priorityProcessingTasks[priority] = task;
            }

            // Esperar a que todas las tareas terminen
            await Task.WhenAll(_priorityProcessingTasks.Values);
        }

        private async Task ProcessPriorityQueueAsync(
            string priority,
            TimeSpan interval,
            CancellationToken stoppingToken)
        {
            var reader = _priorityQueue!.GetReader(priority);
            var batch = new List<StructuredLogEntry>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Recopilar logs de esta prioridad
                    while (await reader.WaitToReadAsync(stoppingToken))
                    {
                        while (reader.TryRead(out var logEntry))
                        {
                            batch.Add(logEntry);
                        }
                    }

                    // Procesar batch si tiene elementos
                    if (batch.Count > 0)
                    {
                        await ProcessBatchAsync(batch, stoppingToken);
                        batch.Clear();
                    }

                    // Delay según prioridad
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando cola de prioridad {Priority}", priority);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }

            // Procesar logs restantes
            if (batch.Count > 0)
            {
                await ProcessBatchAsync(batch, CancellationToken.None);
            }
        }

        private async Task ProcessStandardQueueAsync(CancellationToken stoppingToken)
        {
            // 🔍 LOGGING TEMPORAL DE DIAGNÓSTICO
            _logger.LogInformation("🔵 [DIAG] IntelligentLogProcessor.ProcessStandardQueueAsync() iniciado");

            var reader = _standardQueue!.Reader;
            var batch = new List<StructuredLogEntry>();
            var config = _configurationManager.Current.Batching;
            var maxInterval = TimeSpan.FromMilliseconds(config.MaxBatchIntervalMs);
            var batchStartTime = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Recopilar logs
                    while (await reader.WaitToReadAsync(stoppingToken))
                    {
                        _logger.LogInformation("🔵 [DIAG] IntelligentLogProcessor: Esperando logs en cola...");
                        while (reader.TryRead(out var logEntry))
                        {
                            _logger.LogInformation("✅ [DIAG] IntelligentLogProcessor: Log leído de cola - Message: {Message}, Timestamp: {Timestamp}", 
                                logEntry.Message, logEntry.Timestamp);
                            batch.Add(logEntry);

                            // Si el batch está lleno o ha pasado el intervalo, procesar
                            if (batch.Count >= config.DefaultBatchSize ||
                                (batch.Count > 0 && DateTime.UtcNow - batchStartTime >= maxInterval))
                            {
                                _logger.LogInformation("🔵 [DIAG] IntelligentLogProcessor: Procesando batch de {Count} logs", batch.Count);
                                await ProcessBatchAsync(batch, stoppingToken);
                                batch.Clear();
                                batchStartTime = DateTime.UtcNow;
                            }
                        }
                    }

                    // Procesar batch final si tiene elementos
                    if (batch.Count > 0 && DateTime.UtcNow - batchStartTime >= maxInterval)
                    {
                        await ProcessBatchAsync(batch, stoppingToken);
                        batch.Clear();
                        batchStartTime = DateTime.UtcNow;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando cola estándar");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }

            // Procesar logs restantes
            if (batch.Count > 0)
            {
                await ProcessBatchAsync(batch, CancellationToken.None);
            }
        }

        private async Task ProcessBatchAsync(List<StructuredLogEntry> batch, CancellationToken cancellationToken)
        {
            if (batch.Count == 0)
                return;

            try
            {
                // OPTIMIZACIÓN: Completar enriquecimiento antes de procesar batches (en background)
                // Eliminar LINQ Select().ToList() - usar foreach directo para reducir allocations
                var enrichedBatch = new List<StructuredLogEntry>(batch.Count);
                foreach (var logEntry in batch)
                {
                    // Si necesita enriquecimiento completo, completarlo ahora
                    if (logEntry.Properties.ContainsKey("_NeedsFullEnrichment"))
                    {
                        enrichedBatch.Add(_enrichLogEntryUseCase.CompleteEnrichment(logEntry));
                    }
                    else
                    {
                        enrichedBatch.Add(logEntry);
                    }
                }

                // OPTIMIZACIÓN: Agrupar logs por sink sin LINQ GroupBy().ToList()
                // Usar Dictionary para agrupación más eficiente
                var logsBySink = new Dictionary<string, List<StructuredLogEntry>>();
                foreach (var log in enrichedBatch)
                {
                    var sinkName = GetSinkName(log);
                    if (!logsBySink.TryGetValue(sinkName, out var sinkLogs))
                    {
                        sinkLogs = new List<StructuredLogEntry>();
                        logsBySink[sinkName] = sinkLogs;
                    }
                    sinkLogs.Add(log);
                }

                var tasks = new List<Task>(logsBySink.Count);
                foreach (var kvp in logsBySink)
                {
                    var sinkName = kvp.Key;
                    var logs = kvp.Value;

                    // Crear tarea para procesar este sink
                    var task = ProcessSinkBatchAsync(sinkName, logs, cancellationToken);
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando batch de {Count} logs", batch.Count);
            }
        }

        private async Task ProcessSinkBatchAsync(string sinkName, List<StructuredLogEntry> logs, CancellationToken cancellationToken)
        {
            // Crear batches inteligentes
            var batches = await _batchingService.CreateBatchesAsync(logs, sinkName, cancellationToken);

            // Procesar cada batch
            foreach (var logBatch in batches)
            {
                if (_compressionService.IsCompressionEnabled)
                {
                    // Comprimir batch
                    var compressedBatch = await _compressionService.CompressAsync(logBatch, cancellationToken);
                    
                    // Descomprimir y procesar
                    var decompressedBatch = await _compressionService.DecompressAsync(compressedBatch, cancellationToken);
                    
                    // Enviar logs del batch
                    await SendBatchAsync(decompressedBatch.LogEntries, cancellationToken);
                }
                else
                {
                    // Enviar logs del batch sin compresión
                    await SendBatchAsync(logBatch.LogEntries, cancellationToken);
                }
            }
        }

        private async Task SendBatchAsync(List<StructuredLogEntry> logEntries, CancellationToken cancellationToken)
        {
            // Los logs ya están completamente enriquecidos en ProcessBatchAsync
            // Solo enviar a sinks

            // Procesar logs en paralelo
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(logEntries, parallelOptions, async (logEntry, ct) =>
            {
                try
                {
                    await _sendLogUseCase.ExecuteAsync(logEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando log en batch");
                }
            });
        }

        private string GetSinkName(StructuredLogEntry logEntry)
        {
            // Determinar sink basado en configuración o categoría
            // Por ahora, usar "Default" como sink genérico
            return "Default";
        }
    }
}

