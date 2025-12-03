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
    /// Tests adicionales para DataSanitizationService
    /// </summary>
    public class DataSanitizationServiceAdditionalTests
    {
        [Fact]
        public void Sanitize_ShouldMaskSensitivePropertyNames()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                DataSanitization = new LoggingDataSanitizationConfiguration
                {
                    Enabled = true,
                    SensitivePropertyNames = new List<string> { "Password", "Token" },
                    MaskValue = "***MASKED***"
                }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new DataSanitizationService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry
            {
                Properties = new Dictionary<string, object>
                {
                    { "Password", "secret123" },
                    { "Token", "abc123" },
                    { "Username", "john" }
                }
            };

            // Act
            var result = service.Sanitize(logEntry);

            // Assert
            result.Properties["Password"].Should().Be("***MASKED***");
            result.Properties["Token"].Should().Be("***MASKED***");
            result.Properties["Username"].Should().Be("john"); // Not sensitive
        }

        [Fact]
        public void Sanitize_ShouldHandleNestedDictionaries()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                DataSanitization = new LoggingDataSanitizationConfiguration
                {
                    Enabled = true,
                    SensitivePropertyNames = new List<string> { "Password" },
                    MaskValue = "***MASKED***"
                }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new DataSanitizationService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry
            {
                Properties = new Dictionary<string, object>
                {
                    { "User", new Dictionary<string, object> { { "Password", "secret" }, { "Name", "John" } } }
                }
            };

            // Act
            var result = service.Sanitize(logEntry);

            // Assert
            var userDict = result.Properties["User"] as Dictionary<string, object>;
            userDict.Should().NotBeNull();
            userDict!["Password"].Should().Be("***MASKED***");
        }

        [Fact]
        public void Sanitize_ShouldReturnSanitizedCopy()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                DataSanitization = new LoggingDataSanitizationConfiguration
                {
                    Enabled = true,
                    SensitivePropertyNames = new List<string> { "Password" },
                    MaskValue = "***MASKED***"
                }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new DataSanitizationService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry
            {
                Properties = new Dictionary<string, object> { { "Password", "secret123" } }
            };
            var originalPassword = logEntry.Properties["Password"].ToString();

            // Act
            var result = service.Sanitize(logEntry);

            // Assert
            result.Properties["Password"].Should().Be("***MASKED***"); // Sanitized copy
            result.Should().NotBeSameAs(logEntry); // Should be a different instance
        }

        private static Mock<ILoggingConfigurationManager> CreateConfigurationManagerMock(LoggingConfiguration config)
        {
            var mock = new Mock<ILoggingConfigurationManager>();
            mock.Setup(x => x.Current).Returns(config);
            return mock;
        }
    }
}

