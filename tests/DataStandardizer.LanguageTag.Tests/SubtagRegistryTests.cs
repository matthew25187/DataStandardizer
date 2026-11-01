using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataStandardizer.LanguageTag.SubtagRegistry;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    public class SubtagRegistryTests
    {
        #region Declarations

        /// <summary>
        /// The number of records in the IANA registry fixture: its File-Date record and the 9,274 records after it, each after a <c>%%</c> line (RFC 5646 §3.1.1).
        /// </summary>
        private const int IanaRegistryRecordCount = 9275;

        /// <summary>
        /// The File-Date of the IANA registry fixture (RFC 5646 §3.1.1).
        /// </summary>
        private static readonly DateTime IanaRegistryFileDate = new DateTime(2025, 5, 15);

        /// <summary>
        /// The date the IANA registry was first filled, which is the Added date of the records it started with (RFC 4646 §3.1, carried over by RFC 5646 §3.1.1).
        /// </summary>
        private static readonly DateTime RegistryCreationDate = new DateTime(2005, 10, 16);

        // Field names of registry records (RFC 5646 §3.1.2).
        private const string TypeFieldName = "Type";
        private const string AddedFieldName = "Added";

        // Values of the Type field (RFC 5646 §3.1.3).
        private const string LanguageType = "language";
        private const string ExtendedLanguageType = "extlang";
        private const string ScriptType = "script";
        private const string RegionType = "region";
        private const string VariantType = "variant";
        private const string GrandfatheredType = "grandfathered";
        private const string RedundantType = "redundant";

        // Subtags of records in the IANA registry fixture.
        private const string EnglishLanguageSubtag = "en";
        private const string AmericanSignLanguageExtendedLanguageSubtag = "ase";
        private const string BurmaRegionSubtag = "BU";
        private const string TraditionalHanScriptSubtag = "Hant";
        private const string NineteenNinetyFourVariantSubtag = "1994";
        private const string CantoneseExtendedLanguageSubtag = "yue";

        /// <summary>
        /// The smallest registry: a record with a Subtag field, after the File-Date record.
        /// </summary>
        private const string MinimalRecordSubtag = "aa";

        private const string EmptyRecordContent = "File-Date: 2025-01-01\n%%\n%%\nType: language\nSubtag: aa\n";
        private const string UnclassifiableRecordContent = "File-Date: 2025-01-01\n%%\nType: language\nDescription: No subtag or tag\n%%\nType: language\nSubtag: aa\n";

        private const string SubtagRegistryContentParameterName = "subtagRegistryContent";
        private const string SubtagRegistryFilePathParameterName = "subtagRegistryFilePath";

        /// <summary>
        /// The two ways to load a registry from its content.
        /// </summary>
        private static readonly Dictionary<string, Func<string, Task<SubtagRegistry.SubtagRegistry>>> ContentLoaders = new Dictionary<string, Func<string, Task<SubtagRegistry.SubtagRegistry>>>
        {
            { nameof(SubtagRegistry.SubtagRegistry.CreateFromContent), content => Task.FromResult(SubtagRegistry.SubtagRegistry.CreateFromContent(content)) },
            { nameof(SubtagRegistry.SubtagRegistry.CreateFromContentAsync), SubtagRegistry.SubtagRegistry.CreateFromContentAsync }
        };

        public static TheoryData<string, Func<string, Task<SubtagRegistry.SubtagRegistry>>> ContentLoaderCases
        {
            get
            {
                var data = new TheoryData<string, Func<string, Task<SubtagRegistry.SubtagRegistry>>>();
                foreach (var loader in ContentLoaders)
                {
                    data.Add(loader.Key, loader.Value);
                }

                return data;
            }
        }

        #endregion

        #region Test: CreateFromContent, CreateFromFile and CreateFromStream

        [Fact]
        public void CreateFromContent_WhenRegistryContentIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => SubtagRegistry.SubtagRegistry.CreateFromContent(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the Subtag Registry content must not be null")
                .WithParameterName(SubtagRegistryContentParameterName);
        }

        [Fact]
        public void CreateFromContent_WhenRegistryContentIsValid_ReturnsDeserializedRegistry()
        {
            // arrange
            var subtagRegistryContent = IanaSubtagRegistryResource.Content;

            // act
            // ReSharper disable once MethodHasAsyncOverload
            var testResult = SubtagRegistry.SubtagRegistry.CreateFromContent(subtagRegistryContent);

            // assert
            testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the Subtag Registry", IanaRegistryRecordCount);
        }

        [Fact]
        public async Task CreateFromContentAsync_WhenRegistryContentIsNull_ThrowsArgumentNullException()
        {
            // act
            Func<Task> testAction = () => SubtagRegistry.SubtagRegistry.CreateFromContentAsync(null);

            // assert
            (await testAction.Should().ThrowAsync<ArgumentNullException>("the Subtag Registry content must not be null"))
                .WithParameterName(SubtagRegistryContentParameterName);
        }

        [Fact]
        public async Task CreateFromContentAsync_WhenRegistryContentIsValid_ReturnsDeserializedRegistry()
        {
            // arrange
            var subtagRegistryContent = IanaSubtagRegistryResource.Content;

            // act
            var testResult = await SubtagRegistry.SubtagRegistry.CreateFromContentAsync(subtagRegistryContent);

            // assert
            testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the Subtag Registry", IanaRegistryRecordCount);
        }

        [Fact]
        public void CreateFromFile_WhenRegistryFilePathIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => SubtagRegistry.SubtagRegistry.CreateFromFile(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the Subtag Registry file path must not be null")
                .WithParameterName(SubtagRegistryFilePathParameterName);
        }

        [Fact]
        public async Task CreateFromFileAsync_WhenRegistryFilePathIsNull_ThrowsArgumentNullException()
        {
            // act
            Func<Task> testAction = () => SubtagRegistry.SubtagRegistry.CreateFromFileAsync(null);

            // assert
            (await testAction.Should().ThrowAsync<ArgumentNullException>("the Subtag Registry file path must not be null"))
                .WithParameterName(SubtagRegistryFilePathParameterName);
        }

        [Fact]
        public void CreateFromStream_WhenStreamIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => _ = SubtagRegistry.SubtagRegistry.CreateFromStream(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>("the stream must not be null");
        }

        [Fact]
        public void CreateFromStream_WhenStreamIsSupplied_ReturnsDeserializedRegistry()
        {
            // arrange
            using (var subtagRegistryStream = new MemoryStream(Encoding.UTF8.GetBytes(IanaSubtagRegistryResource.Content)))
            {
                // act
                // ReSharper disable once MethodHasAsyncOverload
                var testResult = SubtagRegistry.SubtagRegistry.CreateFromStream(subtagRegistryStream);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the subtag registry", IanaRegistryRecordCount);
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenStreamIsNull_ThrowsArgumentNullException()
        {
            // act
            Func<Task> testAction = () => SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(null);

            // assert
            await testAction.Should().ThrowAsync<ArgumentNullException>("the stream must not be null");
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenStreamIsSupplied_ReturnsDeserializedRegistry()
        {
            // arrange
            using (var subtagRegistryStream = new MemoryStream(Encoding.UTF8.GetBytes(IanaSubtagRegistryResource.Content)))
            {
                // act
                var testResult = await SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the subtag registry", IanaRegistryRecordCount);
            }
        }

        [Fact]
        public void CreateFromStream_WhenStreamIsPositioned_ReadsFromCurrentPosition()
        {
            // arrange
            using (var subtagRegistryStream = CreatePositionedRegistryStream(IanaSubtagRegistryResource.Content))
            {
                // act
                // ReSharper disable once MethodHasAsyncOverload
                var testResult = SubtagRegistry.SubtagRegistry.CreateFromStream(subtagRegistryStream);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "only the content after the stream's current position is read");
            }
        }

        [Fact]
        public void CreateFromStream_WhenStreamIsPositioned_LeavesStreamOpenAtItsEnd()
        {
            // arrange
            using (var subtagRegistryStream = CreatePositionedRegistryStream(IanaSubtagRegistryResource.Content))
            {
                // act
                // ReSharper disable once MethodHasAsyncOverload
                SubtagRegistry.SubtagRegistry.CreateFromStream(subtagRegistryStream);

                // assert
                subtagRegistryStream.CanRead.Should().BeTrue("the caller owns the stream");
                subtagRegistryStream.Position.Should().Be(subtagRegistryStream.Length, "the registry is read to the end of the stream");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenStreamIsPositioned_ReadsFromCurrentPosition()
        {
            // arrange
            using (var subtagRegistryStream = CreatePositionedRegistryStream(IanaSubtagRegistryResource.Content))
            {
                // act
                var testResult = await SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "only the content after the stream's current position is read");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenStreamIsPositioned_LeavesStreamOpenAtItsEnd()
        {
            // arrange
            using (var subtagRegistryStream = CreatePositionedRegistryStream(IanaSubtagRegistryResource.Content))
            {
                // act
                await SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream);

                // assert
                subtagRegistryStream.CanRead.Should().BeTrue("the caller owns the stream");
                subtagRegistryStream.Position.Should().Be(subtagRegistryStream.Length, "the registry is read to the end of the stream");
            }
        }

        [Fact]
        public async Task CreateFromContentAsync_WhenTokenIsCancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                // act
                Func<Task> testAction = () => SubtagRegistry.SubtagRegistry.CreateFromContentAsync(IanaSubtagRegistryResource.Content, cancellationTokenSource.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>("the token was cancelled");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenTokenIsCancelledMidRead_ThrowsOperationCanceledException()
        {
            // arrange
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var subtagRegistryStream = new CancelOnFirstReadStream(Encoding.UTF8.GetBytes(IanaSubtagRegistryResource.Content), cancellationTokenSource))
            {
                // act
                Func<Task> testAction = () => SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream, cancellationTokenSource.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>("the token was cancelled after the first read from the stream");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenTokenIsCancelledMidRead_StopsReading()
        {
            // arrange
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var subtagRegistryStream = new CancelOnFirstReadStream(Encoding.UTF8.GetBytes(IanaSubtagRegistryResource.Content), cancellationTokenSource))
            {
                // act
                var testResult = await Record.ExceptionAsync(() => SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream, cancellationTokenSource.Token));

                // assert
                testResult.Should().BeAssignableTo<OperationCanceledException>("the token was cancelled after the first read from the stream");
                subtagRegistryStream.Position.Should().BeLessThan(subtagRegistryStream.Length, "reading stops once the token is cancelled, before the end of the stream");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenTokenIsNotCancelled_ReturnsDeserializedRegistry()
        {
            // arrange
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var subtagRegistryStream = new MemoryStream(Encoding.UTF8.GetBytes(IanaSubtagRegistryResource.Content)))
            {
                // act
                var testResult = await SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream, cancellationTokenSource.Token);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the subtag registry", IanaRegistryRecordCount);
            }
        }

        /// <summary>
        /// A stream that cancels a token the first time it is read, to cancel a load part way through.
        /// </summary>
        private sealed class CancelOnFirstReadStream : MemoryStream
        {
            private readonly CancellationTokenSource _cancellationTokenSource;

            public CancelOnFirstReadStream(byte[] buffer, CancellationTokenSource cancellationTokenSource)
                : base(buffer)
            {
                _cancellationTokenSource = cancellationTokenSource;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                _cancellationTokenSource.Cancel();
                return base.Read(buffer, offset, count);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                _cancellationTokenSource.Cancel();
                return base.ReadAsync(buffer, offset, count, CancellationToken.None);
            }

#if NET
            public override int Read(Span<byte> buffer)
            {
                _cancellationTokenSource.Cancel();
                return base.Read(buffer);
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                _cancellationTokenSource.Cancel();
                return base.ReadAsync(buffer, CancellationToken.None);
            }
#endif
        }

        /// <summary>
        /// A stream with a complete record ahead of the registry content, positioned after that record, so reading from the start of the stream would add one record.
        /// </summary>
        private static MemoryStream CreatePositionedRegistryStream(string registryContent)
        {
            var precedingBytes = Encoding.UTF8.GetBytes("Type: language\nSubtag: qaa\nDescription: Preceding record\nAdded: 2000-01-01\n%%\n");
            var registryBytes = Encoding.UTF8.GetBytes(registryContent);

            var stream = new MemoryStream();
            stream.Write(precedingBytes, 0, precedingBytes.Length);
            stream.Write(registryBytes, 0, registryBytes.Length);
            stream.Position = precedingBytes.Length;
            return stream;
        }

        #endregion

        #region Test: record properties

        [Theory]
        [InlineData(SubtagRegistryRecordType.Region, BurmaRegionSubtag, 2005, 10, 16)]
        [InlineData(SubtagRegistryRecordType.Language, EnglishLanguageSubtag, 2005, 10, 16)]
        [InlineData(SubtagRegistryRecordType.ExtendedLanguage, AmericanSignLanguageExtendedLanguageSubtag, 2009, 7, 29)]
        public void SubtagRecordAdded_WhenRecordHasAddedField_ReturnsAddedDate(SubtagRegistryRecordType type, string subtag, int year, int month, int day)
        {
            // arrange
            var record = GetSubtagRecord(type, subtag);

            // act
            var testResult = record.Added;

            // assert
            testResult.Should().Be(new DateTime(year, month, day), "the {0} subtag {1} was added on that date", type, subtag);
        }

        [Fact]
        public void SubtagRecordDeprecated_WhenRecordHasDeprecatedField_ReturnsDeprecatedDate()
        {
            // arrange
            var record = GetSubtagRecord(SubtagRegistryRecordType.Region, BurmaRegionSubtag);

            // act
            var testResult = record.Deprecated;

            // assert
            testResult.Should().Be(new DateTime(1989, 12, 5), "the region subtag BU was deprecated on that date");
        }

        [Fact]
        public void SubtagRecordDeprecated_WhenRecordHasNoDeprecatedField_ReturnsNull()
        {
            // arrange
            var record = GetSubtagRecord(SubtagRegistryRecordType.Language, EnglishLanguageSubtag);

            // act
            var testResult = record.Deprecated;

            // assert
            testResult.Should().BeNull("the language subtag en is not deprecated");
        }

        [Theory]
        [InlineData(SubtagRegistryRecordType.Region, BurmaRegionSubtag, "MM")]
        [InlineData(SubtagRegistryRecordType.ExtendedLanguage, AmericanSignLanguageExtendedLanguageSubtag, AmericanSignLanguageExtendedLanguageSubtag)]
        public void SubtagRecordPreferredValue_WhenRecordHasPreferredValueField_ReturnsPreferredValue(SubtagRegistryRecordType type, string subtag, string expectedPreferredValue)
        {
            // arrange
            var record = GetSubtagRecord(type, subtag);

            // act
            var testResult = record.PreferredValue;

            // assert
            testResult.Should().Be(expectedPreferredValue, "the {0} subtag {1} has that preferred value", type, subtag);
        }

        [Fact]
        public void SubtagRecordPreferredValue_WhenRecordHasNoPreferredValueField_ReturnsNull()
        {
            // arrange
            var record = GetSubtagRecord(SubtagRegistryRecordType.Language, EnglishLanguageSubtag);

            // act
            var testResult = record.PreferredValue;

            // assert
            testResult.Should().BeNull("the language subtag en has no preferred value");
        }

        [Fact]
        public void SubtagRecordSuppressScript_WhenRecordHasSuppressScriptField_ReturnsSuppressScript()
        {
            // arrange
            var record = GetSubtagRecord(SubtagRegistryRecordType.Language, EnglishLanguageSubtag);

            // act
            var testResult = record.SuppressScript;

            // assert
            testResult.Should().Be("Latn", "the language subtag en has a Suppress-Script of Latn");
        }

        [Fact]
        public void SubtagRecordSuppressScript_WhenRecordHasNoSuppressScriptField_ReturnsNull()
        {
            // arrange
            var record = GetSubtagRecord(SubtagRegistryRecordType.Language, "jbo");

            // act
            var testResult = record.SuppressScript;

            // assert
            testResult.Should().BeNull("the language subtag jbo has no Suppress-Script");
        }

        [Fact]
        public void TagRecordAdded_WhenGrandfatheredTagHasAddedField_ReturnsAddedDate()
        {
            // arrange
            var record = GetGuoyuTagRecord();

            // act
            var testResult = record.Added;

            // assert
            testResult.Should().Be(new DateTime(1999, 12, 18), "the grandfathered tag zh-guoyu was added on that date");
        }

        [Fact]
        public void TagRecordDeprecated_WhenGrandfatheredTagHasDeprecatedField_ReturnsDeprecatedDate()
        {
            // arrange
            var record = GetGuoyuTagRecord();

            // act
            var testResult = record.Deprecated;

            // assert
            testResult.Should().Be(new DateTime(2005, 7, 15), "the grandfathered tag zh-guoyu was deprecated on that date");
        }

        [Fact]
        public void TagRecordPreferredValue_WhenGrandfatheredTagHasPreferredValueField_ReturnsPreferredValue()
        {
            // arrange
            var record = GetGuoyuTagRecord();

            // act
            var testResult = record.PreferredValue;

            // assert
            testResult.Should().Be("cmn", "the grandfathered tag zh-guoyu has a preferred value of cmn");
        }

        [Fact]
        public void TagRecordSuppressScript_WhenGrandfatheredTagHasNoSuppressScriptField_ReturnsNull()
        {
            // arrange
            var record = GetGuoyuTagRecord();

            // act
            var testResult = record.SuppressScript;

            // assert
            testResult.Should().BeNull("the grandfathered tag zh-guoyu has no Suppress-Script");
        }

        [Fact]
        public void FileDateRecordFileDate_WhenRegistryHasFileDate_ReturnsFileDate()
        {
            // arrange
            var record = IanaSubtagRegistryResource.Shared.OfType<SubtagRegistryFileDateRecord>().Single();

            // act
            var testResult = record.FileDate;

            // assert
            testResult.Should().Be(IanaRegistryFileDate, "the registry fixture has that File-Date");
        }

        [Fact]
        public void FileDate_WhenRegistryHasFileDate_ReturnsFileDate()
        {
            // arrange
            var registry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = registry.FileDate;

            // assert
            testResult.Should().Be(IanaRegistryFileDate, "the registry fixture has that File-Date");
        }

        /// <summary>
        /// Every record after the File-Date record of the IANA registry fixture, each with its Type and Added fields as the fixture file has them (RFC 5646 §3.1.2),
        /// read from the file without the registry reader.
        /// </summary>
        private static List<(string Key, SubtagRegistryTagRecordBase Record, string Type, DateTime Added)> ReadEveryTagRecordWithFileFields()
        {
            var fileRecords = IanaSubtagRegistryResource.ReadResourceText().Replace("\r\n", "\n").Split(new[] { "\n%%\n" }, StringSplitOptions.None).Skip(1).ToArray();
            var records = IanaSubtagRegistryResource.Shared.OfType<SubtagRegistryTagRecordBase>().ToArray();
            var result = new List<(string, SubtagRegistryTagRecordBase, string, DateTime)>();
            for (var index = 0; index < records.Length; index++)
            {
                var fields = fileRecords[index].Split('\n').Where(line => line.IndexOf(": ", StringComparison.Ordinal) > 0)
                    .GroupBy(line => line.Substring(0, line.IndexOf(": ", StringComparison.Ordinal)))
                    .ToDictionary(group => group.Key, group => group.First().Substring(group.Key.Length + 2));
                var key = fields.ContainsKey("Subtag") ? fields["Subtag"] : fields["Tag"];
                result.Add((fields[TypeFieldName] + " " + key, records[index], fields[TypeFieldName], DateTime.ParseExact(fields[AddedFieldName], "yyyy-MM-dd", CultureInfo.InvariantCulture)));
            }

            return result;
        }

        /// <summary>
        /// Every record after the File-Date record of the IANA registry fixture, each with the date in its Added field as the fixture file has it (RFC 5646 §3.1.2),
        /// read from the file without the registry reader.
        /// </summary>
        /// <remarks>
        /// The registry keeps its records in the order of the file, so the n-th record of the registry is the n-th record of the file.
        /// </remarks>
        public static TheoryData<string, SubtagRegistryTagRecordBase, DateTime> EveryTagRecordWithFileAdded
        {
            get
            {
                var data = new TheoryData<string, SubtagRegistryTagRecordBase, DateTime>();
                foreach (var row in ReadEveryTagRecordWithFileFields())
                {
                    data.Add(row.Key, row.Record, row.Added);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(EveryTagRecordWithFileAdded), DisableDiscoveryEnumeration = true)]
        public void TagRecordAdded_ForEveryRecordInRegistry_ReturnsDateOfAddedField(string recordKey, SubtagRegistryTagRecordBase record, DateTime expectedResult)
        {
            // act
            var testResult = record.Added;

            // assert
            testResult.Should().Be(expectedResult, "the Added property of the {0} record is the date in its Added field", recordKey);
        }

        /// <summary>
        /// The record type for each value of the Type field (RFC 5646 §3.1.3).
        /// </summary>
        private static readonly Dictionary<string, SubtagRegistryRecordType> RecordTypesByTypeField = new Dictionary<string, SubtagRegistryRecordType>
        {
            [LanguageType] = SubtagRegistryRecordType.Language,
            [ExtendedLanguageType] = SubtagRegistryRecordType.ExtendedLanguage,
            [ScriptType] = SubtagRegistryRecordType.Script,
            [RegionType] = SubtagRegistryRecordType.Region,
            [VariantType] = SubtagRegistryRecordType.Variant,
            [GrandfatheredType] = SubtagRegistryRecordType.Grandfathered,
            [RedundantType] = SubtagRegistryRecordType.Redundant
        };

        /// <summary>
        /// Every record after the File-Date record of the IANA registry fixture, each with the record type for its Type field as the fixture file has it.
        /// </summary>
        public static TheoryData<string, SubtagRegistryTagRecordBase, SubtagRegistryRecordType> EveryTagRecordWithFileType
        {
            get
            {
                var data = new TheoryData<string, SubtagRegistryTagRecordBase, SubtagRegistryRecordType>();
                foreach (var row in ReadEveryTagRecordWithFileFields())
                {
                    data.Add(row.Key, row.Record, RecordTypesByTypeField[row.Type]);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(EveryTagRecordWithFileType), DisableDiscoveryEnumeration = true)]
        public void TagRecordType_ForEveryRecordInRegistry_IsTheTypeInItsTypeField(string recordKey, SubtagRegistryTagRecordBase record, SubtagRegistryRecordType expectedResult)
        {
            // act
            var testResult = record.Type;

            // assert
            testResult.Should().Be(expectedResult, "the {0} record has a Type that RFC 5646 §3.1.3 defines", recordKey);
        }

        [Fact]
        public void TagRecordComments_WhenRecordHasSeveralCommentsFields_ReturnsEveryComment()
        {
            // arrange
            var record = GetSubtagRecord(SubtagRegistryRecordType.Variant, "vaidika");

            // act
            var testResult = record.Comments;

            // assert
            testResult.Should().Equal(
                new[] { "The most ancient dialect of Sanskrit used in verse and prose composed until about the 4th century B.C.E.", "Preferred tag is vsn" },
                "the Comments field may appear more than once per record (ref. RFC 5646 §3.1.2)");
        }

        [Fact]
        public void TagRecordPrefix_WhenRecordHasSeveralPrefixFields_ReturnsEveryPrefixInOrder()
        {
            // arrange
            var record = GetSubtagRecord(SubtagRegistryRecordType.Variant, NineteenNinetyFourVariantSubtag);

            // act
            var testResult = record.Prefix;

            // assert
            testResult.Should().Equal(new[] { "sl-rozaj", "sl-rozaj-biske", "sl-rozaj-njiva", "sl-rozaj-osojs", "sl-rozaj-solba" });
        }

        /// <summary>
        /// Each list of a record cast to a mutable list, and changed.
        /// </summary>
        public static TheoryData<string, Action<SubtagRegistrySubtagRecord>> MutableListChanges => new TheoryData<string, Action<SubtagRegistrySubtagRecord>>
        {
            { nameof(SubtagRegistrySubtagRecord.Description), record => ((IList<string>)record.Description)[0] = "Changed" },
            { nameof(SubtagRegistrySubtagRecord.Prefix), record => ((IList<string>)record.Prefix).Add("Changed") },
            { nameof(SubtagRegistrySubtagRecord.Fields), record => ((IList<KeyValuePair<string, string>>)record.Fields).Clear() }
        };

        [Theory]
        [MemberData(nameof(MutableListChanges), DisableDiscoveryEnumeration = true)]
        public void TagRecordLists_WhenCastToMutableList_CannotBeChanged(string listName, Action<SubtagRegistrySubtagRecord> change)
        {
            // arrange
            var record = SubtagRegistry.SubtagRegistry.CreateFromContent("Type: language\nSubtag: en\nDescription: English\nAdded: 2005-10-16").Languages[0];

            // act
            Action testAction = () => change(record);

            // assert
            testAction.Should().Throw<NotSupportedException>("records are immutable, so their {0} list can't be changed", listName);
        }

        [Fact]
        public void TagRecordDescription_OnRepeatedReads_ReturnsSameCollection()
        {
            // arrange
            var record = GetSubtagRecord(SubtagRegistryRecordType.Language, EnglishLanguageSubtag);
            var firstResult = record.Description;

            // act
            var testResult = record.Description;

            // assert
            testResult.Should().BeSameAs(firstResult, "the read-only view is created once");
            testResult.Should().Equal(new[] { "English" });
        }

        [Theory]
        [MemberData(nameof(ContentLoaderCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFromContent_WhenTypeIsUnknown_ReadsRecordWithUnknownType(string loaderName, Func<string, Task<SubtagRegistry.SubtagRegistry>> load)
        {
            // arrange
            const string content = "Type: future\nSubtag: qaa\nDescription: Private use\nAdded: 2005-10-16";

            // act
            var testResult = await load(content);

            // assert
            var record = testResult.Should().ContainSingle("{0} reads the one record", loaderName).Which.Should().BeOfType<SubtagRegistrySubtagRecord>().Subject;
            record.Type.Should().Be(SubtagRegistryRecordType.Unknown);
            record.Fields.Should().Contain(new KeyValuePair<string, string>(TypeFieldName, "future"), "the field is kept as it was read");
            testResult.Languages.Should().BeEmpty("a record of an unknown type is not a language record");
        }

        [Theory]
        [InlineData("2005-13-01", "Type: language\nSubtag: qaa\nDescription: Private use\nAdded: 2005-13-01\nDeprecated: 2005-13-01")]
        [InlineData("2005-02-30", "Type: language\nSubtag: qaa\nDescription: Private use\nAdded: 2005-02-30\nDeprecated: 2005-02-30")]
        [InlineData("16 Oct 2005", "Type: language\nSubtag: qaa\nDescription: Private use\nAdded: 16 Oct 2005\nDeprecated: 16 Oct 2005")]
        [InlineData("2005-1-16", "Type: language\nSubtag: qaa\nDescription: Private use\nAdded: 2005-1-16\nDeprecated: 2005-1-16")]
        public void CreateFromContent_WhenDateIsMalformed_ReadsDateAsAbsentAndKeepsField(string date, string content)
        {
            // act
            var testResult = SubtagRegistry.SubtagRegistry.CreateFromContent(content);

            // assert
            var record = testResult.Languages.Should().ContainSingle().Subject;
            record.Added.Should().Be(default(DateTime), "{0} is not a date in the yyyy-MM-dd form", date);
            record.Deprecated.Should().BeNull("{0} is not a date in the yyyy-MM-dd form", date);
            record.Fields.Should().Contain(new KeyValuePair<string, string>(AddedFieldName, date));
        }

        /// <summary>
        /// Each typed list of the IANA registry fixture, with the number of records of its type in the fixture and the subtag or tag of the first and last of them.
        /// </summary>
        public static TheoryData<SubtagRegistryRecordType, Func<SubtagRegistry.SubtagRegistry, IReadOnlyList<SubtagRegistryTagRecordBase>>, int> TypedRecordLists =>
            new TheoryData<SubtagRegistryRecordType, Func<SubtagRegistry.SubtagRegistry, IReadOnlyList<SubtagRegistryTagRecordBase>>, int>
            {
                { SubtagRegistryRecordType.Language, registry => registry.Languages, 8268 },
                { SubtagRegistryRecordType.ExtendedLanguage, registry => registry.ExtendedLanguages, 256 },
                { SubtagRegistryRecordType.Script, registry => registry.Scripts, 225 },
                { SubtagRegistryRecordType.Region, registry => registry.Regions, 305 },
                { SubtagRegistryRecordType.Variant, registry => registry.Variants, 127 },
                { SubtagRegistryRecordType.Grandfathered, registry => registry.GrandfatheredTags, 26 },
                { SubtagRegistryRecordType.Redundant, registry => registry.RedundantTags, 67 }
            };

        [Theory]
        [MemberData(nameof(TypedRecordLists), DisableDiscoveryEnumeration = true)]
        public void TypedRecordLists_ForFixture_HoldEveryRecordOfTheirType(SubtagRegistryRecordType type, Func<SubtagRegistry.SubtagRegistry, IReadOnlyList<SubtagRegistryTagRecordBase>> list, int expectedCount)
        {
            // arrange
            var registry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = list(registry);

            // assert
            testResult.Should().HaveCount(expectedCount, "the fixture has {0} records of type {1}", expectedCount, type);
            testResult.Should().OnlyContain(record => record.Type == type);
        }

        public static TheoryData<Func<SubtagRegistry.SubtagRegistry, IReadOnlyList<SubtagRegistrySubtagRecord>>, string, string> SubtagRecordListEnds =>
            new TheoryData<Func<SubtagRegistry.SubtagRegistry, IReadOnlyList<SubtagRegistrySubtagRecord>>, string, string>
            {
                { registry => registry.Languages, MinimalRecordSubtag, "zzj" },
                { registry => registry.ExtendedLanguages, "aao", "zsm" },
                { registry => registry.Scripts, "Adlm", "Zzzz" },
                { registry => registry.Regions, "AA", "419" },
                { registry => registry.Variants, "1606nict", "xsistemo" }
            };

        [Theory]
        [MemberData(nameof(SubtagRecordListEnds), DisableDiscoveryEnumeration = true)]
        public void TypedRecordLists_ForFixture_KeepRegistryOrder(Func<SubtagRegistry.SubtagRegistry, IReadOnlyList<SubtagRegistrySubtagRecord>> list, string expectedFirstSubtag, string expectedLastSubtag)
        {
            // arrange
            var registry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = list(registry);

            // assert
            testResult[0].Subtag.Should().Be(expectedFirstSubtag, "the first record of the type in the fixture is first");
            testResult.Last().Subtag.Should().Be(expectedLastSubtag, "the last record of the type in the fixture is last");
        }

        [Fact]
        public void Count_ForFixture_IsEveryRecordOfEachTypeAndTheFileDateRecord()
        {
            // arrange
            var registry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = registry.Count;

            // assert
            testResult.Should().Be(IanaRegistryRecordCount, "the fixture has a File-Date record and 9,274 records of the seven types");
        }

        #endregion

        #region Test: TryGetSubtag and IsGrandfathered

        [Theory]
        [InlineData(SubtagRegistryRecordType.Language, EnglishLanguageSubtag, EnglishLanguageSubtag)]
        [InlineData(SubtagRegistryRecordType.Language, "EN", EnglishLanguageSubtag)]
        [InlineData(SubtagRegistryRecordType.Language, "qab", "qaa..qtz")]
        [InlineData(SubtagRegistryRecordType.ExtendedLanguage, CantoneseExtendedLanguageSubtag, CantoneseExtendedLanguageSubtag)]
        [InlineData(SubtagRegistryRecordType.Script, "hant", TraditionalHanScriptSubtag)]
        [InlineData(SubtagRegistryRecordType.Script, "Qabx", "Qaaa..Qabx")]
        [InlineData(SubtagRegistryRecordType.Region, "tw", "TW")]
        [InlineData(SubtagRegistryRecordType.Region, "XB", "XA..XZ")]
        [InlineData(SubtagRegistryRecordType.Variant, NineteenNinetyFourVariantSubtag, NineteenNinetyFourVariantSubtag)]
        public void TryGetSubtag_WhenSubtagIsRegistered_ReturnsItsRecord(SubtagRegistryRecordType type, string subtag, string expectedSubtag)
        {
            // arrange
            var registry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = registry.TryGetSubtag(type, subtag, out var record);

            // assert
            testResult.Should().BeTrue("{0} is a registered {1} subtag", subtag, type);
            record.Subtag.Should().Be(expectedSubtag);
            record.Type.Should().Be(type);
        }

        [Theory]
        [InlineData(SubtagRegistryRecordType.Language, "EN", EnglishLanguageSubtag)]
        [InlineData(SubtagRegistryRecordType.Language, "qab", "qaa..qtz")]
        [InlineData(SubtagRegistryRecordType.Script, "hant", TraditionalHanScriptSubtag)]
        [InlineData(SubtagRegistryRecordType.Region, "XB", "XA..XZ")]
        [InlineData(SubtagRegistryRecordType.Variant, NineteenNinetyFourVariantSubtag, NineteenNinetyFourVariantSubtag)]
        public void TryGetSubtag_WithSpanWhenSubtagIsRegistered_ReturnsItsRecord(SubtagRegistryRecordType type, string subtag, string expectedSubtag)
        {
            // arrange
            var registry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = registry.TryGetSubtag(type, subtag.AsSpan(), out var record);

            // assert
            testResult.Should().BeTrue("{0} is a registered {1} subtag", subtag, type);
            record.Subtag.Should().Be(expectedSubtag);
            record.Type.Should().Be(type);
        }

        [Theory]
        [InlineData(SubtagRegistryRecordType.Language, TraditionalHanScriptSubtag)]
        [InlineData(SubtagRegistryRecordType.Script, EnglishLanguageSubtag)]
        [InlineData(SubtagRegistryRecordType.Language, "zz")]
        [InlineData(SubtagRegistryRecordType.Language, "")]
        [InlineData(SubtagRegistryRecordType.Language, "\x0131a")]
        [InlineData(SubtagRegistryRecordType.Grandfathered, "i-klingon")]
        [InlineData(SubtagRegistryRecordType.Unknown, EnglishLanguageSubtag)]
        [InlineData((SubtagRegistryRecordType)99, EnglishLanguageSubtag)]
        public void TryGetSubtag_WhenTypeHasNoSuchSubtag_ReturnsFalse(SubtagRegistryRecordType type, string subtag)
        {
            // arrange
            var registry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = registry.TryGetSubtag(type, subtag, out var record);

            // assert
            testResult.Should().BeFalse("the registry has no {0} subtag {1}", type, subtag);
            record.Should().BeNull();
        }

        [Theory]
        [InlineData(MinimalRecordSubtag)]
        [InlineData("AA")]
        public void TryGetSubtag_WhenTwoRecordsHaveSubtagsThatDifferOnlyInCase_ReturnsTheFirst(string subtag)
        {
            // arrange
            var registry = SubtagRegistry.SubtagRegistry.CreateFromContent(
                "Type: language\nSubtag: ab\nDescription: Other\n%%\nType: language\nSubtag: AA\nDescription: First\n%%\nType: language\nSubtag: aa\nDescription: Second\n%%\nType: language\nSubtag: zz\nDescription: Other");

            // act
            var testResult = registry.TryGetSubtag(SubtagRegistryRecordType.Language, subtag, out var record);

            // assert
            testResult.Should().BeTrue();
            record.Description.Should().Equal(new[] { "First" }, "subtags are compared case-insensitively, and the first record for a subtag is the one found");
        }

        [Fact]
        public void TryGetSubtag_WhenSubtagIsNull_ReturnsFalse()
        {
            // arrange
            var registry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = registry.TryGetSubtag(SubtagRegistryRecordType.Language, (string)null, out var record);

            // assert
            testResult.Should().BeFalse();
            record.Should().BeNull();
        }

        [Theory]
        [InlineData("i-klingon", true)]
        [InlineData("I-KLINGON", true)]
        [InlineData("zh-min-nan", true)]
        [InlineData("zh-Hant", false)]
        [InlineData(EnglishLanguageSubtag, false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsGrandfathered_ForLanguageTag_ReturnsWhetherRegistryHasGrandfatheredRecordForIt(string languageTag, bool expectedResult)
        {
            // arrange
            var registry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = registry.IsGrandfathered(languageTag);

            // assert
            testResult.Should().Be(expectedResult, "zh-Hant is a redundant tag and en a language subtag, not grandfathered tags");
        }

        [Theory]
        [InlineData("i-klingon", true)]
        [InlineData("I-KLINGON", true)]
        [InlineData("zh-min-nan", true)]
        [InlineData("zh-Hant", false)]
        [InlineData(EnglishLanguageSubtag, false)]
        [InlineData("", false)]
        public void IsGrandfathered_ForLanguageTagSpan_ReturnsWhetherRegistryHasGrandfatheredRecordForIt(string languageTag, bool expectedResult)
        {
            // arrange
            var registry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = registry.IsGrandfathered(languageTag.AsSpan());

            // assert
            testResult.Should().Be(expectedResult, "zh-Hant is a redundant tag and en a language subtag, not grandfathered tags");
        }

        #endregion

        #region Test: ToString and reading content

        [Fact]
        public void ToString_WhenRegistryIsLoadedAgainFromIt_WritesSameContent()
        {
            // arrange
            var content = IanaSubtagRegistryResource.Content;

            // act
            var testResult = SubtagRegistry.SubtagRegistry.CreateFromContent(content).ToString();

            // assert
            testResult.Should().Be(content, "every record and field is read and written back as it was");
        }

        [Theory]
        [MemberData(nameof(ContentLoaderCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFromContent_WhenFieldIsFolded_JoinsLinesWithOneSpace(string loaderName, Func<string, Task<SubtagRegistry.SubtagRegistry>> load)
        {
            // arrange
            const string content = "Type: language\nSubtag: ia\nDescription: Interlingua (International Auxiliary Language\n  Association)\nComments: first\n\t  second \n   \n  third\nAdded: 2005-10-16";

            // act
            var testResult = await load(content);

            // assert
            var record = testResult.Languages.Should().ContainSingle("{0} reads the one record", loaderName).Subject;
            record.Description.Should().Equal(new[] { "Interlingua (International Auxiliary Language Association)" }, "a folded line is unfolded with one space (ref. RFC 5646 §3.1.1)");
            record.Comments.Should().Equal(new[] { "first second third" }, "each folded line is trimmed, and a blank one adds nothing");
            record.Added.Should().Be(RegistryCreationDate);
        }

        [Fact]
        public void CreateFromContent_ForEveryFieldInRegistry_HasNoDoubledOrSurroundingSpaces()
        {
            // arrange
            var content = IanaSubtagRegistryResource.Content;

            // act
            var testResult = SubtagRegistry.SubtagRegistry.CreateFromContent(content);

            // assert
            testResult.Should().OnlyContain(
                record => record.Fields.All(field => !field.Value.Contains("  ") && field.Value.Trim() == field.Value),
                "folded lines in the registry fixture are unfolded with one space");
        }

        [Theory]
        [MemberData(nameof(ContentLoaderCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFromContent_WhenFieldNameIsUnknown_ReadsItAsSeparateField(string loaderName, Func<string, Task<SubtagRegistry.SubtagRegistry>> load)
        {
            // arrange
            const string content = "Type: language\nSubtag: qaa\nDescription: Private use\nFuture-Field: some value\nAdded: 2005-10-16";

            // act
            var testResult = await load(content);

            // assert
            var record = testResult.Languages.Should().ContainSingle("{0} reads the one record", loaderName).Subject;
            record.Description.Should().Equal(new[] { "Private use" }, "a field this version doesn't know is not part of the field before it");
            record.Fields.Should().Contain(new KeyValuePair<string, string>("Future-Field", "some value"));
        }

        /// <summary>
        /// Registries with a record that is empty or has no File-Date, Subtag or Tag field, each with both ways to load a registry from its content.
        /// </summary>
        public static TheoryData<string, Func<string, Task<SubtagRegistry.SubtagRegistry>>, string> SkippedRecordCases
        {
            get
            {
                var data = new TheoryData<string, Func<string, Task<SubtagRegistry.SubtagRegistry>>, string>();
                foreach (var loader in ContentLoaders)
                foreach (var content in new[] { EmptyRecordContent, UnclassifiableRecordContent })
                {
                    data.Add(loader.Key, loader.Value, content);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(SkippedRecordCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFromContent_WhenRecordIsEmptyOrUnclassifiable_SkipsItAndReadsTheRest(string loaderName, Func<string, Task<SubtagRegistry.SubtagRegistry>> load, string content)
        {
            // act
            var testResult = await load(content);

            // assert
            testResult.Count.Should().Be(2, "{0} skips a record that is empty or has no File-Date, Subtag or Tag field, and doesn't take it as the end of the registry", loaderName);
            testResult.Languages.Should().ContainSingle(record => record.Subtag == MinimalRecordSubtag);
        }

        #endregion

        #region Helpers

        private static SubtagRegistrySubtagRecord GetSubtagRecord(SubtagRegistryRecordType type, string subtag)
        {
            return IanaSubtagRegistryResource.Shared
                .OfType<SubtagRegistrySubtagRecord>()
                .Single(record => record.Type == type && record.Subtag == subtag);
        }

        private static SubtagRegistryTagRecord GetGuoyuTagRecord()
        {
            return IanaSubtagRegistryResource.Shared.GrandfatheredTags.Single(record => record.Tag == "zh-guoyu");
        }

        #endregion
    }
}
