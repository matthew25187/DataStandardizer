using System.Collections;
using System.Collections.Specialized;
using FluentAssertions;

namespace DataStandardizer.File.CSV.Tests;

/// <summary>
/// Checks that a CSV line behaves as an <see cref="IOrderedDictionary"/> in the same way as the non-generic
/// <see cref="OrderedDictionary"/>, which each test also runs against as the reference implementation.
/// </summary>
public class CsvFileLineDictionaryContractTests
{
    public static TheoryData<string> Implementations => new() { RecordLine, Reference };

    private const string RecordLine = nameof(CsvFileRecordLine);
    private const string Reference = nameof(OrderedDictionary);

    [Theory]
    [MemberData(nameof(Implementations))]
    public void IntIndexer_Get_ReturnsValueAtPosition(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        var value = testLine[1];

        // assert
        value.Should().Be("2");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void IntIndexer_Set_ReplacesValueAtPosition(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        testLine[1] = "two";

        // assert
        Entries(testLine).Should().Equal(Entry("A", "1"), Entry("B", "two"), Entry("C", "3"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void IntIndexer_OutOfRange_Throws(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        var getAction = () => testLine[3];
        var setAction = () => testLine[-1] = "x";

        // assert
        getAction.Should().Throw<ArgumentOutOfRangeException>();
        setAction.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void KeyIndexer_Get_ReturnsValueForKey(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        var value = testLine["C"];

        // assert
        value.Should().Be("3");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void KeyIndexer_GetMissingKey_ReturnsNull(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        var value = testLine["Z"];

        // assert
        value.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void KeyIndexer_SetExistingKey_ReplacesValueInPlace(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        testLine["A"] = "one";

        // assert
        Entries(testLine).Should().Equal(Entry("A", "one"), Entry("B", "2"), Entry("C", "3"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void KeyIndexer_SetMissingKey_AddsAtEnd(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        testLine["D"] = null;

        // assert
        Entries(testLine).Should().Equal(Entry("A", "1"), Entry("B", "2"), Entry("C", "3"), Entry("D", null));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Add_DuplicateKey_Throws(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        var action = () => testLine.Add("B", "x");

        // assert
        action.Should().Throw<ArgumentException>();
        Entries(testLine).Should().Equal(Entry("A", "1"), Entry("B", "2"), Entry("C", "3"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void NullKey_Throws(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        var actions = new Action[]
        {
            () => testLine.Add(null!, "x"),
            () => _ = testLine[null!],
            () => testLine[null!] = "x",
            () => testLine.Contains(null!),
            () => testLine.Remove(null!),
            () => testLine.Insert(0, null!, "x")
        };

        // assert
        foreach (var action in actions)
        {
            action.Should().Throw<ArgumentNullException>();
        }
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Insert_AddsAtPosition(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        testLine.Insert(0, "First", "0");
        testLine.Insert(2, "Middle", "1.5");
        testLine.Insert(testLine.Count, "Last", "4");

        // assert
        Entries(testLine).Should().Equal(
            Entry("First", "0"), Entry("A", "1"), Entry("Middle", "1.5"), Entry("B", "2"), Entry("C", "3"), Entry("Last", "4"));
        testLine["Middle"].Should().Be("1.5");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Insert_DuplicateKeyOrOutOfRange_Throws(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        var duplicateAction = () => testLine.Insert(0, "C", "x");
        var outOfRangeAction = () => testLine.Insert(4, "D", "x");

        // assert
        duplicateAction.Should().Throw<ArgumentException>();
        outOfRangeAction.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void RemoveAt_RemovesAtPosition(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        testLine.RemoveAt(0);

        // assert
        Entries(testLine).Should().Equal(Entry("B", "2"), Entry("C", "3"));
        testLine.Contains("A").Should().BeFalse();
        testLine[0].Should().Be("2");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void RemoveAt_OutOfRange_Throws(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        var action = () => testLine.RemoveAt(3);

        // assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Remove_RemovesKeyAndIgnoresMissingKey(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        testLine.Remove("B");
        testLine.Remove("Z");

        // assert
        Entries(testLine).Should().Equal(Entry("A", "1"), Entry("C", "3"));
        testLine[1].Should().Be("3");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Contains_ReportsKeyPresence(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        var containsPresent = testLine.Contains("A");
        var containsMissing = testLine.Contains("Z");
        var containsValue = testLine.Contains("1");

        // assert
        containsPresent.Should().BeTrue();
        containsMissing.Should().BeFalse();
        containsValue.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Enumeration_AfterInsertsAndRemovals_FollowsPositionOrder(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        testLine.Remove("A");
        testLine.Insert(1, "X", "x");
        testLine.Add("A", "1");
        testLine.RemoveAt(0);

        // assert
        var expected = new[] { Entry("X", "x"), Entry("C", "3"), Entry("A", "1") };
        Entries(testLine).Should().Equal(expected);
        EntriesFrom(((IDictionary)testLine).GetEnumerator()).Should().Equal(expected);
        ((IEnumerable)testLine).Cast<object>().Should().AllBeOfType<DictionaryEntry>().And.Equal(expected.Cast<object>());
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Enumeration_ModifiedDuringEnumeration_Throws(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);
        var enumerator = testLine.GetEnumerator();
        enumerator.MoveNext();

        // act
        testLine.Add("D", "4");
        var action = () => enumerator.MoveNext();

        // assert
        action.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void KeysAndValues_FollowPositionOrder(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);
        testLine.Insert(0, "Z", "26");

        // act
        var keys = testLine.Keys;
        var values = testLine.Values;

        // assert
        keys.Count.Should().Be(4);
        values.Count.Should().Be(4);
        keys.Cast<object>().Should().Equal("Z", "A", "B", "C");
        values.Cast<object>().Should().Equal("26", "1", "2", "3");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void KeysAndValues_CopyTo_CopiesInPositionOrder(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);
        var keys = new object[4];
        var values = new object[4];

        // act
        testLine.Keys.CopyTo(keys, 1);
        testLine.Values.CopyTo(values, 1);

        // assert
        keys.Should().Equal(new object?[] { null, "A", "B", "C" });
        values.Should().Equal(new object?[] { null, "1", "2", "3" });
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void CopyTo_CopiesDictionaryEntriesInPositionOrder(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);
        var entries = new DictionaryEntry[4];
        var objects = new object[3];

        // act
        testLine.CopyTo(entries, 1);
        testLine.CopyTo(objects, 0);

        // assert
        var expected = new[] { Entry("A", "1"), Entry("B", "2"), Entry("C", "3") };
        entries[0].Should().Be(default(DictionaryEntry));
        if (implementation == Reference)
        {
            // The non-generic OrderedDictionary copies from its Hashtable, so its order is unspecified.  CSV lines
            // copy in field order on every target.
            entries.Skip(1).Should().BeEquivalentTo(expected);
            objects.Should().BeEquivalentTo(expected.Cast<object>());
        }
        else
        {
            entries.Skip(1).Should().Equal(expected);
            objects.Should().Equal(expected.Cast<object>());
        }
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void CopyTo_InvalidTarget_Throws(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        var nullAction = () => testLine.CopyTo(null!, 0);
        var negativeIndexAction = () => testLine.CopyTo(new object[3], -1);
        var tooSmallAction = () => testLine.CopyTo(new object[3], 1);

        // assert
        nullAction.Should().Throw<ArgumentNullException>();
        negativeIndexAction.Should().Throw<ArgumentOutOfRangeException>();
        tooSmallAction.Should().Throw<ArgumentException>();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Clear_RemovesAllFields(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act
        testLine.Clear();

        // assert
        testLine.Count.Should().Be(0);
        testLine["A"].Should().BeNull();
        Entries(testLine).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void CollectionProperties_DescribeWritableUnsynchronizedDictionary(string implementation)
    {
        // arrange
        var testLine = CreateLine(implementation);

        // act / assert
        testLine.Count.Should().Be(3);
        testLine.IsFixedSize.Should().BeFalse();
        testLine.IsReadOnly.Should().BeFalse();
        testLine.IsSynchronized.Should().BeFalse();
        testLine.SyncRoot.Should().NotBeNull();
    }

#if NET9_0_OR_GREATER
    [Fact]
    public void NonStringKey_OnGenericBackingStore_ThrowsOnWriteAndIsMissingOnRead()
    {
        // arrange
        IOrderedDictionary testLine = CreateLine(RecordLine);

        // act
        var writeActions = new Action[]
        {
            () => testLine.Add(1, "x"),
            () => ((IDictionary)testLine)[1] = "x",
            () => testLine.Insert(0, 1, "x")
        };
        var value = ((IDictionary)testLine)[1];
        var contains = testLine.Contains(1);
        testLine.Remove(1);

        // assert
        foreach (var action in writeActions)
        {
            action.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("key");
        }

        value.Should().BeNull();
        contains.Should().BeFalse();
        Entries(testLine).Should().Equal(Entry("A", "1"), Entry("B", "2"), Entry("C", "3"));
    }
#endif

    private static IOrderedDictionary CreateLine(string implementation)
    {
        IOrderedDictionary testLine = implementation switch
        {
            RecordLine => new CsvFileRecordLine(),
            Reference => new OrderedDictionary(),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation))
        };
        testLine.Add("A", "1");
        testLine.Add("B", "2");
        testLine.Add("C", "3");

        return testLine;
    }

    private static DictionaryEntry Entry(string key, object? value)
    {
        return new DictionaryEntry(key, value);
    }

    private static List<DictionaryEntry> Entries(IOrderedDictionary testLine)
    {
        return EntriesFrom(testLine.GetEnumerator());
    }

    private static List<DictionaryEntry> EntriesFrom(IDictionaryEnumerator enumerator)
    {
        var entries = new List<DictionaryEntry>();
        while (enumerator.MoveNext())
        {
            entries.Add(new DictionaryEntry(enumerator.Key, enumerator.Value));
            enumerator.Entry.Should().Be((DictionaryEntry)enumerator.Current);
        }

        return entries;
    }
}
