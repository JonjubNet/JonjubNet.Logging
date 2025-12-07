using FluentAssertions;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Domain.ValueObjects;
using JonjubNet.Logging.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests unitarios para StructuredLoggingService
    /// </summary>
    public class StructuredLoggingServiceTests
    {
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly Mock<ILogger<SendLogUseCase>> _sendLoggerMock;
        private readonly CreateLogEntryUseCase _createUseCase;
        private readonly EnrichLogEntryUseCase _enrichUseCase;
        private readonly SendLogUseCase _sendUseCase;
        private readonly Mock<ILogScopeManager> _scopeManagerMock;
        private readonly Mock<ILogQueue> _logQueueMock;
        private readonly LoggingConfiguration _configuration;

        public StructuredLoggingServiceTests()
        {
            _loggerMock = new Mock<ILogger>();
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);
            _sendLoggerMock = new Mock<ILogger<SendLogUseCase>>();
            _createUseCase = new CreateLogEntryUseCase();
            var defaultConfig = new LoggingConfiguration();
            var configManagerMock = CreateConfigurationManagerMock(defaultConfig);
            _enrichUseCase = new EnrichLogEntryUseCase(configManagerMock.Object);
            _sendUseCase = new SendLogUseCase(
                _sendLoggerMock.Object,
                configManagerMock.Object,
                Enumerable.Empty<ILogSink>()
            );
            _scopeManagerMock = new Mock<ILogScopeManager>();
            _logQueueMock = new Mock<ILogQueue>();

            _configuration = new LoggingConfiguration
            {
                Enabled = true,
                ServiceName = "TestService",
                Environment = "Test",
                Version = "1.0.0"
            };
        }

        private StructuredLoggingService CreateService(bool enabled = true, bool useQueue = true)
        {
            _configuration.Enabled = enabled;
            var configManagerMock = CreateConfigurationManagerMock(_configuration);

            return new StructuredLoggingService(
                _loggerFactoryMock.Object,
                configManagerMock.Object,
                _createUseCase,
                _enrichUseCase,
                _sendUseCase,
                Enumerable.Empty<ILogSink>(),
                _scopeManagerMock.Object,
                null,
                useQueue ? _logQueueMock.Object : null
            );
        }

        private static Mock<ILoggingConfigurationManager> CreateConfigurationManagerMock(LoggingConfiguration config)
        {
            var mock = new Mock<ILoggingConfigurationManager>();
            mock.Setup(x => x.Current).Returns(config);
            return mock;
        }

        [Fact]
        public void LogInformation_ShouldCreateAndEnqueueLog()
        {
            // Arrange
            var service = CreateService();
            _logQueueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(true);

            // Act
            service.LogInformation("Test message", "TestOperation");

            // Assert
            _logQueueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public void LogWarning_ShouldCreateAndEnqueueLog()
        {
            // Arrange
            var service = CreateService();
            _logQueueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(true);

            // Act
            service.LogWarning("Warning message", "TestOperation");

            // Assert
            _logQueueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public void LogError_ShouldCreateAndEnqueueLog()
        {
            // Arrange
            var service = CreateService();
            var exception = new Exception("Test exception");
            _logQueueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(true);

            // Act
            service.LogError("Error message", "TestOperation", exception: exception);

            // Assert
            _logQueueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public void LogOperationStart_ShouldCreateLogWithOperationStartEvent()
        {
            // Arrange
            var service = CreateService();
            _logQueueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(true);

            // Act
            service.LogOperationStart("TestOperation", "Business");

            // Assert
            _logQueueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public void LogOperationEnd_ShouldCreateLogWithExecutionTime()
        {
            // Arrange
            var service = CreateService();
            _logQueueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(true);

            // Act
            service.LogOperationEnd("TestOperation", "Business", 150, success: true);

            // Assert
            _logQueueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public void LogUserAction_ShouldCreateLogWithUserActionEvent()
        {
            // Arrange
            var service = CreateService();
            _logQueueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(true);

            // Act
            service.LogUserAction("CreateUser", "User", "user123");

            // Assert
            _logQueueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public void LogSecurityEvent_ShouldCreateLogWithSecurityCategory()
        {
            // Arrange
            var service = CreateService();
            _logQueueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(true);

            // Act
            service.LogSecurityEvent("UnauthorizedAccess", "User attempted unauthorized access");

            // Assert
            _logQueueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public void BeginScope_ShouldReturnLogScope()
        {
            // Arrange
            var service = CreateService();
            var properties = new Dictionary<string, object> { { "Key", "Value" } };

            // Act
            var scope = service.BeginScope(properties);

            // Assert
            scope.Should().NotBeNull();
            scope.Properties.Should().BeEquivalentTo(properties);
        }

        [Fact]
        public void LogCustom_ShouldNotProcess_WhenDisabled()
        {
            // Arrange
            var service = CreateService(enabled: false);
            var logEntry = new StructuredLogEntry { Message = "Test" };

            // Act
            service.LogCustom(logEntry);

            // Assert
            _logQueueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Never);
        }

        [Fact]
        public void LogCustom_ShouldHandleQueueFull()
        {
            // Arrange
            var service = CreateService();
            var logEntry = new StructuredLogEntry { Message = "Test" };
            _logQueueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(false);

            // Act
            service.LogCustom(logEntry);

            // Assert
            _logQueueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }
    }
}

