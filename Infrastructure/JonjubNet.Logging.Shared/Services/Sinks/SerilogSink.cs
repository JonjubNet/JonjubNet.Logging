using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Logging;
using Serilog;

namespace JonjubNet.Logging.Shared.Services.Sinks
{
    /// <summary>
    /// Implementación de ILogSink usando Serilog
    /// Maneja Console, File, HTTP y Elasticsearch según configuración
    /// </summary>
    public class SerilogSink : ILogSink
    {
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly ILogger<SerilogSink> _logger;

        public bool IsEnabled
        {
            get
            {
                var config = _configurationManager.Current;
                return config.Enabled && 
                    (config.Sinks.EnableConsole || 
                     config.Sinks.EnableFile || 
                     config.Sinks.EnableHttp || 
                     config.Sinks.EnableElasticsearch);
            }
        }

        public string Name => "Serilog";

        public SerilogSink(
            ILoggingConfigurationManager configurationManager,
            ILogger<SerilogSink> logger)
        {
            _configurationManager = configurationManager;
            _logger = logger;
        }

        public Task SendAsync(StructuredLogEntry logEntry)
        {
            try
            {
                // Convertir el log entry a formato Serilog
                var level = ParseLogLevel(logEntry.LogLevel);
                var message = logEntry.Message;

                // Crear propiedades para Serilog
                var properties = new Dictionary<string, object>
                {
                    { "ServiceName", logEntry.ServiceName },
                    { "Operation", logEntry.Operation },
                    { "Category", logEntry.Category },
                    { "Environment", logEntry.Environment },
                    { "Version", logEntry.Version },
                    { "MachineName", logEntry.MachineName },
                    { "ProcessId", logEntry.ProcessId },
                    { "ThreadId", logEntry.ThreadId }
                };

                // Agregar propiedades adicionales
                foreach (var prop in logEntry.Properties)
                {
                    properties[prop.Key] = prop.Value;
                }

                // Agregar información HTTP si está disponible
                if (!string.IsNullOrEmpty(logEntry.RequestPath))
                {
                    properties["RequestPath"] = logEntry.RequestPath;
<<<<<<< HEAD
                    if (!string.IsNullOrEmpty(logEntry.RequestMethod))
                    {
                        properties["RequestMethod"] = logEntry.RequestMethod;
                    }
                    if (logEntry.StatusCode.HasValue)
                    {
                        properties["StatusCode"] = logEntry.StatusCode.Value;
                    }
=======
                    properties["RequestMethod"] = logEntry.RequestMethod;
                    properties["StatusCode"] = logEntry.StatusCode;
>>>>>>> 6b8317a7f8fd86192c146f543abc241ef855a4cf
                }

                // Agregar información de usuario
                if (!string.IsNullOrEmpty(logEntry.UserId))
                {
                    properties["UserId"] = logEntry.UserId;
                    properties["UserName"] = logEntry.UserName;
                }

                // Agregar información de correlación
                if (!string.IsNullOrEmpty(logEntry.CorrelationId))
                    properties["CorrelationId"] = logEntry.CorrelationId;
                if (!string.IsNullOrEmpty(logEntry.RequestId))
                    properties["RequestId"] = logEntry.RequestId;
                if (!string.IsNullOrEmpty(logEntry.SessionId))
                    properties["SessionId"] = logEntry.SessionId;

                // Escribir usando Serilog
                Log.Write(level, logEntry.Exception, message, properties.Values.ToArray());

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar log a Serilog");
                return Task.CompletedTask;
            }
        }

        private Serilog.Events.LogEventLevel ParseLogLevel(string logLevel)
        {
            return logLevel.ToUpperInvariant() switch
            {
                "TRACE" => Serilog.Events.LogEventLevel.Verbose,
                "DEBUG" => Serilog.Events.LogEventLevel.Debug,
                "INFORMATION" => Serilog.Events.LogEventLevel.Information,
                "WARNING" => Serilog.Events.LogEventLevel.Warning,
                "ERROR" => Serilog.Events.LogEventLevel.Error,
                "CRITICAL" => Serilog.Events.LogEventLevel.Fatal,
                "FATAL" => Serilog.Events.LogEventLevel.Fatal,
                _ => Serilog.Events.LogEventLevel.Information
            };
        }
    }
}

