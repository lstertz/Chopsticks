using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Chopsticks.Benchmarks;

var config = DefaultConfig.Instance;

#if DEBUG
Console.WriteLine("WARNING: Running in DEBUG mode. Results will not be accurate.");
Console.WriteLine("Build in Release mode for accurate benchmarks: dotnet run -c Release");
Console.WriteLine();
#endif

if (args.Length == 0)
{
    Console.WriteLine("Chopsticks Performance Benchmarks");
    Console.WriteLine("==================================");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run -c Release -- [filter]");
    Console.WriteLine();
    Console.WriteLine("Available benchmark classes:");
    Console.WriteLine("  MessageDispatchBenchmarks    - Message dispatch hot path (CHOP-001, CHOP-002, CHOP-005)");
    Console.WriteLine("  DependencyResolutionBenchmarks - DI resolution (CHOP-003, CHOP-004, CHOP-010)");
    Console.WriteLine("  HandlingResultBenchmarks     - Static properties and exceptions (CHOP-015, CHOP-017)");
    Console.WriteLine("  RegistrationBenchmarks       - Handler registration (CHOP-011)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run -c Release             # Run all benchmarks");
    Console.WriteLine("  dotnet run -c Release -- --filter *Dispatch*  # Run dispatch benchmarks only");
    Console.WriteLine("  dotnet run -c Release -- --filter *Resolution* # Run resolution benchmarks only");
    Console.WriteLine();
}

BenchmarkSwitcher.FromAssembly(typeof(MessageDispatchBenchmarks).Assembly).Run(args, config);
