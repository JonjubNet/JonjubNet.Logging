using FluentAssertions;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Domain.ValueObjects;
using Xunit;

namespace JonjubNet.Logging.Application.Tests.UseCases
{
    /// <summary>
    /// Tests unitarios para CreateLogEntryUseCase
    /// Sigue las mejores prácticas: AAA Pattern, FluentAssertions, Theory donde sea apropiado
    /// </summary>
    public class CreateLogEntryUseCaseTests
    {
        private readonly CreateLogEntryUseCase _useCase;

        public CreateLogEntryUseCaseTests()
        {
            _useCase = new CreateLogEntryUseCase();
        }

        [Fact]
        public void Execute_ShouldCreateLogEntry_WithValidParameters()
        {
            // Arrange
            var message = "Test message";
            var logLevel = LogLevelValue.Information;
            var operation = "TestOperation";
            var category = LogCategoryValue.Business;

            // Act
            var result = _useCase.Execute(message, logLevel, operation, category);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be(message);
            result.LogLevel.Should().Be(logLevel.Value);
            result.Operation.Should().Be(operation);
            result.Category.Should().Be(category.Value);
            result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData("Trace")]
        [InlineData("Debug")]
        [InlineData("Information")]
        [InlineData("Warning")]
        [InlineData("Error")]
        [InlineData("Critical")]
        [InlineData("Fatal")]
        public void Execute_ShouldCreateLogEntry_WithAllLogLevels(string levelName)
        {
            // Arrange
            var logLevel = LogLevelValue.FromString(levelName);
            var message = $"Test message for {levelName}";

            // Act
            var result = _useCase.Execute(message, logLevel);

            // Assert
            result.Should().NotBeNull();
            result.LogLevel.Should().Be(levelName);
            result.Message.Should().Be(message);
        }

        [Theory]
        [InlineData("General")]
        [InlineData("Security")]
        [InlineData("Audit")]
        [InlineData("Performance")]
        [InlineData("UserAction")]
        [InlineData("System")]
        [InlineData("Business")]
        [InlineData("Integration")]
        [InlineData("Database")]
        [InlineData("External")]
        [InlineData("BusinessLogic")]
        public void Execute_ShouldCreateLogEntry_WithAllCategories(string categoryName)
        {
            // Arrange
            var category = LogCategoryValue.FromString(categoryName);
            var message = "Test message";

            // Act
            var result = _useCase.Execute(message, LogLevelValue.Information, category: category);

            // Assert
            result.Should().NotBeNull();
            result.Category.Should().Be(categoryName);
        }

        [Fact]
        public void Execute_ShouldCreateLogEntry_WithException()
        {
            // Arrange
            var message = "Test message";
            var logLevel = LogLevelValue.Error;
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = _useCase.Execute(message, logLevel, exception: exception);

            // Assert
            result.Should().NotBeNull();
            result.Exception.Should().Be(exception);
            // StackTrace puede ser null en algunos entornos (optimizaciones del compilador)
            // Lo importante es que la excepción se asigne correctamente
            result.StackTrace.Should().Be(exception.StackTrace);
            result.LogLevel.Should().Be("Error");
        }

        [Fact]
        public void Execute_ShouldCreateLogEntry_WithNullException_ShouldNotSetStackTrace()
        {
            // Arrange
            var message = "Test message";
            var logLevel = LogLevelValue.Information;

            // Act
            var result = _useCase.Execute(message, logLevel, exception: null);

            // Assert
            result.Should().NotBeNull();
            result.Exception.Should().BeNull();
            result.StackTrace.Should().BeNull();
        }

        [Fact]
        public void Execute_ShouldCreateLogEntry_WithProperties()
        {
            // Arrange
            var message = "Test message";
            var logLevel = LogLevelValue.Information;
            var properties = new Dictionary<string, object> { { "Key1", "Value1" } };
            var context = new Dictionary<string, object> { { "Context1", "ContextValue1" } };

            // Act
            var result = _useCase.Execute(message, logLevel, properties: properties, context: context);

            // Assert
            result.Properties.Should().ContainKey("Key1").WhoseValue.Should().Be("Value1");
            result.Context.Should().ContainKey("Context1").WhoseValue.Should().Be("ContextValue1");
        }

