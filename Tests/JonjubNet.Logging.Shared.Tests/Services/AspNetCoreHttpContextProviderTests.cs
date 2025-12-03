using FluentAssertions;
using JonjubNet.Logging.Shared.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text;
using Xunit;

namespace JonjubNet.Logging.Shared.Tests.Services
{
    /// <summary>
    /// Tests unitarios para AspNetCoreHttpContextProvider
    /// </summary>
    public class AspNetCoreHttpContextProviderTests
    {
        [Fact]
        public void GetRequestPath_ShouldReturnPath_WhenHttpContextExists()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContextMock = new Mock<HttpContext>();
            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(x => x.Path).Returns(new PathString("/api/test"));
            httpContextMock.Setup(x => x.Request).Returns(requestMock.Object);
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

            var provider = new AspNetCoreHttpContextProvider(httpContextAccessorMock.Object);

            // Act
            var result = provider.GetRequestPath();

            // Assert
            result.Should().Be("/api/test");
        }

        [Fact]
        public void GetRequestPath_ShouldReturnNull_WhenHttpContextIsNull()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            var provider = new AspNetCoreHttpContextProvider(httpContextAccessorMock.Object);

            // Act
            var result = provider.GetRequestPath();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetRequestMethod_ShouldReturnMethod()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContextMock = new Mock<HttpContext>();
            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(x => x.Method).Returns("POST");
            httpContextMock.Setup(x => x.Request).Returns(requestMock.Object);
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

            var provider = new AspNetCoreHttpContextProvider(httpContextAccessorMock.Object);

            // Act
            var result = provider.GetRequestMethod();

            // Assert
            result.Should().Be("POST");
        }

        [Fact]
        public void GetStatusCode_ShouldReturnStatusCode()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContextMock = new Mock<HttpContext>();
            var responseMock = new Mock<HttpResponse>();
            responseMock.Setup(x => x.StatusCode).Returns(404);
            httpContextMock.Setup(x => x.Response).Returns(responseMock.Object);
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

            var provider = new AspNetCoreHttpContextProvider(httpContextAccessorMock.Object);

            // Act
            var result = provider.GetStatusCode();

            // Assert
            result.Should().Be(404);
        }

        [Fact]
        public void GetClientIp_ShouldReturnIp_FromXForwardedFor()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContextMock = new Mock<HttpContext>();
            var requestMock = new Mock<HttpRequest>();
            var headersMock = new Mock<IHeaderDictionary>();
            headersMock.Setup(x => x["X-Forwarded-For"]).Returns(new Microsoft.Extensions.Primitives.StringValues("192.168.1.1"));
            requestMock.Setup(x => x.Headers).Returns(headersMock.Object);
            httpContextMock.Setup(x => x.Request).Returns(requestMock.Object);
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

            var provider = new AspNetCoreHttpContextProvider(httpContextAccessorMock.Object);

            // Act
            var result = provider.GetClientIp();

            // Assert
            result.Should().Be("192.168.1.1");
        }

        [Fact]
        public void GetUserAgent_ShouldReturnUserAgent()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContextMock = new Mock<HttpContext>();
            var requestMock = new Mock<HttpRequest>();
            var headersMock = new Mock<IHeaderDictionary>();
            headersMock.Setup(x => x["User-Agent"]).Returns(new Microsoft.Extensions.Primitives.StringValues("Mozilla/5.0"));
            requestMock.Setup(x => x.Headers).Returns(headersMock.Object);
            httpContextMock.Setup(x => x.Request).Returns(requestMock.Object);
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

            var provider = new AspNetCoreHttpContextProvider(httpContextAccessorMock.Object);

            // Act
            var result = provider.GetUserAgent();

            // Assert
            result.Should().Be("Mozilla/5.0");
        }

        [Fact]
        public void GetRequestHeaders_ShouldExcludeSensitiveHeaders()
        {
            // Arrange
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContextMock = new Mock<HttpContext>();
            var requestMock = new Mock<HttpRequest>();
            var headersMock = new Mock<IHeaderDictionary>();
            headersMock.Setup(x => x.GetEnumerator()).Returns(new List<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>
            {
                new("Authorization", "Bearer token"),
                new("Content-Type", "application/json")
            }.GetEnumerator());
            requestMock.Setup(x => x.Headers).Returns(headersMock.Object);
            httpContextMock.Setup(x => x.Request).Returns(requestMock.Object);
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

            var provider = new AspNetCoreHttpContextProvider(httpContextAccessorMock.Object);

            // Act
            var result = provider.GetRequestHeaders(new List<string> { "Authorization" });

            // Assert
            result.Should().NotBeNull();
            result.Should().NotContainKey("Authorization");
            result.Should().ContainKey("Content-Type");
        }
    }
}

