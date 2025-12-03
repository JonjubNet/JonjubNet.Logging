using FluentAssertions;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Shared.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests unitarios para LogQueue
    /// </summary>
    public class LogQueueTests
    {
        [Fact]
        public void TryEnqueue_ShouldReturnTrue_WhenQueueHasSpace()
        {
            // Arrange
            var config = Options.Create(new LoggingConfiguration());
            var queue = new LogQueue(config);
            var logEntry = new StructuredLogEntry { Message = "Test" };

            // Act
            var result = queue.TryEnqueue(logEntry);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void TryEnqueue_ShouldEnqueueMultipleEntries()
        {
            // Arrange
            var config = Options.Create(new LoggingConfiguration());
            var queue = new LogQueue(config);
            var logEntry1 = new StructuredLogEntry { Message = "Test1" };
            var logEntry2 = new StructuredLogEntry { Message = "Test2" };

            // Act
            var result1 = queue.TryEnqueue(logEntry1);
            var result2 = queue.TryEnqueue(logEntry2);

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
        }

        [Fact]
        public void Reader_ShouldNotBeNull()
        {
            // Arrange
            var config = Options.Create(new LoggingConfiguration());
            var queue = new LogQueue(config);

            // Act & Assert
            queue.Reader.Should().NotBeNull();
        }

        [Fact]
        public void Capacity_ShouldBeSet()
        {
            // Arrange
            var config = Options.Create(new LoggingConfiguration());
            var queue = new LogQueue(config);

            // Act & Assert
            queue.Capacity.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Complete_ShouldCompleteWriter()
        {
            // Arrange
            var config = Options.Create(new LoggingConfiguration());
            var queue = new LogQueue(config);

            // Act
            queue.Complete();

            // Assert - No exception should be thrown
            queue.Reader.Completion.IsCompleted.Should().BeTrue();
        }
    }
}

