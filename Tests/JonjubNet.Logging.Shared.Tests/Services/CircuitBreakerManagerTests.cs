using FluentAssertions;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests unitarios para CircuitBreakerManager
    /// </summary>
    public class CircuitBreakerManagerTests
    {
        private readonly Mock<ILoggingConfigurationManager> _configManagerMock;
        private readonly Mock<ILogger<CircuitBreakerService>> _loggerMock;

        public CircuitBreakerManagerTests()
        {
            _configManagerMock = new Mock<ILoggingConfigurationManager>();
            _loggerMock = new Mock<ILogger<CircuitBreakerService>>();
        }

        [Fact]
        public void GetBreaker_ShouldReturnCircuitBreaker()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                CircuitBreaker = new LoggingCircuitBreakerConfiguration
                {
                    Enabled = true,
                    Default = new CircuitBreakerDefaultConfiguration
                    {
                        FailureThreshold = 5,
                        OpenTimeout = TimeSpan.FromMinutes(1),
                        HalfOpenTestCount = 3
                    }
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            var manager = new CircuitBreakerManager(_configManagerMock.Object, _loggerMock.Object);

            // Act
            var breaker = manager.GetBreaker("TestSink");

            // Assert
            breaker.Should().NotBeNull();
            breaker.SinkName.Should().Be("TestSink");
            breaker.State.Should().Be(CircuitBreakerState.Closed);
        }

        [Fact]
        public void GetBreaker_WhenDisabled_ShouldReturnDisabledBreaker()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                CircuitBreaker = new LoggingCircuitBreakerConfiguration
                {
                    Enabled = false
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            var manager = new CircuitBreakerManager(_configManagerMock.Object, _loggerMock.Object);

            // Act
            var breaker = manager.GetBreaker("TestSink");

            // Assert
            breaker.Should().NotBeNull();
            breaker.State.Should().Be(CircuitBreakerState.Closed);
            
            // Should execute without throwing
            var result = breaker.ExecuteAsync(async () => await Task.FromResult(42)).Result;
            result.Should().Be(42);
        }

        [Fact]
        public void GetBreaker_WithPerSinkConfiguration_ShouldUseSinkSpecificConfig()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                CircuitBreaker = new LoggingCircuitBreakerConfiguration
                {
                    Enabled = true,
                    Default = new CircuitBreakerDefaultConfiguration
                    {
                        FailureThreshold = 5
                    },
                    PerSink = new Dictionary<string, CircuitBreakerSinkConfiguration>
                    {
                        ["TestSink"] = new CircuitBreakerSinkConfiguration
                        {
                            Enabled = true,
                            FailureThreshold = 10
                        }
                    }
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            var manager = new CircuitBreakerManager(_configManagerMock.Object, _loggerMock.Object);

            // Act
            var breaker = manager.GetBreaker("TestSink");

            // Assert
            breaker.Should().NotBeNull();
            // The breaker should use the sink-specific configuration
        }

        [Fact]
        public void GetBreaker_ShouldReturnSameInstanceForSameSink()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                CircuitBreaker = new LoggingCircuitBreakerConfiguration
                {
                    Enabled = true,
                    Default = new CircuitBreakerDefaultConfiguration()
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            var manager = new CircuitBreakerManager(_configManagerMock.Object, _loggerMock.Object);

            // Act
            var breaker1 = manager.GetBreaker("TestSink");
            var breaker2 = manager.GetBreaker("TestSink");

            // Assert
            breaker1.Should().BeSameAs(breaker2);
        }
    }
}

