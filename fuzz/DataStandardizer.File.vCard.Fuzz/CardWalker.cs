using System.Collections;
using System.Reflection;

namespace DataStandardizer.File.vCard.Fuzz;

/// <summary>
/// Asks a card that a reader gave for everything an application would: the value of each content line, and every typed member of the card, of its
/// version views and of what they give, since the card decodes typed values only when asked.
/// </summary>
/// <remarks>
/// A getter documents no exceptions, so any exception from one is a finding.  The decoding methods of <see cref="VcardContentLine"/> document
/// <see cref="VcardFileException"/> for a value they can't read, which a card may hold when <see cref="VcardOptions.AllowInvalidValues"/> is set or
/// the line isn't of the type those methods are for.
/// </remarks>
internal static class CardWalker
{
    private const BindingFlags Getters = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DoNotWrapExceptions;

    private static readonly Assembly VcardAssembly = typeof(VcardCard).Assembly;

    /// <summary>
    /// The types whose members are read for a card: the card itself, and the views of it for each version, which an application reaches by casting.
    /// </summary>
    private static readonly Type[] CardViews = [typeof(VcardCard), typeof(IVcard40Card), typeof(IVcard30Card), typeof(IVcard21Card)];

    public static void Walk(VcardCard card)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        WalkCard(card, visited);
    }

    public static void ReadLine(VcardContentLine line)
    {
        _ = line.LegacySyntax;
        _ = line.TryGetTransferEncoding(out _);
        _ = line.TryGetCharset(out _);
        foreach (var parameter in line.Parameters)
        {
            _ = line.GetParameter(parameter.Name);
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

        foreach (var line in card.Properties)
        {
            ReadLine(line);
        }

        foreach (var view in CardViews)
        {
            ReadMembers(card, view, visited);
        }
    }

    /// <summary>
    /// Gets every property that <paramref name="type"/> declares or inherits on <paramref name="instance"/>, and walks what each gives.
    /// </summary>
    private static void ReadMembers(object instance, Type type, HashSet<object> visited)
    {
        foreach (var property in type.GetProperties(Getters))
        {
            if (property.GetIndexParameters().Length == 0 && property.GetMethod is { IsPublic: true })
            {
                WalkValue(property.GetValue(instance, Getters, null, null, null), visited);
            }
        }
    }

    /// <summary>
    /// Walks a value that a member gave: a card, the items of a collection, or the members of another type of this package.  A value of a type from
    /// elsewhere is left as it is, since getting it was the read.
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
            case IEnumerable items:
                if (!value.GetType().IsValueType && !visited.Add(value))
                {
                    return;
                }

                foreach (var item in items)
                {
                    WalkValue(item, visited);
                }

                return;
        }

        var type = value.GetType();
        if (type.Assembly == VcardAssembly && (type.IsValueType || visited.Add(value)))
        {
            ReadMembers(value, type, visited);
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
}
