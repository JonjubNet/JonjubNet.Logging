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
    /// Tests adicionales para LogFilterService
    /// </summary>
    public class LogFilterServiceAdditionalTests
    {
        [Fact]
        public void ShouldLog_ShouldFilterByCategoryLogLevel()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                Filters = new LoggingFiltersConfiguration
                {
                    FilterByLogLevel = true,
                    CategoryLogLevels = new Dictionary<string, string>
                    {
                        { "Security", "Warning" }
                    }
                }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogFilterService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry
            {
                Category = "Security",
                LogLevel = "Information" // Below Warning
            };

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldLog_ShouldAllowHigherLogLevel()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                Filters = new LoggingFiltersConfiguration
                {
                    FilterByLogLevel = true,
                    CategoryLogLevels = new Dictionary<string, string>
                    {
                        { "Security", "Warning" }
                    }
                }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogFilterService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry
            {
                Category = "Security",
                LogLevel = "Error" // Above Warning
            };

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeTrue();
        }

        private static Mock<ILoggingConfigurationManager> CreateConfigurationManagerMock(LoggingConfiguration config)
        {
            var mock = new Mock<ILoggingConfigurationManager>();
            mock.Setup(x => x.Current).Returns(config);
            return mock;
        }
    }
}

