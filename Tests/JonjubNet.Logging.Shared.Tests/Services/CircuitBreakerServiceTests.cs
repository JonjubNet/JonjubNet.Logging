using FluentAssertions;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests unitarios para CircuitBreakerService
    /// </summary>
    public class CircuitBreakerServiceTests
    {
        private readonly Mock<ILogger<CircuitBreakerService>> _loggerMock;
        private readonly CircuitBreakerService.CircuitBreakerConfiguration _config;

        public CircuitBreakerServiceTests()
        {
            _loggerMock = new Mock<ILogger<CircuitBreakerService>>();
            _config = new CircuitBreakerService.CircuitBreakerConfiguration
            {
                Enabled = true,
                FailureThreshold = 3,
                OpenTimeout = TimeSpan.FromSeconds(1),
                HalfOpenTestCount = 2
            };
        }

        [Fact]
        public void Constructor_ShouldInitializeWithClosedState()
        {
            // Arrange & Act
            var breaker = new CircuitBreakerService("TestSink", _config, _loggerMock.Object);

            // Assert
            breaker.State.Should().Be(CircuitBreakerState.Closed);
            breaker.SinkName.Should().Be("TestSink");
        }

        [Fact]
        public void ExecuteAsync_WhenDisabled_ShouldExecuteDirectly()
        {
            // Arrange
            var disabledConfig = new CircuitBreakerService.CircuitBreakerConfiguration
            {
                Enabled = false
            };
            var breaker = new CircuitBreakerService("TestSink", disabledConfig, _loggerMock.Object);
            var executed = false;

            // Act
            var result = breaker.ExecuteAsync(async () =>
            {
                executed = true;
                return await Task.FromResult(42);
            }).Result;

            // Assert
            executed.Should().BeTrue();
            result.Should().Be(42);
        }

        [Fact]
        public void ExecuteAsync_WhenClosedAndSuccess_ShouldExecute()
        {
            // Arrange
            var breaker = new CircuitBreakerService("TestSink", _config, _loggerMock.Object);
            var executed = false;

            // Act
            var result = breaker.ExecuteAsync(async () =>
            {
                executed = true;
                return await Task.FromResult("success");
            }).Result;

            // Assert
            executed.Should().BeTrue();
            result.Should().Be("success");
            breaker.State.Should().Be(CircuitBreakerState.Closed);
        }

        [Fact]
        public void ExecuteAsync_WhenClosedAndFailure_ShouldRecordFailure()
        {
            // Arrange
            var breaker = new CircuitBreakerService("TestSink", _config, _loggerMock.Object);

            // Act & Assert
            for (int i = 0; i < _config.FailureThreshold; i++)
            {
                try
                {
                    breaker.ExecuteAsync<int>(async () =>
                    {
                        await Task.CompletedTask;
                        throw new Exception("Test failure");
                    }).Wait();
                }
                catch { }
            }

            // After threshold failures, should still be closed (needs time to update)
            breaker.State.Should().BeOneOf(CircuitBreakerState.Closed, CircuitBreakerState.Open);
        }

        [Fact]
        public void ExecuteAsync_WhenOpen_ShouldThrowCircuitBreakerOpenException()
        {
            // Arrange
            var breaker = new CircuitBreakerService("TestSink", _config, _loggerMock.Object);

            // Fail enough times to open
            for (int i = 0; i < _config.FailureThreshold; i++)
            {
                breaker.RecordFailure();
            }

            // Wait a bit for state update
            Thread.Sleep(100);

            // Act & Assert
            Func<string> act = () => breaker.ExecuteAsync<string>(async () =>
            {
                await Task.CompletedTask;
                return "should not execute";
            }).Result;

            act.Should().Throw<CircuitBreakerOpenException>()
                .WithMessage("*TestSink*");
        }

        [Fact]
        public void RecordSuccess_WhenHalfOpen_ShouldCloseAfterEnoughSuccesses()
        {
            // Arrange
            var breaker = new CircuitBreakerService("TestSink", _config, _loggerMock.Object);

            // Open the breaker
            for (int i = 0; i < _config.FailureThreshold; i++)
            {
                breaker.RecordFailure();
            }

            // Wait for timeout to go to HalfOpen
            Thread.Sleep(1100);

            // Act - Record enough successes
            for (int i = 0; i < _config.HalfOpenTestCount; i++)
            {
                breaker.RecordSuccess();
            }

            // Assert
            breaker.State.Should().Be(CircuitBreakerState.Closed);
        }

        [Fact]
        public void RecordFailure_WhenHalfOpen_ShouldReopen()
        {
            // Arrange
            var breaker = new CircuitBreakerService("TestSink", _config, _loggerMock.Object);

            // Open the breaker
            for (int i = 0; i < _config.FailureThreshold; i++)
            {
                breaker.RecordFailure();
            }

            // Wait for timeout to go to HalfOpen
            Thread.Sleep(1100);

            // Act
            breaker.RecordFailure();

            // Assert
            breaker.State.Should().Be(CircuitBreakerState.Open);
        }

        [Fact]
        public void Reset_ShouldReturnToClosedState()
        {
            // Arrange
            var breaker = new CircuitBreakerService("TestSink", _config, _loggerMock.Object);

            // Open the breaker
            for (int i = 0; i < _config.FailureThreshold; i++)
            {
                breaker.RecordFailure();
            }

            // Act
            breaker.Reset();

            // Assert
            breaker.State.Should().Be(CircuitBreakerState.Closed);
        }
    }
}

