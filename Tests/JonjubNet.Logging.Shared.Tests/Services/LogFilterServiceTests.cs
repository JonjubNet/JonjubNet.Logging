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
    /// Tests unitarios para LogFilterService
    /// Sigue las mejores prácticas: AAA Pattern, Theory para múltiples casos, FluentAssertions
    /// </summary>
    public class LogFilterServiceTests
    {
        [Fact]
        public void ShouldLog_ShouldReturnFalse_WhenCategoryExcluded()
        {
            // Arrange
            var config = CreateConfiguration(excludedCategories: new List<string> { "Debug", "Trace" });
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogFilterService(configManagerMock.Object);
            var logEntry = CreateLogEntry(category: "Debug");

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldLog_ShouldReturnFalse_WhenOperationExcluded()
        {
            // Arrange
            var config = CreateConfiguration(excludedOperations: new List<string> { "HealthCheck" });
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogFilterService(configManagerMock.Object);
            var logEntry = CreateLogEntry(operation: "HealthCheck");

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldLog_ShouldReturnFalse_WhenUserExcluded()
        {
            // Arrange
            var config = CreateConfiguration(excludedUsers: new List<string> { "System" });
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogFilterService(configManagerMock.Object);
            var logEntry = CreateLogEntry(userId: "System");

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldLog_ShouldReturnFalse_WhenLogLevelBelowMinimum()
        {
            // Arrange
            var config = CreateConfiguration(
                filterByLogLevel: true,
                categoryLogLevels: new Dictionary<string, string> { { "Security", "Warning" } });
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogFilterService(configManagerMock.Object);
            var logEntry = CreateLogEntry(logLevel: "Information", category: "Security");

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ShouldLog_ShouldReturnTrue_WhenLogLevelAboveMinimum()
        {
            // Arrange
            var config = CreateConfiguration(
                filterByLogLevel: true,
                categoryLogLevels: new Dictionary<string, string> { { "Security", "Warning" } });
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogFilterService(configManagerMock.Object);
            var logEntry = CreateLogEntry(logLevel: "Error", category: "Security");

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldLog_ShouldReturnTrue_WhenNoFiltersMatch()
        {
            // Arrange
            var config = CreateConfiguration();
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogFilterService(configManagerMock.Object);
            var logEntry = CreateLogEntry();

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ShouldLog_ShouldBeCaseInsensitive()
        {
            // Arrange
            var config = CreateConfiguration(excludedCategories: new List<string> { "debug" });
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new LogFilterService(configManagerMock.Object);
            var logEntry = CreateLogEntry(category: "DEBUG");

            // Act
            var result = service.ShouldLog(logEntry);

            // Assert
            result.Should().BeFalse();
        }

        // Helper methods
        private static LoggingConfiguration CreateConfiguration(
            List<string>? excludedCategories = null,
            List<string>? excludedOperations = null,
            List<string>? excludedUsers = null,
            bool filterByLogLevel = false,
            Dictionary<string, string>? categoryLogLevels = null)
        {
            return new LoggingConfiguration
            {
                Filters = new LoggingFiltersConfiguration
                {
                    ExcludedCategories = excludedCategories ?? new List<string>(),
                    ExcludedOperations = excludedOperations ?? new List<string>(),
                    ExcludedUsers = excludedUsers ?? new List<string>(),
                    FilterByLogLevel = filterByLogLevel,
                    CategoryLogLevels = categoryLogLevels ?? new Dictionary<string, string>()
                }
            };
        }

        private static StructuredLogEntry CreateLogEntry(
            string logLevel = "Information",
            string category = "General",
            string operation = "TestOperation",
            string? userId = null)
        {
            return new StructuredLogEntry
            {
                Message = "Test message",
                LogLevel = logLevel,
                Category = category,
                Operation = operation,
                UserId = userId ?? string.Empty,
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

