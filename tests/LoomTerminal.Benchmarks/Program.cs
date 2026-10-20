using BenchmarkDotNet.Running;

namespace LoomTerminal.Benchmarks;

/// <summary>Entry point: lets BenchmarkDotNet pick benchmarks from the command line (e.g. --filter "*PlainText*").</summary>
public static class Program
{
    /// <summary>Runs the benchmarks selected on the command line.</summary>
    public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
