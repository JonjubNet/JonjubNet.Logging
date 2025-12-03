using JonjubNet.Logging.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Implementación de IHttpContextProvider usando ASP.NET Core
    /// </summary>
    public class AspNetCoreHttpContextProvider : IHttpContextProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AspNetCoreHttpContextProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public object? GetHttpContext()
        {
            return _httpContextAccessor.HttpContext;
        }

        public string? GetRequestHeader(string headerName)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request?.Headers == null)
                return null;

            return httpContext.Request.Headers[headerName].FirstOrDefault();
        }

        public string? GetRequestPath()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request?.Path.ToString();
        }

        public string? GetRequestMethod()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request?.Method;
        }

        public int? GetStatusCode()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Response?.StatusCode;
        }

        public string? GetClientIp()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            // Intentar obtener la IP real del cliente (considerando proxies)
            var ip = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                     httpContext.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                     httpContext.Connection.RemoteIpAddress?.ToString();

            return ip;
        }

        public string? GetUserAgent()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request?.Headers["User-Agent"].FirstOrDefault();
        }

        public string? GetQueryString()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request?.QueryString.ToString();
        }

        public Dictionary<string, string>? GetRequestHeaders(IEnumerable<string>? sensitiveHeaders = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request?.Headers == null)
                return null;

            var headers = new Dictionary<string, string>();
            var sensitiveSet = sensitiveHeaders?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

            foreach (var header in httpContext.Request.Headers)
            {
                if (!sensitiveSet.Contains(header.Key))
                {
                    headers[header.Key] = header.Value.ToString();
                }
            }

            return headers;
        }

        public Dictionary<string, string>? GetResponseHeaders()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Response?.Headers == null)
                return null;

            var headers = new Dictionary<string, string>();
            foreach (var header in httpContext.Response.Headers)
            {
                headers[header.Key] = header.Value.ToString();
            }

            return headers;
        }

        public string? GetRequestBody(int maxSizeBytes = 10240)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request?.Body == null)
                return null;

            try
            {
                // Verificar si el body ya fue leído
                if (httpContext.Request.Body.CanSeek)
                {
                    httpContext.Request.Body.Position = 0;
                }

                using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
                var buffer = new char[Math.Min(maxSizeBytes, 1024)];
                var read = reader.Read(buffer, 0, buffer.Length);
                
                if (read > 0)
                {
                    return new string(buffer, 0, read);
                }
            }
            catch
            {
                // Si no se puede leer el body, retornar null
            }

            return null;
        }

        public string? GetResponseBody(int maxSizeBytes = 10240)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Response?.Body == null)
                return null;

            try
            {
                // Nota: Leer el body de la respuesta es más complejo y generalmente
                // requiere middleware personalizado. Por ahora retornamos null.
                // Esto puede implementarse con un middleware que capture el body.
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}

