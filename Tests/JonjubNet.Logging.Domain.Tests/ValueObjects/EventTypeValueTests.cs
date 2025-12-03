using FluentAssertions;
using JonjubNet.Logging.Domain.ValueObjects;
using Xunit;

namespace JonjubNet.Logging.Domain.Tests.ValueObjects
{
    /// <summary>
    /// Tests unitarios para EventTypeValue
    /// Sigue las mejores prácticas: AAA Pattern, Theory, FluentAssertions
    /// </summary>
    public class EventTypeValueTests
    {
        [Theory]
        [InlineData("OperationStart")]
        [InlineData("OperationEnd")]
        [InlineData("UserAction")]
        [InlineData("SecurityEvent")]
        [InlineData("AuditEvent")]
        [InlineData("Custom")]
        public void FromString_ShouldCreateValidEventType(string eventTypeName)
        {
            // Act
            var result = EventTypeValue.FromString(eventTypeName);

            // Assert
            result.Value.Should().Be(eventTypeName);
        }

        [Fact]
        public void FromString_ShouldThrowException_WhenEventTypeIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventTypeValue.FromString(""));
            Assert.Throws<ArgumentException>(() => EventTypeValue.FromString("   "));
        }

        [Fact]
        public void FromString_ShouldThrowException_WhenEventTypeIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventTypeValue.FromString(null!));
        }

        [Fact]
        public void Equality_ShouldWorkCorrectly()
        {
            // Arrange
            var eventType1 = EventTypeValue.OperationStart;
            var eventType2 = EventTypeValue.FromString("OperationStart");
            var eventType3 = EventTypeValue.OperationEnd;

            // Assert
            (eventType1 == eventType2).Should().BeTrue();
            (eventType1 != eventType3).Should().BeTrue();
            eventType1.Equals(eventType2).Should().BeTrue();
        }

        [Fact]
        public void PredefinedEventTypes_ShouldBeAccessible()
        {
            // Assert
            EventTypeValue.OperationStart.Value.Should().Be("OperationStart");
            EventTypeValue.OperationEnd.Value.Should().Be("OperationEnd");
            EventTypeValue.UserAction.Value.Should().Be("UserAction");
            EventTypeValue.SecurityEvent.Value.Should().Be("SecurityEvent");
            EventTypeValue.AuditEvent.Value.Should().Be("AuditEvent");
            EventTypeValue.Custom.Value.Should().Be("Custom");
        }

        [Fact]
        public void ToString_ShouldReturnValue()
        {
            // Arrange
            var eventType = EventTypeValue.FromString("CustomEventType");

            // Act
            var result = eventType.ToString();

            // Assert
            result.Should().Be("CustomEventType");
        }
    }
}

