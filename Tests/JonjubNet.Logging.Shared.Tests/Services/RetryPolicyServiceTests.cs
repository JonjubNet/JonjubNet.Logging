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
    /// Tests unitarios para RetryPolicyManager y RetryPolicyService
    /// </summary>
    public class RetryPolicyServiceTests
    {
        private readonly Mock<ILoggingConfigurationManager> _configManagerMock;
        private readonly Mock<ILogger<RetryPolicyService>> _loggerMock;

        public RetryPolicyServiceTests()
        {
            _configManagerMock = new Mock<ILoggingConfigurationManager>();
            _loggerMock = new Mock<ILogger<RetryPolicyService>>();
        }

        [Fact]
        public void GetPolicy_WhenDisabled_ShouldReturnNoRetryPolicy()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                RetryPolicy = new LoggingRetryPolicyConfiguration
                {
                    Enabled = false
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            var manager = new RetryPolicyManager(_configManagerMock.Object, _loggerMock.Object);

            // Act
            var policy = manager.GetPolicy("TestSink");

            // Assert
            policy.Should().NotBeNull();
            policy.MaxRetries.Should().Be(0);
        }

        [Fact]
        public void GetPolicy_WithExponentialBackoff_ShouldReturnExponentialBackoffPolicy()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                RetryPolicy = new LoggingRetryPolicyConfiguration
                {
                    Enabled = true,
                    Default = new RetryPolicyDefaultConfiguration
                    {
                        Strategy = "ExponentialBackoff",
                        MaxRetries = 3,
                        InitialDelay = TimeSpan.FromMilliseconds(100),
                        MaxDelay = TimeSpan.FromSeconds(1),
                        BackoffMultiplier = 2.0
                    }
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            var manager = new RetryPolicyManager(_configManagerMock.Object, _loggerMock.Object);

            // Act
            var policy = manager.GetPolicy("TestSink");

            // Assert
            policy.Should().NotBeNull();
            policy.MaxRetries.Should().Be(3);
        }

        [Fact]
        public void ExecuteAsync_WithSuccess_ShouldNotRetry()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                RetryPolicy = new LoggingRetryPolicyConfiguration
                {
                    Enabled = true,
                    Default = new RetryPolicyDefaultConfiguration
                    {
                        Strategy = "ExponentialBackoff",
                        MaxRetries = 3,
                        InitialDelay = TimeSpan.FromMilliseconds(10)
                    }
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            var manager = new RetryPolicyManager(_configManagerMock.Object, _loggerMock.Object);
            var policy = manager.GetPolicy("TestSink");
            var attemptCount = 0;

            // Act
            var result = policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                return await Task.FromResult(42);
            }).Result;

            // Assert
            result.Should().Be(42);
            attemptCount.Should().Be(1);
        }

        [Fact]
        public void ExecuteAsync_WithTransientFailure_ShouldRetry()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                RetryPolicy = new LoggingRetryPolicyConfiguration
                {
                    Enabled = true,
                    Default = new RetryPolicyDefaultConfiguration
                    {
                        Strategy = "FixedDelay",
                        MaxRetries = 2,
                        InitialDelay = TimeSpan.FromMilliseconds(10)
                    }
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            var manager = new RetryPolicyManager(_configManagerMock.Object, _loggerMock.Object);
            var policy = manager.GetPolicy("TestSink");
            var attemptCount = 0;

            // Act
            try
            {
                policy.ExecuteAsync(async () =>
                {
                    attemptCount++;
                    if (attemptCount < 2)
                    {
                        throw new Exception("Transient failure");
                    }
                    return await Task.FromResult(42);
                }).Wait();
            }
            catch { }

            // Assert
            attemptCount.Should().BeGreaterOrEqualTo(2);
        }

        [Fact]
        public void ExecuteAsync_WithNonRetryableException_ShouldNotRetry()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                RetryPolicy = new LoggingRetryPolicyConfiguration
                {
                    Enabled = true,
                    Default = new RetryPolicyDefaultConfiguration
                    {
                        Strategy = "ExponentialBackoff",
                        MaxRetries = 3,
                        InitialDelay = TimeSpan.FromMilliseconds(10)
                    },
                    NonRetryableExceptions = new List<string>
                    {
                        "System.ArgumentException"
                    }
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            var manager = new RetryPolicyManager(_configManagerMock.Object, _loggerMock.Object);
            var policy = manager.GetPolicy("TestSink");
            var attemptCount = 0;

            // Act & Assert
            Func<int> act = () => policy.ExecuteAsync<int>(async () =>
            {
                attemptCount++;
                throw new ArgumentException("Non-retryable");
            }).Result;

            act.Should().Throw<ArgumentException>();
            attemptCount.Should().Be(1); // Should not retry
        }

        [Fact]
        public void ExecuteAsync_WhenRetriesExhausted_ShouldThrowRetryExhaustedException()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                RetryPolicy = new LoggingRetryPolicyConfiguration
                {
                    Enabled = true,
                    Default = new RetryPolicyDefaultConfiguration
                    {
                        Strategy = "FixedDelay",
                        MaxRetries = 2,
                        InitialDelay = TimeSpan.FromMilliseconds(10)
                    }
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            var manager = new RetryPolicyManager(_configManagerMock.Object, _loggerMock.Object);
            var policy = manager.GetPolicy("TestSink");

            // Act & Assert
            Func<int> act = () => policy.ExecuteAsync<int>(async () =>
            {
                throw new Exception("Always fails");
            }).Result;

            act.Should().Throw<RetryExhaustedException>();
        }

        [Fact]
        public void GetDelay_ShouldReturnCorrectDelay()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                RetryPolicy = new LoggingRetryPolicyConfiguration
                {
                    Enabled = true,
                    Default = new RetryPolicyDefaultConfiguration
                    {
                        Strategy = "FixedDelay",
                        MaxRetries = 3,
                        InitialDelay = TimeSpan.FromMilliseconds(100)
                    }
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            var manager = new RetryPolicyManager(_configManagerMock.Object, _loggerMock.Object);
            var policy = manager.GetPolicy("TestSink");

            // Act
            var delay1 = policy.GetDelay(1);
            var delay2 = policy.GetDelay(2);

            // Assert
            delay1.Should().BeGreaterThan(TimeSpan.Zero);
            delay2.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public void GetPolicy_WithPerSinkConfiguration_ShouldUseSinkSpecificConfig()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                RetryPolicy = new LoggingRetryPolicyConfiguration
                {
                    Enabled = true,
                    Default = new RetryPolicyDefaultConfiguration
                    {
                        Strategy = "ExponentialBackoff",
                        MaxRetries = 3
                    },
                    PerSink = new Dictionary<string, RetryPolicySinkConfiguration>
                    {
                        ["TestSink"] = new RetryPolicySinkConfiguration
                        {
                            Enabled = true,
                            Strategy = "FixedDelay",
                            MaxRetries = 5
                        }
                    }
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            var manager = new RetryPolicyManager(_configManagerMock.Object, _loggerMock.Object);

            // Act
            var policy = manager.GetPolicy("TestSink");

            // Assert
            policy.Should().NotBeNull();
            policy.MaxRetries.Should().Be(5);
        }
    }
}

