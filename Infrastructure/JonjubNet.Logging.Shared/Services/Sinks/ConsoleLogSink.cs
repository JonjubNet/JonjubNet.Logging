using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging.Shared.Services.Sinks
{
    /// <summary>
    /// Implementación de ILogSink para Console
    /// </summary>
    public class ConsoleLogSink : ILogSink
    {
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly ILogger<ConsoleLogSink> _logger;

        public bool IsEnabled => _configurationManager.Current.Enabled && _configurationManager.Current.Sinks.EnableConsole;

        public string Name => "Console";

        public ConsoleLogSink(
            ILoggingConfigurationManager configurationManager,
            ILogger<ConsoleLogSink> logger)
        {
            _configurationManager = configurationManager;
            _logger = logger;
        }

        public Task SendAsync(StructuredLogEntry logEntry)
        {
            try
            {
                var json = logEntry.ToJson();
                Console.WriteLine(json);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al escribir log en consola");
                return Task.CompletedTask;
            }
        }
    }
}

