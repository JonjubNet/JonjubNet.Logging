using FluentAssertions;
using JonjubNet.Logging.Domain.Common;
using System.Text.Json;
using Xunit;

namespace JonjubNet.Logging.Domain.Tests.Common
{
    /// <summary>
    /// Tests unitarios para JsonSerializerOptionsCache
    /// </summary>
    public class JsonSerializerOptionsCacheTests
    {
        [Fact]
        public void Default_ShouldReturnCachedOptions()
        {
            // Act
            var options1 = JsonSerializerOptionsCache.Default;
            var options2 = JsonSerializerOptionsCache.Default;

            // Assert
            options1.Should().NotBeNull();
            options2.Should().NotBeNull();
            options1.Should().BeSameAs(options2); // Debe ser la misma instancia (cache)
        }

        [Fact]
        public void Default_ShouldHaveCorrectConfiguration()
        {
            // Act
            var options = JsonSerializerOptionsCache.Default;

            // Assert
            options.WriteIndented.Should().BeFalse();
            options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        }
    }
}

