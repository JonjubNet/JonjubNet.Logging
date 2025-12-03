using FluentAssertions;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Application.Tests.UseCases
{
    /// <summary>
    /// Tests unitarios para EnrichLogEntryUseCase
    /// Sigue las mejores prácticas: AAA Pattern, Moq para mocks, FluentAssertions
    /// </summary>
    public class EnrichLogEntryUseCaseTests
    {
        private readonly Mock<ILogger<EnrichLogEntryUseCase>> _loggerMock;
        private readonly LoggingConfiguration _defaultConfiguration;

        public EnrichLogEntryUseCaseTests()
        {
            _loggerMock = new Mock<ILogger<EnrichLogEntryUseCase>>();
            _defaultConfiguration = CreateDefaultConfiguration();
        }

        [Fact]
        public void Execute_ShouldEnrichLogEntry_WithServiceInfo()
        {
            // Arrange
            var config = CreateConfigurationWithEnrichment(includeServiceInfo: true);
            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(configManagerMock.Object);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.ServiceName.Should().Be(config.ServiceName);
            result.Environment.Should().Be(config.Environment);
            result.Version.Should().Be(config.Version);
        }

        [Fact]
        public void Execute_ShouldNotOverrideExistingServiceInfo_WhenAlreadySet()
        {
            // Arrange
            var config = CreateConfigurationWithEnrichment(includeServiceInfo: true);
            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(configManagerMock.Object);
            var logEntry = CreateBasicLogEntry();
            logEntry.ServiceName = "ExistingService";
            logEntry.Environment = "ExistingEnv";
            logEntry.Version = "ExistingVersion";

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.ServiceName.Should().Be("ExistingService");
            result.Environment.Should().Be("ExistingEnv");
            result.Version.Should().Be("ExistingVersion");
        }

        [Fact]
        public void Execute_ShouldEnrichLogEntry_WithMachineName()
        {
            // Arrange
            var config = CreateConfigurationWithEnrichment(includeMachineName: true);
            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(configManagerMock.Object);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.MachineName.Should().Be(Environment.MachineName);
        }

        [Fact]
        public void Execute_ShouldEnrichLogEntry_WithProcessInfo()
        {
            // Arrange
            var config = CreateConfigurationWithEnrichment(includeProcess: true);
            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(configManagerMock.Object);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.ProcessId.Should().Be(Environment.ProcessId.ToString());
        }

        [Fact]
        public void Execute_ShouldEnrichLogEntry_WithThreadInfo()
        {
            // Arrange
            var config = CreateConfigurationWithEnrichment(includeThread: true);
            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(configManagerMock.Object);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.ThreadId.Should().Be(Thread.CurrentThread.ManagedThreadId.ToString());
        }

        [Fact]
        public void Execute_ShouldEnrichLogEntry_WithCurrentUserService()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            var userServiceMock = new Mock<ICurrentUserService>();
            userServiceMock.Setup(x => x.GetCurrentUserId()).Returns("User123");
            userServiceMock.Setup(x => x.GetCurrentUserName()).Returns("John Doe");

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(
                configManagerMock.Object,
                currentUserService: userServiceMock.Object);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.UserId.Should().Be("User123");
            result.UserName.Should().Be("John Doe");
            userServiceMock.Verify(x => x.GetCurrentUserId(), Times.Once);
            userServiceMock.Verify(x => x.GetCurrentUserName(), Times.Once);
        }

        [Fact]
        public void Execute_ShouldEnrichLogEntry_WithoutCurrentUserService_WhenNull()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(configManagerMock.Object, currentUserService: null!);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.UserId.Should().BeEmpty();
            result.UserName.Should().BeEmpty();
        }

        [Fact]
        public void Execute_ShouldEnrichLogEntry_WithHttpContextProvider()
        {
            // Arrange
            var config = CreateConfigurationWithHttpCapture(
                includeRequestHeaders: true,
                includeQueryString: true);
            var httpContextMock = new Mock<IHttpContextProvider>();
            httpContextMock.Setup(x => x.GetRequestPath()).Returns("/api/test");
            httpContextMock.Setup(x => x.GetRequestMethod()).Returns("GET");
            httpContextMock.Setup(x => x.GetStatusCode()).Returns(200);
            httpContextMock.Setup(x => x.GetClientIp()).Returns("127.0.0.1");
            httpContextMock.Setup(x => x.GetUserAgent()).Returns("TestAgent");
            httpContextMock.Setup(x => x.GetQueryString()).Returns("?param=value");
            httpContextMock.Setup(x => x.GetRequestHeaders(It.IsAny<List<string>>()))
                .Returns(new Dictionary<string, string> { { "Header1", "Value1" } });

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(
                configManagerMock.Object,
                httpContextProvider: httpContextMock.Object);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.RequestPath.Should().Be("/api/test");
            result.RequestMethod.Should().Be("GET");
            result.StatusCode.Should().Be(200);
            result.ClientIp.Should().Be("127.0.0.1");
            result.UserAgent.Should().Be("TestAgent");
            result.QueryString.Should().Be("?param=value");
            result.RequestHeaders.Should().ContainKey("Header1");
        }

        [Fact]
        public void Execute_ShouldEnrichLogEntry_WithHttpContextProvider_ExcludingSensitiveHeaders()
        {
            // Arrange
            var config = CreateConfigurationWithHttpCapture(
                includeRequestHeaders: true,
                sensitiveHeaders: new List<string> { "Authorization" });
            var httpContextMock = new Mock<IHttpContextProvider>();
            httpContextMock.Setup(x => x.GetRequestHeaders(It.Is<List<string>>(h => h.Contains("Authorization"))))
                .Returns(new Dictionary<string, string> { { "Header1", "Value1" } });

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(
                configManagerMock.Object,
                httpContextProvider: httpContextMock.Object);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            httpContextMock.Verify(x => x.GetRequestHeaders(
                It.Is<List<string>>(h => h.Contains("Authorization"))), Times.Once);
        }

        [Fact]
        public void Execute_ShouldEnrichLogEntry_WithStaticProperties()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            config.Enrichment.StaticProperties["StaticKey1"] = "StaticValue1";
            config.Enrichment.StaticProperties["StaticKey2"] = "StaticValue2";

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(configManagerMock.Object);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.Properties.Should().ContainKey("StaticKey1").WhoseValue.Should().Be("StaticValue1");
            result.Properties.Should().ContainKey("StaticKey2").WhoseValue.Should().Be("StaticValue2");
        }

        [Fact]
        public void Execute_ShouldNotOverrideExistingProperties_WithStaticProperties()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            config.Enrichment.StaticProperties["ExistingKey"] = "StaticValue";

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(configManagerMock.Object);
            var logEntry = CreateBasicLogEntry();
            logEntry.Properties["ExistingKey"] = "ExistingValue";

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.Properties["ExistingKey"].Should().Be("ExistingValue");
        }

        [Fact]
        public void Execute_ShouldEnrichLogEntry_WithLogScopeManager()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            var scopeManagerMock = new Mock<ILogScopeManager>();
            var scopeProperties = new Dictionary<string, object>
            {
                { "ScopeKey1", "ScopeValue1" },
                { "ScopeKey2", "ScopeValue2" }
            };
            scopeManagerMock.Setup(x => x.GetCurrentScopeProperties()).Returns(scopeProperties);

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(
                configManagerMock.Object,
                scopeManager: scopeManagerMock.Object);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.Properties.Should().ContainKey("ScopeKey1").WhoseValue.Should().Be("ScopeValue1");
            result.Properties.Should().ContainKey("ScopeKey2").WhoseValue.Should().Be("ScopeValue2");
            scopeManagerMock.Verify(x => x.GetCurrentScopeProperties(), Times.Once);
        }

        [Fact]
        public void Execute_ShouldNotOverrideExistingProperties_WithScopeProperties()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            var scopeManagerMock = new Mock<ILogScopeManager>();
            var scopeProperties = new Dictionary<string, object> { { "ExistingKey", "ScopeValue" } };
            scopeManagerMock.Setup(x => x.GetCurrentScopeProperties()).Returns(scopeProperties);

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(
                configManagerMock.Object,
                scopeManager: scopeManagerMock.Object);
            var logEntry = CreateBasicLogEntry();
            logEntry.Properties["ExistingKey"] = "ExistingValue";

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.Properties["ExistingKey"].Should().Be("ExistingValue");
        }

        [Fact]
        public void Execute_ShouldEnrichLogEntry_WithRequestBody_WhenEnabled()
        {
            // Arrange
            var config = CreateConfigurationWithHttpCapture(
                includeRequestBody: true,
                maxBodySizeBytes: 1024);
            var httpContextMock = new Mock<IHttpContextProvider>();
            httpContextMock.Setup(x => x.GetRequestBody(1024)).Returns("Request body content");

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(
                configManagerMock.Object,
                httpContextProvider: httpContextMock.Object);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.RequestBody.Should().Be("Request body content");
            httpContextMock.Verify(x => x.GetRequestBody(1024), Times.Once);
        }

        [Fact]
        public void Execute_ShouldEnrichLogEntry_WithResponseBody_WhenEnabled()
        {
            // Arrange
            var config = CreateConfigurationWithHttpCapture(
                includeResponseBody: true,
                maxBodySizeBytes: 2048);
            var httpContextMock = new Mock<IHttpContextProvider>();
            httpContextMock.Setup(x => x.GetResponseBody(2048)).Returns("Response body content");

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(
                configManagerMock.Object,
                httpContextProvider: httpContextMock.Object);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.ResponseBody.Should().Be("Response body content");
            httpContextMock.Verify(x => x.GetResponseBody(2048), Times.Once);
        }

        [Fact]
        public void Execute_ShouldNotEnrichLogEntry_WhenHttpCaptureIsNull()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            config.Enrichment.HttpCapture = null;
            var httpContextMock = new Mock<IHttpContextProvider>();

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(
                configManagerMock.Object,
                httpContextProvider: httpContextMock.Object);
            var logEntry = CreateBasicLogEntry();

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.RequestPath.Should().BeNull();
            httpContextMock.Verify(x => x.GetRequestPath(), Times.Never);
        }

        [Fact]
        public void Execute_ShouldReturnSameLogEntry_WhenNoEnrichmentEnabled()
        {
            // Arrange
            var config = CreateDefaultConfiguration();
            config.Enrichment.IncludeServiceInfo = false;
            config.Enrichment.IncludeMachineName = false;
            config.Enrichment.IncludeProcess = false;
            config.Enrichment.IncludeThread = false;

            var configManagerMock = CreateConfigurationManagerMock(config);
            var useCase = new EnrichLogEntryUseCase(configManagerMock.Object);
            var logEntry = CreateBasicLogEntry();
            var originalTimestamp = logEntry.Timestamp;

            // Act
            var result = useCase.Execute(logEntry);

            // Assert
            result.Should().BeSameAs(logEntry);
            result.Timestamp.Should().Be(originalTimestamp);
        }

        // Helper methods
        private static StructuredLogEntry CreateBasicLogEntry()
        {
            return new StructuredLogEntry
            {
                Message = "Test message",
                LogLevel = "Information",
                Operation = "TestOperation",
                Category = "General",
                Timestamp = DateTime.UtcNow,
                Properties = new Dictionary<string, object>(),
                Context = new Dictionary<string, object>()
            };
        }

        private static LoggingConfiguration CreateDefaultConfiguration()
        {
            return new LoggingConfiguration
            {
                ServiceName = "TestService",
                Environment = "Test",
                Version = "1.0.0",
                Enabled = true,
                Enrichment = new LoggingEnrichmentConfiguration
                {
                    IncludeServiceInfo = true,
                    IncludeMachineName = true,
                    IncludeProcess = true,
                    IncludeThread = true,
                    StaticProperties = new Dictionary<string, object>(),
                    HttpCapture = new LoggingHttpCaptureConfiguration
                    {
                        IncludeRequestHeaders = false,
                        IncludeResponseHeaders = false,
                        IncludeQueryString = false,
                        IncludeRequestBody = false,
                        IncludeResponseBody = false,
                        MaxBodySizeBytes = 1024,
                        SensitiveHeaders = new List<string>()
                    }
                }
            };
        }

        private static LoggingConfiguration CreateConfigurationWithEnrichment(
            bool includeServiceInfo = false,
            bool includeMachineName = false,
            bool includeProcess = false,
            bool includeThread = false)
        {
            var config = CreateDefaultConfiguration();
            config.Enrichment.IncludeServiceInfo = includeServiceInfo;
            config.Enrichment.IncludeMachineName = includeMachineName;
            config.Enrichment.IncludeProcess = includeProcess;
            config.Enrichment.IncludeThread = includeThread;
            return config;
        }

        private static LoggingConfiguration CreateConfigurationWithHttpCapture(
            bool includeRequestHeaders = false,
            bool includeResponseHeaders = false,
            bool includeQueryString = false,
            bool includeRequestBody = false,
            bool includeResponseBody = false,
            int maxBodySizeBytes = 1024,
            List<string>? sensitiveHeaders = null)
        {
            var config = CreateDefaultConfiguration();
            config.Enrichment.HttpCapture = new LoggingHttpCaptureConfiguration
            {
                IncludeRequestHeaders = includeRequestHeaders,
                IncludeResponseHeaders = includeResponseHeaders,
                IncludeQueryString = includeQueryString,
                IncludeRequestBody = includeRequestBody,
                IncludeResponseBody = includeResponseBody,
                MaxBodySizeBytes = maxBodySizeBytes,
                SensitiveHeaders = sensitiveHeaders ?? new List<string>()
            };
            return config;
        }

        private static Mock<ILoggingConfigurationManager> CreateConfigurationManagerMock(LoggingConfiguration config)
        {
            var mock = new Mock<ILoggingConfigurationManager>();
            mock.Setup(x => x.Current).Returns(config);
            return mock;
        }
    }
}

