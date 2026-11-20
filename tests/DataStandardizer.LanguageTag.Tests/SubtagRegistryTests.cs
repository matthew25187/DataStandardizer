using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataStandardizer.LanguageTag.Registry;
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

        private const string SubtagRegistryContentParameterName = "subtagRegistryContent";
        private const string SubtagRegistryFilePathParameterName = "subtagRegistryFilePath";

        /// <summary>
        /// The two ways to load a registry from its content.
        /// </summary>
        private static readonly Dictionary<string, Func<string, Task<SubtagRegistry>>> ContentLoaders = new Dictionary<string, Func<string, Task<SubtagRegistry>>>
        {
            { nameof(SubtagRegistry.CreateFromContent), content => Task.FromResult(SubtagRegistry.CreateFromContent(content)) },
            { nameof(SubtagRegistry.CreateFromContentAsync), SubtagRegistry.CreateFromContentAsync }
        };

        public static TheoryData<string, Func<string, Task<SubtagRegistry>>> ContentLoaderCases
        {
            get
            {
                var data = new TheoryData<string, Func<string, Task<SubtagRegistry>>>();
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
            Action testAction = () => SubtagRegistry.CreateFromContent(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the Subtag Registry content must not be null")
                .WithParameterName(SubtagRegistryContentParameterName);
        }

        [Fact]
        public void CreateFromContent_WithAcceptBareLineFeedForIanaRegistry_ReturnsDeserializedRegistry()
        {
            // arrange
            var subtagRegistryContent = IanaSubtagRegistryResource.Text;

            // act
            // ReSharper disable once MethodHasAsyncOverload
            var testResult = SubtagRegistry.CreateFromContent(subtagRegistryContent, SubtagRegistryLoadOptions.AcceptBareLineFeed);

            // assert
            testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the Subtag Registry", IanaRegistryRecordCount);
        }

        [Fact]
        public async Task CreateFromContentAsync_WhenRegistryContentIsNull_ThrowsArgumentNullException()
        {
            // act
            Func<Task> testAction = () => SubtagRegistry.CreateFromContentAsync(null);

            // assert
            (await testAction.Should().ThrowAsync<ArgumentNullException>("the Subtag Registry content must not be null"))
                .WithParameterName(SubtagRegistryContentParameterName);
        }

        [Fact]
        public async Task CreateFromContentAsync_WithAcceptBareLineFeedForIanaRegistry_ReturnsDeserializedRegistry()
        {
            // arrange
            var subtagRegistryContent = IanaSubtagRegistryResource.Text;

            // act
            var testResult = await SubtagRegistry.CreateFromContentAsync(subtagRegistryContent, SubtagRegistryLoadOptions.AcceptBareLineFeed, CancellationToken.None);

            // assert
            testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the Subtag Registry", IanaRegistryRecordCount);
        }

        [Fact]
        public void CreateFromFile_WhenRegistryFilePathIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => SubtagRegistry.CreateFromFile(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the Subtag Registry file path must not be null")
                .WithParameterName(SubtagRegistryFilePathParameterName);
        }

        [Fact]
        public async Task CreateFromFileAsync_WhenRegistryFilePathIsNull_ThrowsArgumentNullException()
        {
            // act
            Func<Task> testAction = () => SubtagRegistry.CreateFromFileAsync(null);

            // assert
            (await testAction.Should().ThrowAsync<ArgumentNullException>("the Subtag Registry file path must not be null"))
                .WithParameterName(SubtagRegistryFilePathParameterName);
        }

        [Fact]
        public void CreateFromStream_WhenStreamIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => _ = SubtagRegistry.CreateFromStream(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>("the stream must not be null");
        }

        [Fact]
        public void CreateFromStream_WithAcceptBareLineFeedForIanaRegistry_ReturnsDeserializedRegistry()
        {
            // arrange
            using (var subtagRegistryStream = new MemoryStream(Encoding.UTF8.GetBytes(IanaSubtagRegistryResource.Text)))
            {
                // act
                // ReSharper disable once MethodHasAsyncOverload
                var testResult = SubtagRegistry.CreateFromStream(subtagRegistryStream, SubtagRegistryLoadOptions.AcceptBareLineFeed);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the subtag registry", IanaRegistryRecordCount);
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenStreamIsNull_ThrowsArgumentNullException()
        {
            // act
            Func<Task> testAction = () => SubtagRegistry.CreateFromStreamAsync(null);

            // assert
            await testAction.Should().ThrowAsync<ArgumentNullException>("the stream must not be null");
        }

        [Fact]
        public async Task CreateFromStreamAsync_WithAcceptBareLineFeedForIanaRegistry_ReturnsDeserializedRegistry()
        {
            // arrange
            using (var subtagRegistryStream = new MemoryStream(Encoding.UTF8.GetBytes(IanaSubtagRegistryResource.Text)))
            {
                // act
                var testResult = await SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream, SubtagRegistryLoadOptions.AcceptBareLineFeed, CancellationToken.None);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the subtag registry", IanaRegistryRecordCount);
            }
        }

        [Fact]
        public void CreateFromStream_WhenStreamIsPositioned_ReadsFromCurrentPosition()
        {
            // arrange
            using (var subtagRegistryStream = CreatePositionedRegistryStream(IanaSubtagRegistryResource.Text))
            {
                // act
                // ReSharper disable once MethodHasAsyncOverload
                var testResult = SubtagRegistry.CreateFromStream(subtagRegistryStream, SubtagRegistryLoadOptions.AcceptBareLineFeed);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "only the content after the stream's current position is read");
            }
        }

        [Fact]
        public void CreateFromStream_WhenStreamIsPositioned_LeavesStreamOpenAtItsEnd()
        {
            // arrange
            using (var subtagRegistryStream = CreatePositionedRegistryStream(IanaSubtagRegistryResource.Text))
            {
                // act
                // ReSharper disable once MethodHasAsyncOverload
                SubtagRegistry.CreateFromStream(subtagRegistryStream, SubtagRegistryLoadOptions.AcceptBareLineFeed);

                // assert
                subtagRegistryStream.CanRead.Should().BeTrue("the caller owns the stream");
                subtagRegistryStream.Position.Should().Be(subtagRegistryStream.Length, "the registry is read to the end of the stream");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenStreamIsPositioned_ReadsFromCurrentPosition()
        {
            // arrange
            using (var subtagRegistryStream = CreatePositionedRegistryStream(IanaSubtagRegistryResource.Text))
            {
                // act
                var testResult = await SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream, SubtagRegistryLoadOptions.AcceptBareLineFeed, CancellationToken.None);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "only the content after the stream's current position is read");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenStreamIsPositioned_LeavesStreamOpenAtItsEnd()
        {
            // arrange
            using (var subtagRegistryStream = CreatePositionedRegistryStream(IanaSubtagRegistryResource.Text))
            {
                // act
                await SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream, SubtagRegistryLoadOptions.AcceptBareLineFeed, CancellationToken.None);

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
                Func<Task> testAction = () => SubtagRegistry.CreateFromContentAsync(IanaSubtagRegistryResource.Text, SubtagRegistryLoadOptions.AcceptBareLineFeed, cancellationTokenSource.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>("the token was cancelled");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenTokenIsCancelledMidRead_ThrowsOperationCanceledException()
        {
            // arrange
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var subtagRegistryStream = new CancelOnFirstReadStream(Encoding.UTF8.GetBytes(IanaSubtagRegistryResource.Text), cancellationTokenSource))
            {
                // act
                Func<Task> testAction = () => SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream, SubtagRegistryLoadOptions.AcceptBareLineFeed, cancellationTokenSource.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>("the token was cancelled after the first read from the stream");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenTokenIsCancelledMidRead_StopsReading()
        {
            // arrange
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var subtagRegistryStream = new CancelOnFirstReadStream(Encoding.UTF8.GetBytes(IanaSubtagRegistryResource.Text), cancellationTokenSource))
            {
                // act
                var testResult = await Record.ExceptionAsync(() => SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream, SubtagRegistryLoadOptions.AcceptBareLineFeed, cancellationTokenSource.Token));

                // assert
                testResult.Should().BeAssignableTo<OperationCanceledException>("the token was cancelled after the first read from the stream");
                subtagRegistryStream.Position.Should().Be(CancelOnFirstReadStream.MaximumReadLength, "reading stops after the first read, whose bytes hold the whole File-Date line, because the token is checked before each line");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenTokenIsCancelledAfterLastRecordIsRead_ThrowsOperationCanceledException()
        {
            // arrange
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var subtagRegistryStream = new CancelAtEndStream(Encoding.UTF8.GetBytes("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n"), cancellationTokenSource))
            {
                // act
                Func<Task> testAction = () => SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream, cancellationTokenSource.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>("the token was cancelled when the end of the stream was read, after the last record");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenTokenIsNotCancelled_ReturnsDeserializedRegistry()
        {
            // arrange
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var subtagRegistryStream = new MemoryStream(Encoding.UTF8.GetBytes(IanaSubtagRegistryResource.Text)))
            {
                // act
                var testResult = await SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream, SubtagRegistryLoadOptions.AcceptBareLineFeed, cancellationTokenSource.Token);

                // assert
                testResult.Count.Should().Be(IanaRegistryRecordCount, "there are {0} records in the subtag registry", IanaRegistryRecordCount);
            }
        }

        /// <summary>
        /// A stream that cancels a token the first time it is read, to cancel a load part way through.
        /// Each read returns at most <see cref="MaximumReadLength"/> bytes, so how far the load gets doesn't depend on how many bytes the framework's <see cref="StreamReader"/> asks for.
        /// </summary>
        private sealed class CancelOnFirstReadStream : MemoryStream
        {
            /// <summary>
            /// The most bytes one read returns: more than the registry's first line, <c>File-Date: 2025-05-15</c> and its line feed, and fewer than any <see cref="StreamReader"/> buffer.
            /// </summary>
            public const int MaximumReadLength = 64;

            private readonly CancellationTokenSource _cancellationTokenSource;

            public CancelOnFirstReadStream(byte[] buffer, CancellationTokenSource cancellationTokenSource)
                : base(buffer)
            {
                _cancellationTokenSource = cancellationTokenSource;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                _cancellationTokenSource.Cancel();
                return base.Read(buffer, offset, Math.Min(count, MaximumReadLength));
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                _cancellationTokenSource.Cancel();
                return base.ReadAsync(buffer, offset, Math.Min(count, MaximumReadLength), CancellationToken.None);
            }

#if NET
            public override int Read(Span<byte> buffer)
            {
                _cancellationTokenSource.Cancel();
                return base.Read(buffer.Slice(0, Math.Min(buffer.Length, MaximumReadLength)));
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                _cancellationTokenSource.Cancel();
                return base.ReadAsync(buffer.Slice(0, Math.Min(buffer.Length, MaximumReadLength)), CancellationToken.None);
            }
#endif
        }

        /// <summary>
        /// A stream that cancels a token when a read finds the end of the stream, to cancel a load after its last line has been read.
        /// </summary>
        private sealed class CancelAtEndStream : MemoryStream
        {
            private readonly CancellationTokenSource _cancellationTokenSource;

            public CancelAtEndStream(byte[] buffer, CancellationTokenSource cancellationTokenSource)
                : base(buffer)
            {
                _cancellationTokenSource = cancellationTokenSource;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return CancelAtEnd(base.Read(buffer, offset, count));
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return CancelAtEnd(await base.ReadAsync(buffer, offset, count, CancellationToken.None));
            }

#if NET
            public override int Read(Span<byte> buffer)
            {
                return CancelAtEnd(base.Read(buffer));
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return CancelAtEnd(await base.ReadAsync(buffer, CancellationToken.None));
            }
#endif

            private int CancelAtEnd(int bytesRead)
            {
                if (bytesRead == 0) _cancellationTokenSource.Cancel();
                return bytesRead;
            }
        }

        /// <summary>
        /// A stream with a complete tag record ahead of the registry content, positioned after that record, so reading from the start of the stream would fail,
        /// as the first record is not the File-Date record.
        /// </summary>
        private static MemoryStream CreatePositionedRegistryStream(string registryContent)
        {
            var precedingBytes = Encoding.UTF8.GetBytes("Type: language\r\nSubtag: qaa\r\nDescription: Preceding record\r\nAdded: 2000-01-01\r\n%%\r\n");
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
            var record = SubtagRegistry.CreateFromContent("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: en\r\nDescription: English\r\nAdded: 2005-10-16\r\n").Languages[0];

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

            // act
            var testResult = (First: record.Description, Second: record.Description);

            // assert
            testResult.Second.Should().BeSameAs(testResult.First, "the read-only view is created once");
            testResult.Second.Should().Equal(new[] { "English" });
        }

        /// <summary>
        /// Each typed list of the IANA registry fixture, with the number of records of its type in the fixture and the subtag or tag of the first and last of them.
        /// </summary>
        public static TheoryData<SubtagRegistryRecordType, Func<SubtagRegistry, IReadOnlyList<SubtagRegistryTagRecordBase>>, int> TypedRecordLists =>
            new TheoryData<SubtagRegistryRecordType, Func<SubtagRegistry, IReadOnlyList<SubtagRegistryTagRecordBase>>, int>
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
        public void TypedRecordLists_ForFixture_HoldEveryRecordOfTheirType(SubtagRegistryRecordType type, Func<SubtagRegistry, IReadOnlyList<SubtagRegistryTagRecordBase>> list, int expectedCount)
        {
            // arrange
            var registry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = list(registry);

            // assert
            testResult.Should().HaveCount(expectedCount, "the fixture has {0} records of type {1}", expectedCount, type);
            testResult.Should().OnlyContain(record => record.Type == type);
        }

        public static TheoryData<Func<SubtagRegistry, IReadOnlyList<SubtagRegistrySubtagRecord>>, string, string> SubtagRecordListEnds =>
            new TheoryData<Func<SubtagRegistry, IReadOnlyList<SubtagRegistrySubtagRecord>>, string, string>
            {
                { registry => registry.Languages, MinimalRecordSubtag, "zzj" },
                { registry => registry.ExtendedLanguages, "aao", "zsm" },
                { registry => registry.Scripts, "Adlm", "Zzzz" },
                { registry => registry.Regions, "AA", "419" },
                { registry => registry.Variants, "1606nict", "xsistemo" }
            };

        [Theory]
        [MemberData(nameof(SubtagRecordListEnds), DisableDiscoveryEnumeration = true)]
        public void TypedRecordLists_ForFixture_KeepRegistryOrder(Func<SubtagRegistry, IReadOnlyList<SubtagRegistrySubtagRecord>> list, string expectedFirstSubtag, string expectedLastSubtag)
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
        public void TryGetSubtag_WhenTwoRecordsHaveTheSameSubtag_ReturnsTheFirst(string subtag)
        {
            // arrange
            var registry = SubtagRegistry.CreateFromContent(
                "File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: ab\r\nDescription: Other\r\nAdded: 2005-10-16\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: First\r\nAdded: 2005-10-16\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Second\r\nAdded: 2005-10-16\r\n%%\r\nType: language\r\nSubtag: zz\r\nDescription: Other\r\nAdded: 2005-10-16\r\n");

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

        [Fact]
        public void IsGrandfathered_ForRecordOfTagThatTheAbnfDoesNotList_ReturnsTrue()
        {
            // arrange
            var registry = SubtagRegistry.CreateFromContent("File-Date: 2025-01-01\r\n%%\r\nType: grandfathered\r\nTag: a-bb\r\nDescription: T\r\nAdded: 2005-10-16\r\n");

            // act
            var testResult = registry.IsGrandfathered("a-bb");

            // assert
            testResult.Should().BeTrue("this method reports the registry's records, whether or not validation can use them");
        }

        #endregion

        #region Test: ToString and reading content

        [Fact]
        public void ToString_WhenRegistryIsLoadedAgainFromIt_WritesSameContent()
        {
            // arrange
            var content = IanaSubtagRegistryResource.Content;

            // act
            var testResult = SubtagRegistry.CreateFromContent(content).ToString();

            // assert
            testResult.Should().Be(content, "every record and field is read and written back as it was");
        }

        [Fact]
        public void ToString_ForRegistry_WritesCrLfAtEndOfEveryLine()
        {
            // arrange
            var registry = SubtagRegistry.CreateFromContent(CrLfRegistry);

            // act
            var testResult = registry.ToString();

            // assert
            testResult.Should().Be(CrLfRegistry, "every line of the registry, the last one included, ends with CRLF on every platform (ref. RFC 5646 §3.1.1)");
        }

        [Fact]
        public void RecordToString_ForRecord_WritesEachFieldAsLineEndingWithCrLf()
        {
            // arrange
            var record = SubtagRegistry.CreateFromContent(CrLfRegistry).Languages[0];

            // act
            var testResult = record.ToString();

            // assert
            testResult.Should().Be("Type: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n", "each field ends with CRLF (ref. RFC 5646 §3.1.1)");
        }

        [Theory]
        [MemberData(nameof(ContentLoaderCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFromContent_WhenFieldIsFolded_JoinsLinesWithOneSpace(string loaderName, Func<string, Task<SubtagRegistry>> load)
        {
            // arrange
            const string content = "File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: ia\r\nDescription: Interlingua (International Auxiliary Language\r\n  Association)\r\nComments: first \r\n  second\r\n   third\r\nAdded: 2005-10-16\r\n";

            // act
            var testResult = await load(content);

            // assert
            var record = testResult.Languages.Should().ContainSingle("{0} reads the one record", loaderName).Subject;
            record.Description.Should().Equal(new[] { "Interlingua (International Auxiliary Language Association)" }, "a folded line is unfolded with one space (ref. RFC 5646 §3.1.1)");
            record.Comments.Should().Equal(new[] { "first second third" }, "each folded line is trimmed, and a space may come before the line break where a folded line follows");
            record.Added.Should().Be(RegistryCreationDate);
        }

        [Fact]
        public void CreateFromContent_ForEveryFieldInRegistry_HasNoDoubledOrSurroundingSpaces()
        {
            // arrange
            var content = IanaSubtagRegistryResource.Text;

            // act
            var testResult = SubtagRegistry.CreateFromContent(content, SubtagRegistryLoadOptions.AcceptBareLineFeed);

            // assert
            testResult.Should().OnlyContain(
                record => record.Fields.All(field => !field.Value.Contains("  ") && field.Value.Trim() == field.Value),
                "folded lines in the registry fixture are unfolded with one space");
        }

        [Theory]
        [MemberData(nameof(ContentLoaderCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFromContent_WhenFieldNameIsUnknown_ReadsItAsSeparateField(string loaderName, Func<string, Task<SubtagRegistry>> load)
        {
            // arrange
            const string content = "File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: qaa\r\nDescription: Private use\r\nFuture-Field: some value\r\nAdded: 2005-10-16\r\n";

            // act
            var testResult = await load(content);

            // assert
            var record = testResult.Languages.Should().ContainSingle("{0} reads the one record", loaderName).Subject;
            record.Description.Should().Equal(new[] { "Private use" }, "a field this version doesn't know is not part of the field before it");
            record.Fields.Should().Contain(new KeyValuePair<string, string>("Future-Field", "some value"));
        }

        [Theory]
        [MemberData(nameof(ContentLoaderCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFromContent_WhenFieldThatRfcDoesNotDefineRepeatsInSubtagRecord_ReadsEveryOccurrence(string loaderName, Func<string, Task<SubtagRegistry>> load)
        {
            // arrange
            const string content = "File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\nFuture-Field: two\r\nFuture-Field: three\r\n";

            // act
            var testResult = await load(content);

            // assert
            testResult.Count.Should().Be(2, "{0} ignores fields that RFC 5646 §3.1.2 doesn't define when it checks the rules of a subtag record", loaderName);
            testResult.Languages.Should().ContainSingle().Which.Fields.Should().Equal(
                new KeyValuePair<string, string>(TypeFieldName, LanguageType),
                new KeyValuePair<string, string>("Subtag", "aa"),
                new KeyValuePair<string, string>("Description", "Afar"),
                new KeyValuePair<string, string>(AddedFieldName, "2005-10-16"),
                new KeyValuePair<string, string>("Future-Field", "two"),
                new KeyValuePair<string, string>("Future-Field", "three"));
        }

        /// <summary>
        /// A Description with a character outside US-ASCII, U+00FC, which a Description may hold (ref. RFC 5646 §3.1.5).
        /// </summary>
        private const string NonAsciiDescription = "Volap\x00FCk";

        /// <summary>
        /// A Comments value outside US-ASCII, U+00E9, which Comments may hold (ref. RFC 5646 §3.1.12).
        /// </summary>
        private const string NonAsciiComment = "\x00E9";

        /// <summary>
        /// A value outside US-ASCII, U+00F1, of a field that RFC 5646 doesn't define, which the rules ignore (ref. RFC 5646 §3.1.2).
        /// </summary>
        private const string NonAsciiUndefinedFieldValue = "\x00F1";

        /// <summary>
        /// A registry whose Description, Comments and undefined field hold characters outside US-ASCII.
        /// </summary>
        private const string NonAsciiFieldsRegistry = "File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: vo\r\nDescription: " + NonAsciiDescription + "\r\nComments: " + NonAsciiComment + "\r\nFuture-Field: " + NonAsciiUndefinedFieldValue + "\r\nAdded: 2005-10-16\r\n";

        [Theory]
        [MemberData(nameof(ContentLoaderCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFromContent_WhenDescriptionCommentsOrUndefinedFieldHoldsNonAsciiCharacter_ReadsField(string loaderName, Func<string, Task<SubtagRegistry>> load)
        {
            // arrange
            const string content = NonAsciiFieldsRegistry;

            // act
            var testResult = await load(content);

            // assert
            var record = testResult.Languages.Should().ContainSingle(
                "{0} restricts only the fields that RFC 5646 defines, other than Description and Comments, to printable US-ASCII (ref. RFC 5646 §3.1.1, §3.1.2, §3.1.5, §3.1.12)",
                loaderName).Subject;
            record.Description.Should().Equal(NonAsciiDescription);
            record.Comments.Should().Equal(NonAsciiComment);
            record.Fields.Should().Contain(new KeyValuePair<string, string>("Future-Field", NonAsciiUndefinedFieldValue));
        }

        [Theory]
        [MemberData(nameof(ContentLoaderCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFromContent_WhenFieldSeparatorHasSpaces_ReadsField(string loaderName, Func<string, Task<SubtagRegistry>> load)
        {
            // arrange
            const string content = "File-Date: 2025-01-01\r\n%%\r\nType  :  language\r\nSubtag : qaa\r\nDescription: Private use\r\nAdded: 2005-10-16\r\n";

            // act
            var testResult = await load(content);

            // assert
            testResult.Languages.Should().ContainSingle("{0} reads the spaces around the colon as part of the separator (ref. RFC 5646 §3.1.1)", loaderName)
                .Which.Subtag.Should().Be("qaa");
        }

        /// <summary>
        /// Registries that the record-jar format of RFC 5646 §3.1.1, or the records of §3.1.2 and §3.1.3, don't allow, each with the message of the
        /// <see cref="FormatException"/> it fails with, which names its line.
        /// </summary>
        private static readonly (string Content, string ExpectedMessage)[] MalformedRegistries =
        {
            // a line that doesn't start with a space is not a folded line (field-body)
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: fr\r\nDescription: French\r\nUnited Kingdom\r\n",
                "Line 6 has no colon, so it is neither a field nor a folded line, which starts with a space (ref. RFC 5646 §3.1.1)."),
            // field-name = (ALPHA / DIGIT) [*(ALPHA / DIGIT / "-") (ALPHA / DIGIT)]
            ("File-Date: 2025-01-01\r\n%%\r\n-Type: language\r\nSubtag: aa\r\n",
                "Line 3 has a field name that starts or ends with a hyphen (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\n: language\r\nSubtag: aa\r\n",
                "Line 3 has no field name before its colon (ref. RFC 5646 §3.1.1)."),
            // a byte order mark left in the content, U+FEFF
            ("\xFEFFFile-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\n",
                "Line 1 has the character U+FEFF in its field name, which holds only letters, digits and hyphens (ref. RFC 5646 §3.1.1)."),
            // a no-break space, U+00A0, is not SP
            ("File-Date: 2025-01-01\r\n%%\r\nType\xA0: language\r\nSubtag: aa\r\n",
                "Line 3 has the character U+00A0 in its field name, which holds only letters, digits and hyphens (ref. RFC 5646 §3.1.1)."),
            // records are separated by lines of "%%" alone
            ("File-Date: 2025-01-01\r\n%% \r\nType: language\r\nSubtag: aa\r\n",
                "Line 2 has spaces after the \"%%\" that separates records, which is a line of \"%%\" alone (ref. RFC 5646 §3.1.1)."),
            // a tab is not white space in field-sep or field-body, nor one of CHARS
            ("File-Date: 2025-01-01\r\n%%\r\nType\t: language\r\nSubtag: aa\r\n",
                "Line 3 holds the control character U+0009, which a field may not hold (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: ia\r\nDescription: Interlingua\r\n\tAssociation\r\n",
                "Line 6 holds the control character U+0009, which a field may not hold (ref. RFC 5646 §3.1.1)."),
            // record = 1*field, with no blank lines
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\n\r\nSubtag: aa\r\n",
                "Line 4 is blank, which a record may not hold (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\n%%\r\nType: language\r\nSubtag: aa\r\n",
                "The record that ends at line 3 has no fields (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\n",
                "The registry ends after the \"%%\" at line 2, with no record after it (ref. RFC 5646 §3.1.1)."),
            ("",
                "The registry has no records (ref. RFC 5646 §3.1.1)."),
            // a folded line continues a field, and holds more than spaces
            ("File-Date: 2025-01-01\r\n%%\r\n  Type: language\r\nSubtag: aa\r\n",
                "Line 3 is a folded line, but no field comes before it in its record (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nComments: first\r\n   \r\n  second\r\n",
                "Line 6 is a folded line with nothing but spaces (ref. RFC 5646 §3.1.1)."),
            // field-body ends with CHARS
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa \r\nDescription: Afar\r\n",
                "The Subtag field ends with a space at line 4 (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: ia\r\nDescription: Interlingua\r\n  Association \r\n",
                "The Description field ends with a space at line 6 (ref. RFC 5646 §3.1.1)."),
            // File-Date, Added and Deprecated are each an RFC 3339 full-date: 4DIGIT "-" 2DIGIT "-" 2DIGIT, with a month and a day of that month
            ("File-Date: 2025-13-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n",
                "The File-Date field at line 1 has the value '2025-13-01', which is not an RFC 3339 full-date (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-1\r\n",
                "The Added field at line 6 has the value '2005-10-1', which is not an RFC 3339 full-date (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-02-30\r\n",
                "The Added field at line 6 has the value '2005-02-30', which is not an RFC 3339 full-date (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 16 Oct 2005\r\n",
                "The Added field at line 6 has the value '16 Oct 2005', which is not an RFC 3339 full-date (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: region\r\nSubtag: BU\r\nDescription: Burma\r\nAdded: 2005-10-16\r\nDeprecated: 1989-12-5\r\n",
                "The Deprecated field at line 7 has the value '1989-12-5', which is not an RFC 3339 full-date (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n  extra\r\n",
                "The Added field at line 6 has the value '2005-10-16 extra', which is not an RFC 3339 full-date (ref. RFC 5646 §3.1.1)."),
            // 1900 is not a leap year in the Gregorian calendar (RFC 3339 §5.7)
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 1900-02-29\r\n",
                "The Added field at line 6 has the value '1900-02-29', which is not an RFC 3339 full-date (ref. RFC 5646 §3.1.1)."),
            // the year 0000 is a full-date, but before the first year a DateTime holds
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 0000-02-29\r\n",
                "The Added field at line 6 has the value '0000-02-29', a full-date in the year 0000, which is before the year 0001 that a DateTime starts at."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: grandfathered\r\nTag: i-ami\r\nDescription: Amis\r\nAdded: 1999-05-25\r\nDeprecated: 2009-07-29\r\nDeprecated: 2009-7-29\r\n",
                "The Deprecated field at line 8 is the second in its record, which may have only one (ref. RFC 5646 §3.1.2)."),
            // the kind of record is told by its File-Date, Subtag or Tag field, and its type by its Type field, which each need a value
            ("File-Date: 2025-01-01\r\n%%\r\nType:\r\nSubtag: aa\r\n",
                "The Type field at line 3 has no value (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag:\r\n",
                "The Subtag field at line 4 has no value (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtg: en\r\nDescription: English\r\n",
                "The record at line 3 has no File-Date, Subtag or Tag field (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n%%\r\nType: language\r\nDescription: No subtag or tag\r\n",
                "The record at line 8 has no File-Date, Subtag or Tag field (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nTag: aa\r\n",
                "The record at line 3 has more than one of the File-Date, Subtag and Tag fields (ref. RFC 5646 §3.1.2)."),
            // the first record is the File-Date record, which occurs once and has a single field
            ("Type: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n",
                "The first record, at line 1, is not the File-Date record (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nFile-Date: 2025-01-02\r\n",
                "The record at line 3 is a second File-Date record, which occurs only once, as the first record (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\nComments: Test\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n",
                "The File-Date record at line 1 has a field other than File-Date (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\nFuture-Field: Test\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n",
                "The File-Date record at line 1 has a field other than File-Date (ref. RFC 5646 §3.1.2)."),
            // every tag and subtag record has a Description and an Added field
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has no Description field (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription:\r\nAdded: 2005-10-16\r\n",
                "The Description field at line 5 has no value (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\n",
                "The record at line 3 has no Added field (ref. RFC 5646 §3.1.2)."),
            // a field other than Description, Comments and Prefix appears at most once in a record
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nSubtag: ab\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n",
                "The Subtag field at line 5 is the second in its record, which may have only one (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nType: script\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n",
                "The Type field at line 4 is the second in its record, which may have only one (ref. RFC 5646 §3.1.2)."),
            // every tag and subtag record has a Type, which is one of the values of §3.1.3 that goes with its Tag or Subtag field
            ("File-Date: 2025-01-01\r\n%%\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has no Type field (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: future\r\nSubtag: qaa\r\nDescription: Private use\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has the Type 'future', which is not one that RFC 5646 §3.1.3 defines."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: grandfathered\r\nSubtag: i-klingon\r\nDescription: Klingon\r\nAdded: 1999-05-26\r\n",
                "The record at line 3 has a Subtag field, but its Type 'grandfathered' is one whose records have a Tag field (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nTag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has a Tag field, but its Type 'language' is one whose records have a Subtag field (ref. RFC 5646 §3.1.2)."),
            // Prefix appears only in extlang and variant records (§3.1.2), with or without a value, and exactly once in an extlang record (§3.1.8)
            ("File-Date: 2025-01-01\r\n%%\r\nType: region\r\nSubtag: BU\r\nDescription: Burma\r\nAdded: 2005-10-16\r\nPrefix: en\r\n",
                "The record at line 3 has a Prefix field, which only records of the Type 'extlang' or 'variant' may have, but its Type is 'region' (ref. RFC 5646 §3.1.2)."),
            // a Prefix holds a language tag, so it has a value, whatever the Type of its record (§3.1.2, §3.1.8)
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\nPrefix:\r\n",
                "The Prefix field at line 7 has no value (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: extlang\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nPrefix:\r\n",
                "The Prefix field at line 7 has no value (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: extlang\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\n",
                "The record at line 3 has 0 Prefix fields, but a record of the Type 'extlang' has exactly one (ref. RFC 5646 §3.1.8)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: extlang\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nPrefix: ar\r\nPrefix: aao\r\n",
                "The record at line 3 has 2 Prefix fields, but a record of the Type 'extlang' has exactly one (ref. RFC 5646 §3.1.8)."),
            // Suppress-Script, Macrolanguage and Scope appear only in language and extlang records (§3.1.2, §3.1.9, §3.1.10)
            ("File-Date: 2025-01-01\r\n%%\r\nType: script\r\nSubtag: Latn\r\nDescription: Latin\r\nAdded: 2005-10-16\r\nSuppress-Script: Latn\r\n",
                "The record at line 3 has a Suppress-Script field, which only records of the Type 'language' or 'extlang' may have, but its Type is 'script' (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: script\r\nSubtag: Hans\r\nDescription: Han (Simplified variant)\r\nAdded: 2005-10-16\r\nMacrolanguage: zh\r\n",
                "The record at line 3 has a Macrolanguage field, which only records of the Type 'language' or 'extlang' may have, but its Type is 'script' (ref. RFC 5646 §3.1.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: variant\r\nSubtag: rozaj\r\nDescription: Resian\r\nAdded: 2005-10-16\r\nScope: special\r\n",
                "The record at line 3 has a Scope field, which only records of the Type 'language' or 'extlang' may have, but its Type is 'variant' (ref. RFC 5646 §3.1.2)."),
            // Scope permits "macrolanguage", "collection", "special" and "private-use" alone (§3.1.2)
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\nScope: individual\r\n",
                "The record at line 3 has the Scope 'individual', which is not one of the values 'macrolanguage', 'collection', 'special' and 'private-use' that RFC 5646 §3.1.2 permits."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\nScope: Special\r\n",
                "The record at line 3 has the Scope 'Special', which is not one of the values 'macrolanguage', 'collection', 'special' and 'private-use' that RFC 5646 §3.1.2 permits."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\nScope:\r\n",
                "The Scope field at line 7 has no value (ref. RFC 5646 §3.1.2)."),
            // a Prefix contains a valid language tag (§3.1.2, §3.1.8): well-formed, and repeating no variant subtag or extension singleton (§2.2.9)
            ("File-Date: 2025-01-01\r\n%%\r\nType: variant\r\nSubtag: rozaj\r\nDescription: Resian\r\nAdded: 2005-10-16\r\nPrefix: !!\r\n",
                "The record at line 3 has the Prefix '!!', which is not a well-formed language tag that repeats no variant subtag or extension singleton, as a valid language tag is (ref. RFC 5646 §2.2.9, §3.1.2, §3.1.8)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: extlang\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nPrefix: ar-\r\n",
                "The record at line 3 has the Prefix 'ar-', which is not a well-formed language tag that repeats no variant subtag or extension singleton, as a valid language tag is (ref. RFC 5646 §2.2.9, §3.1.2, §3.1.8)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: variant\r\nSubtag: biske\r\nDescription: The San Giorgio dialect of Resian\r\nAdded: 2007-07-05\r\nPrefix: sl-rozaj\r\nPrefix: sl-rozaj-rozaj\r\n",
                "The record at line 3 has the Prefix 'sl-rozaj-rozaj', which is not a well-formed language tag that repeats no variant subtag or extension singleton, as a valid language tag is (ref. RFC 5646 §2.2.9, §3.1.2, §3.1.8)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: variant\r\nSubtag: rozaj\r\nDescription: Resian\r\nAdded: 2005-10-16\r\nPrefix: sl-a-bb-a-cc\r\n",
                "The record at line 3 has the Prefix 'sl-a-bb-a-cc', which is not a well-formed language tag that repeats no variant subtag or extension singleton, as a valid language tag is (ref. RFC 5646 §2.2.9, §3.1.2, §3.1.8)."),
            // a Suppress-Script contains a script subtag, four letters (§2.2.3, §3.1.2, §3.1.9)
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\nSuppress-Script: 12\r\n",
                "The record at line 3 has the Suppress-Script '12', which is not a script subtag of four letters (ref. RFC 5646 §2.2.3, §3.1.2, §3.1.9)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\nSuppress-Script: Latin\r\n",
                "The record at line 3 has the Suppress-Script 'Latin', which is not a script subtag of four letters (ref. RFC 5646 §2.2.3, §3.1.2, §3.1.9)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\nSuppress-Script:\r\n",
                "The Suppress-Script field at line 7 has no value (ref. RFC 5646 §3.1.2)."),
            // a Macrolanguage contains a primary language subtag, two to eight letters (§2.2.1, §3.1.2, §3.1.10)
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nMacrolanguage: a\r\n",
                "The record at line 3 has the Macrolanguage 'a', which is not a primary language subtag of two to eight letters (ref. RFC 5646 §2.2.1, §3.1.2, §3.1.10)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nMacrolanguage: ar-DZ\r\n",
                "The record at line 3 has the Macrolanguage 'ar-DZ', which is not a primary language subtag of two to eight letters (ref. RFC 5646 §2.2.1, §3.1.2, §3.1.10)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nMacrolanguage:\r\n",
                "The Macrolanguage field at line 7 has no value (ref. RFC 5646 §3.1.2)."),
            // a Preferred-Value takes the form §3.1.2 gives it for the record's Type: a primary language subtag for language, a subtag of the same
            // type for script, region and variant, and an extended language range of RFC 4647 §2.2 for extlang, grandfathered and redundant
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: in\r\nDescription: Indonesian\r\nAdded: 2005-10-16\r\nDeprecated: 1989-01-01\r\nPreferred-Value: id-ID\r\n",
                "The record at line 3 has the Preferred-Value 'id-ID', which is not a primary language subtag, as a record of the Type 'language' has (ref. RFC 5646 §3.1.2, §3.1.7)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: script\r\nSubtag: Qaai\r\nDescription: Inherited\r\nAdded: 2005-10-16\r\nDeprecated: 2007-07-05\r\nPreferred-Value: Zin\r\n",
                "The record at line 3 has the Preferred-Value 'Zin', which is not a script subtag, as a record of the Type 'script' has (ref. RFC 5646 §3.1.2, §3.1.7)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: region\r\nSubtag: BU\r\nDescription: Burma\r\nAdded: 2005-10-16\r\nDeprecated: 1989-12-05\r\nPreferred-Value: en-US\r\n",
                "The record at line 3 has the Preferred-Value 'en-US', which is not a region subtag, as a record of the Type 'region' has (ref. RFC 5646 §3.1.2, §3.1.7)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: region\r\nSubtag: BU\r\nDescription: Burma\r\nAdded: 2005-10-16\r\nDeprecated: 1989-12-05\r\nPreferred-Value: M1\r\n",
                "The record at line 3 has the Preferred-Value 'M1', which is not a region subtag, as a record of the Type 'region' has (ref. RFC 5646 §3.1.2, §3.1.7)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: variant\r\nSubtag: heploc\r\nDescription: Hepburn romanization, Library of Congress method\r\nAdded: 2009-10-01\r\nDeprecated: 2010-02-07\r\nPreferred-Value: abcd\r\n",
                "The record at line 3 has the Preferred-Value 'abcd', which is not a variant subtag, as a record of the Type 'variant' has (ref. RFC 5646 §3.1.2, §3.1.7)."),
            // an extlang record has a Preferred-Value, identical to its Subtag (§2.2.2)
            ("File-Date: 2025-01-01\r\n%%\r\nType: extlang\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nPrefix: ar\r\n",
                "The record at line 3 has no Preferred-Value field, which a record of the Type 'extlang' has (ref. RFC 5646 §2.2.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: extlang\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nPreferred-Value: ar-aao\r\nPrefix: ar\r\n",
                "The record at line 3 has the Preferred-Value 'ar-aao', which is not identical to its Subtag 'aao', as in a record of the Type 'extlang' (ref. RFC 5646 §2.2.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: extlang\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nPreferred-Value: AAO\r\nPrefix: ar\r\n",
                "The record at line 3 has the Preferred-Value 'AAO', which is not identical to its Subtag 'aao', as in a record of the Type 'extlang' (ref. RFC 5646 §2.2.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: extlang\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nPreferred-Value: *\r\nPrefix: ar\r\n",
                "The record at line 3 has the Preferred-Value '*', which is not identical to its Subtag 'aao', as in a record of the Type 'extlang' (ref. RFC 5646 §2.2.2)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: grandfathered\r\nTag: i-klingon\r\nDescription: Klingon\r\nAdded: 1999-05-26\r\nDeprecated: 2004-02-24\r\nPreferred-Value: tlh--x\r\n",
                "The record at line 3 has the Preferred-Value 'tlh--x', which is not an extended language range of RFC 4647 §2.2, as a record of the Type 'grandfathered' has (ref. RFC 5646 §3.1.2, §3.1.7)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: redundant\r\nTag: zh-cmn-Hans\r\nDescription: Mandarin Chinese (Simplified)\r\nAdded: 2005-07-15\r\nDeprecated: 2009-07-29\r\nPreferred-Value: cmn-Hansabcde\r\n",
                "The record at line 3 has the Preferred-Value 'cmn-Hansabcde', which is not an extended language range of RFC 4647 §2.2, as a record of the Type 'redundant' has (ref. RFC 5646 §3.1.2, §3.1.7)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: in\r\nDescription: Indonesian\r\nAdded: 2005-10-16\r\nDeprecated: 1989-01-01\r\nPreferred-Value:\r\n",
                "The Preferred-Value field at line 8 has no value (ref. RFC 5646 §3.1.2)."),
            // a Subtag has the form of its Type (§2.2.1 to §2.2.5) and the case of §3.1.4: titlecase for a script, upper case for a region, and lower
            // case for every other Type
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: en US!\r\nDescription: English\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has the Subtag 'en US!', which is not a subtag of the Type 'language', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: EN\r\nDescription: English\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has the Subtag 'EN', which is not a subtag of the Type 'language', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: extlang\r\nSubtag: aaoo\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nPrefix: ar\r\n",
                "The record at line 3 has the Subtag 'aaoo', which is not a subtag of the Type 'extlang', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: script\r\nSubtag: latn\r\nDescription: Latin\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has the Subtag 'latn', which is not a subtag of the Type 'script', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: region\r\nSubtag: 12\r\nDescription: Twelve\r\nAdded: 2025-01-01\r\n",
                "The record at line 3 has the Subtag '12', which is not a subtag of the Type 'region', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: region\r\nSubtag: us\r\nDescription: United States\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has the Subtag 'us', which is not a subtag of the Type 'region', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: variant\r\nSubtag: abcd\r\nDescription: Four letters\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has the Subtag 'abcd', which is not a subtag of the Type 'variant', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: variant\r\nSubtag: Rozaj\r\nDescription: Resian\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has the Subtag 'Rozaj', which is not a subtag of the Type 'variant', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4."),
            // a range has the form and case of its Type at both ends, which have the same length, with the first not after the second (§3.1.1)
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: qaa..QTZ\r\nDescription: Private use\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has the Subtag 'qaa..QTZ', which is not a subtag of the Type 'language', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: qtz..qaa\r\nDescription: Private use\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has the Subtag 'qtz..qaa', which is not a subtag of the Type 'language', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: qaa..qtzz\r\nDescription: Private use\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has the Subtag 'qaa..qtzz', which is not a subtag of the Type 'language', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: region\r\nSubtag: QM..\r\nDescription: Private use\r\nAdded: 2005-10-16\r\n",
                "The record at line 3 has the Subtag 'QM..', which is not a subtag of the Type 'region', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4."),
            // a Tag is formatted as §2.1.1 describes (§3.1.4)
            ("File-Date: 2025-01-01\r\n%%\r\nType: grandfathered\r\nTag: foo bar\r\nDescription: Foo\r\nAdded: 1999-05-25\r\n",
                "The record at line 3 has the Tag 'foo bar', which is not formatted as RFC 5646 §2.1.1 describes (ref. RFC 5646 §3.1.4)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: redundant\r\nTag: zh-hans\r\nDescription: simplified Chinese\r\nAdded: 2005-04-11\r\n",
                "The record at line 3 has the Tag 'zh-hans', which is not formatted as RFC 5646 §2.1.1 describes (ref. RFC 5646 §3.1.4)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: grandfathered\r\nTag: en-gb-oed\r\nDescription: English, Oxford English Dictionary spelling\r\nAdded: 2003-07-09\r\n",
                "The record at line 3 has the Tag 'en-gb-oed', which is not formatted as RFC 5646 §2.1.1 describes (ref. RFC 5646 §3.1.4)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: grandfathered\r\nTag: I-ami\r\nDescription: Amis\r\nAdded: 1999-05-25\r\n",
                "The record at line 3 has the Tag 'I-ami', which is not formatted as RFC 5646 §2.1.1 describes (ref. RFC 5646 §3.1.4)."),
            // only a subtag of two or four letters has a capital, so one with a digit is lower case (§2.1.1)
            ("File-Date: 2025-01-01\r\n%%\r\nType: grandfathered\r\nTag: en-A1\r\nDescription: Digit\r\nAdded: 1999-05-25\r\n",
                "The record at line 3 has the Tag 'en-A1', which is not formatted as RFC 5646 §2.1.1 describes (ref. RFC 5646 §3.1.4)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: redundant\r\nTag: en--US\r\nDescription: Empty subtag\r\nAdded: 2005-04-11\r\n",
                "The record at line 3 has the Tag 'en--US', which is not formatted as RFC 5646 §2.1.1 describes (ref. RFC 5646 §3.1.4)."),
            // the fields that RFC 5646 defines, except Description and Comments, hold printable US-ASCII characters alone (§3.1.1)
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: \x00E9n\r\nDescription: English\r\nAdded: 2005-10-16\r\n",
                "The Subtag field at line 4 holds the character U+00E9, which is not one of the printable US-ASCII characters that the field is restricted to (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: r\x00E9gion\r\nSubtag: US\r\nDescription: United States\r\nAdded: 2005-10-16\r\n",
                "The Type field at line 3 holds the character U+00E9, which is not one of the printable US-ASCII characters that the field is restricted to (ref. RFC 5646 §3.1.1)."),
            ("File-Date: 2025-01-01\r\n%%\r\nType: variant\r\nSubtag: rozaj\r\nDescription: Resian\r\nAdded: 2005-10-16\r\nPrefix: sl\x00E9\r\n",
                "The Prefix field at line 7 holds the character U+00E9, which is not one of the printable US-ASCII characters that the field is restricted to (ref. RFC 5646 §3.1.1)."),
            // DEL, U+007F, is one of CHARS but not a printable US-ASCII character
            ("File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\nPreferred-Value: a\x007F\r\n",
                "The Preferred-Value field at line 7 holds the character U+007F, which is not one of the printable US-ASCII characters that the field is restricted to (ref. RFC 5646 §3.1.1)."),
        };

        /// <summary>
        /// Each malformed registry, with both ways to load a registry from its content.
        /// </summary>
        public static TheoryData<string, Func<string, Task<SubtagRegistry>>, string, string> MalformedRegistryCases
        {
            get
            {
                var data = new TheoryData<string, Func<string, Task<SubtagRegistry>>, string, string>();
                foreach (var loader in ContentLoaders)
                foreach (var registry in MalformedRegistries)
                {
                    data.Add(loader.Key, loader.Value, registry.Content, registry.ExpectedMessage);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(MalformedRegistryCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFromContent_WhenRegistryIsMalformed_ThrowsFormatExceptionNamingTheLine(string loaderName, Func<string, Task<SubtagRegistry>> load, string content, string expectedMessage)
        {
            // act
            Func<Task> testAction = () => load(content);

            // assert
            (await testAction.Should().ThrowAsync<FormatException>("{0} reads only the format of RFC 5646 §3.1.1", loaderName))
                .Which.Message.Should().Be(expectedMessage);
        }

        /// <summary>
        /// Registries whose Prefix, Suppress-Script, Macrolanguage or Preferred-Value field has the form RFC 5646 §3.1.2 gives it, in a case other than
        /// the registry's own or with a wildcard, which RFC 5646 permits: §3.1.4 sets the case of the Subtag and Tag fields alone, and the case of a
        /// language tag is not significant (§2.1.1); an extended language range may hold "*" (RFC 4647 §2.2).
        /// </summary>
        public static TheoryData<string> WellFormedFieldRegistries => new TheoryData<string>
        {
            "File-Date: 2025-01-01\r\n%%\r\nType: variant\r\nSubtag: biske\r\nDescription: The San Giorgio dialect of Resian\r\nAdded: 2007-07-05\r\nPrefix: SL-Rozaj\r\n",
            "File-Date: 2025-01-01\r\n%%\r\nType: variant\r\nSubtag: rozaj\r\nDescription: Resian\r\nAdded: 2005-10-16\r\nPrefix: i-klingon\r\n",
            "File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\nSuppress-Script: LATN\r\n",
            "File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nMacrolanguage: AR\r\n",
            "File-Date: 2025-01-01\r\n%%\r\nType: region\r\nSubtag: BU\r\nDescription: Burma\r\nAdded: 2005-10-16\r\nDeprecated: 1989-12-05\r\nPreferred-Value: mm\r\n",
            "File-Date: 2025-01-01\r\n%%\r\nType: region\r\nSubtag: 062\r\nDescription: South-Central Asia\r\nAdded: 2005-10-16\r\nDeprecated: 2010-08-16\r\nPreferred-Value: 034\r\n",
            "File-Date: 2025-01-01\r\n%%\r\nType: variant\r\nSubtag: heploc\r\nDescription: Hepburn romanization, Library of Congress method\r\nAdded: 2009-10-01\r\nDeprecated: 2010-02-07\r\nPreferred-Value: ALALC97\r\n",
            "File-Date: 2025-01-01\r\n%%\r\nType: script\r\nSubtag: Qaai\r\nDescription: Inherited\r\nAdded: 2005-10-16\r\nDeprecated: 2007-07-05\r\nPreferred-Value: zinh\r\n",
            "File-Date: 2025-01-01\r\n%%\r\nType: redundant\r\nTag: zh-cmn-Hans\r\nDescription: Mandarin Chinese (Simplified)\r\nAdded: 2005-07-15\r\nDeprecated: 2009-07-29\r\nPreferred-Value: cmn-*-1994\r\n",
            "File-Date: 2025-01-01\r\n%%\r\nType: extlang\r\nSubtag: aao\r\nDescription: Algerian Saharan Arabic\r\nAdded: 2009-07-29\r\nPreferred-Value: aao\r\nPrefix: AR\r\n",
        };

        [Theory]
        [MemberData(nameof(WellFormedFieldRegistries))]
        public void CreateFromContent_WhenFieldHasTheFormOfItsField_ReturnsRegistry(string content)
        {
            // act
            var testResult = SubtagRegistry.CreateFromContent(content);

            // assert
            testResult.Count.Should().Be(MinimalRegistryRecordCount, "the record's fields have the forms RFC 5646 §3.1.2 gives them, in any case");
        }

        #endregion

        #region Test: line breaks

        /// <summary>
        /// The description of the record of <see cref="MinimalRecordSubtag"/> in each of the registries of line breaks.
        /// </summary>
        private const string MinimalRecordDescription = "Afar";

        /// <summary>
        /// The number of records in each of the registries of line breaks: the File-Date record and the record of <see cref="MinimalRecordSubtag"/>.
        /// </summary>
        private const int MinimalRegistryRecordCount = 2;

        /// <summary>
        /// The smallest registry, with CRLF at the end of every line, the last one included, as RFC 5646 §3.1.1 requires.
        /// </summary>
        private const string CrLfRegistry = "File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16\r\n";

        /// <summary>
        /// <see cref="CrLfRegistry"/> with a line feed alone at the end of every line, as IANA publishes the registry.
        /// </summary>
        private const string LineFeedRegistry = "File-Date: 2025-01-01\n%%\nType: language\nSubtag: aa\nDescription: Afar\nAdded: 2005-10-16\n";

        /// <summary>
        /// <see cref="CrLfRegistry"/> with a line feed alone at the end of every other line.
        /// </summary>
        private const string MixedLineBreakRegistry = "File-Date: 2025-01-01\r\n%%\nType: language\r\nSubtag: aa\nDescription: Afar\r\nAdded: 2005-10-16\n";

        /// <summary>
        /// <see cref="CrLfRegistry"/> with no line break after its last line.
        /// </summary>
        private const string CrLfRegistryWithoutFinalLineBreak = "File-Date: 2025-01-01\r\n%%\r\nType: language\r\nSubtag: aa\r\nDescription: Afar\r\nAdded: 2005-10-16";

        /// <summary>
        /// <see cref="LineFeedRegistry"/> with no line break after its last line.
        /// </summary>
        private const string LineFeedRegistryWithoutFinalLineBreak = "File-Date: 2025-01-01\n%%\nType: language\nSubtag: aa\nDescription: Afar\nAdded: 2005-10-16";

        /// <summary>
        /// <see cref="LineFeedRegistry"/> with a carriage return alone, not a line feed, at the end of its first line, which is then one line with the
        /// <c>%%</c> after it.
        /// </summary>
        private const string CarriageReturnRegistry = "File-Date: 2025-01-01\r%%\nType: language\nSubtag: aa\nDescription: Afar\nAdded: 2005-10-16\n";

        /// <summary>
        /// <see cref="CrLfRegistry"/> with a carriage return alone at the end of every line, which is then one line with no line break after it.
        /// </summary>
        private const string BareCarriageReturnRegistry = "File-Date: 2025-01-01\r%%\rType: language\rSubtag: aa\rDescription: Afar\rAdded: 2005-10-16\r";

        private const string LineFeedMessage = "Line 1 ends with a line feed alone, not the CRLF that ends every line (ref. RFC 5646 §3.1.1).";
        private const string SecondLineFeedMessage = "Line 2 ends with a line feed alone, not the CRLF that ends every line (ref. RFC 5646 §3.1.1).";
        private const string MissingFinalLineBreakMessage = "Line 6, the last, has no CRLF after it, which ends every line, the last one included (ref. RFC 5646 §3.1.1).";
        private const string CarriageReturnMessage =
            "Line 1 holds a carriage return that no line feed follows, which is not a line break but a control character, which a field may not hold (ref. RFC 5646 §3.1.1).";

        private const string OptionsParameterName = "options";

        /// <summary>
        /// The path of a file that a test never opens, as the method it is passed to throws before it opens the file.
        /// </summary>
        private const string UnopenedRegistryFilePath = "registry.txt";

        /// <summary>
        /// A value that <see cref="SubtagRegistryLoadOptions"/> doesn't define.
        /// </summary>
        private const SubtagRegistryLoadOptions UndefinedLoadOptions = (SubtagRegistryLoadOptions)(1 << 2);

        /// <summary>
        /// Each way to load a registry with the default options, from its content or from a stream of its UTF-8 bytes, and through a reader that returns
        /// one character per read, so that the carriage return and the line feed of each CRLF come from different reads.
        /// </summary>
        private static readonly Dictionary<string, Func<string, Task<SubtagRegistry>>> DefaultLoaders = new Dictionary<string, Func<string, Task<SubtagRegistry>>>
        {
            { nameof(SubtagRegistry.CreateFromContent), content => Task.FromResult(SubtagRegistry.CreateFromContent(content)) },
            { nameof(SubtagRegistry.CreateFromContentAsync), SubtagRegistry.CreateFromContentAsync },
            { nameof(SubtagRegistry.CreateFromContentAsync) + " with a token", content => SubtagRegistry.CreateFromContentAsync(content, CancellationToken.None) },
            { nameof(SubtagRegistry.CreateFromStream), content => Task.FromResult(LoadFromStream(content, stream => SubtagRegistry.CreateFromStream(stream))) },
            { nameof(SubtagRegistry.CreateFromStreamAsync), content => LoadFromStreamAsync(content, SubtagRegistry.CreateFromStreamAsync) },
            { nameof(SubtagRegistry.CreateFromStreamAsync) + " with a token", content => LoadFromStreamAsync(content, stream => SubtagRegistry.CreateFromStreamAsync(stream, CancellationToken.None)) },
            { nameof(SubtagRegistry.Read) + " one character at a time", content => Task.FromResult(SubtagRegistry.Read(new OneCharacterTextReader(content), SubtagRegistryLoadOptions.None)) },
            {
                nameof(SubtagRegistry.ReadAsync) + " one character at a time",
                content => SubtagRegistry.ReadAsync(new OneCharacterTextReader(content), SubtagRegistryLoadOptions.None, CancellationToken.None)
            }
        };

        /// <summary>
        /// Each way to load a registry with options, from its content or from a stream of its UTF-8 bytes, as <see cref="DefaultLoaders"/> does.
        /// </summary>
        private static readonly Dictionary<string, Func<string, SubtagRegistryLoadOptions, Task<SubtagRegistry>>> OptionLoaders =
            new Dictionary<string, Func<string, SubtagRegistryLoadOptions, Task<SubtagRegistry>>>
            {
                { nameof(SubtagRegistry.CreateFromContent), (content, options) => Task.FromResult(SubtagRegistry.CreateFromContent(content, options)) },
                { nameof(SubtagRegistry.CreateFromContentAsync), (content, options) => SubtagRegistry.CreateFromContentAsync(content, options, CancellationToken.None) },
                { nameof(SubtagRegistry.CreateFromStream), (content, options) => Task.FromResult(LoadFromStream(content, stream => SubtagRegistry.CreateFromStream(stream, options))) },
                { nameof(SubtagRegistry.CreateFromStreamAsync), (content, options) => LoadFromStreamAsync(content, stream => SubtagRegistry.CreateFromStreamAsync(stream, options, CancellationToken.None)) },
                { nameof(SubtagRegistry.Read) + " one character at a time", (content, options) => Task.FromResult(SubtagRegistry.Read(new OneCharacterTextReader(content), options)) },
                {
                    nameof(SubtagRegistry.ReadAsync) + " one character at a time",
                    (content, options) => SubtagRegistry.ReadAsync(new OneCharacterTextReader(content), options, CancellationToken.None)
                }
            };

        /// <summary>
        /// Registries whose line breaks the default options accept: CRLF at the end of every line.
        /// </summary>
        public static TheoryData<string, Func<string, Task<SubtagRegistry>>> CrLfRegistryCases
        {
            get
            {
                var data = new TheoryData<string, Func<string, Task<SubtagRegistry>>>();
                foreach (var loader in DefaultLoaders)
                {
                    data.Add(loader.Key, loader.Value);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(CrLfRegistryCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFrom_WhenEveryLineEndsWithCrLf_ReturnsRegistry(string loaderName, Func<string, Task<SubtagRegistry>> load)
        {
            // act
            var testResult = await load(CrLfRegistry);

            // assert
            testResult.Count.Should().Be(MinimalRegistryRecordCount, "{0} reads the File-Date record and the record after it", loaderName);
            var record = testResult.Languages.Should().ContainSingle().Subject;
            record.Subtag.Should().Be(MinimalRecordSubtag);
            record.Description.Should().Equal(new[] { MinimalRecordDescription }, "the CRLF is not part of the line it ends");
            record.Added.Should().Be(RegistryCreationDate);
        }

        /// <summary>
        /// Registries whose line breaks the default options don't accept, with each way to load a registry with them, and the message of the
        /// <see cref="FormatException"/> each fails with.
        /// </summary>
        public static TheoryData<string, Func<string, Task<SubtagRegistry>>, string, string> NonCrLfRegistryCases
        {
            get
            {
                var registries = new[]
                {
                    (LineFeedRegistry, LineFeedMessage),
                    (CrLfRegistryWithoutFinalLineBreak, MissingFinalLineBreakMessage),
                    (CarriageReturnRegistry, CarriageReturnMessage),
                    (MixedLineBreakRegistry, SecondLineFeedMessage),
                    (BareCarriageReturnRegistry, CarriageReturnMessage)
                };
                var data = new TheoryData<string, Func<string, Task<SubtagRegistry>>, string, string>();
                foreach (var loader in DefaultLoaders)
                foreach (var registry in registries)
                {
                    data.Add(loader.Key, loader.Value, registry.Item1, registry.Item2);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(NonCrLfRegistryCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFrom_WhenLineDoesNotEndWithCrLf_ThrowsFormatExceptionNamingTheLine(string loaderName, Func<string, Task<SubtagRegistry>> load, string content, string expectedMessage)
        {
            // act
            Func<Task> testAction = () => load(content);

            // assert
            (await testAction.Should().ThrowAsync<FormatException>("{0} ends every line with CRLF by default (ref. RFC 5646 §3.1.1)", loaderName))
                .Which.Message.Should().Be(expectedMessage);
        }

        /// <summary>
        /// Registries with the options that accept their line breaks, with each way to load a registry with options.
        /// </summary>
        public static TheoryData<string, Func<string, SubtagRegistryLoadOptions, Task<SubtagRegistry>>, SubtagRegistryLoadOptions, string> AcceptedLineBreakCases
        {
            get
            {
                var registries = new[]
                {
                    (SubtagRegistryLoadOptions.AcceptBareLineFeed, LineFeedRegistry),
                    (SubtagRegistryLoadOptions.AcceptBareLineFeed, MixedLineBreakRegistry),
                    (SubtagRegistryLoadOptions.AcceptBareLineFeed, CrLfRegistry),
                    (SubtagRegistryLoadOptions.AcceptMissingFinalLineBreak, CrLfRegistryWithoutFinalLineBreak),
                    (SubtagRegistryLoadOptions.AcceptMissingFinalLineBreak, CrLfRegistry),
                    (SubtagRegistryLoadOptions.AcceptBareLineFeed | SubtagRegistryLoadOptions.AcceptMissingFinalLineBreak, LineFeedRegistryWithoutFinalLineBreak),
                    (SubtagRegistryLoadOptions.None, CrLfRegistry)
                };
                var data = new TheoryData<string, Func<string, SubtagRegistryLoadOptions, Task<SubtagRegistry>>, SubtagRegistryLoadOptions, string>();
                foreach (var loader in OptionLoaders)
                foreach (var registry in registries)
                {
                    data.Add(loader.Key, loader.Value, registry.Item1, registry.Item2);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(AcceptedLineBreakCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFrom_WhenOptionAcceptsTheLineBreaks_ReturnsRegistry(
            string loaderName, Func<string, SubtagRegistryLoadOptions, Task<SubtagRegistry>> load, SubtagRegistryLoadOptions options, string content)
        {
            // act
            var testResult = await load(content, options);

            // assert
            testResult.Count.Should().Be(MinimalRegistryRecordCount, "{0} with {1} reads the File-Date record and the record after it", loaderName, options);
            var record = testResult.Languages.Should().ContainSingle().Subject;
            record.Subtag.Should().Be(MinimalRecordSubtag);
            record.Description.Should().Equal(new[] { MinimalRecordDescription }, "the line break is not part of the line it ends");
            record.Added.Should().Be(RegistryCreationDate);
        }

        /// <summary>
        /// Registries with options that don't accept their line breaks, each turning on only what it names, with each way to load a registry with options,
        /// and the message of the <see cref="FormatException"/> each fails with.
        /// </summary>
        public static TheoryData<string, Func<string, SubtagRegistryLoadOptions, Task<SubtagRegistry>>, SubtagRegistryLoadOptions, string, string> RejectedLineBreakCases
        {
            get
            {
                var registries = new[]
                {
                    (SubtagRegistryLoadOptions.AcceptMissingFinalLineBreak, LineFeedRegistry, LineFeedMessage),
                    (SubtagRegistryLoadOptions.AcceptBareLineFeed, CrLfRegistryWithoutFinalLineBreak, MissingFinalLineBreakMessage),
                    (SubtagRegistryLoadOptions.AcceptBareLineFeed, LineFeedRegistryWithoutFinalLineBreak, MissingFinalLineBreakMessage),
                    (SubtagRegistryLoadOptions.AcceptBareLineFeed | SubtagRegistryLoadOptions.AcceptMissingFinalLineBreak, CarriageReturnRegistry, CarriageReturnMessage),
                    (SubtagRegistryLoadOptions.AcceptBareLineFeed | SubtagRegistryLoadOptions.AcceptMissingFinalLineBreak, BareCarriageReturnRegistry, CarriageReturnMessage)
                };
                var data = new TheoryData<string, Func<string, SubtagRegistryLoadOptions, Task<SubtagRegistry>>, SubtagRegistryLoadOptions, string, string>();
                foreach (var loader in OptionLoaders)
                foreach (var registry in registries)
                {
                    data.Add(loader.Key, loader.Value, registry.Item1, registry.Item2, registry.Item3);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(RejectedLineBreakCases), DisableDiscoveryEnumeration = true)]
        public async Task CreateFrom_WhenOptionDoesNotAcceptTheLineBreaks_ThrowsFormatExceptionNamingTheLine(
            string loaderName, Func<string, SubtagRegistryLoadOptions, Task<SubtagRegistry>> load, SubtagRegistryLoadOptions options, string content, string expectedMessage)
        {
            // act
            Func<Task> testAction = () => load(content, options);

            // assert
            (await testAction.Should().ThrowAsync<FormatException>("{0} with {1} accepts only the line breaks it names", loaderName, options))
                .Which.Message.Should().Be(expectedMessage);
        }

        /// <summary>
        /// Each method that takes options, called with a value that <see cref="SubtagRegistryLoadOptions"/> doesn't define.
        /// </summary>
        public static TheoryData<string, Func<Task>> UndefinedOptionsCalls => new TheoryData<string, Func<Task>>
        {
            { nameof(SubtagRegistry.CreateFromContent), () => Task.FromResult(SubtagRegistry.CreateFromContent(CrLfRegistry, UndefinedLoadOptions)) },
            { nameof(SubtagRegistry.CreateFromContentAsync), () => SubtagRegistry.CreateFromContentAsync(CrLfRegistry, UndefinedLoadOptions, CancellationToken.None) },
            { nameof(SubtagRegistry.CreateFromStream), () => Task.FromResult(LoadFromStream(CrLfRegistry, stream => SubtagRegistry.CreateFromStream(stream, UndefinedLoadOptions))) },
            { nameof(SubtagRegistry.CreateFromStreamAsync), () => LoadFromStreamAsync(CrLfRegistry, stream => SubtagRegistry.CreateFromStreamAsync(stream, UndefinedLoadOptions, CancellationToken.None)) },
            // the options are checked before the file is opened, so the path is never used
            { nameof(SubtagRegistry.CreateFromFile), () => Task.FromResult(SubtagRegistry.CreateFromFile(UnopenedRegistryFilePath, UndefinedLoadOptions)) },
            { nameof(SubtagRegistry.CreateFromFileAsync), () => SubtagRegistry.CreateFromFileAsync(UnopenedRegistryFilePath, UndefinedLoadOptions, CancellationToken.None) }
        };

        [Theory]
        [MemberData(nameof(UndefinedOptionsCalls), DisableDiscoveryEnumeration = true)]
        public async Task CreateFrom_WhenOptionsAreUndefined_ThrowsArgumentOutOfRangeException(string methodName, Func<Task> call)
        {
            // act
            var testAction = call;

            // assert
            (await testAction.Should().ThrowAsync<ArgumentOutOfRangeException>("{0} takes only the options that SubtagRegistryLoadOptions defines", methodName))
                .WithParameterName(OptionsParameterName);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Loads a registry from a stream of the UTF-8 bytes of its content, and disposes the stream.
        /// </summary>
        private static SubtagRegistry LoadFromStream(string content, Func<Stream, SubtagRegistry> load)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                return load(stream);
            }
        }

        /// <inheritdoc cref="LoadFromStream"/>
        private static async Task<SubtagRegistry> LoadFromStreamAsync(string content, Func<Stream, Task<SubtagRegistry>> load)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                return await load(stream);
            }
        }

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
