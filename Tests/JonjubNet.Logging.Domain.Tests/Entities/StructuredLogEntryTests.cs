using FluentAssertions;
using JonjubNet.Logging.Domain.Entities;
using System.Text.Json;
using Xunit;

namespace JonjubNet.Logging.Domain.Tests.Entities
{
    /// <summary>
    /// Tests unitarios para StructuredLogEntry
    /// Sigue las mejores prácticas: AAA Pattern, FluentAssertions
    /// </summary>
    public class StructuredLogEntryTests
    {
        [Fact]
        public void ToJson_ShouldSerializeAllProperties()
        {
            // Arrange
            var logEntry = new StructuredLogEntry
            {
                ServiceName = "TestService",
                Operation = "TestOperation",
                LogLevel = "Information",
                Message = "Test message",
                Category = "General",
                EventType = "Custom",
                UserId = "user123",
                UserName = "TestUser",
                Environment = "Development",
                Version = "1.0.0",
                MachineName = "TestMachine",
                ProcessId = "1234",
                ThreadId = "5678",
                Timestamp = DateTime.UtcNow,
                Properties = new Dictionary<string, object> { { "Key1", "Value1" } },
                Context = new Dictionary<string, object> { { "ContextKey", "ContextValue" } }
            };

            // Act
            var json = logEntry.ToJson();

            // Assert
            json.Should().NotBeNullOrEmpty();
            json.Should().Contain("TestService");
            json.Should().Contain("TestOperation");
            json.Should().Contain("Test message");
            json.Should().Contain("user123");
            
            // Verificar que es JSON válido
            var deserialized = JsonSerializer.Deserialize<JsonElement>(json);
            deserialized.GetProperty("serviceName").GetString().Should().Be("TestService");
        }

        [Fact]
        public void ToJson_ShouldHandleNullProperties()
        {
            // Arrange
            var logEntry = new StructuredLogEntry
            {
                ServiceName = "TestService",
                Message = "Test",
                LogLevel = "Information"
            };

            // Act
            var json = logEntry.ToJson();

            // Assert
            json.Should().NotBeNullOrEmpty();
            var deserialized = JsonSerializer.Deserialize<JsonElement>(json);
            deserialized.GetProperty("serviceName").GetString().Should().Be("TestService");
        }

        [Fact]
        public void ToJson_ShouldHandleException()
        {
            // Arrange
            var logEntry = new StructuredLogEntry
            {
                ServiceName = "TestService",
                Message = "Test",
                LogLevel = "Error",
                Exception = new InvalidOperationException("Test exception")
            };

            // Act
            var json = logEntry.ToJson();

            // Assert
            json.Should().NotBeNullOrEmpty();
            json.Should().Contain("Test exception");
        }

        [Fact]
        public void ToJson_ShouldHandleEmptyPropertiesAndContext()
        {
            // Arrange
            var logEntry = new StructuredLogEntry
            {
                ServiceName = "TestService",
                Message = "Test",
                LogLevel = "Information",
                Properties = new Dictionary<string, object>(),
                Context = new Dictionary<string, object>()
            };

            // Act
            var json = logEntry.ToJson();

            // Assert
            json.Should().NotBeNullOrEmpty();
            var deserialized = JsonSerializer.Deserialize<JsonElement>(json);
            deserialized.GetProperty("properties").ValueKind.Should().Be(JsonValueKind.Object);
            deserialized.GetProperty("context").ValueKind.Should().Be(JsonValueKind.Object);
        }

        [Fact]
        public void ToJson_ShouldIncludeHttpInformation()
        {
            // Arrange
            var logEntry = new StructuredLogEntry
            {
                ServiceName = "TestService",
                Message = "Test",
                LogLevel = "Information",
                RequestPath = "/api/test",
                RequestMethod = "GET",
                StatusCode = 200,
                ClientIp = "127.0.0.1",
                UserAgent = "TestAgent",
                CorrelationId = "corr123",
                RequestId = "req123",
                SessionId = "sess123"
            };

            // Act
            var json = logEntry.ToJson();

            // Assert
            json.Should().Contain("/api/test");
            json.Should().Contain("GET");
            json.Should().Contain("200");
            json.Should().Contain("127.0.0.1");
            json.Should().Contain("corr123");
        }

        [Fact]
        public void ToJson_ShouldSetDefaultTimestamp()
        {
            // Arrange
            var logEntry = new StructuredLogEntry
            {
                ServiceName = "TestService",
                Message = "Test",
                LogLevel = "Information"
            };

            // Act
            var json = logEntry.ToJson();

            // Assert
            logEntry.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }
}

