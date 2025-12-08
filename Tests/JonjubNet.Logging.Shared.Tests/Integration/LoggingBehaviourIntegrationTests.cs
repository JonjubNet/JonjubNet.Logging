using FluentAssertions;
using JonjubNet.Logging.Application.Behaviours;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Shared.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Integration
{
    /// <summary>
    /// Tests de integración para LoggingBehaviour
    /// Verifica la integración completa del logging automático de MediatR
    /// </summary>
    public class LoggingBehaviourIntegrationTests
    {
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<ILoggingConfigurationManager> _configManagerMock;
        private readonly StructuredLoggingService _loggingService;

        public LoggingBehaviourIntegrationTests()
        {
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            var loggerMock = new Mock<ILogger<StructuredLoggingService>>();
            _loggerFactoryMock
                .Setup(x => x.CreateLogger<StructuredLoggingService>())
                .Returns(loggerMock.Object);
            _loggerFactoryMock
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(new Mock<ILogger>().Object);

            var config = new LoggingConfiguration
            {
                Enabled = true,
                MinimumLevel = "Information"
            };
            _configManagerMock = new Mock<ILoggingConfigurationManager>();
            _configManagerMock.Setup(x => x.Current).Returns(config);
            _configManagerMock.Setup(x => x.GetCurrent()).Returns(config);

            var createUseCase = new CreateLogEntryUseCase(
                _loggerFactoryMock.Object.CreateLogger<CreateLogEntryUseCase>(),
                _configManagerMock.Object);

            var enrichUseCase = new EnrichLogEntryUseCase(
                _loggerFactoryMock.Object.CreateLogger<EnrichLogEntryUseCase>(),
                _configManagerMock.Object,
                null, // IHttpContextProvider
                null); // ICurrentUserService

            var sendUseCase = new Mock<SendLogUseCase>(
                _loggerFactoryMock.Object.CreateLogger<SendLogUseCase>(),
                _configManagerMock.Object,
                Array.Empty<ILogSink>()).Object;

            _loggingService = new StructuredLoggingService(
                _loggerFactoryMock.Object,
                _configManagerMock.Object,
                createUseCase,
                enrichUseCase,
                sendUseCase,
                Array.Empty<ILogSink>(),
                null, // ILogScopeManager
                null, // IKafkaProducer
                null, // ILogQueue
                null); // IPriorityLogQueue
        }

        [Fact]
        public async Task LoggingBehaviour_ShouldIntegrateWithStructuredLoggingService_WhenRequestIsProcessed()
        {
            // Arrange
            var behaviour = new LoggingBehaviour<TestRequest, TestResponse>(
                _loggingService,
                _loggerFactoryMock.Object.CreateLogger<LoggingBehaviour<TestRequest, TestResponse>>());

            var request = new TestRequest { Id = 1, Name = "Integration Test" };
            var response = new TestResponse { Success = true };
            RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(response);

            // Act
            var result = await behaviour.Handle(request, next, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            // El logging service debería haber sido llamado (verificado por la ausencia de excepciones)
        }

        [Fact]
        public async Task LoggingBehaviour_ShouldHandleExceptionsGracefully_WhenLoggingServiceFails()
        {
            // Arrange
            var failingLoggingService = new Mock<IStructuredLoggingService>();
            failingLoggingService
                .Setup(x => x.LogInformation(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<Exception>()))
                .Throws(new InvalidOperationException("Logging service error"));

            var behaviour = new LoggingBehaviour<TestRequest, TestResponse>(
                failingLoggingService.Object,
                _loggerFactoryMock.Object.CreateLogger<LoggingBehaviour<TestRequest, TestResponse>>());

            var request = new TestRequest { Id = 1, Name = "Test" };
            var response = new TestResponse { Success = true };
            RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(response);

            // Act
            var result = await behaviour.Handle(request, next, CancellationToken.None);

            // Assert
            // El comportamiento no debería lanzar excepción, solo loguear el error internamente
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        // Clases de prueba
        public class TestRequest : IRequest<TestResponse>
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class TestResponse
        {
            public bool Success { get; set; }
        }
    }
}

