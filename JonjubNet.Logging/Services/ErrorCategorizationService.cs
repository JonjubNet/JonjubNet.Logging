using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace JonjubNet.Logging.Services
{
    /// <summary>
    /// Implementación genérica del servicio de categorización de errores
    /// No conoce excepciones específicas del dominio - las aplicaciones deben registrarlas
    /// </summary>
    public class ErrorCategorizationService : IErrorCategorizationService
    {
        private readonly ConcurrentDictionary<Type, bool> _functionalErrorTypes = new();
        private readonly ConcurrentDictionary<Type, bool> _technicalErrorTypes = new();

        public ErrorCategorizationService()
        {
            // Registrar excepciones estándar de .NET como funcionales o técnicas
            RegisterStandardExceptionTypes();
        }

        public bool IsFunctionalError(Exception exception)
        {
            if (exception == null)
                return false;

            var exceptionType = exception.GetType();

            // Verificar si está registrado como funcional
            if (_functionalErrorTypes.ContainsKey(exceptionType))
                return true;

            // Verificar si está registrado como técnico
            if (_technicalErrorTypes.ContainsKey(exceptionType))
                return false;

            // Verificar herencia (si alguna clase base está registrada)
            var baseType = exceptionType.BaseType;
            while (baseType != null && baseType != typeof(Exception))
            {
                if (_functionalErrorTypes.ContainsKey(baseType))
                    return true;
                if (_technicalErrorTypes.ContainsKey(baseType))
                    return false;
                baseType = baseType.BaseType;
            }

            // Por defecto, excepciones estándar de .NET son funcionales si son de argumento o validación
            if (exception is ArgumentException || 
                exception is ArgumentNullException ||
                exception is InvalidOperationException)
            {
                return true;
            }

            // Por defecto, son errores técnicos
            return false;
        }

        public string GetErrorCategory(Exception exception)
        {
            if (IsFunctionalError(exception))
            {
                return "BusinessLogic";
            }

            return "Technical";
        }

        public LogLevel GetLogLevel(Exception exception)
        {
            // Errores funcionales se registran como Warning (no son errores del sistema)
            if (IsFunctionalError(exception))
            {
                return LogLevel.Warning;
            }

            // Errores críticos del sistema
            if (exception is OutOfMemoryException ||
                exception is StackOverflowException ||
                exception is System.Threading.ThreadAbortException)
            {
                return LogLevel.Critical;
            }

            // Errores técnicos normales
            return LogLevel.Error;
        }

        public string GetErrorType(Exception exception)
        {
            if (exception == null)
                return "Unknown";

            var exceptionType = exception.GetType();

            // Retornar el nombre del tipo sin el namespace
            var typeName = exceptionType.Name;

            // Mapear tipos estándar de .NET a nombres más descriptivos
            // IMPORTANTE: Los tipos más específicos (derivados) deben ir antes que los tipos base
            switch (exception)
            {
                case OutOfMemoryException:
                    return "OutOfMemory";
                case StackOverflowException:
                    return "StackOverflow";
                case System.Threading.ThreadAbortException:
                    return "ThreadAbort";
                case ArgumentNullException:
                    return "ArgumentNull";
                case ArgumentException:
                    return "Argument";
                case InvalidOperationException:
                    return "InvalidOperation";
                case KeyNotFoundException:
                    return "NotFound";
                default:
                    return typeName;
            }
        }

        public void RegisterFunctionalErrorType(Type exceptionType)
        {
            if (exceptionType == null)
                throw new ArgumentNullException(nameof(exceptionType));

            if (!typeof(Exception).IsAssignableFrom(exceptionType))
                throw new ArgumentException($"El tipo {exceptionType.Name} debe heredar de Exception", nameof(exceptionType));

            _functionalErrorTypes.TryAdd(exceptionType, true);
        }

        public void RegisterTechnicalErrorType(Type exceptionType)
        {
            if (exceptionType == null)
                throw new ArgumentNullException(nameof(exceptionType));

            if (!typeof(Exception).IsAssignableFrom(exceptionType))
                throw new ArgumentException($"El tipo {exceptionType.Name} debe heredar de Exception", nameof(exceptionType));

            _technicalErrorTypes.TryAdd(exceptionType, true);
        }

        private void RegisterStandardExceptionTypes()
        {
            // Excepciones estándar de .NET que son funcionales
            RegisterFunctionalErrorType(typeof(ArgumentException));
            RegisterFunctionalErrorType(typeof(ArgumentNullException));
            RegisterFunctionalErrorType(typeof(InvalidOperationException));
            RegisterFunctionalErrorType(typeof(KeyNotFoundException));

            // Excepciones estándar de .NET que son técnicas
            RegisterTechnicalErrorType(typeof(OutOfMemoryException));
            RegisterTechnicalErrorType(typeof(StackOverflowException));
            RegisterTechnicalErrorType(typeof(System.Threading.ThreadAbortException));
            RegisterTechnicalErrorType(typeof(System.IO.IOException));
            RegisterTechnicalErrorType(typeof(System.Net.Sockets.SocketException));
        }
    }
}

