using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// Checks that a CSV line behaves as an ordered, string-keyed collection of fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The project runs these tests once for each target framework, and the line has a different backing store on each: the
    /// <c>net10.0</c> build of the package uses the generic <c>OrderedDictionary&lt;string, object?&gt;</c>, and the
    /// <c>netstandard2.0</c> build that <c>net48</c> gets uses the package's own ordered store.  The expected results are the
    /// ones the non-generic <c>OrderedDictionary</c> gave in version 1.x for positions, order and exceptions.
    /// </para>
    /// <para>
    /// Each line starts with the fields A = 1, B = 2 and C = 3, in that order.  The members that change a line return
    /// nothing, so their tests look at the line's entries afterwards, in the order that enumerating it gives them.
    /// </para>
    /// </remarks>
    public class CsvFileLineDictionaryContractTests
    {
        // The fields each line starts with.
        private const string KeyA = "A";
        private const string KeyB = "B";
        private const string KeyC = "C";
        private const string ValueA = "1";
        private const string ValueB = "2";
        private const string ValueC = "3";
        private const int FieldCount = 3;

        // Keys and values the tests add.
        private const string MissingKey = "Z";
        private const string NewKey = "D";
        private const string NewValue = "x";
        private const string OtherNewKey = "E";
        private const string OtherNewValue = "y";

        private const string ReplacementValue = "two";
        private const int NotFound = -1;

        private static readonly KeyValuePair<string, object> EntryA = new KeyValuePair<string, object>(KeyA, ValueA);
        private static readonly KeyValuePair<string, object> EntryB = new KeyValuePair<string, object>(KeyB, ValueB);
        private static readonly KeyValuePair<string, object> EntryC = new KeyValuePair<string, object>(KeyC, ValueC);
        private static readonly KeyValuePair<string, object> NewEntry = new KeyValuePair<string, object>(NewKey, NewValue);

        /// <summary>
        /// Every member that takes a field name, called with a null name.
        /// </summary>
        public static TheoryData<LineOperation> NullKeyOperations => new TheoryData<LineOperation>
        {
            new LineOperation("Add", line => line.Add(null, NewValue)),
            new LineOperation("get key indexer", line => _ = line[null]),
            new LineOperation("set key indexer", line => line[null] = NewValue),
            new LineOperation("Contains", line => line.Contains(null)),
            new LineOperation("IndexOf", line => line.IndexOf(null)),
            new LineOperation("TryGetValue", line => line.TryGetValue(null, out _)),
            new LineOperation("Remove", line => line.Remove(null)),
            new LineOperation("Insert", line => line.Insert(0, null, NewValue))
        };

        /// <summary>
        /// Every member that takes a position, called with a position past the last field.
        /// </summary>
        public static TheoryData<LineOperation> IndexPastEndOperations => new TheoryData<LineOperation>
        {
            new LineOperation("get int indexer", line => _ = line[FieldCount]),
            new LineOperation("set int indexer", line => line[FieldCount] = NewValue),
            new LineOperation("GetAt", line => line.GetAt(FieldCount)),
            new LineOperation("RemoveAt", line => line.RemoveAt(FieldCount)),
            new LineOperation("Insert", line => line.Insert(FieldCount + 1, NewKey, NewValue)),
            new LineOperation("Keys indexer", line => _ = line.Keys[FieldCount]),
            new LineOperation("Values indexer", line => _ = line.Values[FieldCount])
        };

        /// <summary>
        /// Every member that takes a position, called with a negative position.
        /// </summary>
        public static TheoryData<LineOperation> NegativeIndexOperations => new TheoryData<LineOperation>
        {
            new LineOperation("get int indexer", line => _ = line[-1]),
            new LineOperation("set int indexer", line => line[-1] = NewValue),
            new LineOperation("GetAt", line => line.GetAt(-1)),
            new LineOperation("RemoveAt", line => line.RemoveAt(-1)),
            new LineOperation("Insert", line => line.Insert(-1, NewKey, NewValue)),
            new LineOperation("Keys indexer", line => _ = line.Keys[-1]),
            new LineOperation("Values indexer", line => _ = line.Values[-1])
        };

        /// <summary>
        /// Every change that adds or removes a field, which invalidates an enumerator of the line.
        /// </summary>
        public static TheoryData<LineOperation> StructuralChanges => new TheoryData<LineOperation>
        {
            new LineOperation("Add", line => line.Add(NewKey, NewValue)),
            new LineOperation("set key indexer to a new key", line => line[NewKey] = NewValue),
            new LineOperation("Insert", line => line.Insert(0, NewKey, NewValue)),
            new LineOperation("Remove", line => line.Remove(KeyB)),
            new LineOperation("RemoveAt", line => line.RemoveAt(1)),
            new LineOperation("Clear", line => line.Clear())
        };

        /// <summary>
        /// Every change that leaves the fields of the line in place, which doesn't invalidate an enumerator of the line.
        /// </summary>
        public static TheoryData<LineOperation> InPlaceChanges => new TheoryData<LineOperation>
        {
            new LineOperation("set key indexer to an existing key", line => line[KeyB] = ReplacementValue),
            new LineOperation("set int indexer", line => line[1] = ReplacementValue),
            new LineOperation("Remove a missing key", line => line.Remove(MissingKey))
        };

        /// <summary>
        /// The ways to enumerate a line, its field names and its values, each with the change that invalidates it.
        /// </summary>
        public static TheoryData<LineEnumeration> Enumerations => new TheoryData<LineEnumeration>
        {
            new LineEnumeration("line", line => ((IEnumerable<KeyValuePair<string, object>>)line).GetEnumerator()),
            new LineEnumeration("line as IEnumerable", line => ((IEnumerable)line).GetEnumerator()),
            new LineEnumeration("Keys", line => line.Keys.GetEnumerator()),
            new LineEnumeration("Values", line => line.Values.GetEnumerator())
        };

        [Fact]
        public void IntIndexer_Get_ReturnsValueAtPosition()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine[1];

            // assert
            testResult.Should().Be(ValueB);
        }

        [Fact]
        public void IntIndexer_Set_ReplacesValueAtPosition()
        {
            // arrange
            var testLine = CreateLine();

            // act
            testLine[1] = ReplacementValue;

            // assert
            Entries(testLine).Should().Equal(EntryA, new KeyValuePair<string, object>(KeyB, ReplacementValue), EntryC);
        }

        [Theory]
        [MemberData(nameof(IndexPastEndOperations), DisableDiscoveryEnumeration = true)]
        public void PositionalMember_IndexPastEnd_ThrowsArgumentOutOfRangeException(LineOperation operation)
        {
            // arrange
            var testLine = CreateLine();

            // act
            Action testAction = () => operation.Invoke(testLine);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [MemberData(nameof(NegativeIndexOperations), DisableDiscoveryEnumeration = true)]
        public void PositionalMember_NegativeIndex_ThrowsArgumentOutOfRangeException(LineOperation operation)
        {
            // arrange
            var testLine = CreateLine();

            // act
            Action testAction = () => operation.Invoke(testLine);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void KeyIndexer_Get_ReturnsValueForKey()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine[KeyC];

            // assert
            testResult.Should().Be(ValueC);
        }

        [Fact]
        public void KeyIndexer_GetMissingKey_ReturnsNull()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine[MissingKey];

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void KeyIndexer_GetKeyInDifferentCase_ReturnsNull()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine["a"];

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void KeyIndexer_SetExistingKey_ReplacesValueInPlace()
        {
            // arrange
            var testLine = CreateLine();

            // act
            testLine[KeyA] = ReplacementValue;

            // assert
            Entries(testLine).Should().Equal(new KeyValuePair<string, object>(KeyA, ReplacementValue), EntryB, EntryC);
        }

        [Fact]
        public void KeyIndexer_SetMissingKey_AddsAtEnd()
        {
            // arrange
            var testLine = CreateLine();

            // act
            testLine[NewKey] = null;

            // assert
            Entries(testLine).Should().Equal(EntryA, EntryB, EntryC, new KeyValuePair<string, object>(NewKey, null));
        }

        [Fact]
        public void Add_NewKey_AddsAtEnd()
        {
            // arrange
            var testLine = CreateLine();

            // act
            testLine.Add(NewKey, NewValue);

            // assert
            Entries(testLine).Should().Equal(EntryA, EntryB, EntryC, NewEntry);
        }

        [Fact]
        public void Add_PastInitialCapacity_KeepsPositionOrder()
        {
            // arrange
            var testLine = CreateLine();
            testLine.Add(NewKey, NewValue);

            // act
            testLine.Add(OtherNewKey, OtherNewValue);

            // assert
            Entries(testLine).Should().Equal(EntryA, EntryB, EntryC, NewEntry, new KeyValuePair<string, object>(OtherNewKey, OtherNewValue));
        }

        [Fact]
        public void Add_DuplicateKey_ThrowsArgumentException()
        {
            // arrange
            var testLine = CreateLine();

            // act
            Action testAction = () => testLine.Add(KeyB, NewValue);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Add_DuplicateKey_LeavesFieldsUnchanged()
        {
            // arrange
            var testLine = CreateLine();

            // act
            _ = Record.Exception(() => testLine.Add(KeyB, NewValue));

            // assert
            Entries(testLine).Should().Equal(EntryA, EntryB, EntryC);
        }

        [Theory]
        [MemberData(nameof(NullKeyOperations), DisableDiscoveryEnumeration = true)]
        public void KeyedMember_NullKey_ThrowsArgumentNullException(LineOperation operation)
        {
            // arrange
            var testLine = CreateLine();

            // act
            Action testAction = () => operation.Invoke(testLine);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Insert_AtStart_AddsBeforeFirstField()
        {
            // arrange
            var testLine = CreateLine();

            // act
            testLine.Insert(0, NewKey, NewValue);

            // assert
            Entries(testLine).Should().Equal(NewEntry, EntryA, EntryB, EntryC);
        }

        [Fact]
        public void Insert_InMiddle_AddsAtPosition()
        {
            // arrange
            var testLine = CreateLine();

            // act
            testLine.Insert(1, NewKey, NewValue);

            // assert
            Entries(testLine).Should().Equal(EntryA, NewEntry, EntryB, EntryC);
        }

        [Fact]
        public void Insert_AtEnd_AddsAfterLastField()
        {
            // arrange
            var testLine = CreateLine();

            // act
            testLine.Insert(FieldCount, NewKey, NewValue);

            // assert
            Entries(testLine).Should().Equal(EntryA, EntryB, EntryC, NewEntry);
        }

        [Fact]
        public void KeyIndexer_AfterInsertInMiddle_ReturnsInsertedValue()
        {
            // arrange
            var testLine = CreateLine();
            testLine.Insert(1, NewKey, NewValue);

            // act
            var testResult = testLine[NewKey];

            // assert
            testResult.Should().Be(NewValue);
        }

        [Theory]
        [InlineData(KeyA, 0)]
        [InlineData(NewKey, 1)]
        [InlineData(KeyB, 2)]
        [InlineData(KeyC, 3)]
        public void IndexOf_AfterInsertInMiddle_ReturnsShiftedPosition(string key, int expected)
        {
            // arrange
            var testLine = CreateLine();
            testLine.Insert(1, NewKey, NewValue);

            // act
            var testResult = testLine.IndexOf(key);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Insert_DuplicateKey_ThrowsArgumentException()
        {
            // arrange
            var testLine = CreateLine();

            // act
            Action testAction = () => testLine.Insert(0, KeyC, NewValue);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Insert_DuplicateKey_LeavesFieldsUnchanged()
        {
            // arrange
            var testLine = CreateLine();

            // act
            _ = Record.Exception(() => testLine.Insert(0, KeyC, NewValue));

            // assert
            Entries(testLine).Should().Equal(EntryA, EntryB, EntryC);
        }

        [Fact]
        public void Insert_NullKeyAndIndexPastEnd_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            var testLine = CreateLine();

            // act
            Action testAction = () => testLine.Insert(FieldCount + 1, null, NewValue);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void RemoveAt_FirstPosition_RemovesField()
        {
            // arrange
            var testLine = CreateLine();

            // act
            testLine.RemoveAt(0);

            // assert
            Entries(testLine).Should().Equal(EntryB, EntryC);
        }

        [Fact]
        public void Contains_AfterRemoveAt_ReturnsFalseForRemovedKey()
        {
            // arrange
            var testLine = CreateLine();
            testLine.RemoveAt(0);

            // act
            var testResult = testLine.Contains(KeyA);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IntIndexer_AfterRemoveAt_ReturnsValueAtNewPosition()
        {
            // arrange
            var testLine = CreateLine();
            testLine.RemoveAt(0);

            // act
            var testResult = testLine[0];

            // assert
            testResult.Should().Be(ValueB);
        }

        [Theory]
        [InlineData(KeyA, NotFound)]
        [InlineData(KeyB, 0)]
        [InlineData(KeyC, 1)]
        public void IndexOf_AfterRemoveAt_ReturnsShiftedPosition(string key, int expected)
        {
            // arrange
            var testLine = CreateLine();
            testLine.RemoveAt(0);

            // act
            var testResult = testLine.IndexOf(key);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Remove_ExistingKey_RemovesField()
        {
            // arrange
            var testLine = CreateLine();

            // act
            testLine.Remove(KeyB);

            // assert
            Entries(testLine).Should().Equal(EntryA, EntryC);
        }

        [Fact]
        public void Remove_ExistingKey_ReturnsTrue()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine.Remove(KeyB);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Remove_MissingKey_LeavesFieldsUnchanged()
        {
            // arrange
            var testLine = CreateLine();

            // act
            testLine.Remove(MissingKey);

            // assert
            Entries(testLine).Should().Equal(EntryA, EntryB, EntryC);
        }

        [Fact]
        public void Remove_MissingKey_ReturnsFalse()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine.Remove(MissingKey);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IntIndexer_AfterRemove_ReturnsValueAtNewPosition()
        {
            // arrange
            var testLine = CreateLine();
            testLine.Remove(KeyB);

            // act
            var testResult = testLine[1];

            // assert
            testResult.Should().Be(ValueC);
        }

        [Theory]
        [InlineData(KeyA, 0)]
        [InlineData(KeyB, NotFound)]
        [InlineData(KeyC, 1)]
        public void IndexOf_AfterRemove_ReturnsShiftedPosition(string key, int expected)
        {
            // arrange
            var testLine = CreateLine();
            testLine.Remove(KeyB);

            // act
            var testResult = testLine.IndexOf(key);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(NewKey, 0)]
        [InlineData(KeyC, 1)]
        [InlineData(KeyA, 2)]
        [InlineData(KeyB, NotFound)]
        public void IndexOf_AfterInsertsAndRemovals_ReturnsPosition(string key, int expected)
        {
            // arrange
            var testLine = CreateRearrangedLine();

            // act
            var testResult = testLine.IndexOf(key);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Contains_PresentKey_ReturnsTrue()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine.Contains(KeyA);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Contains_MissingKey_ReturnsFalse()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine.Contains(MissingKey);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Contains_ValueNotKey_ReturnsFalse()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine.Contains(ValueA);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IndexOf_MissingKey_ReturnsMinusOne()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine.IndexOf(MissingKey);

            // assert
            testResult.Should().Be(NotFound);
        }

        [Fact]
        public void TryGetValue_PresentKey_ReturnsTrueAndValue()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine.TryGetValue(KeyB, out var value);

            // assert
            testResult.Should().BeTrue();
            value.Should().Be(ValueB);
        }

        [Fact]
        public void TryGetValue_PresentKeyWithNullValue_ReturnsTrueAndNull()
        {
            // arrange
            var testLine = CreateLine();
            testLine.Add(NewKey, null);

            // act
            var testResult = testLine.TryGetValue(NewKey, out var value);

            // assert
            testResult.Should().BeTrue();
            value.Should().BeNull();
        }

        [Fact]
        public void TryGetValue_MissingKey_ReturnsFalseAndNull()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine.TryGetValue(MissingKey, out var value);

            // assert
            testResult.Should().BeFalse();
            value.Should().BeNull();
        }

        [Fact]
        public void GetAt_MiddlePosition_ReturnsEntry()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine.GetAt(1);

            // assert
            testResult.Should().Be(EntryB);
        }

        [Fact]
        public void GetEnumerator_AfterInsertsAndRemovals_FollowsPositionOrder()
        {
            // arrange
            var testLine = CreateRearrangedLine();

            // act
            var testResult = ((IEnumerable<KeyValuePair<string, object>>)testLine).GetEnumerator();

            // assert
            Remaining(testResult).Should().Equal(NewEntry, EntryC, EntryA);
        }

        [Fact]
        public void EnumerableGetEnumerator_AfterInsertsAndRemovals_FollowsPositionOrderAsKeyValuePairs()
        {
            // arrange
            var testLine = CreateRearrangedLine();

            // act
            var testResult = ((IEnumerable)testLine).GetEnumerator();

            // assert
            Remaining(testResult).Should().Equal(NewEntry, EntryC, EntryA);
        }

        [Theory]
        [MemberData(nameof(Enumerations), DisableDiscoveryEnumeration = true)]
        public void MoveNext_AfterFieldAdded_ThrowsInvalidOperationException(LineEnumeration enumeration)
        {
            // arrange
            var testLine = CreateLine();
            var enumerator = enumeration.Start(testLine);
            enumerator.MoveNext();
            testLine.Add(NewKey, NewValue);

            // act
            Action testAction = () => enumerator.MoveNext();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [MemberData(nameof(Enumerations), DisableDiscoveryEnumeration = true)]
        public void MoveNext_FieldAddedBeforeFirstMoveNext_ThrowsInvalidOperationException(LineEnumeration enumeration)
        {
            // arrange
            var testLine = CreateLine();
            var enumerator = enumeration.Start(testLine);
            testLine.Add(NewKey, NewValue);

            // act
            Action testAction = () => enumerator.MoveNext();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [MemberData(nameof(Enumerations), DisableDiscoveryEnumeration = true)]
        public void MoveNext_FieldAddedAfterEnumerationEnded_ThrowsInvalidOperationException(LineEnumeration enumeration)
        {
            // arrange
            var testLine = CreateLine();
            var enumerator = enumeration.Start(testLine);
            _ = RemainingItems(enumerator);
            testLine.Add(NewKey, NewValue);

            // act
            Action testAction = () => enumerator.MoveNext();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [MemberData(nameof(Enumerations), DisableDiscoveryEnumeration = true)]
        public void Reset_AfterFieldAdded_ThrowsInvalidOperationException(LineEnumeration enumeration)
        {
            // arrange
            var testLine = CreateLine();
            var enumerator = enumeration.Start(testLine);
            enumerator.MoveNext();
            testLine.Add(NewKey, NewValue);

            // act
            Action testAction = () => enumerator.Reset();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [MemberData(nameof(Enumerations), DisableDiscoveryEnumeration = true)]
        public void Reset_ValueReplacedAfterEnumerationEnded_DoesNotThrow(LineEnumeration enumeration)
        {
            // arrange
            var testLine = CreateLine();
            var enumerator = enumeration.Start(testLine);
            _ = RemainingItems(enumerator);
            testLine[KeyA] = ReplacementValue;

            // act
            Action testAction = () => enumerator.Reset();

            // assert
            testAction.Should().NotThrow();
        }

        [Theory]
        [MemberData(nameof(StructuralChanges), DisableDiscoveryEnumeration = true)]
        public void MoveNext_AfterStructuralChange_ThrowsInvalidOperationException(LineOperation change)
        {
            // arrange
            var testLine = CreateLine();
            var enumerator = ((IEnumerable<KeyValuePair<string, object>>)testLine).GetEnumerator();
            enumerator.MoveNext();
            change.Invoke(testLine);

            // act
            Action testAction = () => enumerator.MoveNext();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [MemberData(nameof(InPlaceChanges), DisableDiscoveryEnumeration = true)]
        public void MoveNext_AfterInPlaceChange_DoesNotThrow(LineOperation change)
        {
            // arrange
            var testLine = CreateLine();
            var enumerator = ((IEnumerable<KeyValuePair<string, object>>)testLine).GetEnumerator();
            enumerator.MoveNext();
            change.Invoke(testLine);

            // act
            Action testAction = () => enumerator.MoveNext();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void MoveNext_AfterValueReplacedAhead_ReturnsReplacedValue()
        {
            // arrange
            var testLine = CreateLine();
            var enumerator = ((IEnumerable<KeyValuePair<string, object>>)testLine).GetEnumerator();
            enumerator.MoveNext();
            testLine[KeyB] = ReplacementValue;

            // act
            var testResult = Remaining(enumerator);

            // assert
            testResult.Should().Equal(new KeyValuePair<string, object>(KeyB, ReplacementValue), EntryC);
        }

        [Fact]
        public void MoveNext_AfterEmptyLineCleared_DoesNotThrow()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            var enumerator = ((IEnumerable<KeyValuePair<string, object>>)testLine).GetEnumerator();
            testLine.Clear();

            // act
            Action testAction = () => enumerator.MoveNext();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void Keys_AfterInsertAtStart_FollowsPositionOrder()
        {
            // arrange
            var testLine = CreateLine();
            testLine.Insert(0, NewKey, NewValue);

            // act
            var testResult = testLine.Keys;

            // assert
            testResult.Should().Equal(NewKey, KeyA, KeyB, KeyC);
        }

        [Fact]
        public void Values_AfterInsertAtStart_FollowsPositionOrder()
        {
            // arrange
            var testLine = CreateLine();
            testLine.Insert(0, NewKey, NewValue);

            // act
            var testResult = testLine.Values;

            // assert
            testResult.Should().Equal(NewValue, ValueA, ValueB, ValueC);
        }

        [Fact]
        public void Keys_ReadAfterRemoveAt_ReflectsLine()
        {
            // arrange
            var testLine = CreateLine();
            var testResult = testLine.Keys;

            // act
            testLine.RemoveAt(0);

            // assert
            testResult.Should().Equal(KeyB, KeyC);
        }

        [Fact]
        public void Values_ReadAfterValueReplaced_ReflectsLine()
        {
            // arrange
            var testLine = CreateLine();
            var testResult = testLine.Values;

            // act
            testLine[KeyC] = ReplacementValue;

            // assert
            testResult.Should().Equal(ValueA, ValueB, ReplacementValue);
        }

        [Fact]
        public void KeysIndexer_LastPosition_ReturnsKey()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine.Keys[2];

            // assert
            testResult.Should().Be(KeyC);
        }

        [Fact]
        public void ValuesIndexer_LastPosition_ReturnsValue()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine.Values[2];

            // assert
            testResult.Should().Be(ValueC);
        }

        [Fact]
        public void KeysCount_AfterInsertAtStart_ReturnsFieldCount()
        {
            // arrange
            const int expectedCount = 4;
            var testLine = CreateLine();
            testLine.Insert(0, NewKey, NewValue);

            // act
            var testResult = testLine.Keys.Count;

            // assert
            testResult.Should().Be(expectedCount);
        }

        [Fact]
        public void ValuesCount_AfterInsertAtStart_ReturnsFieldCount()
        {
            // arrange
            const int expectedCount = 4;
            var testLine = CreateLine();
            testLine.Insert(0, NewKey, NewValue);

            // act
            var testResult = testLine.Values.Count;

            // assert
            testResult.Should().Be(expectedCount);
        }

        [Fact]
        public void Clear_ThreeFields_RemovesAllFields()
        {
            // arrange
            var testLine = CreateLine();

            // act
            testLine.Clear();

            // assert
            Entries(testLine).Should().BeEmpty();
        }

        [Fact]
        public void Count_AfterClear_ReturnsZero()
        {
            // arrange
            var testLine = CreateLine();
            testLine.Clear();

            // act
            var testResult = testLine.Count;

            // assert
            testResult.Should().Be(0);
        }

        [Fact]
        public void KeyIndexer_AfterClear_ReturnsNull()
        {
            // arrange
            var testLine = CreateLine();
            testLine.Clear();

            // act
            var testResult = testLine[KeyA];

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void Add_AfterClear_AddsAtStart()
        {
            // arrange
            var testLine = CreateLine();
            testLine.Clear();

            // act
            testLine.Add(NewKey, NewValue);

            // assert
            Entries(testLine).Should().Equal(new[] { NewEntry });
        }

        [Fact]
        public void Count_ThreeFields_ReturnsThree()
        {
            // arrange
            var testLine = CreateLine();

            // act
            var testResult = testLine.Count;

            // assert
            testResult.Should().Be(FieldCount);
        }

        /// <summary>
        /// Creates a line with the fields A = 1, B = 2 and C = 3.
        /// </summary>
        private static ICsvFileLine CreateLine()
        {
            ICsvFileLine line = new CsvFileRecordLine();
            line.Add(KeyA, ValueA);
            line.Add(KeyB, ValueB);
            line.Add(KeyC, ValueC);

            return line;
        }

        /// <summary>
        /// Creates a line whose fields have been removed, inserted and added, so that it holds D = x, C = 3 and A = 1.
        /// </summary>
        private static ICsvFileLine CreateRearrangedLine()
        {
            var line = CreateLine();
            line.Remove(KeyA);
            line.Insert(1, NewKey, NewValue);
            line.Add(KeyA, ValueA);
            line.RemoveAt(0);

            return line;
        }

        /// <summary>
        /// Gets the entries of a line, in the order that enumerating it gives them.
        /// </summary>
        private static List<KeyValuePair<string, object>> Entries(ICsvFileLine line) => line.ToList();

        /// <summary>
        /// Gets the entries that remain in an enumerator of a line.
        /// </summary>
        private static List<KeyValuePair<string, object>> Remaining(IEnumerator enumerator) =>
            new List<KeyValuePair<string, object>>(new EntrySequence(enumerator));

        /// <summary>
        /// Gets the items that remain in an enumerator of a line, its field names or its values.
        /// </summary>
        private static List<object> RemainingItems(IEnumerator enumerator) => new List<object>(new ItemSequence(enumerator));

        /// <summary>
        /// A call to a member of a line.
        /// </summary>
        public sealed class LineOperation
        {
            private readonly string _name;
            private readonly Action<ICsvFileLine> _operation;

            public LineOperation(string name, Action<ICsvFileLine> operation)
            {
                _name = name;
                _operation = operation;
            }

            public void Invoke(ICsvFileLine line) => _operation(line);

            public override string ToString() => _name;
        }

        /// <summary>
        /// A way to start enumerating a line.
        /// </summary>
        public sealed class LineEnumeration
        {
            private readonly string _name;
            private readonly Func<ICsvFileLine, IEnumerator> _start;

            public LineEnumeration(string name, Func<ICsvFileLine, IEnumerator> start)
            {
                _name = name;
                _start = start;
            }

            public IEnumerator Start(ICsvFileLine line) => _start(line);

            public override string ToString() => _name;
        }

        /// <summary>
        /// The items that remain in an enumerator, whatever their type.
        /// </summary>
        private sealed class ItemSequence : IEnumerable<object>
        {
            private readonly IEnumerator _enumerator;

            public ItemSequence(IEnumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public IEnumerator<object> GetEnumerator() => new ItemEnumerator(_enumerator);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ItemEnumerator : IEnumerator<object>
        {
            private readonly IEnumerator _enumerator;

            public ItemEnumerator(IEnumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public object Current => _enumerator.Current;

            public bool MoveNext() => _enumerator.MoveNext();

            public void Reset() => _enumerator.Reset();

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// The entries that remain in an enumerator of a line.
        /// </summary>
        private sealed class EntrySequence : IEnumerable<KeyValuePair<string, object>>
        {
            private readonly IEnumerator _enumerator;

            public EntrySequence(IEnumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => new EntryEnumerator(_enumerator);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class EntryEnumerator : IEnumerator<KeyValuePair<string, object>>
        {
            private readonly IEnumerator _enumerator;

            public EntryEnumerator(IEnumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public KeyValuePair<string, object> Current => (KeyValuePair<string, object>)_enumerator.Current;

            object IEnumerator.Current => Current;

            public bool MoveNext() => _enumerator.MoveNext();

            public void Reset() => _enumerator.Reset();

            public void Dispose()
            {
            }
        }
    }
}
