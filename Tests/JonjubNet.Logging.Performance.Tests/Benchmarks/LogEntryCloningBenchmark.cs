using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Shared.Services;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace JonjubNet.Logging.Performance.Tests.Benchmarks;

/// <summary>
/// Benchmarks para comparar diferentes métodos de clonado de log entries
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class LogEntryCloningBenchmark
{
    private StructuredLogEntry _logEntry = null!;
    private ILogDataSanitizationService _logDataSanitizationService = null!;

    [GlobalSetup]
    public void Setup()
    {
        _logEntry = CreateSampleLogEntry();

        var configuration = new LoggingConfiguration
        {
            DataSanitization = new LoggingDataSanitizationConfiguration
            {
                Enabled = false // Deshabilitar sanitización para medir solo clonado
            }
        };

        var mockOptions = new Mock<IOptions<LoggingConfiguration>>();
        mockOptions.Setup(m => m.Value).Returns(configuration);

        _logDataSanitizationService = new LogDataSanitizationService(mockOptions.Object);
    }

    /// <summary>
    /// Clonado usando serialización/deserialización JSON (método antiguo, no optimizado)
    /// </summary>
    [Benchmark(Baseline = true)]
    public StructuredLogEntry CloneViaJsonSerialization()
    {
        var json = JsonSerializer.Serialize(_logEntry);
        return JsonSerializer.Deserialize<StructuredLogEntry>(json)!;
    }

    /// <summary>
    /// Clonado manual optimizado (método actual implementado)
    /// </summary>
    [Benchmark]
    public StructuredLogEntry CloneViaManualCloning()
    {
        // Simular el método CloneLogEntry optimizado
        var cloned = new StructuredLogEntry
        {
            ServiceName = _logEntry.ServiceName,
            Operation = _logEntry.Operation,
            LogLevel = _logEntry.LogLevel,
            Message = _logEntry.Message,
            Category = _logEntry.Category,
            EventType = _logEntry.EventType,
            UserId = _logEntry.UserId,
            UserName = _logEntry.UserName,
            Environment = _logEntry.Environment,
            Version = _logEntry.Version,
            MachineName = _logEntry.MachineName,
            ProcessId = _logEntry.ProcessId,
            ThreadId = _logEntry.ThreadId,
            Exception = _logEntry.Exception,
            StackTrace = _logEntry.StackTrace,
            Timestamp = _logEntry.Timestamp,
            RequestPath = _logEntry.RequestPath,
            RequestMethod = _logEntry.RequestMethod,
            StatusCode = _logEntry.StatusCode,
            ClientIp = _logEntry.ClientIp,
            UserAgent = _logEntry.UserAgent,
            CorrelationId = _logEntry.CorrelationId,
            RequestId = _logEntry.RequestId,
            SessionId = _logEntry.SessionId,
            QueryString = _logEntry.QueryString,
            RequestBody = _logEntry.RequestBody,
            ResponseBody = _logEntry.ResponseBody
        };

        if (_logEntry.Properties != null && _logEntry.Properties.Count > 0)
        {
            cloned.Properties = new Dictionary<string, object>(_logEntry.Properties);
        }
        else
        {
            cloned.Properties = new Dictionary<string, object>();
        }

        if (_logEntry.Context != null && _logEntry.Context.Count > 0)
        {
            cloned.Context = new Dictionary<string, object>(_logEntry.Context);
        }
        else
        {
            cloned.Context = new Dictionary<string, object>();
        }

        if (_logEntry.RequestHeaders != null && _logEntry.RequestHeaders.Count > 0)
        {
            cloned.RequestHeaders = new Dictionary<string, string>(_logEntry.RequestHeaders);
        }

        return cloned;
    }

    /// <summary>
    /// Clonado usando el servicio de sanitización (que internamente usa clonado optimizado)
    /// </summary>
    [Benchmark]
    public StructuredLogEntry CloneViaSanitizationService()
    {
        return _logDataSanitizationService.Sanitize(_logEntry);
    }

    private static StructuredLogEntry CreateSampleLogEntry()
    {
        return new StructuredLogEntry
        {
            ServiceName = "TestService",
            Operation = "TestOperation",
            LogLevel = "Information",
            Message = "This is a test log message",
            Category = "Performance",
            EventType = "TestEvent",
            UserId = "user123",
            UserName = "Test User",
            Environment = "Development",
            Version = "1.0.0",
            MachineName = "TEST-MACHINE",
            ProcessId = "12345",
            ThreadId = "67890",
            Timestamp = DateTime.UtcNow,
            RequestPath = "/api/test/endpoint",
            RequestMethod = "GET",
            StatusCode = 200,
            ClientIp = "192.168.1.100",
            UserAgent = "Mozilla/5.0",
            CorrelationId = Guid.NewGuid().ToString(),
            RequestId = Guid.NewGuid().ToString(),
            SessionId = Guid.NewGuid().ToString(),
            Properties = new Dictionary<string, object>
            {
                { "Property1", "Value1" },
                { "Property2", 12345 },
                { "Property3", true },
                { "Property4", DateTime.UtcNow },
                { "NestedProperty", new Dictionary<string, object>
                    {
                        { "Nested1", "NestedValue1" },
                        { "Nested2", 98765 }
                    }
                }
            },
            Context = new Dictionary<string, object>
            {
                { "Context1", "ContextValue1" },
                { "Context2", "ContextValue2" }
            },
            RequestHeaders = new Dictionary<string, string>
            {
                { "Authorization", "Bearer token123" },
                { "Content-Type", "application/json" }
            }
        };
    }
}

