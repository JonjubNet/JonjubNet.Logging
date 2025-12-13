using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Shared.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace JonjubNet.Logging.Performance.Tests.Benchmarks;

/// <summary>
/// Benchmarks para medir el rendimiento de la sanitización de datos
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class DataSanitizationBenchmark
{
    private StructuredLogEntry _logEntryWithSensitiveData = null!;
    private IDataSanitizationService _dataSanitizationService = null!;
    private ILogDataSanitizationService _logDataSanitizationService = null!;

    [GlobalSetup]
    public void Setup()
    {
        _logEntryWithSensitiveData = CreateLogEntryWithSensitiveData();

        // Configurar servicios de sanitización
        var configuration = new LoggingConfiguration
        {
            DataSanitization = new LoggingDataSanitizationConfiguration
            {
                Enabled = true,
                SensitivePropertyNames = new List<string> { "password", "token", "secret", "apiKey", "creditCard" },
                SensitivePatterns = new List<string>
                {
                    @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", // Credit card
                    @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", // Email
                    @"\b\d{3}-\d{2}-\d{4}\b" // SSN
                },
                MaskValue = "***MASKED***",
                MaskPartial = false
            }
        };

        var mockConfigManager = new Mock<ILoggingConfigurationManager>();
        mockConfigManager.Setup(m => m.Current).Returns(configuration);

        var mockOptions = new Mock<IOptions<LoggingConfiguration>>();
        mockOptions.Setup(m => m.Value).Returns(configuration);

        _dataSanitizationService = new DataSanitizationService(mockConfigManager.Object);
        _logDataSanitizationService = new LogDataSanitizationService(mockOptions.Object);
    }

    /// <summary>
    /// Sanitización usando DataSanitizationService
    /// </summary>
    [Benchmark(Baseline = true)]
    public StructuredLogEntry DataSanitizationService_Sanitize()
    {
        return _dataSanitizationService.Sanitize(_logEntryWithSensitiveData);
    }

    /// <summary>
    /// Sanitización usando LogDataSanitizationService
    /// </summary>
    [Benchmark]
    public StructuredLogEntry LogDataSanitizationService_Sanitize()
    {
        return _logDataSanitizationService.Sanitize(_logEntryWithSensitiveData);
    }

    private static StructuredLogEntry CreateLogEntryWithSensitiveData()
    {
        return new StructuredLogEntry
        {
            ServiceName = "TestService",
            Operation = "TestOperation",
            LogLevel = "Information",
            Message = "User login attempt with email user@example.com",
            Category = "Security",
            EventType = "UserAction",
            UserId = "user123",
            UserName = "Test User",
            Environment = "Development",
            Version = "1.0.0",
            MachineName = "TEST-MACHINE",
            ProcessId = "12345",
            ThreadId = "67890",
            Timestamp = DateTime.UtcNow,
            RequestPath = "/api/login",
            RequestMethod = "POST",
            StatusCode = 200,
            ClientIp = "192.168.1.100",
            UserAgent = "Mozilla/5.0",
            CorrelationId = Guid.NewGuid().ToString(),
            RequestId = Guid.NewGuid().ToString(),
            SessionId = Guid.NewGuid().ToString(),
            Properties = new Dictionary<string, object>
            {
                { "password", "MySecretPassword123!" },
                { "apiKey", "sk-1234567890abcdef" },
                { "creditCard", "4532-1234-5678-9010" },
                { "email", "user@example.com" },
                { "ssn", "123-45-6789" },
                { "normalProperty", "NormalValue" }
            },
            Context = new Dictionary<string, object>
            {
                { "token", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" },
                { "secret", "MySecretKey123" }
            },
            RequestHeaders = new Dictionary<string, string>
            {
                { "Authorization", "Bearer secret-token-12345" },
                { "X-API-Key", "api-key-12345" },
                { "Content-Type", "application/json" }
            },
            RequestBody = "{\"password\":\"MyPassword123\",\"username\":\"user@example.com\"}",
            ResponseBody = "{\"token\":\"secret-token-12345\"}"
        };
    }
}

