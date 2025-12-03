using JonjubNet.Logging.Application.Interfaces;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Implementación de ILogScope que agrega propiedades a todos los logs dentro del scope
    /// </summary>
    public class LogScope : ILogScope
    {
        private readonly Dictionary<string, object> _properties;
        private bool _disposed = false;

        public LogScope(Dictionary<string, object> properties)
        {
            // Reutilizar el diccionario si es pequeño para evitar allocations
            _properties = properties ?? new Dictionary<string, object>();
            var stack = LogScopeManager._scopeStack.Value ??= new System.Collections.Concurrent.ConcurrentStack<Dictionary<string, object>>();
            stack.Push(_properties);
        }

        public Dictionary<string, object> Properties => _properties;

        public void Dispose()
        {
            if (!_disposed)
            {
                var stack = LogScopeManager._scopeStack.Value;
                if (stack != null && !stack.IsEmpty)
                {
                    stack.TryPop(out _);
                }
                _disposed = true;
            }
        }
    }
}
