using System.Threading.Channels;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Servicio en background que procesa logs de la cola de forma eficiente
    /// Elimina overhead de Task.Run y proporciona mejor control de recursos
    /// </summary>
    public class LogProcessingBackgroundService : BackgroundService
    {
        private readonly ILogger<LogProcessingBackgroundService> _logger;
        private readonly LogQueue _logQueue;
        private readonly SendLogUseCase _sendLogUseCase;
        private readonly int _batchSize;
        private readonly TimeSpan _batchDelay;

        public LogProcessingBackgroundService(
            ILogger<LogProcessingBackgroundService> logger,
            LogQueue logQueue,
            SendLogUseCase sendLogUseCase)
        {
            _logger = logger;
            _logQueue = logQueue;
            _sendLogUseCase = sendLogUseCase;
            _batchSize = 100; // Procesar en lotes para mejor throughput
            _batchDelay = TimeSpan.FromMilliseconds(100); // Delay entre lotes
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = _logQueue.Reader;
            var batch = new List<StructuredLogEntry>(_batchSize);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Recopilar logs en batch
                    while (batch.Count < _batchSize && await reader.WaitToReadAsync(stoppingToken))
                    {
                        while (batch.Count < _batchSize && reader.TryRead(out var logEntry))
                        {
                            batch.Add(logEntry);
                        }
                    }

                    // Procesar batch
                    if (batch.Count > 0)
                    {
                        await ProcessBatchAsync(batch, stoppingToken);
                        batch.Clear();
                    }

                    // Pequeño delay para evitar CPU spinning cuando no hay logs
                    if (batch.Count == 0)
                    {
                        await Task.Delay(_batchDelay, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Shutdown graceful
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en procesamiento de logs en background");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }

            // Procesar logs restantes al cerrar
            if (batch.Count > 0)
            {
                await ProcessBatchAsync(batch, CancellationToken.None);
            }
        }

        private async Task ProcessBatchAsync(List<StructuredLogEntry> batch, CancellationToken cancellationToken)
        {
            // Procesar en paralelo para mejor throughput
            // Usar Parallel.ForEachAsync para mejor control de concurrencia y menos overhead
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(batch, parallelOptions, async (logEntry, ct) =>
            {
                try
                {
                    await _sendLogUseCase.ExecuteAsync(logEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar log en batch");
                }
            });
        }
    }
}

