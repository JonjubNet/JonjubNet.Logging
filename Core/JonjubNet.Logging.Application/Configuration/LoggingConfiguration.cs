namespace JonjubNet.Logging.Application.Configuration
{
    /// <summary>
    /// Configuración genérica para logging estructurado
    /// </summary>
    public class LoggingConfiguration
    {
        public const string SectionName = "StructuredLogging";

        /// <summary>
        /// Habilitar/deshabilitar logging estructurado
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Nivel mínimo de log
        /// </summary>
        public string MinimumLevel { get; set; } = "Information";

        /// <summary>
        /// Información del servicio
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Configuración de sinks (destinos)
        /// </summary>
        public LoggingSinksConfiguration Sinks { get; set; } = new();

        /// <summary>
        /// Configuración de filtros
        /// </summary>
        public LoggingFiltersConfiguration Filters { get; set; } = new();

        /// <summary>
        /// Configuración de enriquecimiento
        /// </summary>
        public LoggingEnrichmentConfiguration Enrichment { get; set; } = new();

        /// <summary>
        /// Configuración de correlación
        /// </summary>
        public LoggingCorrelationConfiguration Correlation { get; set; } = new();

        /// <summary>
        /// Configuración de Kafka Producer
        /// </summary>
        public LoggingKafkaProducerConfiguration KafkaProducer { get; set; } = new();

        /// <summary>
        /// Configuración de Sampling y Rate Limiting
        /// </summary>
        public LoggingSamplingConfiguration Sampling { get; set; } = new();

        /// <summary>
        /// Configuración de Sanitización de Datos
        /// </summary>
        public LoggingDataSanitizationConfiguration DataSanitization { get; set; } = new();

        /// <summary>
        /// Configuración de Circuit Breaker para sinks
        /// </summary>
        public LoggingCircuitBreakerConfiguration CircuitBreaker { get; set; } = new();

        /// <summary>
        /// Configuración de Retry Policies
        /// </summary>
        public LoggingRetryPolicyConfiguration RetryPolicy { get; set; } = new();

        /// <summary>
        /// Configuración de Dead Letter Queue
        /// </summary>
        public LoggingDeadLetterQueueConfiguration DeadLetterQueue { get; set; } = new();

        /// <summary>
        /// Configuración de Batching Inteligente
        /// </summary>
        public LoggingBatchingConfiguration Batching { get; set; } = new();
    }

    /// <summary>
    /// Configuración de sinks (destinos de log)
    /// </summary>
    public class LoggingSinksConfiguration
    {
        public bool EnableConsole { get; set; } = true;
        public bool EnableFile { get; set; } = true;
        public bool EnableHttp { get; set; } = false;
        public bool EnableElasticsearch { get; set; } = false;

        public LoggingFileConfiguration File { get; set; } = new();
        public LoggingHttpConfiguration Http { get; set; } = new();
        public LoggingElasticsearchConfiguration Elasticsearch { get; set; } = new();
    }

    /// <summary>
    /// Configuración de archivo
    /// </summary>
    public class LoggingFileConfiguration
    {
        public string Path { get; set; } = "logs/log-.txt";
        public string RollingInterval { get; set; } = "Day";
        public int RetainedFileCountLimit { get; set; } = 30;
        public long FileSizeLimitBytes { get; set; } = 100 * 1024 * 1024; // 100MB
        public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    }

    /// <summary>
    /// Configuración HTTP
    /// </summary>
    public class LoggingHttpConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/json";
        public int BatchPostingLimit { get; set; } = 100;
        public int PeriodSeconds { get; set; } = 2;
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    /// <summary>
    /// Configuración Elasticsearch
    /// </summary>
    public class LoggingElasticsearchConfiguration
    {
        public string Url { get; set; } = string.Empty;
        public string IndexFormat { get; set; } = "logs-{0:yyyy.MM.dd}";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableAuthentication { get; set; } = false;
    }

    /// <summary>
    /// Configuración de filtros
    /// </summary>
    public class LoggingFiltersConfiguration
    {
        public List<string> ExcludedCategories { get; set; } = new();
        public List<string> ExcludedOperations { get; set; } = new();
        public List<string> ExcludedUsers { get; set; } = new();
        public bool FilterByLogLevel { get; set; } = true;
        public Dictionary<string, string> CategoryLogLevels { get; set; } = new();
        public Dictionary<string, string> OperationLogLevels { get; set; } = new();
    }

    /// <summary>
    /// Configuración de enriquecimiento
    /// </summary>
    public class LoggingEnrichmentConfiguration
    {
        public bool IncludeEnvironment { get; set; } = true;
        public bool IncludeProcess { get; set; } = true;
        public bool IncludeThread { get; set; } = true;
        public bool IncludeMachineName { get; set; } = true;
        public bool IncludeServiceInfo { get; set; } = true;
        public Dictionary<string, object> StaticProperties { get; set; } = new();
        
        /// <summary>
        /// Configuración de captura de datos HTTP
        /// </summary>
        public LoggingHttpCaptureConfiguration HttpCapture { get; set; } = new();
    }

    /// <summary>
    /// Configuración de captura de datos HTTP (headers, body, etc.)
    /// </summary>
    public class LoggingHttpCaptureConfiguration
    {
        /// <summary>
        /// Capturar headers HTTP de la petición
        /// </summary>
        public bool IncludeRequestHeaders { get; set; } = true;
        
        /// <summary>
        /// Capturar headers HTTP de la respuesta
        /// </summary>
        public bool IncludeResponseHeaders { get; set; } = false;
        
        /// <summary>
        /// Capturar query string de la petición
        /// </summary>
        public bool IncludeQueryString { get; set; } = true;
        
        /// <summary>
        /// Capturar body de la petición HTTP
        /// </summary>
        public bool IncludeRequestBody { get; set; } = false;
        
        /// <summary>
        /// Capturar body de la respuesta HTTP
        /// </summary>
        public bool IncludeResponseBody { get; set; } = false;
        
        /// <summary>
        /// Tamaño máximo del body a capturar (en bytes). Si el body es mayor, se trunca.
        /// </summary>
        public int MaxBodySizeBytes { get; set; } = 10240; // 10KB por defecto
        
        /// <summary>
        /// Headers sensibles que NO deben capturarse (por seguridad)
        /// </summary>
        public List<string> SensitiveHeaders { get; set; } = new() 
        { 
            "Authorization", 
            "Cookie", 
            "X-API-Key", 
            "X-Auth-Token" 
        };
    }

    /// <summary>
    /// Configuración de correlación
    /// </summary>
    public class LoggingCorrelationConfiguration
    {
        public bool EnableCorrelationId { get; set; } = true;
        public bool EnableRequestId { get; set; } = true;
        public bool EnableSessionId { get; set; } = true;
        public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";
        public string RequestIdHeader { get; set; } = "X-Request-ID";
        public string SessionIdHeader { get; set; } = "X-Session-ID";
    }

    /// <summary>
    /// Configuración de Kafka Producer
    /// Soporta múltiples tipos de conexión:
    /// 1. Conexión directa nativa (BootstrapServers) - Protocolo binario nativo de Kafka
    /// 2. Conexión HTTP/HTTPS (ProducerUrl) - A través de Kafka REST Proxy
    /// 3. Webhook HTTP/HTTPS (ProducerUrl + UseWebhook) - Envío directo a endpoint webhook
    /// </summary>
    public class LoggingKafkaProducerConfiguration
    {
        public bool Enabled { get; set; } = false;
        
        /// <summary>
        /// Conexión directa nativa: BootstrapServers de Kafka (ej: "localhost:9092")
        /// Tiene prioridad sobre ProducerUrl si está configurado
        /// </summary>
        public string? BootstrapServers { get; set; }
        
        /// <summary>
        /// URL del producer para REST Proxy o Webhook
        /// Para REST Proxy: "http://kafka-rest:8082" o "https://kafka-rest:8443"
        /// Para Webhook: "http://webhook-url" o "https://webhook-url" (con UseWebhook=true)
        /// </summary>
        public string ProducerUrl { get; set; } = "http://localhost:8080/api/logs";
        
        /// <summary>
        /// Indica si ProducerUrl es un webhook (true) o REST Proxy (false)
        /// </summary>
        public bool UseWebhook { get; set; } = false;
        
        public string Topic { get; set; } = "structured-logs";
        public int TimeoutSeconds { get; set; } = 5;
        public int BatchSize { get; set; } = 100;
        public int LingerMs { get; set; } = 5;
        public int RetryCount { get; set; } = 3;
        public bool EnableCompression { get; set; } = true;
        public string CompressionType { get; set; } = "gzip";
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    /// <summary>
    /// Configuración de Sampling y Rate Limiting
    /// </summary>
    public class LoggingSamplingConfiguration
    {
        /// <summary>
        /// Habilitar/deshabilitar sampling
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Tasas de sampling por nivel de log (0.0 a 1.0)
        /// Ejemplo: "Information": 0.1 = 10% de logs Information se registrarán
        /// </summary>
        public Dictionary<string, double> SamplingRates { get; set; } = new();

        /// <summary>
        /// Límite máximo de logs por minuto por nivel
        /// </summary>
        public Dictionary<string, int> MaxLogsPerMinute { get; set; } = new();

        /// <summary>
        /// Categorías que nunca deben ser muestreadas (siempre se registran)
        /// </summary>
        public List<string> NeverSampleCategories { get; set; } = new() { "Security", "Audit", "Error", "Critical" };

        /// <summary>
        /// Niveles que nunca deben ser muestreados (siempre se registran)
        /// </summary>
        public List<string> NeverSampleLevels { get; set; } = new() { "Error", "Critical" };
    }

    /// <summary>
    /// Configuración de Sanitización de Datos Sensibles
    /// </summary>
    public class LoggingDataSanitizationConfiguration
    {
        /// <summary>
        /// Habilitar/deshabilitar sanitización de datos
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Nombres de propiedades que contienen datos sensibles y deben ser enmascarados
        /// </summary>
        public List<string> SensitivePropertyNames { get; set; } = new()
        {
            "Password", "Passwd", "Pwd",
            "CreditCard", "CardNumber", "CCNumber",
            "SSN", "SocialSecurityNumber",
            "Email", "EmailAddress",
            "Phone", "PhoneNumber", "Mobile",
            "Token", "AccessToken", "RefreshToken",
            "ApiKey", "Secret", "SecretKey",
            "Authorization", "AuthToken"
        };

        /// <summary>
        /// Patrones regex para detectar datos sensibles en valores
        /// </summary>
        public List<string> SensitivePatterns { get; set; } = new()
        {
            @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", // Tarjetas de crédito
            @"\b\d{3}-\d{2}-\d{4}\b", // SSN formato XXX-XX-XXXX
            @"\b\d{3}\.\d{3}\.\d{4}\b", // SSN formato XXX.XXX.XXXX
            @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b" // Emails (opcional, puede ser muy agresivo)
        };

        /// <summary>
        /// Valor a usar para enmascarar datos sensibles
        /// </summary>
        public string MaskValue { get; set; } = "***REDACTED***";

        /// <summary>
        /// Si es true, muestra los últimos 4 caracteres (útil para tarjetas de crédito)
        /// </summary>
        public bool MaskPartial { get; set; } = true;

        /// <summary>
        /// Número de caracteres finales a mostrar cuando MaskPartial es true
        /// </summary>
        public int PartialMaskLength { get; set; } = 4;
    }

    /// <summary>
    /// Configuración de Circuit Breaker para sinks
    /// </summary>
    public class LoggingCircuitBreakerConfiguration
    {
        /// <summary>
        /// Habilitar/deshabilitar circuit breaker
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Configuración por defecto para todos los sinks
        /// </summary>
        public CircuitBreakerDefaultConfiguration Default { get; set; } = new();

        /// <summary>
        /// Configuración específica por sink
        /// </summary>
        public Dictionary<string, CircuitBreakerSinkConfiguration> PerSink { get; set; } = new();
    }

    /// <summary>
    /// Configuración por defecto de Circuit Breaker
    /// </summary>
    public class CircuitBreakerDefaultConfiguration
    {
        /// <summary>
        /// Número de fallos antes de abrir el circuit breaker
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Tiempo antes de probar de nuevo cuando está abierto
        /// </summary>
        public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Número de intentos en estado HalfOpen
        /// </summary>
        public int HalfOpenTestCount { get; set; } = 3;
    }

    /// <summary>
    /// Configuración de Circuit Breaker por sink
    /// </summary>
    public class CircuitBreakerSinkConfiguration
    {
        /// <summary>
        /// Habilitar/deshabilitar para este sink específico
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Número de fallos antes de abrir el circuit breaker
        /// </summary>
        public int? FailureThreshold { get; set; }

        /// <summary>
        /// Tiempo antes de probar de nuevo cuando está abierto
        /// </summary>
        public TimeSpan? OpenTimeout { get; set; }

        /// <summary>
        /// Número de intentos en estado HalfOpen
        /// </summary>
        public int? HalfOpenTestCount { get; set; }
    }

    /// <summary>
    /// Configuración de Retry Policies
    /// </summary>
    public class LoggingRetryPolicyConfiguration
    {
        /// <summary>
        /// Habilitar/deshabilitar retry policies
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Configuración por defecto para todos los sinks
        /// </summary>
        public RetryPolicyDefaultConfiguration Default { get; set; } = new();

        /// <summary>
        /// Configuración específica por sink
        /// </summary>
        public Dictionary<string, RetryPolicySinkConfiguration> PerSink { get; set; } = new();

        /// <summary>
        /// Tipos de excepciones que NO deben reintentarse (nombres completos de tipo)
        /// </summary>
        public List<string> NonRetryableExceptions { get; set; } = new()
        {
            "System.ArgumentException",
            "System.UnauthorizedAccessException",
            "System.ArgumentNullException"
        };
    }

    /// <summary>
    /// Configuración por defecto de Retry Policy
    /// </summary>
    public class RetryPolicyDefaultConfiguration
    {
        /// <summary>
        /// Estrategia de retry (NoRetry, FixedDelay, ExponentialBackoff, JitteredExponentialBackoff)
        /// </summary>
        public string Strategy { get; set; } = "ExponentialBackoff";

        /// <summary>
        /// Número máximo de reintentos
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Delay inicial entre reintentos
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Delay máximo entre reintentos
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Multiplicador para exponential backoff
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;
    }

    /// <summary>
    /// Configuración de Retry Policy por sink
    /// </summary>
    public class RetryPolicySinkConfiguration
    {
        /// <summary>
        /// Habilitar/deshabilitar para este sink específico
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Estrategia de retry
        /// </summary>
        public string? Strategy { get; set; }

        /// <summary>
        /// Número máximo de reintentos
        /// </summary>
        public int? MaxRetries { get; set; }

        /// <summary>
        /// Delay inicial
        /// </summary>
        public TimeSpan? InitialDelay { get; set; }

        /// <summary>
        /// Delay máximo
        /// </summary>
        public TimeSpan? MaxDelay { get; set; }

        /// <summary>
        /// Multiplicador de backoff
        /// </summary>
        public double? BackoffMultiplier { get; set; }
    }

    /// <summary>
    /// Configuración de Dead Letter Queue
    /// </summary>
    public class LoggingDeadLetterQueueConfiguration
    {
        /// <summary>
        /// Habilitar/deshabilitar Dead Letter Queue
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Tamaño máximo de la cola
        /// </summary>
        public int MaxSize { get; set; } = 10000;

        /// <summary>
        /// Intervalo para reintentos automáticos
        /// </summary>
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Número máximo de reintentos por item
        /// </summary>
        public int MaxRetriesPerItem { get; set; } = 10;

        /// <summary>
        /// Período de retención de items
        /// </summary>
        public TimeSpan ItemRetentionPeriod { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Habilitar reintentos automáticos
        /// </summary>
        public bool AutoRetry { get; set; } = true;

        /// <summary>
        /// Tipo de almacenamiento (InMemory, File, Database)
        /// </summary>
        public string Storage { get; set; } = "InMemory";

        /// <summary>
        /// Ruta para persistencia en archivo (si Storage = File)
        /// </summary>
        public string? PersistencePath { get; set; }
    }

    /// <summary>
    /// Configuración de Batching Inteligente
    /// </summary>
    public class LoggingBatchingConfiguration
    {
        /// <summary>
        /// Habilitar batching inteligente
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Tamaño máximo de batch por defecto
        /// </summary>
        public int DefaultBatchSize { get; set; } = 100;

        /// <summary>
        /// Intervalo máximo de tiempo para agrupar logs (en milisegundos)
        /// </summary>
        public int MaxBatchIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Tamaño máximo de batch por sink (sinkName -> batchSize)
        /// </summary>
        public Dictionary<string, int> BatchSizeBySink { get; set; } = new();

        /// <summary>
        /// Habilitar compresión de batches
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// Nivel de compresión (None, Fastest, Optimal, SmallestSize)
        /// </summary>
        public string CompressionLevel { get; set; } = "Optimal";

        /// <summary>
        /// Habilitar colas separadas por prioridad
        /// </summary>
        public bool EnablePriorityQueues { get; set; } = true;

        /// <summary>
        /// Capacidad de cola por prioridad (priority -> capacity)
        /// </summary>
        public Dictionary<string, int> QueueCapacityByPriority { get; set; } = new()
        {
            { "Critical", 10000 },
            { "Error", 5000 },
            { "Warning", 3000 },
            { "Information", 2000 },
            { "Debug", 1000 },
            { "Trace", 500 }
        };

        /// <summary>
        /// Habilitar procesamiento prioritario de errores críticos
        /// </summary>
        public bool EnablePriorityProcessing { get; set; } = true;

        /// <summary>
        /// Intervalo de procesamiento para logs críticos (en milisegundos)
        /// </summary>
        public int CriticalProcessingIntervalMs { get; set; } = 100;

        /// <summary>
        /// Intervalo de procesamiento para logs normales (en milisegundos)
        /// </summary>
        public int NormalProcessingIntervalMs { get; set; } = 1000;
    }
}

