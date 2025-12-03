using JonjubNet.Logging.Application.Interfaces;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Implementación de IHttpContextProvider que no requiere ASP.NET Core
    /// Retorna null para todas las operaciones - útil para aplicaciones sin HTTP
    /// </summary>
    public class NullHttpContextProvider : IHttpContextProvider
    {
        public object? GetHttpContext()
        {
            return null;
        }

        public string? GetRequestHeader(string headerName)
        {
            return null;
        }

        public string? GetRequestPath()
        {
            return null;
        }

        public string? GetRequestMethod()
        {
            return null;
        }

        public int? GetStatusCode()
        {
            return null;
        }

        public string? GetClientIp()
        {
            return null;
        }

        public string? GetUserAgent()
        {
            return null;
        }

        public string? GetQueryString()
        {
            return null;
        }

        public Dictionary<string, string>? GetRequestHeaders(IEnumerable<string>? sensitiveHeaders = null)
        {
            return null;
        }

        public Dictionary<string, string>? GetResponseHeaders()
        {
            return null;
        }

        public string? GetRequestBody(int maxSizeBytes = 10240)
        {
            return null;
        }

        public string? GetResponseBody(int maxSizeBytes = 10240)
        {
            return null;
        }
    }
}

