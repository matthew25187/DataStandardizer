using BenchmarkDotNet.Attributes;
using DataStandardizer.LanguageTag.SubtagRegistry;

namespace DataStandardizer.LanguageTag.Benchmarks;

/// <summary>
/// Loads the IANA Language Subtag Registry fixture, and reads the fields of every record of a pre-loaded copy.
/// </summary>
[MemoryDiagnoser]
public class RegistryBenchmarks
{
    private byte[] _registryBytes = [];
    private SubtagRegistry.SubtagRegistry _registry = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _registryBytes = BenchmarkData.ReadRegistryBytes();
        _registry = BenchmarkData.LoadRegistry();
    }

    [Benchmark]
    public int Registry_CreateFromStream()
    {
        using var stream = new MemoryStream(_registryBytes, writable: false);
        return SubtagRegistry.SubtagRegistry.CreateFromStream(stream).Count;
    }

    [Benchmark]
    public int Registry_RecordProperties()
    {
        var length = 0;
        foreach (var record in _registry)
        {
            if (record is SubtagRegistryTagRecordBase tagRecord)
            {
                length += tagRecord.Type.Length;
                length += tagRecord switch
                {
                    SubtagRegistrySubtagRecord subtagRecord => subtagRecord.Subtag.Length,
                    SubtagRegistryTagRecord tagOnlyRecord => tagOnlyRecord.Tag.Length,
                    _ => 0,
                };
                length += tagRecord.Added.Day;
                length += tagRecord.Description.Length;
            }
        }

        return length;
    }
}
