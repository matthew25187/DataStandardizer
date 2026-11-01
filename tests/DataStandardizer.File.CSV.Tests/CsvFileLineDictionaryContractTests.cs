using System.Collections;
using System.Collections.Specialized;
using FluentAssertions;

namespace DataStandardizer.File.CSV.Tests;

/// <summary>
/// Checks that a CSV line behaves as an <see cref="IOrderedDictionary"/> in the same way as the non-generic
/// <see cref="OrderedDictionary"/>, which each test also runs against as the reference implementation.
/// </summary>
/// <remarks>
/// Each line starts with the fields A = 1, B = 2 and C = 3, in that order.  The members that change a line return
/// nothing, so their tests look at the line's entries afterwards, in the order that enumerating it gives them.
/// </remarks>
public class CsvFileLineDictionaryContractTests
{
    private const string RecordLine = nameof(CsvFileRecordLine);
    private const string Reference = nameof(OrderedDictionary);

    // The fields each line starts with.
    private const string KeyA = "A";
    private const string KeyB = "B";
    private const string KeyC = "C";
    private const string ValueA = "1";
    private const string ValueB = "2";
    private const string ValueC = "3";
    private const int FieldCount = 3;

    // Length of an array that the fields are copied to, starting at index 1.
    private const int CopyTargetLength = 4;

    // Keys and values the tests add.
    private const string MissingKey = "Z";
    private const string NewKey = "D";
    private const string NewValue = "x";

    // Names of the members that the operations call.
    private const string AddMember = "Add";
    private const string KeyIndexerSetMember = "set key indexer";
    private const string InsertMember = "Insert";

    private static readonly DictionaryEntry EntryA = new(KeyA, ValueA);
    private static readonly DictionaryEntry EntryB = new(KeyB, ValueB);
    private static readonly DictionaryEntry EntryC = new(KeyC, ValueC);
    private static readonly DictionaryEntry NewEntry = new(NewKey, NewValue);

    public static TheoryData<LineImplementation> Implementations => new()
    {
        new LineImplementation(RecordLine, () => new CsvFileRecordLine()),
        new LineImplementation(Reference, () => new OrderedDictionary())
    };

    /// <summary>
    /// Every member that takes a key, called with a null key, for each implementation.
    /// </summary>
    public static TheoryData<LineImplementation, LineOperation> NullKeyOperations()
    {
        var operations = new[]
        {
            new LineOperation(AddMember, line => line.Add(null!, NewValue)),
            new LineOperation("get key indexer", line => _ = line[null!]),
            new LineOperation(KeyIndexerSetMember, line => line[null!] = NewValue),
            new LineOperation("Contains", line => line.Contains(null!)),
            new LineOperation("Remove", line => line.Remove(null!)),
            new LineOperation(InsertMember, line => line.Insert(0, null!, NewValue))
        };

        var data = new TheoryData<LineImplementation, LineOperation>();
        foreach (var implementation in Implementations)
        {
            foreach (var operation in operations)
            {
                data.Add(implementation, operation);
            }
        }

        return data;
    }

