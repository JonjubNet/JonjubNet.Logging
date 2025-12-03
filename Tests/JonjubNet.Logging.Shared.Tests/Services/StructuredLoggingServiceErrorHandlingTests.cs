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
    /// Tests para manejo de errores en StructuredLoggingService
    /// </summary>
    public class StructuredLoggingServiceErrorHandlingTests
    {
        [Fact]
        public void LogCustom_ShouldHandleException_WhenEnrichmentFails()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<StructuredLoggingService>>();
            var enrichUseCaseMock = new Mock<EnrichLogEntryUseCase>(configManagerMock.Object);
            enrichUseCaseMock.Setup(x => x.Execute(It.IsAny<StructuredLogEntry>())).Throws(new Exception("Enrichment error"));
            var queueMock = new Mock<ILogQueue>();
            var scopeManagerMock = new Mock<ILogScopeManager>();

            var sendLoggerMock = new Mock<ILogger<SendLogUseCase>>();
            var service = new StructuredLoggingService(
                loggerMock.Object,
                configManagerMock.Object,
                new CreateLogEntryUseCase(),
                enrichUseCaseMock.Object,
                new SendLogUseCase(sendLoggerMock.Object, configManagerMock.Object, Enumerable.Empty<ILogSink>()),
                Enumerable.Empty<ILogSink>(),
                scopeManagerMock.Object,
                null,
                queueMock.Object
            );

            var logEntry = new StructuredLogEntry { Message = "Test" };

            // Act
            service.LogCustom(logEntry);

            // Assert - Should not throw, error should be logged
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
                ),
                Times.Once
            );
        }

        [Fact]
        public void Log_ShouldHandleException_WhenCreateFails()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<StructuredLoggingService>>();
            var createUseCaseMock = new Mock<CreateLogEntryUseCase>();
            createUseCaseMock.Setup(x => x.Execute(
                It.IsAny<string>(),
                It.IsAny<Domain.ValueObjects.LogLevelValue>(),
                It.IsAny<string>(),
                It.IsAny<Domain.ValueObjects.LogCategoryValue>(),
                It.IsAny<Domain.ValueObjects.EventTypeValue>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<Exception>()
            )).Throws(new Exception("Create error"));
            var scopeManagerMock = new Mock<ILogScopeManager>();

            var sendLoggerMock = new Mock<ILogger<SendLogUseCase>>();
            var service = new StructuredLoggingService(
                loggerMock.Object,
                configManagerMock.Object,
                createUseCaseMock.Object,
                new EnrichLogEntryUseCase(configManagerMock.Object),
                new SendLogUseCase(sendLoggerMock.Object, configManagerMock.Object, Enumerable.Empty<ILogSink>()),
                Enumerable.Empty<ILogSink>(),
                scopeManagerMock.Object,
                null,
                null
            );

            // Act
            service.LogInformation("Test message");

            // Assert - Should not throw, error should be logged
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
                ),
                Times.Once
            );
        }

        [Fact]
        public void LogCustom_ShouldUseFallback_WhenQueueIsNull()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<StructuredLoggingService>>();
            var sendUseCaseMock = new Mock<SendLogUseCase>(
                loggerMock.Object,
                configManagerMock.Object,
                Enumerable.Empty<ILogSink>()
            );
            var scopeManagerMock = new Mock<ILogScopeManager>();

            var sendLoggerMock = new Mock<ILogger<SendLogUseCase>>();
            var service = new StructuredLoggingService(
                loggerMock.Object,
                configManagerMock.Object,
                new CreateLogEntryUseCase(),
                new EnrichLogEntryUseCase(configManagerMock.Object),
                sendUseCaseMock.Object,
                Enumerable.Empty<ILogSink>(),
                scopeManagerMock.Object,
                null,
                null // No queue - should use fallback
            );

            var logEntry = new StructuredLogEntry { Message = "Test" };

            // Act
            service.LogCustom(logEntry);

            // Assert - Wait a bit for Task.Run to complete
            Thread.Sleep(100);
            sendUseCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        private static Mock<ILoggingConfigurationManager> CreateConfigurationManagerMock(LoggingConfiguration config)
        {
            var mock = new Mock<ILoggingConfigurationManager>();
            mock.Setup(x => x.Current).Returns(config);
            return mock;
        }
    }
}