        [Fact]
        public void Execute_ShouldCreateLogEntry_WithNullProperties_ShouldCreateEmptyDictionary()
        {
            // Arrange
            var message = "Test message";
            var logLevel = LogLevelValue.Information;

            // Act
            var result = _useCase.Execute(message, logLevel, properties: null, context: null);

            // Assert
            result.Properties.Should().NotBeNull().And.BeEmpty();
            result.Context.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void Execute_ShouldCreateLogEntry_WithEventType()
        {
            // Arrange
            var message = "Test message";
            var logLevel = LogLevelValue.Information;
            var eventType = EventTypeValue.OperationStart;

            // Act
            var result = _useCase.Execute(message, logLevel, eventType: eventType);

            // Assert
            result.Should().NotBeNull();
            result.EventType.Should().Be(eventType.Value);
        }

        [Fact]
        public void Execute_ShouldCreateLogEntry_WithNullEventType_ShouldNotSetEventType()
        {
            // Arrange
            var message = "Test message";
            var logLevel = LogLevelValue.Information;

            // Act
            var result = _useCase.Execute(message, logLevel, eventType: null);

            // Assert
            result.Should().NotBeNull();
            result.EventType.Should().BeNull();
        }

        [Fact]
        public void Execute_ShouldCreateLogEntry_WithNullCategory_ShouldUseGeneralCategory()
        {
            // Arrange
            var message = "Test message";
            var logLevel = LogLevelValue.Information;

            // Act
            var result = _useCase.Execute(message, logLevel, category: null);

            // Assert
            result.Should().NotBeNull();
            result.Category.Should().Be(LogCategoryValue.General.Value);
        }

        [Fact]
        public void Execute_ShouldCreateLogEntry_WithEmptyOperation_ShouldSetEmptyString()
        {
            // Arrange
            var message = "Test message";
            var logLevel = LogLevelValue.Information;

            // Act
            var result = _useCase.Execute(message, logLevel, operation: "");

            // Assert
            result.Should().NotBeNull();
            result.Operation.Should().BeEmpty();
        }

        [Fact]
        public void Execute_ShouldCreateLogEntry_WithNullMessage_ShouldSetEmptyString()
        {
            // Arrange
            string? message = null;
            var logLevel = LogLevelValue.Information;

            // Act
            var result = _useCase.Execute(message!, logLevel);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().BeEmpty();
        }

        [Fact]
        public void Execute_ShouldCreateLogEntry_WithComplexProperties()
        {
            // Arrange
            var message = "Test message";
            var logLevel = LogLevelValue.Information;
            var properties = new Dictionary<string, object>
            {
                { "StringValue", "test" },
                { "IntValue", 42 },
                { "BoolValue", true },
                { "DoubleValue", 3.14 },
                { "NestedObject", new { Key = "Value" } }
            };

            // Act
            var result = _useCase.Execute(message, logLevel, properties: properties);

            // Assert
            result.Properties.Should().HaveCount(5);
            result.Properties["StringValue"].Should().Be("test");
            result.Properties["IntValue"].Should().Be(42);
            result.Properties["BoolValue"].Should().Be(true);
            result.Properties["DoubleValue"].Should().Be(3.14);
        }

        [Fact]
        public void Execute_ShouldCreateLogEntry_WithTimestampCloseToNow()
        {
            // Arrange
            var beforeExecution = DateTime.UtcNow;
            var message = "Test message";
            var logLevel = LogLevelValue.Information;

            // Act
            var result = _useCase.Execute(message, logLevel);
            var afterExecution = DateTime.UtcNow;

            // Assert
            result.Timestamp.Should().BeAfter(beforeExecution.AddSeconds(-1));
            result.Timestamp.Should().BeBefore(afterExecution.AddSeconds(1));
        }
    }
}

