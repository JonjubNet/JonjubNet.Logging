using FluentAssertions;
using JonjubNet.Logging.Shared.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests adicionales para ErrorCategorizationService para aumentar cobertura
    /// </summary>
    public class ErrorCategorizationServiceAdditionalTests
    {
        [Fact]
        public void GetErrorCategory_ShouldReturnCorrectCategory_ForDifferentExceptionTypes()
        {
            // Arrange
            var service = new ErrorCategorizationService();

            // Act & Assert
            service.GetErrorCategory(new TimeoutException()).Should().Be("Timeout");
            service.GetErrorCategory(new OutOfMemoryException()).Should().Be("Resource");
            service.GetErrorCategory(new StackOverflowException()).Should().Be("Resource");
            service.GetErrorCategory(new InvalidOperationException()).Should().Be("Operation");
            service.GetErrorCategory(new NotSupportedException()).Should().Be("Operation");
            service.GetErrorCategory(new NotImplementedException()).Should().Be("Operation");
            service.GetErrorCategory(new UnauthorizedAccessException()).Should().Be("Security");
            service.GetErrorCategory(new ArgumentException()).Should().Be("Validation");
            service.GetErrorCategory(new ArgumentNullException()).Should().Be("Validation");
            service.GetErrorCategory(new ArgumentOutOfRangeException()).Should().Be("Validation");
            service.GetErrorCategory(new Exception()).Should().Be("Technical");
        }

        [Fact]
        public void GetLogLevel_ShouldReturnCorrectLevel_ForDifferentExceptionTypes()
        {
            // Arrange
            var service = new ErrorCategorizationService();

            // Act & Assert
            service.GetLogLevel(new OutOfMemoryException()).Should().Be(LogLevel.Critical);
            service.GetLogLevel(new StackOverflowException()).Should().Be(LogLevel.Critical);
            service.GetLogLevel(new UnauthorizedAccessException()).Should().Be(LogLevel.Warning);
            service.GetLogLevel(new ArgumentException()).Should().Be(LogLevel.Warning);
            service.GetLogLevel(new ArgumentNullException()).Should().Be(LogLevel.Warning);
            service.GetLogLevel(new ArgumentOutOfRangeException()).Should().Be(LogLevel.Warning);
            service.GetLogLevel(new Exception()).Should().Be(LogLevel.Error);
        }

        [Fact]
        public void RegisterFunctionalErrorType_ShouldThrow_WhenTypeIsNull()
        {
            // Arrange
            var service = new ErrorCategorizationService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.RegisterFunctionalErrorType(null!));
        }

        [Fact]
        public void RegisterFunctionalErrorType_ShouldThrow_WhenTypeIsNotException()
        {
            // Arrange
            var service = new ErrorCategorizationService();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.RegisterFunctionalErrorType(typeof(string)));
        }

        [Fact]
        public void RegisterTechnicalErrorType_ShouldThrow_WhenTypeIsNull()
        {
            // Arrange
            var service = new ErrorCategorizationService();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.RegisterTechnicalErrorType(null!));
        }

        [Fact]
        public void RegisterTechnicalErrorType_ShouldThrow_WhenTypeIsNotException()
        {
            // Arrange
            var service = new ErrorCategorizationService();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.RegisterTechnicalErrorType(typeof(int)));
        }

        [Fact]
        public void RegisterFunctionalErrorType_ShouldRegisterAndCategorizeCorrectly()
        {
            // Arrange
            var service = new ErrorCategorizationService();
            var customException = new InvalidOperationException("Custom");

            // Act
            service.RegisterFunctionalErrorType(typeof(InvalidOperationException));

            // Assert
            service.IsFunctionalError(customException).Should().BeTrue();
            service.GetErrorCategory(customException).Should().Be("Business");
        }

        [Fact]
        public void IsFunctionalError_ShouldCheckInheritance()
        {
            // Arrange
            var service = new ErrorCategorizationService();
            service.RegisterFunctionalErrorType(typeof(ArgumentException));

            // Act & Assert
            service.IsFunctionalError(new ArgumentNullException()).Should().BeTrue();
            service.IsFunctionalError(new ArgumentOutOfRangeException()).Should().BeTrue();
        }
    }
}

