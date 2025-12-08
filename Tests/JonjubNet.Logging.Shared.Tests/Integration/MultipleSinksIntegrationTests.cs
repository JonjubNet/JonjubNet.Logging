using FluentAssertions;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Domain.ValueObjects;
using JonjubNet.Logging.Shared.Services;
using JonjubNet.Logging.Shared.Services.Sinks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Integration
{
    /// <summary>
    /// Tests de integración para múltiples sinks
    /// Verifica que los logs se envíen correctamente a todos los sinks habilitados
    /// </summary>
    public class MultipleSinksIntegrationTests
    {
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<ILoggingConfigurationManager> _configManagerMock;
        private readonly LoggingConfiguration _defaultConfiguration;

        public MultipleSinksIntegrationTests()
        {
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _loggerFactoryMock
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(new Mock<ILogger>().Object);

            _defaultConfiguration = CreateDefaultConfiguration();
            _configManagerMock = CreateConfigurationManagerMock(_defaultConfiguration);
        }

        [Fact]
        public async Task SendLogUseCase_ShouldSendToAllEnabledSinks_WhenMultipleSinksAreConfigured()
        {
            // Arrange
            var consoleSink = new Mock<ILogSink>();
            consoleSink.Setup(x => x.IsEnabled).Returns(true);
            consoleSink.Setup(x => x.Name).Returns("Console");
            consoleSink.Setup(x => x.SendAsync(It.IsAny<StructuredLogEntry>())).Returns(Task.CompletedTask);

            var fileSink = new Mock<ILogSink>();
            fileSink.Setup(x => x.IsEnabled).Returns(true);
            fileSink.Setup(x => x.Name).Returns("File");
            fileSink.Setup(x => x.SendAsync(It.IsAny<StructuredLogEntry>())).Returns(Task.CompletedTask);

            var httpSink = new Mock<ILogSink>();
            httpSink.Setup(x => x.IsEnabled).Returns(false); // Deshabilitado
            httpSink.Setup(x => x.Name).Returns("HTTP");

            var sinks = new[] { consoleSink.Object, fileSink.Object, httpSink.Object };

            var loggerMock = new Mock<ILogger<SendLogUseCase>>();
            var useCase = new SendLogUseCase(
                loggerMock.Object,
                _configManagerMock.Object,
                sinks);

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            consoleSink.Verify(x => x.SendAsync(logEntry), Times.Once);
            fileSink.Verify(x => x.SendAsync(logEntry), Times.Once);
            httpSink.Verify(x => x.SendAsync(It.IsAny<StructuredLogEntry>()), Times.Never);
        }

        [Fact]
        public async Task SendLogUseCase_ShouldContinueSending_WhenOneSinkFails()
        {
            // Arrange
            var consoleSink = new Mock<ILogSink>();
            consoleSink.Setup(x => x.IsEnabled).Returns(true);
            consoleSink.Setup(x => x.Name).Returns("Console");
            consoleSink.Setup(x => x.SendAsync(It.IsAny<StructuredLogEntry>()))
                .ThrowsAsync(new InvalidOperationException("Sink error"));

            var fileSink = new Mock<ILogSink>();
            fileSink.Setup(x => x.IsEnabled).Returns(true);
            fileSink.Setup(x => x.Name).Returns("File");
            fileSink.Setup(x => x.SendAsync(It.IsAny<StructuredLogEntry>())).Returns(Task.CompletedTask);

            var sinks = new[] { consoleSink.Object, fileSink.Object };

            var loggerMock = new Mock<ILogger<SendLogUseCase>>();
            var useCase = new SendLogUseCase(
                loggerMock.Object,
                _configManagerMock.Object,
                sinks);

            var logEntry = CreateBasicLogEntry();

            // Act
            await useCase.ExecuteAsync(logEntry);

            // Assert
            consoleSink.Verify(x => x.SendAsync(logEntry), Times.Once);
            fileSink.Verify(x => x.SendAsync(logEntry), Times.Once); // Debe continuar aunque el primero falle
        }

        [Fact]
        public async Task SendLogUseCase_ShouldProcessSinksInParallel_ForBetterPerformance()
        {
            // Arrange
            var delays = new List<DateTime>();
            var consoleSink = new Mock<ILogSink>();
            consoleSink.Setup(x => x.IsEnabled).Returns(true);
            consoleSink.Setup(x => x.Name).Returns("Console");
            consoleSink.Setup(x => x.SendAsync(It.IsAny<StructuredLogEntry>()))
                .Returns(async () =>
                {
                    delays.Add(DateTime.UtcNow);
                    await Task.Delay(100);
                });

            var fileSink = new Mock<ILogSink>();
            fileSink.Setup(x => x.IsEnabled).Returns(true);
            fileSink.Setup(x => x.Name).Returns("File");
            fileSink.Setup(x => x.SendAsync(It.IsAny<StructuredLogEntry>()))
                .Returns(async () =>
                {
                    delays.Add(DateTime.UtcNow);
                    await Task.Delay(100);
                });

            var sinks = new[] { consoleSink.Object, fileSink.Object };

            var loggerMock = new Mock<ILogger<SendLogUseCase>>();
            var useCase = new SendLogUseCase(
                loggerMock.Object,
                _configManagerMock.Object,
                sinks);

            var logEntry = CreateBasicLogEntry();
            var startTime = DateTime.UtcNow;

            // Act
            await useCase.ExecuteAsync(logEntry);
            var endTime = DateTime.UtcNow;
            var totalTime = (endTime - startTime).TotalMilliseconds;

            // Assert
            delays.Should().HaveCount(2);
            // Si se procesan en paralelo, el tiempo total debería ser ~100ms, no ~200ms
            totalTime.Should().BeLessThan(150); // Con margen para overhead
        }

        private static StructuredLogEntry CreateBasicLogEntry()
        {
            return new StructuredLogEntry(
                LogLevelValue.Information,
                LogCategoryValue.Application,
                "Test message",
                "TestOperation",
                DateTime.UtcNow);
        }

        private static LoggingConfiguration CreateDefaultConfiguration()
        {
            return new LoggingConfiguration
            {
                Enabled = true,
                MinimumLevel = "Information"
            };
        }

        private static Mock<ILoggingConfigurationManager> CreateConfigurationManagerMock(LoggingConfiguration config)
        {
            var mock = new Mock<ILoggingConfigurationManager>();
            mock.Setup(x => x.Current).Returns(config);
            mock.Setup(x => x.GetCurrent()).Returns(config);
            return mock;
        }
    }
}

