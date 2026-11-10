using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace DataStandardizer.File.vCard.Fuzz;

/// <summary>
/// Asks the cards that a reader gave for everything an application would: the value of each content line, the checks and lookups of each card, and
/// every typed member of the card, of its version views and of what they give, since the card decodes typed values only when asked.
/// </summary>
/// <remarks>
/// A getter documents no exceptions, so any exception from one is a finding.  The decoding methods of <see cref="VcardContentLine"/> document
/// <see cref="VcardFileException"/> for a value they can't read, which a card may hold when <see cref="VcardOptions.AllowInvalidValues"/> is set or
/// the line isn't of the type those methods are for.  The other methods called here document only exceptions for their arguments.
/// <para>
/// Every lookup by name is by the names this package defines, not by the names in the data, so that the walk stays linear in the size of the
/// input.  A lookup for each name in the data would make the harness, not the package, quadratic in the number of properties or parameters.
/// </para>
/// </remarks>
internal static class CardWalker
{
    private const BindingFlags Getters = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DoNotWrapExceptions;

    private static readonly Assembly VcardAssembly = typeof(VcardCard).Assembly;

    /// <summary>
    /// The types whose members are read for a card: the card itself, and the views of it for each version, which an application reaches by casting.
    /// </summary>
    private static readonly Type[] CardViews = [typeof(VcardCard), typeof(IVcard40Card), typeof(IVcard30Card), typeof(IVcard21Card)];

    /// <summary>
    /// The methods of a property list that rank or group its items, which take no arguments and don't change the list.
    /// </summary>
    private static readonly string[] ListQueries = ["GetPreferred", "GetAlternatives", "GetOrderedByIndex"];

    private static readonly VcardVersion[] Versions = Enum.GetValues<VcardVersion>();

    public static readonly string[] PropertyNames = GetNames(typeof(VcardPropertyNames));

    public static readonly string[] ParameterNames = GetNames(typeof(VcardParameterNames));

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesByType = new();

    private static readonly ConcurrentDictionary<Type, MethodInfo[]> ListQueriesByType = new();

    /// <summary>
    /// Walks each card, and resolves the <c>MEMBER</c> properties of each against all of them.
    /// </summary>
    public static void Walk(IReadOnlyList<VcardCard> cards)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var card in cards)
        {
            WalkCard(card, visited);
            WalkValue(card.ResolveMembers(cards), visited);
        }
    }

    public static void ReadLine(VcardContentLine line)
    {
        _ = line.LegacySyntax;
        _ = line.TryGetTransferEncoding(out _);
        _ = line.TryGetCharset(out _);
        foreach (var name in ParameterNames)
        {
            _ = line.GetParameter(name);
        }

        try
        {
            line.GetTextValue();
        }
        catch (VcardFileException)
        {
        }

        try
        {
            line.GetDecodedValue();
        }
        catch (VcardFileException)
        {
        }

        try
        {
            line.GetDecodedText(out _);
        }
        catch (VcardFileException)
        {
        }
    }

    private static void WalkCard(VcardCard card, HashSet<object> visited)
    {
        if (!visited.Add(card))
        {
            return;
        }

        // The lines are walked through VcardCard.Properties, with the other members.
        foreach (var view in CardViews)
        {
            ReadMembers(card, view, visited);
        }

        _ = card.ToString();
        WalkValue(card.Validate(), visited);
        foreach (var version in Versions)
        {
            WalkValue(card.Validate(version), visited);
        }

        WalkValue(card.GetDuplicatePropertyIds(), visited);
        foreach (var name in PropertyNames)
        {
            WalkValue(card.GetProperties(name), visited);
            WalkValue(card.GetAlternatives(name), visited);
            WalkValue(card.GetPreferred(name), visited);
        }
    }

    /// <summary>
    /// Gets every property that <paramref name="type"/> declares or inherits on <paramref name="instance"/>, and walks what each gives.
    /// </summary>
    private static void ReadMembers(object instance, Type type, HashSet<object> visited)
    {
        foreach (var property in PropertiesByType.GetOrAdd(type, FindGetters))
        {
            WalkValue(property.GetValue(instance, Getters, null, null, null), visited);
        }
    }

    /// <summary>
    /// Walks a value that a member gave: a card; a typed value; a content line; or another type of this package, whose members are read and, if it
    /// is a collection, its items too.  A collection from elsewhere has its items walked.  Any other value is left as it is, since getting it was
    /// the read.
    /// </summary>
    private static void WalkValue(object? value, HashSet<object> visited)
    {
        switch (value)
        {
            case null or string:
                return;
            case VcardCard card:
                WalkCard(card, visited);
                return;
            case VcardTypedValue typedValue:
                WalkValue(ReadTypedValue(typedValue), visited);
                return;
            case VcardContentLine line:
                if (visited.Add(line))
                {
                    ReadLine(line);
                    ReadMembers(line, line.GetType(), visited);
                }

                return;
        }

        var type = value.GetType();
        if (!type.IsValueType && !visited.Add(value))
        {
            return;
        }

        var isPackageType = type.Assembly == VcardAssembly;
        if (isPackageType)
        {
            ReadMembers(value, type, visited);
            foreach (var query in ListQueriesByType.GetOrAdd(type, FindListQueries))
            {
                WalkValue(query.Invoke(value, Getters, null, null, null), visited);
            }
        }

        if (value is IEnumerable items)
        {
            foreach (var item in items)
            {
                WalkValue(item, visited);
            }
        }
    }

    /// <summary>
    /// Gets the one member of a <see cref="VcardTypedValue"/> that its <see cref="VcardTypedValue.ValueType"/> names, since the others document
    /// <see cref="InvalidOperationException"/> for a value of another type.
    /// </summary>
    private static object ReadTypedValue(VcardTypedValue value)
    {
        return value.ValueType switch
        {
            VcardValueType.Text => value.Text,
            VcardValueType.Uri => value.Uri,
            VcardValueType.DateAndOrTime => value.DateAndOrTime,
            VcardValueType.UtcOffset => value.UtcOffset,
            _ => throw new InvalidOperationException($"A typed value read from data has the value type {value.ValueType}, which it documents it can't have.")
        };
    }

    private static PropertyInfo[] FindGetters(Type type)
    {
        return type.GetProperties(Getters).Where(property => property.GetIndexParameters().Length == 0 && property.GetMethod is { IsPublic: true }).ToArray();
    }

    private static MethodInfo[] FindListQueries(Type type)
    {
        return ListQueries.Select(name => type.GetMethod(name, Getters, Type.EmptyTypes)).OfType<MethodInfo>().ToArray();
    }

    private static string[] GetNames(Type namesType)
    {
        return namesType.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
