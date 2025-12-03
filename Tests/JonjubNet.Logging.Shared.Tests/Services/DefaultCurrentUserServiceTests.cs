using FluentAssertions;
using JonjubNet.Logging.Shared.Services;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests unitarios para DefaultCurrentUserService
    /// </summary>
    public class DefaultCurrentUserServiceTests
    {
        [Fact]
        public void GetCurrentUserId_ShouldReturnNull()
        {
            // Arrange
            var service = new DefaultCurrentUserService();

            // Act
            var result = service.GetCurrentUserId();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetCurrentUserName_ShouldReturnNull()
        {
            // Arrange
            var service = new DefaultCurrentUserService();

            // Act
            var result = service.GetCurrentUserName();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetCurrentUserEmail_ShouldReturnNull()
        {
            // Arrange
            var service = new DefaultCurrentUserService();

            // Act
            var result = service.GetCurrentUserEmail();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetCurrentUserRoles_ShouldReturnEmpty()
        {
            // Arrange
            var service = new DefaultCurrentUserService();

            // Act
            var result = service.GetCurrentUserRoles();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void IsInRole_ShouldReturnFalse()
        {
            // Arrange
            var service = new DefaultCurrentUserService();

            // Act
            var result = service.IsInRole("Admin");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsAuthenticated_ShouldReturnFalse()
        {
            // Arrange
            var service = new DefaultCurrentUserService();

            // Act
            var result = service.IsAuthenticated();

            // Assert
            result.Should().BeFalse();
        }
    }
}

