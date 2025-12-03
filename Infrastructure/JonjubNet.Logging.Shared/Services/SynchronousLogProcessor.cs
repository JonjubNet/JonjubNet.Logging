using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Procesador síncrono de logs para aplicaciones sin BackgroundService
    /// Procesa logs inmediatamente cuando se agregan a la cola
    /// </summary>
    public class SynchronousLogProcessor
    {
        private readonly ILogger<SynchronousLogProcessor> _logger;
        private readonly LogQueue _logQueue;
        private readonly SendLogUseCase _sendLogUseCase;
        private readonly EnrichLogEntryUseCase _enrichLogEntryUseCase;
        private readonly Task _processingTask;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public SynchronousLogProcessor(
            ILogger<SynchronousLogProcessor> logger,
            LogQueue logQueue,
            SendLogUseCase sendLogUseCase,
            EnrichLogEntryUseCase enrichLogEntryUseCase)
        {
            _logger = logger;
            _logQueue = logQueue;
            _sendLogUseCase = sendLogUseCase;
            _enrichLogEntryUseCase = enrichLogEntryUseCase;
            _cancellationTokenSource = new CancellationTokenSource();

            // Iniciar procesamiento en background thread
            _processingTask = Task.Run(() => ProcessLogsAsync(_cancellationTokenSource.Token));
        }

        private async Task ProcessLogsAsync(CancellationToken cancellationToken)
        {
            var reader = _logQueue.Reader;
            var batch = new List<StructuredLogEntry>(100);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Recopilar logs en batch
                    while (batch.Count < 100 && await reader.WaitToReadAsync(cancellationToken))
                    {
                        while (batch.Count < 100 && reader.TryRead(out var logEntry))
                        {
                            batch.Add(logEntry);
                        }
                    }

                    // Procesar batch
                    if (batch.Count > 0)
                    {
                        await ProcessBatchAsync(batch);
                        batch.Clear();
                    }
                    else
                    {
                        // Si no hay logs, esperar un poco antes de revisar de nuevo
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Cancelación normal
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en procesamiento síncrono de logs");
                    await Task.Delay(1000, cancellationToken); // Esperar antes de reintentar
                }
            }

            // Procesar logs restantes al cancelar
            if (batch.Count > 0)
            {
                await ProcessBatchAsync(batch);
            }
        }

        private async Task ProcessBatchAsync(List<StructuredLogEntry> batch)
        {
            try
            {
                // OPTIMIZACIÓN: Completar enriquecimiento antes de enviar (en background)
                var enrichedEntries = batch.Select(logEntry =>
                {
                    // Si necesita enriquecimiento completo, completarlo ahora
                    if (logEntry.Properties.ContainsKey("_NeedsFullEnrichment"))
                    {
                        return _enrichLogEntryUseCase.CompleteEnrichment(logEntry);
                    }
                    return logEntry;
                }).ToList();

                // Procesar en paralelo para mejor throughput
                var tasks = enrichedEntries.Select(logEntry => _sendLogUseCase.ExecuteAsync(logEntry));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar batch de logs");
            }
        }

        /// <summary>
        /// Detiene el procesamiento de logs
        /// </summary>
        public async Task StopAsync()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                await _processingTask;
            }
            catch (OperationCanceledException)
            {
                // Esperado
            }
            _cancellationTokenSource.Dispose();
        }
    }
}

