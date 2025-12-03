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
    /// Tests adicionales para LogSamplingService
    /// </summary>
    public class LogSamplingServiceAdditionalTests
    {
        [Fact]
        public void ShouldLog_ShouldReturnTrue_WhenDisabled()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                Sampling = new LoggingSamplingConfiguration
                {
                    Enabled = false
                }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogSamplingService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry { LogLevel = "Information" };

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldLog_ShouldReturnTrue_ForNeverSampleLevels()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                Sampling = new LoggingSamplingConfiguration
                {
                    Enabled = true,
                    NeverSampleLevels = new List<string> { "Error", "Critical" }
                }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogSamplingService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry { LogLevel = "Error" };

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldLog_ShouldReturnTrue_ForNeverSampleCategories()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                Sampling = new LoggingSamplingConfiguration
                {
                    Enabled = true,
                    NeverSampleCategories = new List<string> { "Security", "Audit" }
                }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogSamplingService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry { LogLevel = "Information", Category = "Security" };

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldLog_ShouldRespectRateLimit()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                Sampling = new LoggingSamplingConfiguration
                {
                    Enabled = true,
                    MaxLogsPerMinute = new Dictionary<string, int>
                    {
                        { "Information", 2 }
                    }
                }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogSamplingService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry { LogLevel = "Information" };

            // Act
            var result1 = service.ShouldLog(logEntry);
            var result2 = service.ShouldLog(logEntry);
            var result3 = service.ShouldLog(logEntry); // Should be rate limited

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            result3.Should().BeFalse(); // Rate limited
        }

        private static Mock<ILoggingConfigurationManager> CreateConfigurationManagerMock(LoggingConfiguration config)
        {
            var mock = new Mock<ILoggingConfigurationManager>();
            mock.Setup(x => x.Current).Returns(config);
            return mock;
        }
    }
}

