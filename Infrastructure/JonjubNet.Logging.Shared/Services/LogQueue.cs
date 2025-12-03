using System.Threading.Channels;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Options;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Implementación de cola de logs usando Channel para backpressure y performance óptima
    /// </summary>
    public class LogQueue : ILogQueue
    {
        private readonly Channel<StructuredLogEntry> _channel;
        private readonly LoggingConfiguration _configuration;

        public int Count => _channel.Reader.CanCount ? _channel.Reader.Count : 0;
        public int Capacity { get; }

        public LogQueue(IOptions<LoggingConfiguration> configuration)
        {
            _configuration = configuration.Value;
            
            // Capacidad configurable, por defecto 10000 logs
            Capacity = 10000; // Puede ser configurable en el futuro
            
            var options = new BoundedChannelOptions(Capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest, // Descartar logs antiguos si está llena
                SingleReader = false, // Múltiples readers para paralelismo
                SingleWriter = false // Múltiples writers
            };

            _channel = Channel.CreateBounded<StructuredLogEntry>(options);
        }

        public bool TryEnqueue(StructuredLogEntry logEntry)
        {
            return _channel.Writer.TryWrite(logEntry);
        }

        /// <summary>
        /// Obtiene el reader del channel para procesamiento
        /// </summary>
        public ChannelReader<StructuredLogEntry> Reader => _channel.Reader;

        /// <summary>
        /// Completa el writer (para shutdown graceful)
        /// </summary>
        public void Complete()
        {
            _channel.Writer.Complete();
        }
    }
}

