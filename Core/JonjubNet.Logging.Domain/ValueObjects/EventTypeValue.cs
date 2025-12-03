namespace JonjubNet.Logging.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa un tipo de evento
    /// Inmutable y con validación
    /// </summary>
    public sealed class EventTypeValue : IEquatable<EventTypeValue>
    {
        private static readonly HashSet<string> ValidEventTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "OperationStart", "OperationEnd", "UserAction", "SecurityEvent", "AuditEvent", "Custom"
        };

        /// <summary>
        /// Valor del tipo de evento
        /// </summary>
        public string Value { get; }

        // Valores predefinidos
        public static EventTypeValue OperationStart => new("OperationStart");
        public static EventTypeValue OperationEnd => new("OperationEnd");
        public static EventTypeValue UserAction => new("UserAction");
        public static EventTypeValue SecurityEvent => new("SecurityEvent");
        public static EventTypeValue AuditEvent => new("AuditEvent");
        public static EventTypeValue Custom => new("Custom");

        private EventTypeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("El tipo de evento no puede estar vacío", nameof(value));

            Value = value;
        }

        /// <summary>
        /// Crea un EventTypeValue desde un string
        /// </summary>
        public static EventTypeValue FromString(string value)
        {
            return new EventTypeValue(value);
        }

        /// <summary>
        /// Crea un EventTypeValue personalizado (no predefinido)
        /// </summary>
        public static EventTypeValue CustomType(string value)
        {
            return new EventTypeValue(value);
        }

        /// <summary>
        /// Verifica si es un tipo de evento predefinido
        /// </summary>
        public bool IsPredefined => ValidEventTypes.Contains(Value);

        public bool Equals(EventTypeValue? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is EventTypeValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        public static bool operator ==(EventTypeValue? left, EventTypeValue? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(EventTypeValue? left, EventTypeValue? right)
        {
            return !Equals(left, right);
        }

        public override string ToString() => Value;

        public static implicit operator string(EventTypeValue eventType) => eventType.Value;
    }
}

