using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Implementación de Dead Letter Queue para almacenar logs fallidos
    /// </summary>
    public class DeadLetterQueueService : IDeadLetterQueue, IDisposable
    {
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly ILogger<DeadLetterQueueService>? _logger;
        private readonly ConcurrentDictionary<Guid, DeadLetterQueueItem> _items = new();
        private readonly Timer? _autoRetryTimer;
        private readonly object _lock = new();
        private bool _disposed = false;

        public DeadLetterQueueService(
            ILoggingConfigurationManager configurationManager,
            ILogger<DeadLetterQueueService>? logger = null)
        {
            _configurationManager = configurationManager;
            _logger = logger;

            var config = _configurationManager.Current.DeadLetterQueue;
            if (config.Enabled && config.AutoRetry)
            {
                // Iniciar timer para auto-retry
                _autoRetryTimer = new Timer(OnAutoRetry, null, config.RetryInterval, config.RetryInterval);
            }

            // Cargar items persistentes si está configurado
            if (config.Storage == "File" && !string.IsNullOrEmpty(config.PersistencePath))
            {
                LoadFromFile();
            }
        }

        public async Task EnqueueAsync(
            StructuredLogEntry logEntry,
            string sinkName,
            string failureReason,
            Exception? exception = null)
        {
            var config = _configurationManager.Current.DeadLetterQueue;
            if (!config.Enabled)
            {
                return;
            }

            lock (_lock)
            {
                // Verificar límite de tamaño
                if (_items.Count >= config.MaxSize)
                {
                    // Eliminar el más antiguo
                    var oldest = _items.Values.OrderBy(i => i.EnqueuedAt).FirstOrDefault();
                    if (oldest != null)
                    {
                        _items.TryRemove(oldest.Id, out _);
                        _logger?.LogWarning("DLQ alcanzó tamaño máximo, eliminando item más antiguo: {ItemId}", oldest.Id);
                    }
                }

                var item = new DeadLetterQueueItem
                {
                    Id = Guid.NewGuid(),
                    LogEntry = logEntry,
                    SinkName = sinkName,
                    FailureReason = failureReason,
                    Exception = exception,
                    EnqueuedAt = DateTime.UtcNow,
                    RetryCount = 0
                };

                _items.TryAdd(item.Id, item);
                _logger?.LogInformation("Log agregado a DLQ: {ItemId}, Sink: {SinkName}, Razón: {FailureReason}", 
                    item.Id, sinkName, failureReason);

                // Persistir si está configurado
                if (config.Storage == "File" && !string.IsNullOrEmpty(config.PersistencePath))
                {
                    _ = Task.Run(() => SaveToFile());
                }
            }

            await Task.CompletedTask;
        }

        public Task<IEnumerable<DeadLetterQueueItem>> GetFailedLogsAsync(
            int maxCount = 100,
            DateTime? since = null)
        {
            var query = _items.Values.AsEnumerable();

            if (since.HasValue)
            {
                query = query.Where(i => i.EnqueuedAt >= since.Value);
            }

            // OPTIMIZACIÓN: Eliminar ToList() - retornar IEnumerable directamente
            var result = query
                .OrderByDescending(i => i.EnqueuedAt)
                .Take(maxCount);

            return Task.FromResult<IEnumerable<DeadLetterQueueItem>>(result);
        }

        public Task<bool> RetryAsync(Guid itemId)
        {
            if (!_items.TryGetValue(itemId, out var item))
            {
                return Task.FromResult(false);
            }

            var config = _configurationManager.Current.DeadLetterQueue;
            if (item.RetryCount >= config.MaxRetriesPerItem)
            {
                _logger?.LogWarning("Item {ItemId} excedió máximo de reintentos ({MaxRetries})", 
                    itemId, config.MaxRetriesPerItem);
                return Task.FromResult(false);
            }

            // Incrementar contador de reintentos
            item.RetryCount++;
            item.LastRetryAt = DateTime.UtcNow;

            _logger?.LogInformation("Reintentando item {ItemId} (intento {RetryCount}/{MaxRetries})", 
                itemId, item.RetryCount, config.MaxRetriesPerItem);

            // Nota: La lógica real de reintento se maneja en SendLogUseCase
            // Este método solo marca el item para reintento
            return Task.FromResult(true);
        }

        public Task<bool> RetryAllAsync(string? sinkName = null)
        {
            var config = _configurationManager.Current.DeadLetterQueue;
            // OPTIMIZACIÓN: Iterar directamente sin ToList() para reducir allocations
            var itemsToRetry = _items.Values
                .Where(i => sinkName == null || i.SinkName == sinkName)
                .Where(i => i.RetryCount < config.MaxRetriesPerItem);

            var itemsList = itemsToRetry.ToList();
            foreach (var item in itemsList)
            {
                item.RetryCount++;
                item.LastRetryAt = DateTime.UtcNow;
            }

            var count = itemsList.Count;
            var sinkNameValue = sinkName ?? "todos";
            _logger?.LogInformation("Reintentando {Count} items de DLQ (sink: {SinkName})", 
                count, sinkNameValue);

            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid itemId)
        {
            var removed = _items.TryRemove(itemId, out _);
            if (removed)
            {
                _logger?.LogInformation("Item {ItemId} eliminado de DLQ", itemId);
            }

            return Task.FromResult(removed);
        }

        public Task<int> GetCountAsync(string? sinkName = null)
        {
            var count = sinkName == null
                ? _items.Count
                : _items.Values.Count(i => i.SinkName == sinkName);

            return Task.FromResult(count);
        }

        public DeadLetterQueueMetrics GetMetrics()
        {
            // OPTIMIZACIÓN: Iterar directamente sin ToList() para reducir allocations
            var items = _items.Values;
            var bySink = new Dictionary<string, int>();
            foreach (var item in items)
            {
                var sinkName = item.SinkName;
                bySink.TryGetValue(sinkName, out var count);
                bySink[sinkName] = count + 1;
            }

            // OPTIMIZACIÓN: Calcular métricas sin ToList() ni LINQ adicional
            var itemsList = items.ToList(); // Solo para Min/Max que requieren materialización
            return new DeadLetterQueueMetrics
            {
                TotalItems = _items.Count,
                ItemsBySink = bySink.Values.Sum(),
                OldestItemDate = itemsList.Count > 0 ? itemsList.Min(i => i.EnqueuedAt) : null,
                NewestItemDate = itemsList.Count > 0 ? itemsList.Max(i => i.EnqueuedAt) : null,
                ItemsBySinkName = bySink
            };
        }

        private void OnAutoRetry(object? state)
        {
            if (_disposed)
                return;

            try
            {
                var config = _configurationManager.Current.DeadLetterQueue;
                if (!config.Enabled || !config.AutoRetry)
                    return;

                // Limpiar items antiguos
                CleanupOldItems();

                // Nota: La lógica de reintento real se maneja en SendLogUseCase
                // Este timer solo limpia items antiguos
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error en auto-retry de DLQ");
            }
        }

        private void CleanupOldItems()
        {
            var config = _configurationManager.Current.DeadLetterQueue;
            var cutoffDate = DateTime.UtcNow - config.ItemRetentionPeriod;

            // OPTIMIZACIÓN: Iterar directamente sin ToList() para reducir allocations
            var oldItems = new List<DeadLetterQueueItem>();
            foreach (var item in _items.Values)
            {
                if (item.EnqueuedAt < cutoffDate)
                {
                    oldItems.Add(item);
                }
            }

            foreach (var item in oldItems)
            {
                _items.TryRemove(item.Id, out _);
            }

            if (oldItems.Any())
            {
                _logger?.LogInformation("Eliminados {Count} items antiguos de DLQ", oldItems.Count);
            }
        }

        private void LoadFromFile()
        {
            try
            {
                var config = _configurationManager.Current.DeadLetterQueue;
                if (string.IsNullOrEmpty(config.PersistencePath) || !File.Exists(config.PersistencePath))
                    return;

                var json = File.ReadAllText(config.PersistencePath);
                var items = JsonSerializer.Deserialize<List<DeadLetterQueueItem>>(json);
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        _items.TryAdd(item.Id, item);
                    }
                    _logger?.LogInformation("Cargados {Count} items de DLQ desde archivo", items.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al cargar DLQ desde archivo");
            }
        }

        private void SaveToFile()
        {
            try
            {
                var config = _configurationManager.Current.DeadLetterQueue;
                if (string.IsNullOrEmpty(config.PersistencePath))
                    return;

                var directory = Path.GetDirectoryName(config.PersistencePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // OPTIMIZACIÓN: Serializar directamente sin ToList() intermedio
                var items = _items.Values;
                var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(config.PersistencePath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al guardar DLQ en archivo");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _autoRetryTimer?.Dispose();

            // Guardar items si está configurado
            var config = _configurationManager.Current.DeadLetterQueue;
            if (config.Storage == "File" && !string.IsNullOrEmpty(config.PersistencePath))
            {
                SaveToFile();
            }

            _disposed = true;
        }
    }
}

