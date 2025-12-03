using FluentValidation;
using JonjubNet.Logging.Application.Configuration;

namespace JonjubNet.Logging.Shared.Configuration
{
    /// <summary>
    /// Validador para LoggingConfiguration usando FluentValidation
    /// </summary>
    public class LoggingConfigurationValidator : AbstractValidator<LoggingConfiguration>
    {
        public LoggingConfigurationValidator()
        {
            RuleFor(x => x.MinimumLevel)
                .NotEmpty()
                .Must(BeValidLogLevel)
                .WithMessage("MinimumLevel debe ser uno de: Trace, Debug, Information, Warning, Error, Critical, Fatal");

            RuleFor(x => x.ServiceName)
                .NotEmpty()
                .When(x => x.Enabled)
                .WithMessage("ServiceName es requerido cuando el logging está habilitado");

            RuleFor(x => x.Sinks)
                .NotNull()
                .SetValidator(new LoggingSinksConfigurationValidator());

            RuleFor(x => x.KafkaProducer)
                .NotNull()
                .SetValidator(new LoggingKafkaProducerConfigurationValidator());
        }

        private static bool BeValidLogLevel(string level)
        {
            if (string.IsNullOrWhiteSpace(level))
                return false;

            var validLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "Fatal" };
            return validLevels.Contains(level, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Validador para LoggingSinksConfiguration
    /// </summary>
    public class LoggingSinksConfigurationValidator : AbstractValidator<LoggingSinksConfiguration>
    {
        public LoggingSinksConfigurationValidator()
        {
            When(x => x.EnableFile, () =>
            {
                RuleFor(x => x.File)
                    .NotNull()
                    .SetValidator(new LoggingFileConfigurationValidator());
            });

            When(x => x.EnableHttp, () =>
            {
                RuleFor(x => x.Http)
                    .NotNull()
                    .SetValidator(new LoggingHttpConfigurationValidator());
            });

            When(x => x.EnableElasticsearch, () =>
            {
                RuleFor(x => x.Elasticsearch)
                    .NotNull()
                    .SetValidator(new LoggingElasticsearchConfigurationValidator());
            });
        }
    }

    /// <summary>
    /// Validador para LoggingFileConfiguration
    /// </summary>
    public class LoggingFileConfigurationValidator : AbstractValidator<LoggingFileConfiguration>
    {
        public LoggingFileConfigurationValidator()
        {
            RuleFor(x => x.Path)
                .NotEmpty()
                .WithMessage("La ruta del archivo de log es requerida");

            RuleFor(x => x.RetainedFileCountLimit)
                .GreaterThan(0)
                .WithMessage("RetainedFileCountLimit debe ser mayor que 0");

            RuleFor(x => x.FileSizeLimitBytes)
                .GreaterThan(0)
                .WithMessage("FileSizeLimitBytes debe ser mayor que 0");
        }
    }

    /// <summary>
    /// Validador para LoggingHttpConfiguration
    /// </summary>
    public class LoggingHttpConfigurationValidator : AbstractValidator<LoggingHttpConfiguration>
    {
        public LoggingHttpConfigurationValidator()
        {
            RuleFor(x => x.Url)
                .NotEmpty()
                .Must(BeValidUrl)
                .WithMessage("La URL debe ser válida");

            RuleFor(x => x.BatchPostingLimit)
                .GreaterThan(0)
                .WithMessage("BatchPostingLimit debe ser mayor que 0");

            RuleFor(x => x.PeriodSeconds)
                .GreaterThan(0)
                .WithMessage("PeriodSeconds debe ser mayor que 0");
        }

        private static bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }

    /// <summary>
    /// Validador para LoggingElasticsearchConfiguration
    /// </summary>
    public class LoggingElasticsearchConfigurationValidator : AbstractValidator<LoggingElasticsearchConfiguration>
    {
        public LoggingElasticsearchConfigurationValidator()
        {
            RuleFor(x => x.Url)
                .NotEmpty()
                .Must(BeValidUrl)
                .WithMessage("La URL de Elasticsearch debe ser válida");

            When(x => x.EnableAuthentication, () =>
            {
                RuleFor(x => x.Username)
                    .NotEmpty()
                    .WithMessage("Username es requerido cuando la autenticación está habilitada");

                RuleFor(x => x.Password)
                    .NotEmpty()
                    .WithMessage("Password es requerido cuando la autenticación está habilitada");
            });
        }

        private static bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }

    /// <summary>
    /// Validador para LoggingKafkaProducerConfiguration
    /// </summary>
    public class LoggingKafkaProducerConfigurationValidator : AbstractValidator<LoggingKafkaProducerConfiguration>
    {
        public LoggingKafkaProducerConfigurationValidator()
        {
            When(x => x.Enabled, () =>
            {
                RuleFor(x => x)
                    .Must(HaveValidConnection)
                    .WithMessage("Debe configurarse BootstrapServers o ProducerUrl cuando Kafka está habilitado");

                RuleFor(x => x.Topic)
                    .NotEmpty()
                    .WithMessage("El topic de Kafka es requerido");

                RuleFor(x => x.TimeoutSeconds)
                    .GreaterThan(0)
                    .WithMessage("TimeoutSeconds debe ser mayor que 0");

                RuleFor(x => x.RetryCount)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("RetryCount debe ser mayor o igual a 0");
            });

            When(x => !string.IsNullOrEmpty(x.BootstrapServers), () =>
            {
                RuleFor(x => x.BootstrapServers)
                    .Must(BeValidKafkaBootstrapServers)
                    .WithMessage("BootstrapServers debe tener el formato 'host:port' o 'host1:port1,host2:port2'");
            });

            When(x => !string.IsNullOrEmpty(x.ProducerUrl), () =>
            {
                RuleFor(x => x.ProducerUrl)
                    .Must(BeValidUrl)
                    .WithMessage("ProducerUrl debe ser una URL válida");
            });
        }

        private static bool HaveValidConnection(LoggingKafkaProducerConfiguration config)
        {
            return !string.IsNullOrEmpty(config.BootstrapServers) || 
                   !string.IsNullOrEmpty(config.ProducerUrl);
        }

        private static bool BeValidKafkaBootstrapServers(string? servers)
        {
            if (string.IsNullOrWhiteSpace(servers))
                return false;

            var serverList = servers.Split(',');
            foreach (var server in serverList)
            {
                var parts = server.Trim().Split(':');
                if (parts.Length != 2)
                    return false;

                if (!int.TryParse(parts[1], out var port) || port <= 0 || port > 65535)
                    return false;
            }

            return true;
        }

        private static bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }
}

