using JonjubNet.Logging.Interfaces;
using System.Collections.Generic;

namespace JonjubNet.Logging.Services
{
    /// <summary>
    /// Implementación por defecto del servicio de usuario actual
    /// </summary>
    public class DefaultCurrentUserService : ICurrentUserService
    {
        /// <summary>
        /// Obtiene el ID del usuario actual (por defecto retorna "Anonymous")
        /// </summary>
        /// <returns>ID del usuario o "Anonymous"</returns>
        public string? GetCurrentUserId()
        {
            return "Anonymous";
        }

        /// <summary>
        /// Obtiene el nombre del usuario actual (por defecto retorna "Anonymous")
        /// </summary>
        /// <returns>Nombre del usuario o "Anonymous"</returns>
        public string? GetCurrentUserName()
        {
            return "Anonymous";
        }

        /// <summary>
        /// Obtiene el email del usuario actual (por defecto retorna null)
        /// </summary>
        /// <returns>Email del usuario o null</returns>
        public string? GetCurrentUserEmail()
        {
            return null;
        }

        /// <summary>
        /// Obtiene los roles del usuario actual (por defecto retorna lista vacía)
        /// </summary>
        /// <returns>Lista de roles del usuario</returns>
        public IEnumerable<string> GetCurrentUserRoles()
        {
            return new List<string>();
        }

        /// <summary>
        /// Verifica si el usuario actual tiene un rol específico (por defecto retorna false)
        /// </summary>
        /// <param name="role">Rol a verificar</param>
        /// <returns>False por defecto</returns>
        public bool IsInRole(string role)
        {
            return false;
        }

        /// <summary>
        /// Verifica si el usuario actual está autenticado (por defecto retorna false)
        /// </summary>
        /// <returns>False por defecto</returns>
        public bool IsAuthenticated()
        {
            return false;
        }
    }
}
