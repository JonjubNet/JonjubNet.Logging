namespace JonjubNet.Logging.Configuration
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
}
