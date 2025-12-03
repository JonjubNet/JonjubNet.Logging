using FluentAssertions;
using JonjubNet.Logging.Domain.ValueObjects;
using Xunit;

namespace JonjubNet.Logging.Domain.Tests.ValueObjects
{
    /// <summary>
    /// Tests unitarios para LogCategoryValue
    /// Sigue las mejores prácticas: AAA Pattern, Theory, FluentAssertions
    /// </summary>
    public class LogCategoryValueTests
    {
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
        public void FromString_ShouldCreateValidCategory(string categoryName)
        {
            // Act
            var result = LogCategoryValue.FromString(categoryName);

            // Assert
            result.Value.Should().Be(categoryName);
        }

        [Fact]
        public void FromString_ShouldThrowException_WhenCategoryIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => LogCategoryValue.FromString(""));
            Assert.Throws<ArgumentException>(() => LogCategoryValue.FromString("   "));
        }

        [Fact]
        public void FromString_ShouldThrowException_WhenCategoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => LogCategoryValue.FromString(null!));
        }

        [Fact]
        public void Equality_ShouldWorkCorrectly()
        {
            // Arrange
            var category1 = LogCategoryValue.General;
            var category2 = LogCategoryValue.FromString("General");
            var category3 = LogCategoryValue.Security;

            // Assert
            (category1 == category2).Should().BeTrue();
            (category1 != category3).Should().BeTrue();
            category1.Equals(category2).Should().BeTrue();
        }

        [Fact]
        public void PredefinedCategories_ShouldBeAccessible()
        {
            // Assert
            LogCategoryValue.General.Value.Should().Be("General");
            LogCategoryValue.Security.Value.Should().Be("Security");
            LogCategoryValue.Audit.Value.Should().Be("Audit");
            LogCategoryValue.Performance.Value.Should().Be("Performance");
            LogCategoryValue.UserAction.Value.Should().Be("UserAction");
        }

        [Fact]
        public void ToString_ShouldReturnValue()
        {
            // Arrange
            var category = LogCategoryValue.FromString("CustomCategory");

            // Act
            var result = category.ToString();

            // Assert
            result.Should().Be("CustomCategory");
        }
    }
}

