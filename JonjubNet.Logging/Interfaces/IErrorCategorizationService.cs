using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging
{
    /// <summary>
    /// Servicio genérico para categorizar y determinar el nivel de log apropiado para diferentes tipos de errores
    /// Permite que las aplicaciones registren sus excepciones específicas del dominio
    /// </summary>
    public interface IErrorCategorizationService
    {
        /// <summary>
        /// Determina si un error es funcional (de negocio) o técnico (del sistema)
        /// </summary>
        bool IsFunctionalError(Exception exception);

        /// <summary>
        /// Obtiene la categoría del error para logging estructurado
        /// </summary>
        string GetErrorCategory(Exception exception);

        /// <summary>
        /// Obtiene el nivel de log apropiado para el error
        /// </summary>
        LogLevel GetLogLevel(Exception exception);

        /// <summary>
        /// Obtiene el tipo de error específico (DuplicateKey, Validation, Database, etc.)
        /// </summary>
        string GetErrorType(Exception exception);

        /// <summary>
        /// Registra un tipo de excepción como error funcional (de negocio)
        /// </summary>
        void RegisterFunctionalErrorType(Type exceptionType);

        /// <summary>
        /// Registra un tipo de excepción como error técnico (del sistema)
        /// </summary>
        void RegisterTechnicalErrorType(Type exceptionType);
    }
}

