using FluentAssertions;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests adicionales para StructuredLoggingService para aumentar cobertura
    /// </summary>
    public class StructuredLoggingServiceAdditionalTests
    {
        [Fact]
        public void LogDebug_ShouldCreateAndEnqueueLog()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
            var queueMock = new Mock<ILogQueue>();
            queueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(true);
            var scopeManagerMock = new Mock<ILogScopeManager>();

            var sendLoggerMock = new Mock<ILogger<SendLogUseCase>>();
            var service = new StructuredLoggingService(
                loggerFactoryMock.Object,
                configManagerMock.Object,
                new CreateLogEntryUseCase(),
                new EnrichLogEntryUseCase(configManagerMock.Object),
                new SendLogUseCase(sendLoggerMock.Object, configManagerMock.Object, Enumerable.Empty<ILogSink>()),
                Enumerable.Empty<ILogSink>(),
                scopeManagerMock.Object,
                null,
                queueMock.Object
            );

            // Act
            service.LogDebug("Debug message", "TestOperation");

            // Assert
            queueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public void LogTrace_ShouldCreateAndEnqueueLog()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
            var queueMock = new Mock<ILogQueue>();
            queueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(true);
            var scopeManagerMock = new Mock<ILogScopeManager>();

            var sendLoggerMock = new Mock<ILogger<SendLogUseCase>>();
            var service = new StructuredLoggingService(
                loggerFactoryMock.Object,
                configManagerMock.Object,
                new CreateLogEntryUseCase(),
                new EnrichLogEntryUseCase(configManagerMock.Object),
                new SendLogUseCase(sendLoggerMock.Object, configManagerMock.Object, Enumerable.Empty<ILogSink>()),
                Enumerable.Empty<ILogSink>(),
                scopeManagerMock.Object,
                null,
                queueMock.Object
            );

            // Act
            service.LogTrace("Trace message", "TestOperation");

            // Assert
            queueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public void LogCritical_ShouldCreateAndEnqueueLog()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
            var queueMock = new Mock<ILogQueue>();
            queueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(true);
            var scopeManagerMock = new Mock<ILogScopeManager>();

            var sendLoggerMock = new Mock<ILogger<SendLogUseCase>>();
            var service = new StructuredLoggingService(
                loggerFactoryMock.Object,
                configManagerMock.Object,
                new CreateLogEntryUseCase(),
                new EnrichLogEntryUseCase(configManagerMock.Object),
                new SendLogUseCase(sendLoggerMock.Object, configManagerMock.Object, Enumerable.Empty<ILogSink>()),
                Enumerable.Empty<ILogSink>(),
                scopeManagerMock.Object,
                null,
                queueMock.Object
            );

            // Act
            service.LogCritical("Critical message", "TestOperation");

            // Assert
            queueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public void LogAuditEvent_ShouldCreateLogWithAuditCategory()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
            var queueMock = new Mock<ILogQueue>();
            queueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(true);
            var scopeManagerMock = new Mock<ILogScopeManager>();

            var sendLoggerMock = new Mock<ILogger<SendLogUseCase>>();
            var service = new StructuredLoggingService(
                loggerFactoryMock.Object,
                configManagerMock.Object,
                new CreateLogEntryUseCase(),
                new EnrichLogEntryUseCase(configManagerMock.Object),
                new SendLogUseCase(sendLoggerMock.Object, configManagerMock.Object, Enumerable.Empty<ILogSink>()),
                Enumerable.Empty<ILogSink>(),
                scopeManagerMock.Object,
                null,
                queueMock.Object
            );

            // Act
            service.LogAuditEvent("UserCreated", "User was created", "User", "user123");

            // Assert
            queueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public void BeginScope_WithKeyValue_ShouldReturnLogScope()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
            var scopeManagerMock = new Mock<ILogScopeManager>();

            var sendLoggerMock = new Mock<ILogger<SendLogUseCase>>();
            var service = new StructuredLoggingService(
                loggerFactoryMock.Object,
                configManagerMock.Object,
                new CreateLogEntryUseCase(),
                new EnrichLogEntryUseCase(configManagerMock.Object),
                new SendLogUseCase(sendLoggerMock.Object, configManagerMock.Object, Enumerable.Empty<ILogSink>()),
                Enumerable.Empty<ILogSink>(),
                scopeManagerMock.Object,
                null,
                null
            );

            // Act
            var scope = service.BeginScope("Key", "Value");

            // Assert
            scope.Should().NotBeNull();
            scope.Properties.Should().ContainKey("Key");
            scope.Properties["Key"].Should().Be("Value");
        }

        [Fact]
        public void LogOperationEnd_WithFailure_ShouldCreateErrorLog()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
            var queueMock = new Mock<ILogQueue>();
            queueMock.Setup(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>())).Returns(true);
            var scopeManagerMock = new Mock<ILogScopeManager>();

            var sendLoggerMock = new Mock<ILogger<SendLogUseCase>>();
            var service = new StructuredLoggingService(
                loggerFactoryMock.Object,
                configManagerMock.Object,
                new CreateLogEntryUseCase(),
                new EnrichLogEntryUseCase(configManagerMock.Object),
                new SendLogUseCase(sendLoggerMock.Object, configManagerMock.Object, Enumerable.Empty<ILogSink>()),
                Enumerable.Empty<ILogSink>(),
                scopeManagerMock.Object,
                null,
                queueMock.Object
            );

            // Act
            service.LogOperationEnd("TestOperation", "Business", 150, success: false, exception: new Exception("Test error"));

            // Assert
            queueMock.Verify(x => x.TryEnqueue(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        private static Mock<ILoggingConfigurationManager> CreateConfigurationManagerMock(LoggingConfiguration config)
        {
            var mock = new Mock<ILoggingConfigurationManager>();
            mock.Setup(x => x.Current).Returns(config);
            return mock;
        }
    }
}

