using System.Threading.Channels;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Implementación de cola de logs con priorización por nivel/categoría
    /// </summary>
    public class PriorityLogQueue : IPriorityLogQueue
    {
        private readonly Dictionary<string, Channel<StructuredLogEntry>> _priorityChannels = new();
        private readonly Dictionary<string, int> _capacities = new();
        private readonly LoggingBatchingConfiguration _batchingConfig;
        private readonly ILogger<PriorityLogQueue>? _logger;
        private readonly string[] _priorities = { "Critical", "Error", "Warning", "Information", "Debug", "Trace" };

        public PriorityLogQueue(
            ILoggingConfigurationManager configurationManager,
            ILogger<PriorityLogQueue>? logger = null)
        {
            _batchingConfig = configurationManager.Current.Batching;
            _logger = logger;

            if (!_batchingConfig.EnablePriorityQueues)
            {
                return;
            }

            // Crear canales para cada prioridad
            foreach (var priority in _priorities)
            {
                var capacity = _batchingConfig.QueueCapacityByPriority.TryGetValue(priority, out var cap)
                    ? cap
                    : 1000;

                _capacities[priority] = capacity;

                var options = new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = false,
                    SingleWriter = false
                };

                _priorityChannels[priority] = Channel.CreateBounded<StructuredLogEntry>(options);
            }
        }

        public bool TryEnqueue(StructuredLogEntry logEntry)
        {
            if (!_batchingConfig.EnablePriorityQueues)
            {
                return false;
            }

            var priority = DeterminePriority(logEntry);
            
            if (_priorityChannels.TryGetValue(priority, out var channel))
            {
                var result = channel.Writer.TryWrite(logEntry);
                if (!result)
                {
                    _logger?.LogWarning("Cola de prioridad {Priority} llena, descartando log", priority);
                }
                return result;
            }

            return false;
        }

        public ChannelReader<StructuredLogEntry> GetReader(string priority)
        {
            if (_priorityChannels.TryGetValue(priority, out var channel))
            {
                return channel.Reader;
            }

            throw new ArgumentException($"Prioridad '{priority}' no encontrada", nameof(priority));
        }

        public IEnumerable<string> GetPriorities()
        {
            return _priorities;
        }

        public int GetCount(string priority)
        {
            if (_priorityChannels.TryGetValue(priority, out var channel))
            {
                return channel.Reader.CanCount ? channel.Reader.Count : 0;
            }

            return 0;
        }

        public int GetCapacity(string priority)
        {
            return _capacities.TryGetValue(priority, out var capacity) ? capacity : 0;
        }

        private string DeterminePriority(StructuredLogEntry logEntry)
        {
            // Determinar prioridad basada en nivel de log
            return logEntry.LogLevel switch
            {
                "Critical" or "Fatal" => "Critical",
                "Error" => "Error",
                "Warning" => "Warning",
                "Information" => "Information",
                "Debug" => "Debug",
                "Trace" => "Trace",
                _ => "Information"
            };
        }
    }
}

