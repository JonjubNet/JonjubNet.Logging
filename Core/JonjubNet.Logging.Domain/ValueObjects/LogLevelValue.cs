namespace JonjubNet.Logging.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa un nivel de log
    /// Inmutable y con validación
    /// </summary>
    public sealed class LogLevelValue : IEquatable<LogLevelValue>
    {
        private static readonly HashSet<string> ValidLevels = new(StringComparer.OrdinalIgnoreCase)
        {
            "Trace", "Debug", "Information", "Warning", "Error", "Critical", "Fatal"
        };

        /// <summary>
        /// Valor del nivel de log
        /// </summary>
        public string Value { get; }

        // Valores predefinidos
        public static LogLevelValue Trace => new("Trace");
        public static LogLevelValue Debug => new("Debug");
        public static LogLevelValue Information => new("Information");
        public static LogLevelValue Warning => new("Warning");
        public static LogLevelValue Error => new("Error");
        public static LogLevelValue Critical => new("Critical");
        public static LogLevelValue Fatal => new("Fatal");

        private LogLevelValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("El nivel de log no puede estar vacío", nameof(value));

            if (!ValidLevels.Contains(value))
                throw new ArgumentException($"El nivel de log '{value}' no es válido. Valores válidos: {string.Join(", ", ValidLevels)}", nameof(value));

            Value = value;
        }

        /// <summary>
        /// Crea un LogLevelValue desde un string
        /// </summary>
        public static LogLevelValue FromString(string value)
        {
            return new LogLevelValue(value);
        }

        /// <summary>
        /// Intenta crear un LogLevelValue desde un string
        /// </summary>
        public static bool TryFromString(string value, out LogLevelValue? logLevel)
        {
            logLevel = null;
            if (string.IsNullOrWhiteSpace(value) || !ValidLevels.Contains(value))
                return false;

            logLevel = new LogLevelValue(value);
            return true;
        }

        public bool Equals(LogLevelValue? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is LogLevelValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        public static bool operator ==(LogLevelValue? left, LogLevelValue? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LogLevelValue? left, LogLevelValue? right)
        {
            return !Equals(left, right);
        }

        public override string ToString() => Value;

        public static implicit operator string(LogLevelValue logLevel) => logLevel.Value;
    }
}

