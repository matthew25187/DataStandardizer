using BenchmarkDotNet.Running;

namespace DataStandardizer.LanguageTag.Benchmarks;

internal static class Program
{
    private const string ColdStartOption = "--coldstart";

    private static void Main(string[] args)
    {
        if (args.Contains(ColdStartOption, StringComparer.OrdinalIgnoreCase))
        {
            ColdStart.Run();
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
