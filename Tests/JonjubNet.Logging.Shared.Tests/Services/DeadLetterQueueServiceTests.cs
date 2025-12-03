using FluentAssertions;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests unitarios para DeadLetterQueueService
    /// </summary>
    public class DeadLetterQueueServiceTests
    {
        private readonly Mock<ILoggingConfigurationManager> _configManagerMock;
        private readonly Mock<ILogger<DeadLetterQueueService>> _loggerMock;

        public DeadLetterQueueServiceTests()
        {
            _configManagerMock = new Mock<ILoggingConfigurationManager>();
            _loggerMock = new Mock<ILogger<DeadLetterQueueService>>();
        }

        private DeadLetterQueueService CreateService(LoggingConfiguration? config = null)
        {
            config ??= new LoggingConfiguration
            {
                DeadLetterQueue = new LoggingDeadLetterQueueConfiguration
                {
                    Enabled = true,
                    MaxSize = 100,
                    AutoRetry = false
                }
            };
            _configManagerMock.Setup(m => m.Current).Returns(config);
            return new DeadLetterQueueService(_configManagerMock.Object, _loggerMock.Object);
        }

        private StructuredLogEntry CreateTestLogEntry()
        {
            return new StructuredLogEntry
            {
                Message = "Test message",
                LogLevel = "Information",
                Category = "General",
                Operation = "TestOperation"
            };
        }

        [Fact]
        public void EnqueueAsync_WhenEnabled_ShouldAddItem()
        {
            // Arrange
            var service = CreateService();
            var logEntry = CreateTestLogEntry();

            // Act
            service.EnqueueAsync(logEntry, "TestSink", "Test failure").Wait();

            // Assert
            var count = service.GetCountAsync().Result;
            count.Should().Be(1);
        }

        [Fact]
        public void EnqueueAsync_WhenDisabled_ShouldNotAddItem()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                DeadLetterQueue = new LoggingDeadLetterQueueConfiguration
                {
                    Enabled = false
                }
            };
            var service = CreateService(config);
            var logEntry = CreateTestLogEntry();

            // Act
            service.EnqueueAsync(logEntry, "TestSink", "Test failure").Wait();

            // Assert
            var count = service.GetCountAsync().Result;
            count.Should().Be(0);
        }

        [Fact]
        public void EnqueueAsync_WhenMaxSizeReached_ShouldRemoveOldest()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                DeadLetterQueue = new LoggingDeadLetterQueueConfiguration
                {
                    Enabled = true,
                    MaxSize = 2,
                    AutoRetry = false
                }
            };
            var service = CreateService(config);

            // Act
            service.EnqueueAsync(CreateTestLogEntry(), "Sink1", "Failure1").Wait();
            service.EnqueueAsync(CreateTestLogEntry(), "Sink2", "Failure2").Wait();
            service.EnqueueAsync(CreateTestLogEntry(), "Sink3", "Failure3").Wait();

            // Assert
            var count = service.GetCountAsync().Result;
            count.Should().BeLessOrEqualTo(2);
        }

        [Fact]
        public void GetFailedLogsAsync_ShouldReturnItems()
        {
            // Arrange
            var service = CreateService();
            var logEntry = CreateTestLogEntry();
            service.EnqueueAsync(logEntry, "TestSink", "Test failure").Wait();

            // Act
            var items = service.GetFailedLogsAsync().Result;

            // Assert
            items.Should().NotBeNull();
            items.Should().HaveCount(1);
            var item = items.First();
            item.SinkName.Should().Be("TestSink");
            item.FailureReason.Should().Be("Test failure");
        }

        [Fact]
        public void GetFailedLogsAsync_WithMaxCount_ShouldLimitResults()
        {
            // Arrange
            var service = CreateService();
            for (int i = 0; i < 5; i++)
            {
                service.EnqueueAsync(CreateTestLogEntry(), $"Sink{i}", $"Failure{i}").Wait();
            }

            // Act
            var items = service.GetFailedLogsAsync(maxCount: 3).Result;

            // Assert
            items.Should().HaveCount(3);
        }

        [Fact]
        public void RetryAsync_ShouldIncrementRetryCount()
        {
            // Arrange
            var service = CreateService();
            var logEntry = CreateTestLogEntry();
            service.EnqueueAsync(logEntry, "TestSink", "Test failure").Wait();
            var items = service.GetFailedLogsAsync().Result;
            var itemId = items.First().Id;

            // Act
            var result = service.RetryAsync(itemId).Result;

            // Assert
            result.Should().BeTrue();
            var updatedItems = service.GetFailedLogsAsync().Result;
            var updatedItem = updatedItems.First(i => i.Id == itemId);
            updatedItem.RetryCount.Should().Be(1);
            updatedItem.LastRetryAt.Should().NotBeNull();
        }

        [Fact]
        public void RetryAsync_WhenMaxRetriesExceeded_ShouldReturnFalse()
        {
            // Arrange
            var config = new LoggingConfiguration
            {
                DeadLetterQueue = new LoggingDeadLetterQueueConfiguration
                {
                    Enabled = true,
                    MaxRetriesPerItem = 2,
                    AutoRetry = false
                }
            };
            var service = CreateService(config);
            var logEntry = CreateTestLogEntry();
            service.EnqueueAsync(logEntry, "TestSink", "Test failure").Wait();
            var items = service.GetFailedLogsAsync().Result;
            var itemId = items.First().Id;

            // Retry until max
            service.RetryAsync(itemId).Wait();
            service.RetryAsync(itemId).Wait();

            // Act
            var result = service.RetryAsync(itemId).Result;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void RetryAllAsync_ShouldRetryAllItems()
        {
            // Arrange
            var service = CreateService();
            service.EnqueueAsync(CreateTestLogEntry(), "Sink1", "Failure1").Wait();
            service.EnqueueAsync(CreateTestLogEntry(), "Sink2", "Failure2").Wait();

            // Act
            var result = service.RetryAllAsync().Result;

            // Assert
            result.Should().BeTrue();
            var items = service.GetFailedLogsAsync().Result;
            items.All(i => i.RetryCount > 0).Should().BeTrue();
        }

        [Fact]
        public void RetryAllAsync_WithSinkName_ShouldRetryOnlyMatchingSinks()
        {
            // Arrange
            var service = CreateService();
            service.EnqueueAsync(CreateTestLogEntry(), "Sink1", "Failure1").Wait();
            service.EnqueueAsync(CreateTestLogEntry(), "Sink2", "Failure2").Wait();

            // Act
            var result = service.RetryAllAsync("Sink1").Result;

            // Assert
            result.Should().BeTrue();
            var items = service.GetFailedLogsAsync().Result;
            items.First(i => i.SinkName == "Sink1").RetryCount.Should().BeGreaterThan(0);
            items.First(i => i.SinkName == "Sink2").RetryCount.Should().Be(0);
        }

        [Fact]
        public void DeleteAsync_ShouldRemoveItem()
        {
            // Arrange
            var service = CreateService();
            var logEntry = CreateTestLogEntry();
            service.EnqueueAsync(logEntry, "TestSink", "Test failure").Wait();
            var items = service.GetFailedLogsAsync().Result;
            var itemId = items.First().Id;

            // Act
            var result = service.DeleteAsync(itemId).Result;

            // Assert
            result.Should().BeTrue();
            var count = service.GetCountAsync().Result;
            count.Should().Be(0);
        }

        [Fact]
        public void GetCountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var service = CreateService();
            service.EnqueueAsync(CreateTestLogEntry(), "Sink1", "Failure1").Wait();
            service.EnqueueAsync(CreateTestLogEntry(), "Sink2", "Failure2").Wait();

            // Act
            var totalCount = service.GetCountAsync().Result;
            var sinkCount = service.GetCountAsync("Sink1").Result;

            // Assert
            totalCount.Should().Be(2);
            sinkCount.Should().Be(1);
        }

        [Fact]
        public void GetMetrics_ShouldReturnCorrectMetrics()
        {
            // Arrange
            var service = CreateService();
            service.EnqueueAsync(CreateTestLogEntry(), "Sink1", "Failure1").Wait();
            service.EnqueueAsync(CreateTestLogEntry(), "Sink2", "Failure2").Wait();

            // Act
            var metrics = service.GetMetrics();

            // Assert
            metrics.Should().NotBeNull();
            metrics.TotalItems.Should().Be(2);
            metrics.ItemsBySinkName.Should().ContainKey("Sink1");
            metrics.ItemsBySinkName.Should().ContainKey("Sink2");
            metrics.OldestItemDate.Should().NotBeNull();
            metrics.NewestItemDate.Should().NotBeNull();
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            var service = CreateService();
            service.EnqueueAsync(CreateTestLogEntry(), "TestSink", "Test failure").Wait();

            // Act
            service.Dispose();

            // Assert - Should not throw
            service.Should().NotBeNull();
        }
    }
}

