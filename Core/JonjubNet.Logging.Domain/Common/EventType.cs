namespace JonjubNet.Logging.Domain.Common
{
    /// <summary>
    /// Tipos de eventos predefinidos
    /// </summary>
    public static class EventType
    {
        public const string OperationStart = "OperationStart";
        public const string OperationEnd = "OperationEnd";
        public const string UserAction = "UserAction";
        public const string SecurityEvent = "SecurityEvent";
        public const string AuditEvent = "AuditEvent";
        public const string Custom = "Custom";
    }
}

