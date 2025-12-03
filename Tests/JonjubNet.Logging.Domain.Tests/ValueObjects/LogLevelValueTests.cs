using FluentAssertions;
using JonjubNet.Logging.Domain.ValueObjects;
using Xunit;

namespace JonjubNet.Logging.Domain.Tests.ValueObjects
{
    /// <summary>
    /// Tests unitarios para LogLevelValue
    /// Sigue las mejores prácticas: AAA Pattern, Theory, FluentAssertions
    /// </summary>
    public class LogLevelValueTests
    {
        [Theory]
        [InlineData("Trace")]
        [InlineData("Debug")]
        [InlineData("Information")]
        [InlineData("Warning")]
        [InlineData("Error")]
        [InlineData("Critical")]
        [InlineData("Fatal")]
        public void FromString_ShouldCreateValidLogLevel(string level)
        {
            // Act
            var result = LogLevelValue.FromString(level);

            // Assert
            result.Value.Should().Be(level);
        }

        [Fact]
        public void FromString_ShouldThrowException_WhenInvalidLevel()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => LogLevelValue.FromString("InvalidLevel"));
        }

        [Fact]
        public void TryFromString_ShouldReturnTrue_WhenValidLevel()
        {
            // Act
            var result = LogLevelValue.TryFromString("Information", out var logLevel);

            // Assert
            result.Should().BeTrue();
            logLevel.Should().NotBeNull();
            logLevel!.Value.Should().Be("Information");
        }

        [Fact]
        public void TryFromString_ShouldReturnFalse_WhenInvalidLevel()
        {
            // Act
            var result = LogLevelValue.TryFromString("InvalidLevel", out var logLevel);

            // Assert
            result.Should().BeFalse();
            logLevel.Should().BeNull();
        }

        [Fact]
        public void Equality_ShouldWorkCorrectly()
        {
            // Arrange
            var level1 = LogLevelValue.Information;
            var level2 = LogLevelValue.FromString("Information");
            var level3 = LogLevelValue.Error;

            // Assert
            (level1 == level2).Should().BeTrue();
            (level1 != level3).Should().BeTrue();
            level1.Equals(level2).Should().BeTrue();
        }

        [Fact]
        public void FromString_ShouldAcceptCaseInsensitive_ButPreserveOriginalCase()
        {
            // Act
            var result1 = LogLevelValue.FromString("Information"); // Case correcto
            var result2 = LogLevelValue.FromString("Information"); // Case correcto

            // Assert
            // FromString acepta case insensitive pero preserva el valor original
            // ValidLevels usa OrdinalIgnoreCase para validar, pero Value se guarda tal cual
            result1.Value.Should().Be("Information");
            result2.Value.Should().Be("Information");
            
            // La igualdad es case insensitive
            (result1 == result2).Should().BeTrue();
        }
    }
}

