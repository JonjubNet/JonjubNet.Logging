using System.Collections.Frozen;
using JonjubNet.Logging.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Implementación mejorada del servicio de categorización de errores.
    /// Optimizado para .NET 10: usa FrozenSet para lookups thread-safe sin locks.
    /// </summary>
    public class ErrorCategorizationService : IErrorCategorizationService
    {
        private FrozenSet<Type> _functionalErrorTypes = FrozenSet<Type>.Empty;
        private FrozenSet<Type> _technicalErrorTypes;
        private readonly object _lock = new();

        /// <summary>
        /// Inicializa una nueva instancia de ErrorCategorizationService.
        /// </summary>
        public ErrorCategorizationService()
        {
            // Registrar tipos de error técnicos comunes por defecto
            var technicalTypes = new HashSet<Type>
            {
                typeof(SystemException),
                typeof(OutOfMemoryException),
                typeof(StackOverflowException),
                typeof(TimeoutException),
                typeof(InvalidOperationException),
                typeof(NotSupportedException),
                typeof(NotImplementedException)
            };
            
            _technicalErrorTypes = technicalTypes.ToFrozenSet();
        }

        /// <summary>
        /// Determina si un error es funcional (de negocio) o técnico (del sistema)
        /// </summary>
        public bool IsFunctionalError(Exception exception)
        {
            if (exception == null)
                return false;

            var exceptionType = exception.GetType();

            // Verificar si está registrado como error funcional
            if (_functionalErrorTypes.Contains(exceptionType))
                return true;

            // Verificar si está registrado como error técnico
            if (_technicalErrorTypes.Contains(exceptionType))
                return false;

            // Verificar herencia
            foreach (var functionalType in _functionalErrorTypes)
            {
                if (functionalType.IsAssignableFrom(exceptionType))
                    return true;
            }

            foreach (var technicalType in _technicalErrorTypes)
            {
                if (technicalType.IsAssignableFrom(exceptionType))
                    return false;
            }

            // Por defecto, considerar como error técnico
            return false;
        }

        /// <summary>
        /// Obtiene la categoría del error para logging estructurado
        /// </summary>
        public string GetErrorCategory(Exception exception)
        {
            if (exception == null)
                return "Unknown";

            if (IsFunctionalError(exception))
                return "Business";

            var exceptionType = exception.GetType().Name;

            return exceptionType switch
            {
                nameof(TimeoutException) => "Timeout",
                nameof(OutOfMemoryException) => "Resource",
                nameof(StackOverflowException) => "Resource",
                nameof(InvalidOperationException) => "Operation",
                nameof(NotSupportedException) => "Operation",
                nameof(NotImplementedException) => "Operation",
                nameof(UnauthorizedAccessException) => "Security",
                "SecurityException" => "Security",
                nameof(ArgumentException) => "Validation",
                nameof(ArgumentNullException) => "Validation",
                nameof(ArgumentOutOfRangeException) => "Validation",
                _ => "Technical"
            };
        }

        /// <summary>
        /// Obtiene el nivel de log apropiado para el error
        /// </summary>
        public LogLevel GetLogLevel(Exception exception)
        {
            if (exception == null)
                return LogLevel.Warning;

            var exceptionType = exception.GetType().Name;

            return exceptionType switch
            {
                nameof(OutOfMemoryException) => LogLevel.Critical,
                nameof(StackOverflowException) => LogLevel.Critical,
                nameof(UnauthorizedAccessException) => LogLevel.Warning,
                "SecurityException" => LogLevel.Warning,
                nameof(ArgumentException) => LogLevel.Warning,
                nameof(ArgumentNullException) => LogLevel.Warning,
                nameof(ArgumentOutOfRangeException) => LogLevel.Warning,
                _ => IsFunctionalError(exception) ? LogLevel.Warning : LogLevel.Error
            };
        }

        /// <summary>
        /// Obtiene el tipo de error específico
        /// </summary>
        public string GetErrorType(Exception exception)
        {
            if (exception == null)
                return "Unknown";

            return exception.GetType().Name;
        }

        /// <summary>
        /// Registra un tipo de excepción como error funcional (de negocio).
        /// </summary>
        /// <param name="exceptionType">Tipo de excepción a registrar.</param>
        public void RegisterFunctionalErrorType(Type exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            if (!typeof(Exception).IsAssignableFrom(exceptionType))
                throw new ArgumentException($"El tipo debe ser una excepción (hereda de Exception)", nameof(exceptionType));

            lock (_lock)
            {
                var functionalSet = _functionalErrorTypes.ToHashSet();
                functionalSet.Add(exceptionType);
                _functionalErrorTypes = functionalSet.ToFrozenSet();

                var technicalSet = _technicalErrorTypes.ToHashSet();
                technicalSet.Remove(exceptionType);
                _technicalErrorTypes = technicalSet.ToFrozenSet();
            }
        }

        /// <summary>
        /// Registra un tipo de excepción como error técnico (del sistema).
        /// </summary>
        /// <param name="exceptionType">Tipo de excepción a registrar.</param>
        public void RegisterTechnicalErrorType(Type exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            if (!typeof(Exception).IsAssignableFrom(exceptionType))
                throw new ArgumentException($"El tipo debe ser una excepción (hereda de Exception)", nameof(exceptionType));

            lock (_lock)
            {
                var technicalSet = _technicalErrorTypes.ToHashSet();
                technicalSet.Add(exceptionType);
                _technicalErrorTypes = technicalSet.ToFrozenSet();

                var functionalSet = _functionalErrorTypes.ToHashSet();
                functionalSet.Remove(exceptionType);
                _functionalErrorTypes = functionalSet.ToFrozenSet();
            }
        }
    }
}

