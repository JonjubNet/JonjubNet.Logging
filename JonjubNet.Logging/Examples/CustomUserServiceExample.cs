using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace JonjubNet.Logging.Examples
{
    /// <summary>
    /// Ejemplo de implementación personalizada del servicio de usuario actual
    /// </summary>
    public class CustomUserServiceExample : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomUserServiceExample(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Obtiene el ID del usuario actual desde los claims de JWT
        /// </summary>
        /// <returns>ID del usuario o null si no está disponible</returns>
        public string? GetCurrentUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                // Buscar el claim 'sub' (subject) que es estándar en JWT
                var subClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier) ?? 
                              httpContext.User.FindFirst("sub");
                return subClaim?.Value;
            }
            return null;
        }

        /// <summary>
        /// Obtiene el nombre del usuario actual desde los claims
        /// </summary>
        /// <returns>Nombre del usuario o null si no está disponible</returns>
        public string? GetCurrentUserName()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                // Buscar el claim de nombre
                var nameClaim = httpContext.User.FindFirst(ClaimTypes.Name) ?? 
                               httpContext.User.FindFirst("name") ??
                               httpContext.User.FindFirst("preferred_username");
                return nameClaim?.Value;
            }
            return null;
        }

        /// <summary>
        /// Obtiene el email del usuario actual desde los claims
        /// </summary>
        /// <returns>Email del usuario o null si no está disponible</returns>
        public string? GetCurrentUserEmail()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var emailClaim = httpContext.User.FindFirst(ClaimTypes.Email) ?? 
                                httpContext.User.FindFirst("email");
                return emailClaim?.Value;
            }
            return null;
        }

        /// <summary>
        /// Obtiene los roles del usuario actual desde los claims
        /// </summary>
        /// <returns>Lista de roles del usuario</returns>
        public IEnumerable<string> GetCurrentUserRoles()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContext.User.FindAll(ClaimTypes.Role)
                    .Select(c => c.Value)
                    .Concat(httpContext.User.FindAll("role").Select(c => c.Value))
                    .Distinct();
            }
            return new List<string>();
        }

        /// <summary>
        /// Verifica si el usuario actual tiene un rol específico
        /// </summary>
        /// <param name="role">Rol a verificar</param>
        /// <returns>True si el usuario tiene el rol</returns>
        public bool IsInRole(string role)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContext.User.IsInRole(role);
            }
            return false;
        }

        /// <summary>
        /// Verifica si el usuario actual está autenticado
        /// </summary>
        /// <returns>True si el usuario está autenticado</returns>
        public bool IsAuthenticated()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.User?.Identity?.IsAuthenticated == true;
        }
    }

    /// <summary>
    /// Ejemplo de implementación para aplicaciones que no usan autenticación
    /// </summary>
    public class SimpleUserServiceExample : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SimpleUserServiceExample(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetCurrentUserId()
        {
            // Obtener desde headers personalizados
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
        }

        public string? GetCurrentUserName()
        {
            // Obtener desde headers personalizados
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request.Headers["X-User-Name"].FirstOrDefault();
        }

        public string? GetCurrentUserEmail()
        {
            // Obtener desde headers personalizados
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request.Headers["X-User-Email"].FirstOrDefault();
        }

        public IEnumerable<string> GetCurrentUserRoles()
        {
            // Obtener desde headers personalizados (separados por coma)
            var httpContext = _httpContextAccessor.HttpContext;
            var rolesHeader = httpContext?.Request.Headers["X-User-Roles"].FirstOrDefault();
            
            if (!string.IsNullOrEmpty(rolesHeader))
            {
                return rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim());
            }
            
            return new List<string>();
        }

        public bool IsInRole(string role)
        {
            var roles = GetCurrentUserRoles();
            return roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsAuthenticated()
        {
            var userId = GetCurrentUserId();
            return !string.IsNullOrEmpty(userId) && userId != "Anonymous";
        }
    }

    /// <summary>
    /// Ejemplo de implementación para aplicaciones con sesiones
    /// </summary>
    public class SessionUserServiceExample : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionUserServiceExample(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetCurrentUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Session.GetString("UserId");
        }

        public string? GetCurrentUserName()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Session.GetString("UserName");
        }

        public string? GetCurrentUserEmail()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Session.GetString("UserEmail");
        }

        public IEnumerable<string> GetCurrentUserRoles()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var rolesJson = httpContext?.Session.GetString("UserRoles");
            
            if (!string.IsNullOrEmpty(rolesJson))
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<string[]>(rolesJson) ?? new string[0];
                }
                catch
                {
                    return new List<string>();
                }
            }
            
            return new List<string>();
        }

        public bool IsInRole(string role)
        {
            var roles = GetCurrentUserRoles();
            return roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsAuthenticated()
        {
            var userId = GetCurrentUserId();
            return !string.IsNullOrEmpty(userId);
        }
    }
}
