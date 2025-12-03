using FluentAssertions;
using JonjubNet.Logging.Shared.Services;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests unitarios para LogScope
    /// </summary>
    public class LogScopeTests
    {
        [Fact]
        public void Constructor_ShouldSetProperties()
        {
            // Arrange
            var properties = new Dictionary<string, object> { { "Key1", "Value1" }, { "Key2", 123 } };

            // Act
            var scope = new LogScope(properties);

            // Assert
            scope.Properties.Should().BeEquivalentTo(properties);
        }

        [Fact]
        public void Constructor_ShouldHandleNullProperties()
        {
            // Act
            var scope = new LogScope(null!);

            // Assert
            scope.Properties.Should().NotBeNull();
        }

        [Fact]
        public void Dispose_ShouldNotThrowException()
        {
            // Arrange
            var properties = new Dictionary<string, object> { { "Key", "Value" } };
            var scope = new LogScope(properties);

            // Act
            var act = () => scope.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_ShouldBeIdempotent()
        {
            // Arrange
            var properties = new Dictionary<string, object> { { "Key", "Value" } };
            var scope = new LogScope(properties);

            // Act
            scope.Dispose();
            var act = () => scope.Dispose();

            // Assert
            act.Should().NotThrow();
        }
    }
}

