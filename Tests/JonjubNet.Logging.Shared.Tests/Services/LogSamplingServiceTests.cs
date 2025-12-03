using FluentAssertions;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Shared.Services;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests unitarios para LogSamplingService
    /// Sigue las mejores prácticas: AAA Pattern, Theory para múltiples casos, FluentAssertions
    /// </summary>
    public class LogSamplingServiceTests
    {
        [Fact]
        public void ShouldLog_ShouldReturnTrue_WhenSamplingDisabled()
        {
            // Arrange
            var config = CreateConfiguration(samplingEnabled: false);
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogSamplingService(configManagerMock.Object);
            var logEntry = CreateLogEntry("Information");

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldLog_ShouldReturnTrue_WhenNeverSampleLevel()
        {
            // Arrange
            var config = CreateConfiguration(
                samplingEnabled: true,
                neverSampleLevels: new List<string> { "Error", "Critical" });
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogSamplingService(configManagerMock.Object);
            var logEntry = CreateLogEntry("Error");

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldLog_ShouldReturnTrue_WhenNeverSampleCategory()
        {
            // Arrange
            var config = CreateConfiguration(
                samplingEnabled: true,
                neverSampleCategories: new List<string> { "Security", "Audit" });
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogSamplingService(configManagerMock.Object);
            var logEntry = CreateLogEntry("Information", "Security");

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldLog_ShouldRespectRateLimit()
        {
            // Arrange
            var config = CreateConfiguration(
                samplingEnabled: true,
                maxLogsPerMinute: new Dictionary<string, int> { { "Information", 2 } });
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogSamplingService(configManagerMock.Object);
            var logEntry = CreateLogEntry("Information");

            // Act
            var result1 = service.ShouldLog(logEntry);
            var result2 = service.ShouldLog(logEntry);
            var result3 = service.ShouldLog(logEntry); // Debería exceder el límite

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            result3.Should().BeFalse(); // Rate limit alcanzado
        }

        [Fact]
        public void ShouldLog_ShouldRespectSamplingRate()
        {
            // Arrange
            var config = CreateConfiguration(
                samplingEnabled: true,
                samplingRates: new Dictionary<string, double> { { "Debug", 0.0 } }); // 0% = nunca
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogSamplingService(configManagerMock.Object);
            var logEntry = CreateLogEntry("Debug");

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeFalse(); // Sampling rate 0% = nunca loggear
        }

        [Fact]
        public void ShouldLog_ShouldReturnTrue_WhenNoSamplingRateConfigured()
        {
            // Arrange
            var config = CreateConfiguration(
                samplingEnabled: true,
                samplingRates: new Dictionary<string, double>());
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogSamplingService(configManagerMock.Object);
            var logEntry = CreateLogEntry("Information");

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeTrue(); // Sin configuración = siempre loggear
        }

        [Fact]
        public void ShouldLog_ShouldReturnTrue_WhenNoRateLimitConfigured()
        {
            // Arrange
            var config = CreateConfiguration(
                samplingEnabled: true,
                maxLogsPerMinute: new Dictionary<string, int>());
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogSamplingService(configManagerMock.Object);
            var logEntry = CreateLogEntry("Information");

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeTrue(); // Sin límite = siempre loggear
        }

        // Helper methods
        private static LoggingConfiguration CreateConfiguration(
            bool samplingEnabled = true,
            Dictionary<string, double>? samplingRates = null,
            Dictionary<string, int>? maxLogsPerMinute = null,
            List<string>? neverSampleLevels = null,
            List<string>? neverSampleCategories = null)
        {
            return new LoggingConfiguration
            {
                Sampling = new LoggingSamplingConfiguration
                {
                    Enabled = samplingEnabled,
                    SamplingRates = samplingRates ?? new Dictionary<string, double>
                    {
                        { "Debug", 0.1 },
                        { "Information", 1.0 }
                    },
                    MaxLogsPerMinute = maxLogsPerMinute ?? new Dictionary<string, int>(),
                    NeverSampleLevels = neverSampleLevels ?? new List<string> { "Error", "Critical" },
                    NeverSampleCategories = neverSampleCategories ?? new List<string> { "Security", "Audit" }
                }
            };
        }

        private static StructuredLogEntry CreateLogEntry(string logLevel, string category = "General")
        {
            return new StructuredLogEntry
            {
                Message = "Test message",
                LogLevel = logLevel,
                Category = category,
                Timestamp = DateTime.UtcNow,
                Properties = new Dictionary<string, object>(),
                Context = new Dictionary<string, object>()
            };
        }

        private static Mock<ILoggingConfigurationManager> CreateConfigurationManagerMock(LoggingConfiguration config)
        {
            var mock = new Mock<ILoggingConfigurationManager>();
            mock.Setup(x => x.Current).Returns(config);
            return mock;
        }
    }
}