    /// <summary>
    /// The enumerator members that read the entry it is on, with the value each gives for the first entry.
    /// </summary>
    public static TheoryData<LineImplementation, EnumeratorMember, object> EnumeratorMembers()
    {
        var members = new (EnumeratorMember Member, object Expected)[]
        {
            (new EnumeratorMember("Current", enumerator => enumerator.Current), EntryA),
            (new EnumeratorMember("Entry", enumerator => enumerator.Entry), EntryA),
            (new EnumeratorMember("Key", enumerator => enumerator.Key), KeyA),
            (new EnumeratorMember("Value", enumerator => enumerator.Value), ValueA)
        };

        var data = new TheoryData<LineImplementation, EnumeratorMember, object>();
        foreach (var implementation in Implementations)
        {
            foreach (var (member, expected) in members)
            {
                data.Add(implementation, member, expected);
            }
        }

        return data;
    }

#if NET9_0_OR_GREATER
    /// <summary>
    /// Every member that adds a field, called with a key that isn't a string.
    /// </summary>
    public static TheoryData<LineOperation> NonStringKeyWriteOperations => new()
    {
        new LineOperation(AddMember, line => line.Add(1, NewValue)),
        new LineOperation(KeyIndexerSetMember, line => ((IDictionary)line)[1] = NewValue),
        new LineOperation(InsertMember, line => line.Insert(0, 1, NewValue))
    };
#endif

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void IntIndexer_Get_ReturnsValueAtPosition(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        var testResult = testLine[1];

        // assert
        testResult.Should().Be(ValueB);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void IntIndexer_Set_ReplacesValueAtPosition(LineImplementation implementation)
    {
        // arrange
        const string replacementValue = "two";
        var testLine = implementation.Create();

        // act
        testLine[1] = replacementValue;

        // assert
        Entries(testLine).Should().Equal(EntryA, new DictionaryEntry(KeyB, replacementValue), EntryC);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void IntIndexer_GetPastEnd_ThrowsArgumentOutOfRangeException(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        Action testAction = () => _ = testLine[FieldCount];

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void IntIndexer_SetNegativeIndex_ThrowsArgumentOutOfRangeException(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        Action testAction = () => testLine[-1] = NewValue;

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void KeyIndexer_Get_ReturnsValueForKey(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        var testResult = testLine[KeyC];

        // assert
        testResult.Should().Be(ValueC);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void KeyIndexer_GetMissingKey_ReturnsNull(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        var testResult = testLine[MissingKey];

        // assert
        testResult.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void KeyIndexer_SetExistingKey_ReplacesValueInPlace(LineImplementation implementation)
    {
        // arrange
        const string replacementValue = "one";
        var testLine = implementation.Create();

        // act
        testLine[KeyA] = replacementValue;

        // assert
        Entries(testLine).Should().Equal(new DictionaryEntry(KeyA, replacementValue), EntryB, EntryC);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void KeyIndexer_SetMissingKey_AddsAtEnd(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        testLine[NewKey] = null;

        // assert
        Entries(testLine).Should().Equal(EntryA, EntryB, EntryC, new DictionaryEntry(NewKey, null));
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Add_DuplicateKey_ThrowsArgumentException(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        Action testAction = () => testLine.Add(KeyB, NewValue);

        // assert
        testAction.Should().Throw<ArgumentException>();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Add_DuplicateKey_LeavesFieldsUnchanged(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        _ = Record.Exception(() => testLine.Add(KeyB, NewValue));

        // assert
        Entries(testLine).Should().Equal(EntryA, EntryB, EntryC);
    }

    [Theory]
    [MemberData(nameof(NullKeyOperations), DisableDiscoveryEnumeration = true)]
    public void KeyedMember_NullKey_ThrowsArgumentNullException(LineImplementation implementation, LineOperation operation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        Action testAction = () => operation.Invoke(testLine);

        // assert
        testAction.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Insert_AtStart_AddsBeforeFirstField(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        testLine.Insert(0, NewKey, NewValue);

        // assert
        Entries(testLine).Should().Equal(NewEntry, EntryA, EntryB, EntryC);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Insert_InMiddle_AddsAtPosition(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        testLine.Insert(1, NewKey, NewValue);

        // assert
        Entries(testLine).Should().Equal(EntryA, NewEntry, EntryB, EntryC);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Insert_AtEnd_AddsAfterLastField(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        testLine.Insert(FieldCount, NewKey, NewValue);

        // assert
        Entries(testLine).Should().Equal(EntryA, EntryB, EntryC, NewEntry);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void KeyIndexer_AfterInsertInMiddle_ReturnsInsertedValue(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        testLine.Insert(1, NewKey, NewValue);

        // act
        var testResult = testLine[NewKey];

        // assert
        testResult.Should().Be(NewValue);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Insert_DuplicateKey_ThrowsArgumentException(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        Action testAction = () => testLine.Insert(0, KeyC, NewValue);

        // assert
        testAction.Should().Throw<ArgumentException>();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Insert_IndexPastEnd_ThrowsArgumentOutOfRangeException(LineImplementation implementation)
    {
        // arrange
        const int indexPastEnd = 4;
        var testLine = implementation.Create();

        // act
        Action testAction = () => testLine.Insert(indexPastEnd, NewKey, NewValue);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void RemoveAt_FirstPosition_RemovesField(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        testLine.RemoveAt(0);

        // assert
        Entries(testLine).Should().Equal(EntryB, EntryC);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Contains_AfterRemoveAt_ReturnsFalseForRemovedKey(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        testLine.RemoveAt(0);

        // act
        var testResult = testLine.Contains(KeyA);

        // assert
        testResult.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void IntIndexer_AfterRemoveAt_ReturnsValueAtNewPosition(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        testLine.RemoveAt(0);

        // act
        var testResult = testLine[0];

        // assert
        testResult.Should().Be(ValueB);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void RemoveAt_PastEnd_ThrowsArgumentOutOfRangeException(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        Action testAction = () => testLine.RemoveAt(FieldCount);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Remove_ExistingKey_RemovesField(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        testLine.Remove(KeyB);

        // assert
        Entries(testLine).Should().Equal(EntryA, EntryC);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Remove_MissingKey_LeavesFieldsUnchanged(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        testLine.Remove(MissingKey);

        // assert
        Entries(testLine).Should().Equal(EntryA, EntryB, EntryC);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void IntIndexer_AfterRemove_ReturnsValueAtNewPosition(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        testLine.Remove(KeyB);

        // act
        var testResult = testLine[1];

        // assert
        testResult.Should().Be(ValueC);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Contains_PresentKey_ReturnsTrue(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        var testResult = testLine.Contains(KeyA);

        // assert
        testResult.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Contains_MissingKey_ReturnsFalse(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        var testResult = testLine.Contains(MissingKey);

        // assert
        testResult.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Contains_ValueNotKey_ReturnsFalse(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        var testResult = testLine.Contains(ValueA);

        // assert
        testResult.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void GetEnumerator_AfterInsertsAndRemovals_FollowsPositionOrder(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.CreateRearranged();

        // act
        var testResult = testLine.GetEnumerator();

        // assert
        Entries(testResult).Should().Equal(NewEntry, EntryC, EntryA);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void DictionaryGetEnumerator_AfterInsertsAndRemovals_FollowsPositionOrder(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.CreateRearranged();

        // act
        var testResult = ((IDictionary)testLine).GetEnumerator();

        // assert
        Entries(testResult).Should().Equal(NewEntry, EntryC, EntryA);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void EnumerableGetEnumerator_AfterInsertsAndRemovals_FollowsPositionOrderAsDictionaryEntries(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.CreateRearranged();

        // act
        var testResult = ((IEnumerable)testLine).GetEnumerator();

        // assert
        Entries(testResult).Should().Equal(NewEntry, EntryC, EntryA);
    }

    [Theory]
    [MemberData(nameof(EnumeratorMembers), DisableDiscoveryEnumeration = true)]
    public void EnumeratorMember_AfterFirstMoveNext_ReturnsFirstEntry(LineImplementation implementation, EnumeratorMember member, object expected)
    {
        // arrange
        var enumerator = implementation.Create().GetEnumerator();
        enumerator.MoveNext();

        // act
        var testResult = member.Read(enumerator);

        // assert
        testResult.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void MoveNext_AfterLineModified_ThrowsInvalidOperationException(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        var enumerator = testLine.GetEnumerator();
        enumerator.MoveNext();
        testLine.Add(NewKey, NewValue);

        // act
        Action testAction = () => enumerator.MoveNext();

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Keys_AfterInsertAtStart_FollowsPositionOrder(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        testLine.Insert(0, NewKey, NewValue);

        // act
        var testResult = testLine.Keys;

        // assert
        testResult.Should().BeEquivalentTo(new[] { NewKey, KeyA, KeyB, KeyC }, options => options.WithStrictOrdering());
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Values_AfterInsertAtStart_FollowsPositionOrder(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        testLine.Insert(0, NewKey, NewValue);

        // act
        var testResult = testLine.Values;

        // assert
        testResult.Should().BeEquivalentTo(new[] { NewValue, ValueA, ValueB, ValueC }, options => options.WithStrictOrdering());
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void KeysCount_AfterInsertAtStart_ReturnsFieldCount(LineImplementation implementation)
    {
        // arrange
        const int expectedCount = 4;
        var testLine = implementation.Create();
        testLine.Insert(0, NewKey, NewValue);

        // act
        var testResult = testLine.Keys.Count;

        // assert
        testResult.Should().Be(expectedCount);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void ValuesCount_AfterInsertAtStart_ReturnsFieldCount(LineImplementation implementation)
    {
        // arrange
        const int expectedCount = 4;
        var testLine = implementation.Create();
        testLine.Insert(0, NewKey, NewValue);

        // act
        var testResult = testLine.Values.Count;

        // assert
        testResult.Should().Be(expectedCount);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void KeysCopyTo_IndexAfterStart_CopiesInPositionOrder(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        var keys = new object?[CopyTargetLength];

        // act
        testLine.Keys.CopyTo(keys, 1);

        // assert
        keys.Should().Equal(new object?[] { null, KeyA, KeyB, KeyC });
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void ValuesCopyTo_IndexAfterStart_CopiesInPositionOrder(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        var values = new object?[CopyTargetLength];

        // act
        testLine.Values.CopyTo(values, 1);

        // assert
        values.Should().Equal(new object?[] { null, ValueA, ValueB, ValueC });
    }

    [Fact]
    public void CopyTo_RecordLineToDictionaryEntryArray_CopiesInPositionOrder()
    {
        // arrange
        IOrderedDictionary testLine = CreateRecordLine();
        var entries = new DictionaryEntry[CopyTargetLength];

        // act
        testLine.CopyTo(entries, 1);

        // assert
        entries.Should().Equal(default(DictionaryEntry), EntryA, EntryB, EntryC);
    }

    [Fact]
    public void CopyTo_ReferenceToDictionaryEntryArray_CopiesEveryEntry()
    {
        // arrange
        // The non-generic OrderedDictionary copies from its Hashtable, so the order of what it copies is unspecified.
        IOrderedDictionary testLine = CreateReferenceLine();
        var entries = new DictionaryEntry[CopyTargetLength];

        // act
        testLine.CopyTo(entries, 1);

        // assert
        entries[0].Should().Be(default(DictionaryEntry));
        entries.Should().BeEquivalentTo(new[] { default(DictionaryEntry), EntryA, EntryB, EntryC });
    }

    [Fact]
    public void CopyTo_RecordLineToObjectArray_CopiesInPositionOrder()
    {
        // arrange
        IOrderedDictionary testLine = CreateRecordLine();
        var entries = new object[FieldCount];

        // act
        testLine.CopyTo(entries, 0);

        // assert
        entries.Should().Equal(EntryA, EntryB, EntryC);
    }

    [Fact]
    public void CopyTo_ReferenceToObjectArray_CopiesEveryEntry()
    {
        // arrange
        // The non-generic OrderedDictionary copies from its Hashtable, so the order of what it copies is unspecified.
        IOrderedDictionary testLine = CreateReferenceLine();
        var entries = new object[FieldCount];

        // act
        testLine.CopyTo(entries, 0);

        // assert
        entries.Should().BeEquivalentTo(new object[] { EntryA, EntryB, EntryC });
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void CopyTo_NullArray_ThrowsArgumentNullException(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        Action testAction = () => testLine.CopyTo(null!, 0);

        // assert
        testAction.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void CopyTo_NegativeIndex_ThrowsArgumentOutOfRangeException(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        var entries = new object[FieldCount];

        // act
        Action testAction = () => testLine.CopyTo(entries, -1);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void CopyTo_ArrayTooSmallForIndex_ThrowsArgumentException(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        var entries = new object[FieldCount];

        // act
        Action testAction = () => testLine.CopyTo(entries, 1);

        // assert
        testAction.Should().Throw<ArgumentException>();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Clear_ThreeFields_RemovesAllFields(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        testLine.Clear();

        // assert
        Entries(testLine).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Count_AfterClear_ReturnsZero(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        testLine.Clear();

        // act
        var testResult = testLine.Count;

        // assert
        testResult.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void KeyIndexer_AfterClear_ReturnsNull(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();
        testLine.Clear();

        // act
        var testResult = testLine[KeyA];

        // assert
        testResult.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void Count_ThreeFields_ReturnsThree(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        var testResult = testLine.Count;

        // assert
        testResult.Should().Be(FieldCount);
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void IsFixedSize_WritableLine_ReturnsFalse(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        var testResult = testLine.IsFixedSize;

        // assert
        testResult.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void IsReadOnly_WritableLine_ReturnsFalse(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        var testResult = testLine.IsReadOnly;

        // assert
        testResult.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void IsSynchronized_UnsynchronizedLine_ReturnsFalse(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        var testResult = testLine.IsSynchronized;

        // assert
        testResult.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Implementations), DisableDiscoveryEnumeration = true)]
    public void SyncRoot_UnsynchronizedLine_ReturnsObject(LineImplementation implementation)
    {
        // arrange
        var testLine = implementation.Create();

        // act
        var testResult = testLine.SyncRoot;

        // assert
        testResult.Should().NotBeNull();
    }

#if NET9_0_OR_GREATER
    [Theory]
    [MemberData(nameof(NonStringKeyWriteOperations), DisableDiscoveryEnumeration = true)]
    public void KeyedWriteMember_NonStringKeyOnGenericBackingStore_ThrowsArgumentException(LineOperation operation)
    {
        // arrange
        IOrderedDictionary testLine = CreateRecordLine();

        // act
        Action testAction = () => operation.Invoke(testLine);

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("key");
    }

    [Fact]
    public void KeyIndexer_GetNonStringKeyOnGenericBackingStore_ReturnsNull()
    {
        // arrange
        IDictionary testLine = CreateRecordLine();

        // act
        var testResult = testLine[1];

        // assert
        testResult.Should().BeNull();
    }

    [Fact]
    public void Contains_NonStringKeyOnGenericBackingStore_ReturnsFalse()
    {
        // arrange
        IOrderedDictionary testLine = CreateRecordLine();

        // act
        var testResult = testLine.Contains(1);

        // assert
        testResult.Should().BeFalse();
    }

    [Fact]
    public void Remove_NonStringKeyOnGenericBackingStore_LeavesFieldsUnchanged()
    {
        // arrange
        IOrderedDictionary testLine = CreateRecordLine();

        // act
        testLine.Remove(1);

        // assert
        Entries(testLine).Should().Equal(EntryA, EntryB, EntryC);
    }
#endif

    private static CsvFileRecordLine CreateRecordLine() => (CsvFileRecordLine)AddFields(new CsvFileRecordLine());

    private static OrderedDictionary CreateReferenceLine() => (OrderedDictionary)AddFields(new OrderedDictionary());

    private static IOrderedDictionary AddFields(IOrderedDictionary line)
    {
        line.Add(KeyA, ValueA);
        line.Add(KeyB, ValueB);
        line.Add(KeyC, ValueC);

        return line;
    }

    /// <summary>
    /// Gets the entries of a line, in the order that enumerating it gives them.
    /// </summary>
    private static List<DictionaryEntry> Entries(IEnumerable line) => Entries(line.GetEnumerator());

    /// <summary>
    /// Gets the entries that remain in an enumerator of a line.
    /// </summary>
    private static List<DictionaryEntry> Entries(IEnumerator enumerator) => new(new EntrySequence(enumerator));

    /// <summary>
    /// An implementation of <see cref="IOrderedDictionary"/> that a test runs against.
    /// </summary>
    public sealed class LineImplementation(string name, Func<IOrderedDictionary> createEmpty)
    {
        /// <summary>
        /// Creates a line with the fields A = 1, B = 2 and C = 3.
        /// </summary>
        public IOrderedDictionary Create() => AddFields(createEmpty());

        /// <summary>
        /// Creates a line whose fields have been removed, inserted and added, so that it holds D = x, C = 3 and A = 1.
        /// </summary>
        public IOrderedDictionary CreateRearranged()
        {
            var line = Create();
            line.Remove(KeyA);
            line.Insert(1, NewKey, NewValue);
            line.Add(KeyA, ValueA);
            line.RemoveAt(0);

            return line;
        }

        public override string ToString() => name;
    }

    /// <summary>
    /// A call to a member of a line.
    /// </summary>
    public sealed class LineOperation(string name, Action<IOrderedDictionary> operation)
    {
        public void Invoke(IOrderedDictionary line) => operation(line);

        public override string ToString() => name;
    }

    /// <summary>
    /// A member of a line's enumerator that reads the entry the enumerator is on.
    /// </summary>
    public sealed class EnumeratorMember(string name, Func<IDictionaryEnumerator, object?> read)
    {
        public object? Read(IDictionaryEnumerator enumerator) => read(enumerator);

        public override string ToString() => name;
    }

    /// <summary>
    /// The entries that remain in an enumerator of a line.
    /// </summary>
    private sealed class EntrySequence(IEnumerator enumerator) : IEnumerable<DictionaryEntry>
    {
        public IEnumerator<DictionaryEntry> GetEnumerator() => new EntryEnumerator(enumerator);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class EntryEnumerator(IEnumerator enumerator) : IEnumerator<DictionaryEntry>
    {
        public DictionaryEntry Current => (DictionaryEntry)enumerator.Current!;

        object IEnumerator.Current => Current;

        public bool MoveNext() => enumerator.MoveNext();

        public void Reset() => enumerator.Reset();

        public void Dispose()
        {
        }
    }
}
