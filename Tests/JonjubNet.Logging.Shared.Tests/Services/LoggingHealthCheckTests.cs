using FluentAssertions;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Shared.Services;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests unitarios para LoggingHealthCheck
    /// </summary>
    public class LoggingHealthCheckTests
    {
        [Fact]
        public void IsHealthy_ShouldReturnTrue_WhenNoQueue()
        {
            // Arrange
            var healthCheck = new LoggingHealthCheck(null);

            // Act
            var result = healthCheck.IsHealthy();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetQueueStatus_ShouldReturnZero_WhenNoQueue()
        {
            // Arrange
            var healthCheck = new LoggingHealthCheck(null);

            // Act
            var status = healthCheck.GetQueueStatus();

            // Assert
            status.CurrentCount.Should().Be(0);
            status.Capacity.Should().Be(0);
        }

        [Fact]
        public void GetQueueStatus_ShouldReturnQueueInfo_WhenQueueExists()
        {
            // Arrange
            var queueMock = new Mock<ILogQueue>();
            queueMock.Setup(x => x.Count).Returns(100);
            queueMock.Setup(x => x.Capacity).Returns(10000);
            var healthCheck = new LoggingHealthCheck(queueMock.Object);

            // Act
            var status = healthCheck.GetQueueStatus();

            // Assert
            status.CurrentCount.Should().Be(100);
            status.Capacity.Should().Be(10000);
        }
    }
}

