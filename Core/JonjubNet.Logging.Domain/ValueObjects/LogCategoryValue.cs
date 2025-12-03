namespace JonjubNet.Logging.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa una categoría de log
    /// Inmutable y con validación
    /// </summary>
    public sealed class LogCategoryValue : IEquatable<LogCategoryValue>
    {
        private static readonly HashSet<string> ValidCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "General", "Security", "Audit", "Performance", "UserAction", 
            "System", "Business", "Integration", "Database", "External", "BusinessLogic"
        };

        /// <summary>
        /// Valor de la categoría
        /// </summary>
        public string Value { get; }

        // Valores predefinidos
        public static LogCategoryValue General => new("General");
        public static LogCategoryValue Security => new("Security");
        public static LogCategoryValue Audit => new("Audit");
        public static LogCategoryValue Performance => new("Performance");
        public static LogCategoryValue UserAction => new("UserAction");
        public static LogCategoryValue System => new("System");
        public static LogCategoryValue Business => new("Business");
        public static LogCategoryValue Integration => new("Integration");
        public static LogCategoryValue Database => new("Database");
        public static LogCategoryValue External => new("External");
        public static LogCategoryValue BusinessLogic => new("BusinessLogic");

        private LogCategoryValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("La categoría no puede estar vacía", nameof(value));

            Value = value;
        }

        /// <summary>
        /// Crea un LogCategoryValue desde un string
        /// </summary>
        public static LogCategoryValue FromString(string value)
        {
            return new LogCategoryValue(value);
        }

        /// <summary>
        /// Crea un LogCategoryValue personalizado (no predefinido)
        /// </summary>
        public static LogCategoryValue Custom(string value)
        {
            return new LogCategoryValue(value);
        }

        /// <summary>
        /// Verifica si es una categoría predefinida
        /// </summary>
        public bool IsPredefined => ValidCategories.Contains(Value);

        public bool Equals(LogCategoryValue? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is LogCategoryValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        public static bool operator ==(LogCategoryValue? left, LogCategoryValue? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LogCategoryValue? left, LogCategoryValue? right)
        {
            return !Equals(left, right);
        }

        public override string ToString() => Value;

        public static implicit operator string(LogCategoryValue category) => category.Value;
    }
}

