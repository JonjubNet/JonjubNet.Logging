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
    /// Tests unitarios para SerilogSink
    /// </summary>
    public class SerilogSinkTests
    {
        [Fact]
        public async Task SendAsync_ShouldNotThrow_WhenExceptionOccurs()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                Enabled = true,
                Sinks = new LoggingSinksConfiguration
                {
                    EnableConsole = true,
                    EnableFile = true
                }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<SerilogSink>>();
            var sink = new SerilogSink(configManagerMock.Object, loggerMock.Object);
            var logEntry = new StructuredLogEntry 
            { 
                Message = "Test", 
                LogLevel = "Information",
                Exception = new Exception("Test exception")
            };

            // Act
            var act = async () => await sink.SendAsync(logEntry);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public void IsEnabled_ShouldReturnFalse_WhenDisabled()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                Enabled = false,
                Sinks = new LoggingSinksConfiguration
                {
                    EnableConsole = true
                }
            };
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<SerilogSink>>();
            var sink = new SerilogSink(configManagerMock.Object, loggerMock.Object);

            // Act & Assert
            sink.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void Name_ShouldReturnSerilog()
        {
            // Arrange
            var config = new LoggingConfiguration();
            var configManagerMock = CreateConfigurationManagerMock(config);
            var loggerMock = new Mock<ILogger<SerilogSink>>();
            var sink = new SerilogSink(configManagerMock.Object, loggerMock.Object);

            // Act & Assert
            sink.Name.Should().Be("Serilog");
        }

        private static Mock<ILoggingConfigurationManager> CreateConfigurationManagerMock(LoggingConfiguration config)
        {
            var mock = new Mock<ILoggingConfigurationManager>();
            mock.Setup(x => x.Current).Returns(config);
            return mock;
        }
    }
}

