using FluentAssertions;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Shared.Services.Sinks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services.Sinks
{
    /// <summary>
    /// Tests unitarios para ConsoleLogSink
    /// </summary>
    public class ConsoleLogSinkTests
    {
        [Fact]
        public void SendAsync_ShouldWriteToConsole()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                Enabled = true,
                Sinks = new LoggingSinksConfiguration { EnableConsole = true }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<ConsoleLogSink>>();
            var sink = new ConsoleLogSink(configManagerMock.Object, loggerMock.Object);
            var logEntry = new StructuredLogEntry { Message = "Test", LogLevel = "Information" };

            // Act
            var task = sink.SendAsync(logEntry);

            // Assert
            task.Should().NotBeNull();
            task.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public void IsEnabled_ShouldReturnFalse_WhenDisabled()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                Enabled = false,
                Sinks = new LoggingSinksConfiguration { EnableConsole = true }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<ConsoleLogSink>>();
            var sink = new ConsoleLogSink(configManagerMock.Object, loggerMock.Object);

            // Act & Assert
            sink.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void IsEnabled_ShouldReturnFalse_WhenConsoleDisabled()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                Enabled = true,
                Sinks = new LoggingSinksConfiguration { EnableConsole = false }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<ConsoleLogSink>>();
            var sink = new ConsoleLogSink(configManagerMock.Object, loggerMock.Object);

            // Act & Assert
            sink.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void Name_ShouldReturnConsole()
        {
            // Arrange
            var config = new LoggingConfiguration();
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<ConsoleLogSink>>();
            var sink = new ConsoleLogSink(configManagerMock.Object, loggerMock.Object);

            // Act & Assert
            sink.Name.Should().Be("Console");
        }

        private static Mock<ILoggingConfigurationManager> CreateConfigurationManagerMock(LoggingConfiguration config)
        {
            var mock = new Mock<ILoggingConfigurationManager>();
            mock.Setup(x => x.Current).Returns(config);
            return mock;
        }
    }
}

