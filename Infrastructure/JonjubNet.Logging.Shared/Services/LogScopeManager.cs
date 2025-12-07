using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Shared.Common;
using System.Collections.Concurrent;
using System.Threading;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Administrador de scopes de logging usando AsyncLocal para propagación en async/await
    /// </summary>
    public class LogScopeManager : ILogScopeManager
    {
        internal static readonly AsyncLocal<ConcurrentStack<Dictionary<string, object>>> _scopeStack = new();

        /// <summary>
        /// Obtiene todas las propiedades de los scopes activos (más reciente primero)
        /// Optimizado para reducir allocations
        /// </summary>
        public Dictionary<string, object> GetCurrentScopeProperties()
        {
            return GetActiveScopeProperties();
        }

        /// <summary>
        /// Obtiene todas las propiedades de los scopes activos (más reciente primero)
        /// Optimizado para reducir allocations
        /// </summary>
        public static Dictionary<string, object> GetActiveScopeProperties()
        {
            var stack = _scopeStack.Value;
            if (stack == null || stack.IsEmpty)
            {
                // Retornar diccionario vacío reutilizable para evitar allocations
                return new Dictionary<string, object>(0);
            }

            // Estimar capacidad inicial basado en número de scopes
            var estimatedCapacity = stack.Count * 4; // Estimación conservadora
            
            // Usar DictionaryPool para diccionario temporal
            var tempProperties = DictionaryPool.Rent();
            try
            {
                // Pre-allocar capacidad para evitar redimensionamientos
                if (tempProperties.Capacity < estimatedCapacity)
                {
                    tempProperties.EnsureCapacity(estimatedCapacity);
                }

                // Agregar propiedades de todos los scopes activos (del más reciente al más antiguo)
                foreach (var scopeProperties in stack)
                {
                    foreach (var kvp in scopeProperties)
                    {
                        // El scope más reciente tiene prioridad
                        if (!tempProperties.ContainsKey(kvp.Key))
                        {
                            tempProperties[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // Crear nuevo diccionario para retornar (no devolver el del pool)
                return new Dictionary<string, object>(tempProperties);
            }
            finally
            {
                DictionaryPool.Return(tempProperties);
            }
        }

        /// <summary>
        /// Agrega un scope al stack
        /// </summary>
        public static void PushScope(Dictionary<string, object> properties)
        {
            var stack = _scopeStack.Value ??= new ConcurrentStack<Dictionary<string, object>>();
            stack.Push(properties);
        }

        /// <summary>
        /// Remueve el scope más reciente del stack
        /// </summary>
        public static void PopScope()
        {
            var stack = _scopeStack.Value;
            if (stack != null && !stack.IsEmpty)
            {
                stack.TryPop(out _);
            }
        }
    }
}

