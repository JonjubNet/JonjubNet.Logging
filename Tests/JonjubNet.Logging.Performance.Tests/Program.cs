using BenchmarkDotNet.Running;
using JonjubNet.Logging.Performance.Tests.Benchmarks;

namespace JonjubNet.Logging.Performance.Tests;

/// <summary>
/// Programa principal para ejecutar benchmarks de performance del componente de logging
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // Ejecutar todos los benchmarks
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
        
        // También se pueden ejecutar benchmarks específicos:
        // BenchmarkRunner.Run<JsonSerializationBenchmark>();
        // BenchmarkRunner.Run<DataSanitizationBenchmark>();
        // BenchmarkRunner.Run<LogEntryCloningBenchmark>();
        // BenchmarkRunner.Run<LogEntryCreationBenchmark>();
    }
}

