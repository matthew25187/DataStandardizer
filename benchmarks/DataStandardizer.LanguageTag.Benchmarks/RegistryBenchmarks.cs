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
                length += (int)tagRecord.Type;
                length += tagRecord switch
                {
                    SubtagRegistrySubtagRecord subtagRecord => subtagRecord.Subtag.Length,
                    SubtagRegistryTagRecord tagOnlyRecord => tagOnlyRecord.Tag.Length,
                    _ => 0,
                };
                length += tagRecord.Added.Day;
                length += tagRecord.Description.Count;
            }
        }

        return length;
    }

    [Benchmark]
    public int Registry_TryGetSubtag()
    {
        var length = 0;
        if (_registry.TryGetSubtag(SubtagRegistryRecordType.Language, "zh", out var language)) length += language.Description.Count;
        if (_registry.TryGetSubtag(SubtagRegistryRecordType.ExtendedLanguage, "yue", out var extendedLanguage)) length += extendedLanguage.Description.Count;
        if (_registry.TryGetSubtag(SubtagRegistryRecordType.Script, "Hant", out var script)) length += script.Description.Count;
        if (_registry.TryGetSubtag(SubtagRegistryRecordType.Region, "TW", out var region)) length += region.Description.Count;
        if (_registry.TryGetSubtag(SubtagRegistryRecordType.Variant, "1994", out var variant)) length += variant.Description.Count;
        if (_registry.IsGrandfathered("i-klingon")) length++;

        return length;
    }
}
