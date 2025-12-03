using JonjubNet.Logging.Application.Interfaces;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Implementación por defecto de ICurrentUserService
    /// Retorna valores null/vacíos ya que no hay contexto de usuario disponible
    /// </summary>
    public class DefaultCurrentUserService : ICurrentUserService
    {
        public string? GetCurrentUserId() => null;

        public string? GetCurrentUserName() => null;

        public string? GetCurrentUserEmail() => null;

        public IEnumerable<string> GetCurrentUserRoles() => Enumerable.Empty<string>();

        public bool IsInRole(string role) => false;

        public bool IsAuthenticated() => false;
    }
}

