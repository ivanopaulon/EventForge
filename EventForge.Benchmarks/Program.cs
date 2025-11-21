using BenchmarkDotNet.Running;

namespace EventForge.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        // Run all benchmarks in this assembly
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
