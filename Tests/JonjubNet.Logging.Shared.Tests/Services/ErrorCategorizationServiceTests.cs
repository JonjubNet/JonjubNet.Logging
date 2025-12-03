using FluentAssertions;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Shared.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests unitarios para ErrorCategorizationService
    /// Sigue las mejores prácticas: AAA Pattern, Tests de categorización, FluentAssertions
    /// </summary>
    public class ErrorCategorizationServiceTests
    {
        [Fact]
        public void IsFunctionalError_ShouldReturnFalse_ForTechnicalErrors()
        {
            // Arrange
            var service = new ErrorCategorizationService();
            var exception = new HttpRequestException("Network error");

            // Act
            var result = service.IsFunctionalError(exception);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsFunctionalError_ShouldReturnTrue_ForRegisteredFunctionalErrors()
        {
            // Arrange
            var service = new ErrorCategorizationService();
            var exception = new InvalidOperationException("Business rule violation");
            service.RegisterFunctionalErrorType(typeof(InvalidOperationException));

            // Act
            var result = service.IsFunctionalError(exception);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetErrorCategory_ShouldReturnBusiness_ForFunctionalErrors()
        {
            // Arrange
            var service = new ErrorCategorizationService();
            var exception = new InvalidOperationException("Business rule violation");
            service.RegisterFunctionalErrorType(typeof(InvalidOperationException));

            // Act
            var result = service.GetErrorCategory(exception);

            // Assert
            result.Should().Be("Business");
        }

        [Fact]
        public void GetErrorCategory_ShouldReturnTechnical_ForHttpRequestException()
        {
            // Arrange
            var service = new ErrorCategorizationService();
            var exception = new HttpRequestException("Network error");

            // Act
            var result = service.GetErrorCategory(exception);

            // Assert
            result.Should().Be("Technical");
        }

        [Fact]
        public void GetErrorCategory_ShouldReturnSecurity_ForSecurityExceptions()
        {
            // Arrange
            var service = new ErrorCategorizationService();
            var exception = new UnauthorizedAccessException("Access denied");

            // Act
            var result = service.GetErrorCategory(exception);

            // Assert
            result.Should().Be("Security");
        }

        [Fact]
        public void GetLogLevel_ShouldReturnWarning_ForFunctionalErrors()
        {
            // Arrange
            var service = new ErrorCategorizationService();
            var exception = new InvalidOperationException("Business rule violation");
            service.RegisterFunctionalErrorType(typeof(InvalidOperationException));

            // Act
            var result = service.GetLogLevel(exception);

            // Assert
            result.Should().Be(LogLevel.Warning);
        }

        [Fact]
        public void GetLogLevel_ShouldReturnError_ForTechnicalErrors()
        {
            // Arrange
            var service = new ErrorCategorizationService();
            var exception = new HttpRequestException("Network error");

            // Act
            var result = service.GetLogLevel(exception);

            // Assert
            result.Should().Be(LogLevel.Error);
        }

        [Fact]
        public void GetErrorType_ShouldReturnExceptionName()
        {
            // Arrange
            var service = new ErrorCategorizationService();
            var exception = new InvalidOperationException("Error");

            // Act
            var result = service.GetErrorType(exception);

            // Assert
            result.Should().Be("InvalidOperationException");
        }

        [Fact]
        public void RegisterFunctionalErrorType_ShouldRemoveFromTechnicalErrors()
        {
            // Arrange
            var service = new ErrorCategorizationService();
            var exception = new HttpRequestException("Error");

            // Act
            service.RegisterFunctionalErrorType(typeof(HttpRequestException));
            var isFunctional = service.IsFunctionalError(exception);

            // Assert
            isFunctional.Should().BeTrue();
        }

        [Fact]
        public void RegisterTechnicalErrorType_ShouldRemoveFromFunctionalErrors()
        {
            // Arrange
            var service = new ErrorCategorizationService();
            var exception = new InvalidOperationException("Error");
            service.RegisterFunctionalErrorType(typeof(InvalidOperationException));

            // Act
            service.RegisterTechnicalErrorType(typeof(InvalidOperationException));
            var isFunctional = service.IsFunctionalError(exception);

            // Assert
            isFunctional.Should().BeFalse();
        }
    }
}

