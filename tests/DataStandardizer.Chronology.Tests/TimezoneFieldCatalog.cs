using System.Reflection;

namespace DataStandardizer.Chronology.Tests;

/// <summary>
/// The public <see cref="TzDataTimezone"/> fields of <see cref="TzDataTimezone"/> and its nested classes, found by reflection.
/// </summary>
/// <remarks>
/// Only test case factories enumerate these. The catalog is built once and never changes.
/// </remarks>
internal static class TimezoneFieldCatalog
{
    private static readonly Lazy<IReadOnlyList<FieldInfo>> LazyFields = new(() => GetTimezoneFields(typeof(TzDataTimezone)).ToList());

    private static readonly Lazy<IReadOnlyDictionary<string, TzDataTimezone>> LazyTimezones = new(() => LazyFields.Value.ToDictionary(GetIdentifier, field => (TzDataTimezone)field.GetValue(null)!, StringComparer.Ordinal));

    /// <summary>
    /// Gets every timezone field.
    /// </summary>
    internal static IReadOnlyList<FieldInfo> Fields => LazyFields.Value;

    /// <summary>
    /// Gets the timezone of every field, by its identifier.
    /// </summary>
    internal static IReadOnlyDictionary<string, TzDataTimezone> Timezones => LazyTimezones.Value;

    /// <summary>
    /// Gets a test case for the identifier of every timezone field.
    /// </summary>
    internal static TheoryData<string> CreateIdentifierTestCases()
    {
        var testCases = new TheoryData<string>();
        foreach (var identifier in Timezones.Keys)
            testCases.Add(identifier);

        return testCases;
    }

    /// <summary>
    /// Gets a test case for the timezone of every field.
    /// </summary>
    internal static TheoryData<TzDataTimezone> CreateTimezoneTestCases()
    {
        var testCases = new TheoryData<TzDataTimezone>();
        foreach (var timezone in Timezones.Values)
            testCases.Add(timezone);

        return testCases;
    }

    internal static string GetIdentifier(FieldInfo field)
    {
        string? identifier = (TzDataTimezone)field.GetValue(null)!;
        return identifier!;
    }

    private static IEnumerable<FieldInfo> GetTimezoneFields(Type hostType)
    {
        var fields = hostType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.FieldType == typeof(TzDataTimezone));
        var nestedFields = hostType.GetNestedTypes(BindingFlags.Public)
            .SelectMany(GetTimezoneFields);

        return fields.Concat(nestedFields);
    }
}
