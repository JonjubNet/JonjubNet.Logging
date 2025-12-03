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
    /// Tests unitarios para DataSanitizationService
    /// Sigue las mejores prácticas: AAA Pattern, Tests de seguridad, FluentAssertions
    /// </summary>
    public class DataSanitizationServiceTests
    {
        [Fact]
        public void Sanitize_ShouldReturnOriginal_WhenSanitizationDisabled()
        {
            // Arrange
            var config = CreateConfiguration(sanitizationEnabled: false);
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new DataSanitizationService(configManagerMock.Object);
            var logEntry = CreateLogEntryWithSensitiveData();

            // Act
            var result = service.Sanitize(logEntry);

            // Assert
            result.Should().BeSameAs(logEntry);
            result.Properties["Password"].Should().Be("MyPassword123");
        }

        [Fact]
        public void Sanitize_ShouldMaskSensitivePropertyNames()
        {
            // Arrange
            var config = CreateConfiguration(
                sanitizationEnabled: true,
                sensitivePropertyNames: new List<string> { "Password", "CreditCard" });
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new DataSanitizationService(configManagerMock.Object);
            var logEntry = CreateLogEntryWithSensitiveData();

            // Act
            var result = service.Sanitize(logEntry);

            // Assert
            result.Properties["Password"].Should().Be("***REDACTED***");
            result.Properties["CreditCard"].Should().Be("***REDACTED***");
            result.Properties["NormalKey"].Should().Be("NormalValue");
        }

        [Fact]
        public void Sanitize_ShouldNotModifyOriginal()
        {
            // Arrange
            var config = CreateConfiguration(sanitizationEnabled: true);
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new DataSanitizationService(configManagerMock.Object);
            var logEntry = CreateLogEntryWithSensitiveData();
            var originalPassword = logEntry.Properties["Password"];

            // Act
            var result = service.Sanitize(logEntry);

            // Assert
            logEntry.Properties["Password"].Should().Be(originalPassword);
            result.Properties["Password"].Should().Be("***REDACTED***");
        }

        [Fact]
        public void Sanitize_ShouldMaskWithPartial_WhenMaskPartialEnabled()
        {
            // Arrange
            var config = CreateConfiguration(
                sanitizationEnabled: true,
                maskPartial: true,
                partialMaskLength: 4);
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new DataSanitizationService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry
            {
                Properties = new Dictionary<string, object> { { "Password", "MyPassword123" } }
            };

            // Act
            var result = service.Sanitize(logEntry);

            // Assert
            result.Properties["Password"].Should().Be("***REDACTED***d123");
        }

        [Fact]
        public void Sanitize_ShouldSanitizeRequestHeaders()
        {
            // Arrange
            var config = CreateConfiguration(
                sanitizationEnabled: true,
                sensitivePatterns: new List<string> { "Bearer" }); // Patrón para detectar tokens
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new DataSanitizationService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry
            {
                RequestHeaders = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer token123" },
                    { "NormalHeader", "NormalValue" }
                }
            };

            // Act
            var result = service.Sanitize(logEntry);

            // Assert
            result.RequestHeaders.Should().NotBeNull();
            // La sanitización de headers usa SanitizeString que verifica patrones regex
            // Si el valor contiene "Bearer", debería ser sanitizado
            result.RequestHeaders!["Authorization"].Should().Contain("REDACTED");
            result.RequestHeaders["NormalHeader"].Should().Be("NormalValue");
        }

        [Fact]
        public void Sanitize_ShouldSanitizeRequestBody()
        {
            // Arrange
            var config = CreateConfiguration(
                sanitizationEnabled: true,
                sensitivePatterns: new List<string> { "\\b\\d{4}[\\s-]?\\d{4}[\\s-]?\\d{4}[\\s-]?\\d{4}\\b" });
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new DataSanitizationService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry
            {
                RequestBody = "Card number: 1234-5678-9012-3456"
            };

            // Act
            var result = service.Sanitize(logEntry);

            // Assert
            result.RequestBody.Should().Be("***REDACTED***");
        }

        [Fact]
        public void Sanitize_ShouldSanitizeNestedDictionaries()
        {
            // Arrange
            var config = CreateConfiguration(sanitizationEnabled: true);
            var configManagerMock = CreateConfigurationManagerMock(config);
            var service = new DataSanitizationService(configManagerMock.Object);
            var logEntry = new StructuredLogEntry
            {
                Properties = new Dictionary<string, object>
                {
                    { "User", new Dictionary<string, object> { { "Password", "Secret123" } } }
                }
            };

            // Act
            var result = service.Sanitize(logEntry);

            // Assert
            var userDict = result.Properties["User"] as Dictionary<string, object>;
            userDict.Should().NotBeNull();
            userDict!["Password"].Should().Be("***REDACTED***");
        }

        // Helper methods
        private static LoggingConfiguration CreateConfiguration(
            bool sanitizationEnabled = true,
            List<string>? sensitivePropertyNames = null,
            List<string>? sensitivePatterns = null,
            bool maskPartial = false,
            int partialMaskLength = 4)
        {
            return new LoggingConfiguration
            {
                DataSanitization = new LoggingDataSanitizationConfiguration
                {
                    Enabled = sanitizationEnabled,
                    SensitivePropertyNames = sensitivePropertyNames ?? new List<string>
                    {
                        "Password", "CreditCard", "SSN", "Token"
                    },
                    SensitivePatterns = sensitivePatterns ?? new List<string>(),
                    MaskValue = "***REDACTED***",
                    MaskPartial = maskPartial,
                    PartialMaskLength = partialMaskLength
                }
            };
        }

        private static StructuredLogEntry CreateLogEntryWithSensitiveData()
        {
            return new StructuredLogEntry
            {
                Properties = new Dictionary<string, object>
                {
                    { "Password", "MyPassword123" },
                    { "CreditCard", "1234-5678-9012-3456" },
                    { "NormalKey", "NormalValue" }
                },
                Context = new Dictionary<string, object>(),
                RequestHeaders = null,
                ResponseHeaders = null
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

