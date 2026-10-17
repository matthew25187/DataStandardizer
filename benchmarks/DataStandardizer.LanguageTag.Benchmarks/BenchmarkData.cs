using DataStandardizer.LanguageTag.SubtagRegistry;

namespace DataStandardizer.LanguageTag.Benchmarks;

/// <summary>
/// The fixed inputs shared by all benchmarks.
/// </summary>
public static class BenchmarkData
{
    private const string RegistryResourceName = "IanaSubtagRegistry.txt";

    /// <summary>
    /// Well-formed tags covering every kind of subtag, each also valid against the IANA registry fixture.
    /// </summary>
    public static readonly string[] ValidTags =
    [
        "en",
        "es-419",
        "zh-Hant-TW",
        "zh-yue-HK",
        "sr-Latn-RS",
        "de-CH-1901",
        "sl-rozaj-biske-1994",
        "hy-Latn-IT-arevela",
        "gsw-u-sd-chzh",
        "he-IL-u-ca-hebrew-tz-jeruslm",
        "en-US-x-twain",
        "x-private",
    ];

    /// <summary>
    /// Strings that are not well-formed tags, failing in a range of places.
    /// </summary>
    public static readonly string[] InvalidTags =
    [
        "",
        "e",
        "en-",
        "-en",
        "en--US",
        "abcdefghi",
        "en-US-US",
        "zh-Hant-Hans",
        "de-1901-1901",
        "en-a-bbb-a-ccc",
        "en-x-",
        "x-abcdefghi",
    ];

    /// <summary>
    /// A rules-only tag with every kind of subtag: primary language, extended language, script, region, variant, extension and private use.
    /// </summary>
    public const string AllSubtagsTag = "zh-yue-Hant-HK-1994-u-nu-hanidec-x-private";

    /// <summary>
    /// The extension subtag read by <see cref="KeyedSubtagBenchmarks"/>.
    /// </summary>
    public const string KeyedSubtag = "u-sd-chzh";

    /// <summary>
    /// Reads the IANA Language Subtag Registry fixture (shared with the tests) into memory.
    /// </summary>
    public static byte[] ReadRegistryBytes()
    {
        using var resourceStream = typeof(BenchmarkData).Assembly.GetManifestResourceStream(RegistryResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{RegistryResourceName}' not found.");
        using var buffer = new MemoryStream();
        resourceStream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>
    /// Loads the IANA Language Subtag Registry fixture.
    /// </summary>
    public static SubtagRegistry.SubtagRegistry LoadRegistry()
    {
        using var stream = new MemoryStream(ReadRegistryBytes(), writable: false);
        return SubtagRegistry.SubtagRegistry.CreateFromStream(stream);
    }
}
