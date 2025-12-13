using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Logging.Domain.Common;
using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Performance.Tests.Benchmarks;

/// <summary>
/// Benchmarks para comparar diferentes métodos de serialización JSON
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class JsonSerializationBenchmark
{
    private StructuredLogEntry _logEntry = null!;

    [GlobalSetup]
    public void Setup()
    {
        _logEntry = CreateSampleLogEntry();
    }

    /// <summary>
    /// Serialización usando ToJson() (método estándar)
    /// </summary>
    [Benchmark(Baseline = true)]
    public string ToJson()
    {
        return _logEntry.ToJson();
    }

    /// <summary>
    /// Serialización usando JsonSerializationHelper (optimizado con Span/Memory)
    /// </summary>
    [Benchmark]
    public string JsonSerializationHelper_SerializeToJson()
    {
        return JsonSerializationHelper.SerializeToJson(_logEntry);
    }

    /// <summary>
    /// Serialización a bytes UTF-8 usando JsonSerializationHelper
    /// </summary>
    [Benchmark]
    public (byte[] Buffer, int Length) JsonSerializationHelper_SerializeToUtf8Bytes()
    {
        return JsonSerializationHelper.SerializeToUtf8Bytes(_logEntry);
    }

    private static StructuredLogEntry CreateSampleLogEntry()
    {
        return new StructuredLogEntry
        {
            ServiceName = "TestService",
            Operation = "TestOperation",
            LogLevel = "Information",
            Message = "This is a test log message with some content",
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

