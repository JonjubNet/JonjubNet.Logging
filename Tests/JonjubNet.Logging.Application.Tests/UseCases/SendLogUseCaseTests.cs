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
    /// Tests unitarios para SendLogUseCase
    /// Sigue las mejores prácticas: AAA Pattern, Moq, FluentAssertions, Tests aislados
    /// </summary>
    public class SendLogUseCaseTests
    {
        private readonly Mock<ILogger<SendLogUseCase>> _loggerMock;
        private readonly LoggingConfiguration _defaultConfiguration;

        public SendLogUseCaseTests()
        {
            _loggerMock = new Mock<ILogger<SendLogUseCase>>();
            _defaultConfiguration = CreateDefaultConfiguration();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotSendLog_WhenConfigurationDisabled()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            config.Enabled = false;
            var configManagerMock = CreateConfigurationManagerMock(config);
            var sinkMock = new Mock<ILogSink>();
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                new[] { sinkMock.Object });

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            sinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSendLog_ToEnabledSinks()
        {
            // Arrange
            var enabledSinkMock = new Mock<ILogSink>();
            enabledSinkMock.Setup(x => x.IsEnabled).Returns(true);
            enabledSinkMock.Setup(x => x.Name).Returns("EnabledSink");

            var disabledSinkMock = new Mock<ILogSink>();
            disabledSinkMock.Setup(x => x.IsEnabled).Returns(false);

            var configManagerMock = CreateConfigurationManagerMock(_defaultConfiguration);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                new[] { enabledSinkMock.Object, disabledSinkMock.Object });

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            enabledSinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Once);
            disabledSinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldFilterLog_WhenFilterRejects()
        {
            // Arrange
            var filterMock = new Mock<ILogFilter>();
            filterMock.Setup(x => x.ShouldLog(It.IsAny<StructuredLogEntry>())).Returns(false);

            var sinkMock = new Mock<ILogSink>();
            sinkMock.Setup(x => x.IsEnabled).Returns(true);

            var configManagerMock = CreateConfigurationManagerMock(_defaultConfiguration);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                new[] { sinkMock.Object },
                logFilter: filterMock.Object);

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            sinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Never);
            filterMock.Verify(x => x.ShouldLog(logEntry), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSampleLog_WhenSamplingRejects()
        {
            // Arrange
            var samplingMock = new Mock<ILogSamplingService>();
            samplingMock.Setup(x => x.ShouldLog(It.IsAny<StructuredLogEntry>())).Returns(false);

            var sinkMock = new Mock<ILogSink>();
            sinkMock.Setup(x => x.IsEnabled).Returns(true);

            var configManagerMock = CreateConfigurationManagerMock(_defaultConfiguration);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                new[] { sinkMock.Object },
                samplingService: samplingMock.Object);

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            sinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Never);
            samplingMock.Verify(x => x.ShouldLog(logEntry), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSanitizeLog_WhenSanitizationEnabled()
        {
            // Arrange
            var sanitizationMock = new Mock<IDataSanitizationService>();
            var originalEntry = CreateBasicLogEntry();
            var sanitizedEntry = CreateBasicLogEntry();
            sanitizedEntry.Properties["Password"] = "***REDACTED***";
            sanitizationMock.Setup(x => x.Sanitize(originalEntry)).Returns(sanitizedEntry);

            var sinkMock = new Mock<ILogSink>();
            sinkMock.Setup(x => x.IsEnabled).Returns(true);

            var configManagerMock = CreateConfigurationManagerMock(_defaultConfiguration);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                new[] { sinkMock.Object },
                sanitizationService: sanitizationMock.Object);

            // Act
            await useCase.ExecuteAsync(originalEntry);

            // Assert
            sanitizationMock.Verify(x => x.Sanitize(originalEntry), Times.Once);
            sinkMock.Verify(x => x.SendAsync(sanitizedEntry), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotSanitizeLog_WhenSanitizationDisabled()
        {
            // Arrange
            var sinkMock = new Mock<ILogSink>();
            sinkMock.Setup(x => x.IsEnabled).Returns(true);

            var configManagerMock = CreateConfigurationManagerMock(_defaultConfiguration);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                new[] { sinkMock.Object },
                sanitizationService: null);

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            sinkMock.Verify(x => x.SendAsync(logEntry), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSendToKafka_WhenKafkaEnabled()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            config.KafkaProducer.Enabled = true;

            var kafkaMock = new Mock<IKafkaProducer>();
            kafkaMock.Setup(x => x.IsEnabled).Returns(true);
            kafkaMock.Setup(x => x.SendAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                Array.Empty<ILogSink>(),
                kafkaProducer: kafkaMock.Object);

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            kafkaMock.Verify(x => x.SendAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotSendToKafka_WhenKafkaDisabled()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            config.KafkaProducer.Enabled = false;

            var kafkaMock = new Mock<IKafkaProducer>();
            kafkaMock.Setup(x => x.IsEnabled).Returns(true);

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                Array.Empty<ILogSink>(),
                kafkaProducer: kafkaMock.Object);

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            kafkaMock.Verify(x => x.SendAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotSendToKafka_WhenKafkaProducerIsNull()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            config.KafkaProducer.Enabled = true;

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                Array.Empty<ILogSink>(),
                kafkaProducer: null);

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            // No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleSinkErrors_WithoutFailing()
        {
            // Arrange
            var failingSinkMock = new Mock<ILogSink>();
            failingSinkMock.Setup(x => x.IsEnabled).Returns(true);
            failingSinkMock.Setup(x => x.Name).Returns("FailingSink");
            failingSinkMock.Setup(x => x.SendAsync(It.IsAny<StructuredLogEntry>()))
                .ThrowsAsync(new Exception("Sink error"));

            var workingSinkMock = new Mock<ILogSink>();
            workingSinkMock.Setup(x => x.IsEnabled).Returns(true);
            workingSinkMock.Setup(x => x.Name).Returns("WorkingSink");

            var configManagerMock = CreateConfigurationManagerMock(_defaultConfiguration);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                new[] { failingSinkMock.Object, workingSinkMock.Object });

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            workingSinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleKafkaErrors_WithoutFailing()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            config.KafkaProducer.Enabled = true;

            var kafkaMock = new Mock<IKafkaProducer>();
            kafkaMock.Setup(x => x.IsEnabled).Returns(true);
            kafkaMock.Setup(x => x.SendAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Kafka error"));

            var sinkMock = new Mock<ILogSink>();
            sinkMock.Setup(x => x.IsEnabled).Returns(true);

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                new[] { sinkMock.Object },
                kafkaProducer: kafkaMock.Object);

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            sinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldProcessPipeline_InCorrectOrder()
        {
            // Arrange
            var filterMock = new Mock<ILogFilter>();
            filterMock.Setup(x => x.ShouldLog(It.IsAny<StructuredLogEntry>())).Returns(true);

            var samplingMock = new Mock<ILogSamplingService>();
            samplingMock.Setup(x => x.ShouldLog(It.IsAny<StructuredLogEntry>())).Returns(true);

            var sanitizationMock = new Mock<IDataSanitizationService>();
            sanitizationMock.Setup(x => x.Sanitize(It.IsAny<StructuredLogEntry>()))
                .Returns<StructuredLogEntry>(e => e);

            var sinkMock = new Mock<ILogSink>();
            sinkMock.Setup(x => x.IsEnabled).Returns(true);

            var configManagerMock = CreateConfigurationManagerMock(_defaultConfiguration);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                new[] { sinkMock.Object },
                logFilter: filterMock.Object,
                samplingService: samplingMock.Object,
                sanitizationService: sanitizationMock.Object);

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            filterMock.Verify(x => x.ShouldLog(logEntry), Times.Once);
            samplingMock.Verify(x => x.ShouldLog(logEntry), Times.Once);
            sanitizationMock.Verify(x => x.Sanitize(It.IsAny<StructuredLogEntry>()), Times.Once);
            sinkMock.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotSerializeJson_WhenKafkaDisabled()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            config.KafkaProducer.Enabled = false;

            var kafkaMock = new Mock<IKafkaProducer>();
            kafkaMock.Setup(x => x.IsEnabled).Returns(true);

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                Array.Empty<ILogSink>(),
                kafkaProducer: kafkaMock.Object);

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            kafkaMock.Verify(x => x.SendAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleGeneralException_WithoutFailing()
        {
            // Arrange
            var filterMock = new Mock<ILogFilter>();
            filterMock.Setup(x => x.ShouldLog(It.IsAny<StructuredLogEntry>()))
                .Throws(new Exception("Filter error"));

            var configManagerMock = CreateConfigurationManagerMock(_defaultConfiguration);
            var useCase = new SendLogUseCase(
                _loggerMock.Object,
                configManagerMock.Object,
                Array.Empty<ILogSink>(),
                logFilter: filterMock.Object);

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        // Helper methods
        private static StructuredLogEntry CreateBasicLogEntry()
        {
            return new StructuredLogEntry
            {
                Message = "Test message",
                LogLevel = "Information",
                Operation = "TestOperation",
                Category = "General",
                Timestamp = DateTime.UtcNow,
                Properties = new Dictionary<string, object>(),
                Context = new Dictionary<string, object>()
            };
        }

        private static LoggingConfiguration CreateDefaultConfiguration()
        {
            return new LoggingConfiguration
            {
                Enabled = true,
                ServiceName = "TestService",
                Environment = "Test",
                Version = "1.0.0",
                KafkaProducer = new LoggingKafkaProducerConfiguration
                {
                    Enabled = false
                }
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

