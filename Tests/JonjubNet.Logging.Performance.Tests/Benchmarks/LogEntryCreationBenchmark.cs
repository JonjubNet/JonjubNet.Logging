using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Performance.Tests.Benchmarks;

/// <summary>
/// Benchmarks para medir el rendimiento de creación de log entries
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class LogEntryCreationBenchmark
{
    /// <summary>
    /// Creación de log entry básico (sin propiedades)
    /// </summary>
    [Benchmark(Baseline = true)]
    public StructuredLogEntry CreateBasicLogEntry()
    {
        return new StructuredLogEntry
        {
            ServiceName = "TestService",
            Operation = "TestOperation",
            LogLevel = "Information",
            Message = "Test message",
            Category = "Test",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creación de log entry con propiedades
    /// </summary>
    [Benchmark]
    public StructuredLogEntry CreateLogEntryWithProperties()
    {
        return new StructuredLogEntry
        {
            ServiceName = "TestService",
            Operation = "TestOperation",
            LogLevel = "Information",
            Message = "Test message",
            Category = "Test",
            Timestamp = DateTime.UtcNow,
            Properties = new Dictionary<string, object>
            {
                { "Property1", "Value1" },
                { "Property2", 12345 },
                { "Property3", true }
            }
        };
    }

    /// <summary>
    /// Creación de log entry completo (con todas las propiedades)
    /// </summary>
    [Benchmark]
    public StructuredLogEntry CreateFullLogEntry()
    {
        return new StructuredLogEntry
        {
            ServiceName = "TestService",
            Operation = "TestOperation",
            LogLevel = "Information",
            Message = "Test message",
            Category = "Test",
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
                { "Property3", true }
            },
            Context = new Dictionary<string, object>
            {
                { "Context1", "ContextValue1" }
            },
            RequestHeaders = new Dictionary<string, string>
            {
                { "Authorization", "Bearer token123" }
            }
        };
    }
}

