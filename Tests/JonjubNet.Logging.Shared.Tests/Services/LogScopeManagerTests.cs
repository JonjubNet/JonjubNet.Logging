using FluentAssertions;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Shared.Services;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests unitarios para LogScopeManager
    /// Sigue las mejores prácticas: AAA Pattern, Tests de AsyncLocal, FluentAssertions
    /// </summary>
    public class LogScopeManagerTests
    {
        [Fact]
        public void GetCurrentScopeProperties_ShouldReturnEmptyDictionary_WhenNoScopes()
        {
            // Arrange
            var manager = new LogScopeManager();

            // Act
            var result = manager.GetCurrentScopeProperties();

            // Assert
            result.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void GetCurrentScopeProperties_ShouldReturnProperties_WhenScopeExists()
        {
            // Arrange
            var manager = new LogScopeManager();
            var properties = new Dictionary<string, object> { { "Key1", "Value1" } };
            LogScopeManager.PushScope(properties);

            try
            {
                // Act
                var result = manager.GetCurrentScopeProperties();

                // Assert
                result.Should().ContainKey("Key1").WhoseValue.Should().Be("Value1");
            }
            finally
            {
                LogScopeManager.PopScope();
            }
        }

        [Fact]
        public void GetCurrentScopeProperties_ShouldReturnMergedProperties_WhenMultipleScopes()
        {
            // Arrange
            var manager = new LogScopeManager();
            var scope1 = new Dictionary<string, object> { { "Key1", "Value1" }, { "Key2", "Value2" } };
            var scope2 = new Dictionary<string, object> { { "Key2", "Value2Override" }, { "Key3", "Value3" } };

            LogScopeManager.PushScope(scope1);
            LogScopeManager.PushScope(scope2);

            try
            {
                // Act
                var result = manager.GetCurrentScopeProperties();

                // Assert
                result.Should().ContainKey("Key1").WhoseValue.Should().Be("Value1");
                result.Should().ContainKey("Key2").WhoseValue.Should().Be("Value2Override"); // Más reciente tiene prioridad
                result.Should().ContainKey("Key3").WhoseValue.Should().Be("Value3");
            }
            finally
            {
                LogScopeManager.PopScope();
                LogScopeManager.PopScope();
            }
        }

        [Fact]
        public void GetCurrentScopeProperties_ShouldReturnEmptyDictionary_AfterAllScopesPopped()
        {
            // Arrange
            var manager = new LogScopeManager();
            var properties = new Dictionary<string, object> { { "Key1", "Value1" } };
            LogScopeManager.PushScope(properties);
            LogScopeManager.PopScope();

            // Act
            var result = manager.GetCurrentScopeProperties();

            // Assert
            result.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public async Task GetCurrentScopeProperties_ShouldPropagate_AcrossAsyncAwait()
        {
            // Arrange
            var manager = new LogScopeManager();
            var properties = new Dictionary<string, object> { { "AsyncKey", "AsyncValue" } };
            LogScopeManager.PushScope(properties);

            try
            {
                // Act
                var result1 = manager.GetCurrentScopeProperties();
                await Task.Delay(10);
                var result2 = manager.GetCurrentScopeProperties();
                await Task.Run(async () =>
                {
                    await Task.Delay(10);
                    return manager.GetCurrentScopeProperties();
                });

                // Assert
                result1.Should().ContainKey("AsyncKey");
                result2.Should().ContainKey("AsyncKey");
            }
            finally
            {
                LogScopeManager.PopScope();
            }
        }

        [Fact]
        public void PushScope_ShouldAddScope_ToStack()
        {
            // Arrange
            var properties = new Dictionary<string, object> { { "Key1", "Value1" } };

            // Act
            LogScopeManager.PushScope(properties);
            try
            {
                var result = LogScopeManager.GetActiveScopeProperties();

                // Assert
                result.Should().ContainKey("Key1");
            }
            finally
            {
                LogScopeManager.PopScope();
            }
        }

        [Fact]
        public void PopScope_ShouldRemoveScope_FromStack()
        {
            // Arrange
            var properties = new Dictionary<string, object> { { "Key1", "Value1" } };
            LogScopeManager.PushScope(properties);

            // Act
            LogScopeManager.PopScope();
            var result = LogScopeManager.GetActiveScopeProperties();

            // Assert
            result.Should().NotContainKey("Key1");
        }

        [Fact]
        public void PopScope_ShouldNotFail_WhenStackIsEmpty()
        {
            // Arrange & Act
            var act = () => LogScopeManager.PopScope();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GetCurrentScopeProperties_ShouldHandleNullValues()
        {
            // Arrange
            var manager = new LogScopeManager();
            var properties = new Dictionary<string, object> { { "NullKey", null! } };
            LogScopeManager.PushScope(properties);

            try
            {
                // Act
                var result = manager.GetCurrentScopeProperties();

                // Assert
                result.Should().ContainKey("NullKey");
                result["NullKey"].Should().BeNull();
            }
            finally
            {
                LogScopeManager.PopScope();
            }
        }

        [Fact]
        public void GetCurrentScopeProperties_ShouldHandleComplexObjects()
        {
            // Arrange
            var manager = new LogScopeManager();
            var complexObject = new { Name = "Test", Value = 42 };
            var properties = new Dictionary<string, object> { { "Complex", complexObject } };
            LogScopeManager.PushScope(properties);

            try
            {
                // Act
                var result = manager.GetCurrentScopeProperties();

                // Assert
                result.Should().ContainKey("Complex");
                result["Complex"].Should().Be(complexObject);
            }
            finally
            {
                LogScopeManager.PopScope();
            }
        }
    }
}

