namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para proveer acceso al contexto HTTP
    /// Abstrae la dependencia de ASP.NET Core de la capa de Application
    /// </summary>
    public interface IHttpContextProvider
    {
        /// <summary>
        /// Obtiene el contexto HTTP actual si está disponible
        /// </summary>
        /// <returns>El contexto HTTP o null si no está disponible</returns>
        object? GetHttpContext();

        /// <summary>
        /// Obtiene un valor de header HTTP de la petición
        /// </summary>
        /// <param name="headerName">Nombre del header</param>
        /// <returns>Valor del header o null si no existe</returns>
        string? GetRequestHeader(string headerName);

        /// <summary>
        /// Obtiene la ruta de la petición HTTP
        /// </summary>
        /// <returns>Ruta de la petición o null si no está disponible</returns>
        string? GetRequestPath();

        /// <summary>
        /// Obtiene el método HTTP de la petición
        /// </summary>
        /// <returns>Método HTTP o null si no está disponible</returns>
        string? GetRequestMethod();

        /// <summary>
        /// Obtiene el código de estado HTTP de la respuesta
        /// </summary>
        /// <returns>Código de estado o null si no está disponible</returns>
        int? GetStatusCode();

        /// <summary>
        /// Obtiene la IP del cliente
        /// </summary>
        /// <returns>IP del cliente o null si no está disponible</returns>
        string? GetClientIp();

        /// <summary>
        /// Obtiene el User Agent del cliente
        /// </summary>
        /// <returns>User Agent o null si no está disponible</returns>
        string? GetUserAgent();

        /// <summary>
        /// Obtiene el query string de la petición
        /// </summary>
        /// <returns>Query string o null si no está disponible</returns>
        string? GetQueryString();

        /// <summary>
        /// Obtiene todos los headers de la petición (excluyendo headers sensibles)
        /// </summary>
        /// <param name="sensitiveHeaders">Lista de nombres de headers sensibles a excluir</param>
        /// <returns>Diccionario con los headers o null si no está disponible</returns>
        Dictionary<string, string>? GetRequestHeaders(IEnumerable<string>? sensitiveHeaders = null);

        /// <summary>
        /// Obtiene todos los headers de la respuesta
        /// </summary>
        /// <returns>Diccionario con los headers o null si no está disponible</returns>
        Dictionary<string, string>? GetResponseHeaders();

        /// <summary>
        /// Obtiene el body de la petición HTTP
        /// </summary>
        /// <param name="maxSizeBytes">Tamaño máximo a leer en bytes</param>
        /// <returns>Body de la petición o null si no está disponible</returns>
        string? GetRequestBody(int maxSizeBytes = 10240);

        /// <summary>
        /// Obtiene el body de la respuesta HTTP
        /// </summary>
        /// <param name="maxSizeBytes">Tamaño máximo a leer en bytes</param>
        /// <returns>Body de la respuesta o null si no está disponible</returns>
        string? GetResponseBody(int maxSizeBytes = 10240);
    }
}

