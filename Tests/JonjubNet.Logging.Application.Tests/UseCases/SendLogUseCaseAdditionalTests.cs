using FluentAssertions;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Application.Tests.UseCases
{
    /// <summary>
    /// Tests adicionales para SendLogUseCase para aumentar cobertura
    /// </summary>
    public class SendLogUseCaseAdditionalTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldNotSend_WhenDisabled()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = false };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<SendLogUseCase>>();
            var sinkMock = new Mock<ILogSink>();
            var useCase = new SendLogUseCase(loggerMock.Object, configManagerMock.Object, new[] { sinkMock.Object });
            var logEntry = new StructuredLogEntry { Message = "Test" };

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            sinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldFilterLog_WhenFilterRejects()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<SendLogUseCase>>();
            var filterMock = new Mock<ILogFilter>();
            filterMock.Setup(x => x.ShouldLog(It.IsAny<StructuredLogEntry>())).Returns(false);
            var sinkMock = new Mock<ILogSink>();
            var useCase = new SendLogUseCase(
                loggerMock.Object,
                configManagerMock.Object,
                new[] { sinkMock.Object },
                logFilter: filterMock.Object
            );
            var logEntry = new StructuredLogEntry { Message = "Test" };

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            sinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSampleLog_WhenSamplingRejects()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<SendLogUseCase>>();
            var samplingMock = new Mock<ILogSamplingService>();
            samplingMock.Setup(x => x.ShouldLog(It.IsAny<StructuredLogEntry>())).Returns(false);
            var sinkMock = new Mock<ILogSink>();
            var useCase = new SendLogUseCase(
                loggerMock.Object,
                configManagerMock.Object,
                new[] { sinkMock.Object },
                samplingService: samplingMock.Object
            );
            var logEntry = new StructuredLogEntry { Message = "Test" };

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            sinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSanitizeLog_WhenSanitizationEnabled()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<SendLogUseCase>>();
            var sanitizationMock = new Mock<IDataSanitizationService>();
            var sanitizedEntry = new StructuredLogEntry { Message = "Sanitized" };
            sanitizationMock.Setup(x => x.Sanitize(It.IsAny<StructuredLogEntry>())).Returns(sanitizedEntry);
            var sinkMock = new Mock<ILogSink>();
            var useCase = new SendLogUseCase(
                loggerMock.Object,
                configManagerMock.Object,
                new[] { sinkMock.Object },
                sanitizationService: sanitizationMock.Object
            );
            var logEntry = new StructuredLogEntry { Message = "Test", Properties = new Dictionary<string, object> { { "Password", "secret123" } } };

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            sanitizationMock.Verify(x => x.Sanitize(It.IsAny<StructuredLogEntry>()), Times.Once);
            sinkMock.Verify(x => x.SendAsync(sanitizedEntry), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSendToKafka_WhenKafkaEnabled()
        {
            // Arrange
            var config = new LoggingConfiguration 
            { 
                Enabled = true,
                KafkaProducer = new LoggingKafkaProducerConfiguration { Enabled = true }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<SendLogUseCase>>();
            var kafkaMock = new Mock<IKafkaProducer>();
            kafkaMock.Setup(x => x.IsEnabled).Returns(true);
            var sinkMock = new Mock<ILogSink>();
            var useCase = new SendLogUseCase(
                loggerMock.Object,
                configManagerMock.Object,
                new[] { sinkMock.Object },
                kafkaProducer: kafkaMock.Object
            );
            var logEntry = new StructuredLogEntry { Message = "Test" };

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            kafkaMock.Verify(x => x.SendAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleKafkaError()
        {
            // Arrange
            var config = new LoggingConfiguration 
            { 
                Enabled = true,
                KafkaProducer = new LoggingKafkaProducerConfiguration { Enabled = true }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<SendLogUseCase>>();
            var kafkaMock = new Mock<IKafkaProducer>();
            kafkaMock.Setup(x => x.IsEnabled).Returns(true);
            kafkaMock.Setup(x => x.SendAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Kafka error"));
            var sinkMock = new Mock<ILogSink>();
            var useCase = new SendLogUseCase(
                loggerMock.Object,
                configManagerMock.Object,
                new[] { sinkMock.Object },
                kafkaProducer: kafkaMock.Object
            );
            var logEntry = new StructuredLogEntry { Message = "Test" };

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            sinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Once); // Sink should still be called
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleSinkError()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<SendLogUseCase>>();
            var sinkMock = new Mock<ILogSink>();
            sinkMock.Setup(x => x.IsEnabled).Returns(true);
            sinkMock.Setup(x => x.SendAsync(It.IsAny<StructuredLogEntry>())).ThrowsAsync(new Exception("Sink error"));
            var useCase = new SendLogUseCase(loggerMock.Object, configManagerMock.Object, new[] { sinkMock.Object });
            var logEntry = new StructuredLogEntry { Message = "Test" };

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert - Should not throw, error should be logged
            sinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldOnlySendToEnabledSinks()
        {
            // Arrange
            var config = new LoggingConfiguration { Enabled = true };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<SendLogUseCase>>();
            var enabledSink = new Mock<ILogSink>();
            enabledSink.Setup(x => x.IsEnabled).Returns(true);
            var disabledSink = new Mock<ILogSink>();
            disabledSink.Setup(x => x.IsEnabled).Returns(false);
            var useCase = new SendLogUseCase(loggerMock.Object, configManagerMock.Object, new[] { enabledSink.Object, disabledSink.Object });
            var logEntry = new StructuredLogEntry { Message = "Test" };

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            enabledSink.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Once);
            disabledSink.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Never);
        }

        private static Mock<ILoggingConfigurationManager> CreateConfigurationManagerMock(LoggingConfiguration config)
        {
            var mock = new Mock<ILoggingConfigurationManager>();
            mock.Setup(x => x.Current).Returns(config);
            return mock;
        }
    }
}

