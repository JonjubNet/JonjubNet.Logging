namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para obtener información del usuario actual
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Obtiene el ID del usuario actual
        /// </summary>
        /// <returns>ID del usuario o null si no está disponible</returns>
        string? GetCurrentUserId();

        /// <summary>
        /// Obtiene el nombre del usuario actual
        /// </summary>
        /// <returns>Nombre del usuario o null si no está disponible</returns>
        string? GetCurrentUserName();

        /// <summary>
        /// Obtiene el email del usuario actual
        /// </summary>
        /// <returns>Email del usuario o null si no está disponible</returns>
        string? GetCurrentUserEmail();

        /// <summary>
        /// Obtiene los roles del usuario actual
        /// </summary>
        /// <returns>Lista de roles del usuario</returns>
        IEnumerable<string> GetCurrentUserRoles();

        /// <summary>
        /// Verifica si el usuario actual tiene un rol específico
        /// </summary>
        /// <param name="role">Rol a verificar</param>
        /// <returns>True si el usuario tiene el rol</returns>
        bool IsInRole(string role);

        /// <summary>
        /// Verifica si el usuario actual está autenticado
        /// </summary>
        /// <returns>True si el usuario está autenticado</returns>
        bool IsAuthenticated();
    }
}

