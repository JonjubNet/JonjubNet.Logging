using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace JonjubNet.Logging.Domain.Common
{
    /// <summary>
    /// Helpers para optimizar el uso del GC reduciendo allocations innecesarias
    /// </summary>
    public static class GCOptimizationHelpers
    {
        // Diccionario vacío reutilizable para evitar allocations
        private static readonly Dictionary<string, object> EmptyDictionary = new(0);

        // Pool de listas de Task para reutilizar en SendLogUseCase
        private static readonly ObjectPool<List<System.Threading.Tasks.Task>> _taskListPool =
            new DefaultObjectPool<List<System.Threading.Tasks.Task>>(
                new DefaultPooledObjectPolicy<List<System.Threading.Tasks.Task>>());

        /// <summary>
        /// Obtiene un diccionario vacío reutilizable (zero allocations)
        /// </summary>
        public static Dictionary<string, object> GetEmptyDictionary() => EmptyDictionary;

        /// <summary>
        /// Obtiene una lista de Task del pool para reutilizar
        /// </summary>
        public static List<System.Threading.Tasks.Task> RentTaskList()
        {
            return _taskListPool.Get();
        }

        /// <summary>
        /// Devuelve una lista de Task al pool después de limpiarla
        /// </summary>
        public static void ReturnTaskList(List<System.Threading.Tasks.Task> list)
        {
            if (list != null)
            {
                list.Clear();
                _taskListPool.Return(list);
            }
        }

        // Cache de strings comunes para evitar allocations repetidas
        private static readonly Dictionary<int, string> _processIdCache = new();
        private static readonly Dictionary<int, string> _threadIdCache = new();
        private static readonly object _cacheLock = new();

        /// <summary>
        /// Convierte ProcessId a string usando cache para evitar allocations repetidas
        /// </summary>
        public static string ProcessIdToString(int processId)
        {
            lock (_cacheLock)
            {
                if (!_processIdCache.TryGetValue(processId, out var result))
                {
                    result = processId.ToString();
                    _processIdCache[processId] = result;
                    
                    // Limitar tamaño del cache para evitar memory leaks
                    if (_processIdCache.Count > 1000)
                    {
                        _processIdCache.Clear();
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Convierte ThreadId a string usando cache para evitar allocations repetidas
        /// </summary>
        public static string ThreadIdToString(int threadId)
        {
            lock (_cacheLock)
            {
                if (!_threadIdCache.TryGetValue(threadId, out var result))
                {
                    result = threadId.ToString();
                    _threadIdCache[threadId] = result;
                    
                    // Limitar tamaño del cache para evitar memory leaks
                    if (_threadIdCache.Count > 1000)
                    {
                        _threadIdCache.Clear();
                    }
                }
                return result;
            }
        }
    }
}

