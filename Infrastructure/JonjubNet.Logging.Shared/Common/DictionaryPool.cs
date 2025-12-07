using Microsoft.Extensions.ObjectPool;

namespace JonjubNet.Logging.Shared.Common
{
    /// <summary>
    /// Pool de diccionarios para reducir allocations en operaciones de logging frecuentes.
    /// Thread-safe por diseño de Microsoft.Extensions.ObjectPool.
    /// </summary>
    public static class DictionaryPool
    {
        private static readonly ObjectPool<Dictionary<string, object>> _pool = 
            new DefaultObjectPool<Dictionary<string, object>>(
                new DefaultPooledObjectPolicy<Dictionary<string, object>>());

        /// <summary>
        /// Obtiene un diccionario del pool.
        /// </summary>
        /// <returns>Diccionario reutilizable del pool.</returns>
        public static Dictionary<string, object> Rent()
        {
            return _pool.Get();
        }

        /// <summary>
        /// Devuelve un diccionario al pool después de limpiarlo.
        /// </summary>
        /// <param name="dictionary">Diccionario a devolver al pool.</param>
        public static void Return(Dictionary<string, object> dictionary)
        {
            if (dictionary != null)
            {
                dictionary.Clear();
                _pool.Return(dictionary);
            }
        }
    }
}

