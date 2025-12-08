using FluentAssertions;
using JonjubNet.Logging.Application.Behaviours;
using JonjubNet.Logging.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using Xunit;

namespace JonjubNet.Logging.Application.Tests.Behaviours
{
    /// <summary>
    /// Tests unitarios para LoggingBehaviour
    /// Verifica que el logging automático de MediatR funcione correctamente
    /// </summary>
    public class LoggingBehaviourTests
    {
        private readonly Mock<IStructuredLoggingService> _loggingServiceMock;
        private readonly Mock<ILogger<LoggingBehaviour<TestRequest, TestResponse>>> _loggerMock;
        private readonly LoggingBehaviour<TestRequest, TestResponse> _behaviour;

        public LoggingBehaviourTests()
        {
            _loggingServiceMock = new Mock<IStructuredLoggingService>();
            _loggerMock = new Mock<ILogger<LoggingBehaviour<TestRequest, TestResponse>>>();
            _behaviour = new LoggingBehaviour<TestRequest, TestResponse>(
                _loggingServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldLogRequestStart_WhenRequestIsReceived()
        {
            // Arrange
            var request = new TestRequest { Id = 1, Name = "Test" };
            var response = new TestResponse { Success = true };
            RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(response);

            // Act
            var result = await _behaviour.Handle(request, next, CancellationToken.None);

            // Assert
            _loggingServiceMock.Verify(
                x => x.LogInformation(
                    It.Is<string>(m => m.Contains("Iniciando procesamiento de petición")),
                    "MediatR",
                    "Request",
                    It.Is<Dictionary<string, object>>(p => 
                        p.ContainsKey("RequestId") && 
                        p.ContainsKey("RequestType") && 
                        p["RequestType"].ToString() == "TestRequest"),
                    null),
                Times.Once);
            
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldLogRequestSuccess_WhenHandlerSucceeds()
        {
            // Arrange
            var request = new TestRequest { Id = 1, Name = "Test" };
            var response = new TestResponse { Success = true };
            RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(response);

            // Act
            var result = await _behaviour.Handle(request, next, CancellationToken.None);

            // Assert
            _loggingServiceMock.Verify(
                x => x.LogInformation(
                    It.Is<string>(m => m.Contains("Petición completada exitosamente")),
                    "MediatR",
                    "Request",
                    It.Is<Dictionary<string, object>>(p => 
                        p.ContainsKey("RequestId") && 
                        p.ContainsKey("Status") && 
                        p["Status"].ToString() == "Success" &&
                        p.ContainsKey("ExecutionTimeMs")),
                    It.Is<Dictionary<string, object>>(c => 
                        c.ContainsKey("ExecutionTimeMs") &&
                        c.ContainsKey("StartTime") &&
                        c.ContainsKey("EndTime"))),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldLogRequestError_WhenHandlerThrowsException()
        {
            // Arrange
            var request = new TestRequest { Id = 1, Name = "Test" };
            var exception = new InvalidOperationException("Test error");
            RequestHandlerDelegate<TestResponse> next = () => throw exception;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _behaviour.Handle(request, next, CancellationToken.None));

            _loggingServiceMock.Verify(
                x => x.LogError(
                    It.Is<string>(m => m.Contains("Error al procesar petición")),
                    "MediatR",
                    "Request",
                    It.Is<Dictionary<string, object>>(p => 
                        p.ContainsKey("RequestId") && 
                        p.ContainsKey("Status") && 
                        p["Status"].ToString() == "Error" &&
                        p.ContainsKey("ExecutionTimeMs")),
                    It.Is<Dictionary<string, object>>(c => 
                        c.ContainsKey("ExecutionTimeMs") &&
                        c.ContainsKey("ExceptionType")),
                    exception),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldMeasureExecutionTime_WhenHandlerExecutes()
        {
            // Arrange
            var request = new TestRequest { Id = 1, Name = "Test" };
            var response = new TestResponse { Success = true };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(100); // Simular trabajo
                return response;
            };

            // Act
            var result = await _behaviour.Handle(request, next, CancellationToken.None);

            // Assert
            _loggingServiceMock.Verify(
                x => x.LogInformation(
                    It.IsAny<string>(),
                    "MediatR",
                    "Request",
                    It.Is<Dictionary<string, object>>(p => 
                        p.ContainsKey("ExecutionTimeMs") &&
                        (long)p["ExecutionTimeMs"] >= 100), // Debe medir al menos 100ms
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldGenerateUniqueRequestId_ForEachRequest()
        {
            // Arrange
            var request1 = new TestRequest { Id = 1, Name = "Test1" };
            var request2 = new TestRequest { Id = 2, Name = "Test2" };
            var response = new TestResponse { Success = true };
            RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(response);

            var requestIds = new List<string>();

            _loggingServiceMock
                .Setup(x => x.LogInformation(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<Dictionary<string, object>>()))
                .Callback<string, string, string, Dictionary<string, object>?, Dictionary<string, object>?>(
                    (msg, op, cat, props, ctx) =>
                    {
                        if (props != null && props.ContainsKey("RequestId"))
                        {
                            requestIds.Add(props["RequestId"].ToString()!);
                        }
                    });

            // Act
            await _behaviour.Handle(request1, next, CancellationToken.None);
            await _behaviour.Handle(request2, next, CancellationToken.None);

            // Assert
            requestIds.Should().HaveCount(4); // 2 requests * 2 logs cada uno (start + success)
            requestIds.Distinct().Should().HaveCount(2); // Debe haber 2 RequestIds únicos
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

