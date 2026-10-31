using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    public class SubtagRegistryTests : IClassFixture<Bcp47LanguageTagFixture>, IDisposable
    {
        private readonly Bcp47LanguageTagFixture _fixture;
        private string _tempFilePath;

        public SubtagRegistryTests(Bcp47LanguageTagFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void CreateFromContent_WhenRegistryContentIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => SubtagRegistry.SubtagRegistry.CreateFromContent(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the Subtag Registry content must not be null")
                .WithParameterName("subtagRegistryContent");
        }

        [Fact]
        public async Task CreateFromContent_WhenRegistryContentIsValid_ReturnsDeserializedRegistry()
        {
            // arrange
            var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
            var expectedRecordCount = await this.GetRegistryRecordCountAsync(subtagRegistryContent);

            // act
            // ReSharper disable once MethodHasAsyncOverload
            var testResult = SubtagRegistry.SubtagRegistry.CreateFromContent(subtagRegistryContent);

            // assert
            testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the Subtag Registry", expectedRecordCount);
        }

        [Fact]
        public void CreateFromContentAsync_WhenRegistryContentIsNull_ThrowsArgumentNullException()
        {
            // act
            Func<Task> testAction = async () => await SubtagRegistry.SubtagRegistry.CreateFromContentAsync(null);

            // assert
            testAction.Should()
                .ThrowAsync<ArgumentNullException>("the Subtag Registry content must not be null")
                .WithParameterName("subtagRegistryContent");
        }

        [Fact]
        public async Task CreateFromContentAsync_WhenRegistryContentIsValid_ReturnsDeserializedRegistry()
        {
            // arrange
            var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
            var expectedRecordCount = await this.GetRegistryRecordCountAsync(subtagRegistryContent);

            // act
            var testResult = await SubtagRegistry.SubtagRegistry.CreateFromContentAsync(subtagRegistryContent);

            // assert
            testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the Subtag Registry", expectedRecordCount);
        }

        [Fact]
        public void CreateFromFile_WhenRegistryFilePathIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => SubtagRegistry.SubtagRegistry.CreateFromFile(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the Subtag Registry file path must not be null")
                .WithParameterName("subtagRegistryFilePath");
        }

        [Fact]
        public async Task CreateFromFile_WhenRegistryFilePathIsValid_ReturnsDeserializedRegistry()
        {
            // arrange
            var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
            _tempFilePath = await this.CreateRegistryFileAsync(subtagRegistryContent);
            var expectedRecordCount = await GetRegistryRecordCountAsync(subtagRegistryContent);

            // act
            // ReSharper disable once MethodHasAsyncOverload
            var testResult = SubtagRegistry.SubtagRegistry.CreateFromFile(_tempFilePath);

            // assert
            testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the Subtag Registry", expectedRecordCount);
        }

        [Fact]
        public void CreateFromFileAsync_WhenRegistryFilePathIsNull_ThrowsArgumentNullException()
        {
            // act
            Func<Task> testAction = async () => await SubtagRegistry.SubtagRegistry.CreateFromFileAsync(null);

            // assert
            testAction.Should()
                .ThrowAsync<ArgumentNullException>("the Subtag Registry file path must not be null")
                .WithParameterName("subtagRegistryFilePath");
        }

        [Fact]
        public async Task CreateFromFileAsync_WhenRegistryFilePathIsValid_ReturnsDeserializedRegistry()
        {
            // arrange
            var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
            _tempFilePath = await CreateRegistryFileAsync(subtagRegistryContent);
            var expectedRecordCount = await GetRegistryRecordCountAsync(subtagRegistryContent);

            // act
            var testResult = await SubtagRegistry.SubtagRegistry.CreateFromFileAsync(_tempFilePath);

            // assert
            testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the Subtag Registry", expectedRecordCount);
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
        public async Task CreateFromStream_WhenStreamIsSupplied_ReturnsDeserializedRegistry()
        {
            // arrange
            var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
            var subtagRegistryBytes = Encoding.Default.GetBytes(subtagRegistryContent);
            using (var subtagRegistryStream = new MemoryStream(subtagRegistryBytes))
            {
                var expectedRecordCount = await this.GetRegistryRecordCountAsync(subtagRegistryContent);

                // act
                // ReSharper disable once MethodHasAsyncOverload
                var testResult = SubtagRegistry.SubtagRegistry.CreateFromStream(subtagRegistryStream);

                // assert
                testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the subtag registry", expectedRecordCount);
            }
        }

        [Fact]
        public void CreateFromStreamAsync_WhenStreamIsNull_ThrowsArgumentNullException()
        {
            // act
            Func<Task> testAction = async () => _ = await SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(null);

            // assert
            testAction.Should().ThrowAsync<ArgumentNullException>("the stream must not be null");
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenStreamIsSupplied_ReturnsDeserializedRegistry()
        {
            // arrange
            var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
            var subtagRegistryBytes = Encoding.Default.GetBytes(subtagRegistryContent);
            using (var subtagRegistryStream = new MemoryStream(subtagRegistryBytes))
            {
                var expectedRecordCount = await this.GetRegistryRecordCountAsync(subtagRegistryContent);

                // act
                var testResult = await SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream);

                // assert
                testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the subtag registry", expectedRecordCount);
            }
        }

        [Fact]
        public async Task CreateFromStream_WhenStreamIsPositioned_ReadsFromCurrentPositionAndLeavesStreamOpen()
        {
            // arrange
            var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
            using (var subtagRegistryStream = CreatePositionedRegistryStream(subtagRegistryContent, out var startPosition))
            {
                var expectedRecordCount = await this.GetRegistryRecordCountAsync(subtagRegistryContent);

                // act
                // ReSharper disable once MethodHasAsyncOverload
                var testResult = SubtagRegistry.SubtagRegistry.CreateFromStream(subtagRegistryStream);

                // assert
                testResult.Count.Should().Be(expectedRecordCount, "only the content after the stream's current position is read");
                subtagRegistryStream.CanRead.Should().BeTrue("the caller owns the stream");
                subtagRegistryStream.Position.Should().BeGreaterThan(startPosition);
                subtagRegistryStream.Position = 0;
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenStreamIsPositioned_ReadsFromCurrentPositionAndLeavesStreamOpen()
        {
            // arrange
            var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
            using (var subtagRegistryStream = CreatePositionedRegistryStream(subtagRegistryContent, out var startPosition))
            {
                var expectedRecordCount = await this.GetRegistryRecordCountAsync(subtagRegistryContent);

                // act
                var testResult = await SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream);

                // assert
                testResult.Count.Should().Be(expectedRecordCount, "only the content after the stream's current position is read");
                subtagRegistryStream.CanRead.Should().BeTrue("the caller owns the stream");
                subtagRegistryStream.Position.Should().BeGreaterThan(startPosition);
                subtagRegistryStream.Position = 0;
            }
        }

        [Fact]
        public async Task CreateFromContentAsync_WhenTokenIsCancelled_ThrowsOperationCanceledException()
        {
            // arrange
            var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                // act
                Func<Task> testAction = () => SubtagRegistry.SubtagRegistry.CreateFromContentAsync(subtagRegistryContent, cancellationTokenSource.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>("the token was cancelled");
            }
        }

        [Fact]
        public async Task CreateFromFileAsync_WhenTokenIsCancelled_ThrowsOperationCanceledException()
        {
            // arrange
            _tempFilePath = await CreateRegistryFileAsync(_fixture.IanaSubtagRegistry.ToString());
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                // act
                Func<Task> testAction = () => SubtagRegistry.SubtagRegistry.CreateFromFileAsync(_tempFilePath, cancellationTokenSource.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>("the token was cancelled");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenTokenIsCancelledMidRead_ThrowsOperationCanceledException()
        {
            // arrange
            var subtagRegistryBytes = Encoding.UTF8.GetBytes(_fixture.IanaSubtagRegistry.ToString());
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var subtagRegistryStream = new CancelOnFirstReadStream(subtagRegistryBytes, cancellationTokenSource))
            {
                // act
                Func<Task> testAction = () => SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream, cancellationTokenSource.Token);

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>("the token was cancelled after the first read from the stream");
                subtagRegistryStream.Position.Should().BeLessThan(subtagRegistryBytes.Length, "reading stops once the token is cancelled");
            }
        }

        [Fact]
        public async Task CreateFromStreamAsync_WhenTokenIsNotCancelled_ReturnsDeserializedRegistry()
        {
            // arrange
            var subtagRegistryContent = _fixture.IanaSubtagRegistry.ToString();
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var subtagRegistryStream = new MemoryStream(Encoding.UTF8.GetBytes(subtagRegistryContent)))
            {
                var expectedRecordCount = await this.GetRegistryRecordCountAsync(subtagRegistryContent);

                // act
                var testResult = await SubtagRegistry.SubtagRegistry.CreateFromStreamAsync(subtagRegistryStream, cancellationTokenSource.Token);

                // assert
                testResult.Count.Should().Be(expectedRecordCount, "there are {0} records in the subtag registry", expectedRecordCount);
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

        private static MemoryStream CreatePositionedRegistryStream(string registryContent, out long startPosition)
        {
            // a complete record ahead of the registry content, so reading from the start of the stream would add one record
            var precedingContent = "Type: language\nSubtag: qaa\nDescription: Preceding record\nAdded: 2000-01-01\n%%\n";
            var precedingBytes = Encoding.UTF8.GetBytes(precedingContent);
            var registryBytes = Encoding.UTF8.GetBytes(registryContent);

            var stream = new MemoryStream();
            stream.Write(precedingBytes, 0, precedingBytes.Length);
            stream.Write(registryBytes, 0, registryBytes.Length);
            stream.Position = startPosition = precedingBytes.Length;
            return stream;
        }

        [Theory]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Region, "BU", 2005, 10, 16)]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Language, "en", 2005, 10, 16)]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.ExtendedLanguage, "ase", 2009, 7, 29)]
        public void SubtagRecordAdded_WhenRecordHasAddedField_ReturnsAddedDate(SubtagRegistry.SubtagRegistryRecordType type, string subtag, int year, int month, int day)
        {
            // arrange
            var record = this.GetSubtagRecord(type, subtag);

            // act
            var testResult = record.Added;

            // assert
            testResult.Should().Be(new DateTime(year, month, day), "the {0} subtag {1} was added on that date", type, subtag);
        }

        [Fact]
        public void SubtagRecordDeprecated_WhenRecordHasDeprecatedField_ReturnsDeprecatedDate()
        {
            // arrange
            var record = this.GetSubtagRecord(SubtagRegistry.SubtagRegistryRecordType.Region, "BU");

            // act
            var testResult = record.Deprecated;

            // assert
            testResult.Should().Be(new DateTime(1989, 12, 5), "the region subtag BU was deprecated on that date");
        }

        [Fact]
        public void SubtagRecordDeprecated_WhenRecordHasNoDeprecatedField_ReturnsNull()
        {
            // arrange
            var record = this.GetSubtagRecord(SubtagRegistry.SubtagRegistryRecordType.Language, "en");

            // act
            var testResult = record.Deprecated;

            // assert
            testResult.Should().BeNull("the language subtag en is not deprecated");
        }

        [Theory]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Region, "BU", "MM")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.ExtendedLanguage, "ase", "ase")]
        public void SubtagRecordPreferredValue_WhenRecordHasPreferredValueField_ReturnsPreferredValue(SubtagRegistry.SubtagRegistryRecordType type, string subtag, string expectedPreferredValue)
        {
            // arrange
            var record = this.GetSubtagRecord(type, subtag);

            // act
            var testResult = record.PreferredValue;

            // assert
            testResult.Should().Be(expectedPreferredValue, "the {0} subtag {1} has that preferred value", type, subtag);
        }

        [Fact]
        public void SubtagRecordPreferredValue_WhenRecordHasNoPreferredValueField_ReturnsNull()
        {
            // arrange
            var record = this.GetSubtagRecord(SubtagRegistry.SubtagRegistryRecordType.Language, "en");

            // act
            var testResult = record.PreferredValue;

            // assert
            testResult.Should().BeNull("the language subtag en has no preferred value");
        }

        [Fact]
        public void SubtagRecordSuppressScript_WhenRecordHasSuppressScriptField_ReturnsSuppressScript()
        {
            // arrange
            var record = this.GetSubtagRecord(SubtagRegistry.SubtagRegistryRecordType.Language, "en");

            // act
            var testResult = record.SuppressScript;

            // assert
            testResult.Should().Be("Latn", "the language subtag en has a Suppress-Script of Latn");
        }

        [Fact]
        public void SubtagRecordSuppressScript_WhenRecordHasNoSuppressScriptField_ReturnsNull()
        {
            // arrange
            var record = this.GetSubtagRecord(SubtagRegistry.SubtagRegistryRecordType.Language, "jbo");

            // act
            var testResult = record.SuppressScript;

            // assert
            testResult.Should().BeNull("the language subtag jbo has no Suppress-Script");
        }

        [Fact]
        public void TagRecord_WhenGrandfatheredTagHasDateAndPreferredValueFields_ReturnsFieldValues()
        {
            // arrange
            var record = this.GetTagRecord(SubtagRegistry.SubtagRegistryRecordType.Grandfathered, "zh-guoyu");

            // act
            var added = record.Added;
            var deprecated = record.Deprecated;
            var preferredValue = record.PreferredValue;
            var suppressScript = record.SuppressScript;

            // assert
            added.Should().Be(new DateTime(1999, 12, 18), "the grandfathered tag zh-guoyu was added on that date");
            deprecated.Should().Be(new DateTime(2005, 7, 15), "the grandfathered tag zh-guoyu was deprecated on that date");
            preferredValue.Should().Be("cmn", "the grandfathered tag zh-guoyu has a preferred value of cmn");
            suppressScript.Should().BeNull("the grandfathered tag zh-guoyu has no Suppress-Script");
        }

        [Fact]
        public void FileDateRecordFileDate_WhenRegistryHasFileDate_ReturnsFileDate()
        {
            // arrange
            var registry = SubtagRegistry.SubtagRegistry.CreateFromContent(_fixture.IanaSubtagRegistry.ToString());
            var record = registry.OfType<SubtagRegistry.SubtagRegistryFileDateRecord>().Single();

            // act
            var testResult = record.FileDate;

            // assert
            testResult.Should().Be(new DateTime(2025, 5, 15), "the registry fixture has that File-Date");
        }

        [Fact]
        public void TagRecordAdded_ForEveryRecordInRegistry_ReturnsDateOfAddedField()
        {
            // arrange
            var records = SubtagRegistry.SubtagRegistry.CreateFromContent(_fixture.IanaSubtagRegistry.ToString())
                .OfType<SubtagRegistry.SubtagRegistryTagRecordBase>()
                .ToList();

            // act
            var mismatchedRecords = records
                .Where(record => record.Added.ToString("yyyy-MM-dd") != GetFieldValue(record, "Added"))
                .ToList();

            // assert
            records.Should().NotBeEmpty();
            mismatchedRecords.Should().BeEmpty("every record's Added property is the date in its Added field");
        }

        [Fact]
        public void TagRecordProperties_WhenReadFromSeveralThreads_ReturnSameValues()
        {
            // arrange
            var records = SubtagRegistry.SubtagRegistry.CreateFromContent(_fixture.IanaSubtagRegistry.ToString())
                .OfType<SubtagRegistry.SubtagRegistryTagRecordBase>()
                .ToList();

            // act
            var readings = Enumerable.Range(0, 8)
                .AsParallel()
                .Select(_ => records.Select(record => $"{record.Type}|{record.Added:yyyy-MM-dd}|{record.Deprecated:yyyy-MM-dd}|{record.PreferredValue}|{string.Join(",", record.Description)}").ToList())
                .ToList();

            // assert
            readings.Should().AllSatisfy(reading => reading.Should().Equal(readings[0]));
        }

        [Fact]
        public void TagRecordComments_WhenRecordHasSeveralCommentsFields_ReturnsEveryComment()
        {
            // arrange
            var record = this.GetSubtagRecord(SubtagRegistry.SubtagRegistryRecordType.Variant, "vaidika");

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
            var record = this.GetSubtagRecord(SubtagRegistry.SubtagRegistryRecordType.Variant, "1994");

            // act
            var testResult = record.Prefix;

            // assert
            testResult.Should().Equal(new[] { "sl-rozaj", "sl-rozaj-biske", "sl-rozaj-njiva", "sl-rozaj-osojs", "sl-rozaj-solba" });
        }

        [Fact]
        public void TagRecordLists_WhenCastToMutableList_CannotBeChanged()
        {
            // arrange
            var record = this.GetSubtagRecord(SubtagRegistry.SubtagRegistryRecordType.Language, "en");

            // act
            Action changeDescription = () => ((IList<string>)record.Description)[0] = "Changed";
            Action changePrefix = () => ((IList<string>)record.Prefix).Add("Changed");
            Action changeFields = () => ((IList<KeyValuePair<string, string>>)record.Fields).Clear();

            // assert
            changeDescription.Should().Throw<NotSupportedException>("records are immutable");
            changePrefix.Should().Throw<NotSupportedException>("records are immutable");
            changeFields.Should().Throw<NotSupportedException>("records are immutable");
            record.Description.Should().Equal(new[] { "English" });
            record.Description.Should().BeSameAs(record.Description, "the read-only view is created once");
        }

        [Fact]
        public void TagRecordType_ForEveryRecordInRegistry_IsTheTypeInItsTypeField()
        {
            // arrange
            var records = _fixture.IanaSubtagRegistry.OfType<SubtagRegistry.SubtagRegistryTagRecordBase>().ToList();
            var typeNames = new Dictionary<SubtagRegistry.SubtagRegistryRecordType, string>
            {
                [SubtagRegistry.SubtagRegistryRecordType.Language] = "language",
                [SubtagRegistry.SubtagRegistryRecordType.ExtendedLanguage] = "extlang",
                [SubtagRegistry.SubtagRegistryRecordType.Script] = "script",
                [SubtagRegistry.SubtagRegistryRecordType.Region] = "region",
                [SubtagRegistry.SubtagRegistryRecordType.Variant] = "variant",
                [SubtagRegistry.SubtagRegistryRecordType.Grandfathered] = "grandfathered",
                [SubtagRegistry.SubtagRegistryRecordType.Redundant] = "redundant",
            };

            // act
            var mismatchedRecords = records.Where(record => !typeNames.TryGetValue(record.Type, out var typeName) || typeName != GetFieldValue(record, "Type")).ToList();

            // assert
            records.Should().NotBeEmpty();
            mismatchedRecords.Should().BeEmpty("every record in the fixture has a Type that RFC 5646 defines");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CreateFromContent_WhenTypeIsUnknown_ReadsRecordWithUnknownType(bool readAsync)
        {
            // arrange
            var content = "Type: future\nSubtag: qaa\nDescription: Private use\nAdded: 2005-10-16";

            // act
            var registry = await CreateFromContentAsync(content, readAsync);

            // assert
            var record = registry.OfType<SubtagRegistry.SubtagRegistrySubtagRecord>().Single();
            record.Type.Should().Be(SubtagRegistry.SubtagRegistryRecordType.Unknown);
            GetFieldValue(record, "Type").Should().Be("future", "the field is kept as it was read");
            registry.Languages.Should().BeEmpty("a record of an unknown type is not a language record");
        }

        [Theory]
        [InlineData("2005-13-01")]
        [InlineData("2005-02-30")]
        [InlineData("16 Oct 2005")]
        [InlineData("2005-1-16")]
        public void CreateFromContent_WhenDateIsMalformed_ReadsDateAsAbsentAndKeepsField(string date)
        {
            // arrange
            var content = $"Type: language\nSubtag: qaa\nDescription: Private use\nAdded: {date}\nDeprecated: {date}";

            // act
            var registry = SubtagRegistry.SubtagRegistry.CreateFromContent(content);

            // assert
            var record = registry.Languages.Single();
            record.Added.Should().Be(default(DateTime), "{0} is not a date in the yyyy-MM-dd form", date);
            record.Deprecated.Should().BeNull("{0} is not a date in the yyyy-MM-dd form", date);
            GetFieldValue(record, "Added").Should().Be(date);
        }

        [Fact]
        public void TypedRecordLists_ForFixture_HoldEveryRecordOfEachTypeInRegistryOrder()
        {
            // arrange
            var registry = _fixture.IanaSubtagRegistry;
            var subtagRecords = registry.OfType<SubtagRegistry.SubtagRegistrySubtagRecord>().ToList();
            var tagRecords = registry.OfType<SubtagRegistry.SubtagRegistryTagRecord>().ToList();

            // act & assert
            registry.Languages.Should().Equal(subtagRecords.Where(record => record.Type == SubtagRegistry.SubtagRegistryRecordType.Language));
            registry.ExtendedLanguages.Should().Equal(subtagRecords.Where(record => record.Type == SubtagRegistry.SubtagRegistryRecordType.ExtendedLanguage));
            registry.Scripts.Should().Equal(subtagRecords.Where(record => record.Type == SubtagRegistry.SubtagRegistryRecordType.Script));
            registry.Regions.Should().Equal(subtagRecords.Where(record => record.Type == SubtagRegistry.SubtagRegistryRecordType.Region));
            registry.Variants.Should().Equal(subtagRecords.Where(record => record.Type == SubtagRegistry.SubtagRegistryRecordType.Variant));
            registry.GrandfatheredTags.Should().Equal(tagRecords.Where(record => record.Type == SubtagRegistry.SubtagRegistryRecordType.Grandfathered));
            registry.RedundantTags.Should().Equal(tagRecords.Where(record => record.Type == SubtagRegistry.SubtagRegistryRecordType.Redundant));
            (registry.Languages.Count + registry.ExtendedLanguages.Count + registry.Scripts.Count + registry.Regions.Count + registry.Variants.Count
                + registry.GrandfatheredTags.Count + registry.RedundantTags.Count + 1)
                .Should().Be(registry.Count, "every record but the File-Date record has one of the seven types");
            registry.FileDate.Should().Be(new DateTime(2025, 5, 15));
        }

        [Theory]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Language, "en", "en")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Language, "EN", "en")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Language, "qab", "qaa..qtz")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.ExtendedLanguage, "yue", "yue")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Script, "hant", "Hant")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Script, "Qabx", "Qaaa..Qabx")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Region, "tw", "TW")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Region, "XB", "XA..XZ")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Variant, "1994", "1994")]
        public void TryGetSubtag_WhenSubtagIsRegistered_ReturnsItsRecord(SubtagRegistry.SubtagRegistryRecordType type, string subtag, string expectedSubtag)
        {
            // act
            var found = _fixture.IanaSubtagRegistry.TryGetSubtag(type, subtag, out var record);
            var foundBySpan = _fixture.IanaSubtagRegistry.TryGetSubtag(type, subtag.AsSpan(), out var recordBySpan);

            // assert
            found.Should().BeTrue("{0} is a registered {1} subtag", subtag, type);
            record.Subtag.Should().Be(expectedSubtag);
            record.Type.Should().Be(type);
            foundBySpan.Should().BeTrue();
            recordBySpan.Should().BeSameAs(record);
        }

        [Theory]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Language, "Hant")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Script, "en")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Language, "zz")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Language, "")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Language, "\u0131a")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Grandfathered, "i-klingon")]
        [InlineData(SubtagRegistry.SubtagRegistryRecordType.Unknown, "en")]
        [InlineData((SubtagRegistry.SubtagRegistryRecordType)99, "en")]
        public void TryGetSubtag_WhenTypeHasNoSuchSubtag_ReturnsFalse(SubtagRegistry.SubtagRegistryRecordType type, string subtag)
        {
            // act
            var found = _fixture.IanaSubtagRegistry.TryGetSubtag(type, subtag, out var record);

            // assert
            found.Should().BeFalse("the registry has no {0} subtag {1}", type, subtag);
            record.Should().BeNull();
        }

        [Theory]
        [InlineData("aa")]
        [InlineData("AA")]
        public void TryGetSubtag_WhenTwoRecordsHaveSubtagsThatDifferOnlyInCase_ReturnsTheFirst(string subtag)
        {
            // arrange
            var registry = SubtagRegistry.SubtagRegistry.CreateFromContent(
                "Type: language\nSubtag: ab\nDescription: Other\n%%\nType: language\nSubtag: AA\nDescription: First\n%%\nType: language\nSubtag: aa\nDescription: Second\n%%\nType: language\nSubtag: zz\nDescription: Other");

            // act
            var found = registry.TryGetSubtag(SubtagRegistry.SubtagRegistryRecordType.Language, subtag, out var record);

            // assert
            found.Should().BeTrue();
            record.Description.Should().Equal(new[] { "First" }, "subtags are compared case-insensitively, and the first record for a subtag is the one found");
        }

        [Fact]
        public void TryGetSubtag_WhenSubtagIsNull_ReturnsFalse()
        {
            // act
            var found = _fixture.IanaSubtagRegistry.TryGetSubtag(SubtagRegistry.SubtagRegistryRecordType.Language, (string)null, out var record);

            // assert
            found.Should().BeFalse();
            record.Should().BeNull();
        }

        [Theory]
        [InlineData("i-klingon", true)]
        [InlineData("I-KLINGON", true)]
        [InlineData("zh-min-nan", true)]
        [InlineData("zh-Hant", false)]
        [InlineData("en", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsGrandfathered_ForLanguageTag_ReturnsWhetherRegistryHasGrandfatheredRecordForIt(string languageTag, bool expected)
        {
            // act
            var testResult = _fixture.IanaSubtagRegistry.IsGrandfathered(languageTag);

            // assert
            testResult.Should().Be(expected, "zh-Hant is a redundant tag and en a language subtag, not grandfathered tags");
            if (languageTag != null) _fixture.IanaSubtagRegistry.IsGrandfathered(languageTag.AsSpan()).Should().Be(expected);
        }

        [Fact]
        public void ToString_WhenRegistryIsLoadedAgainFromIt_HasSameRecords()
        {
            // arrange
            var registry = _fixture.IanaSubtagRegistry;

            // act
            var reloaded = SubtagRegistry.SubtagRegistry.CreateFromContent(registry.ToString());

            // assert
            reloaded.Select(record => record.ToString()).Should().Equal(registry.Select(record => record.ToString()));
            reloaded.Select(record => record.Fields).Should().BeEquivalentTo(registry.Select(record => record.Fields), options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CreateFromContent_WhenFieldIsFolded_JoinsLinesWithOneSpace(bool readAsync)
        {
            // arrange
            var content = "Type: language\nSubtag: ia\nDescription: Interlingua (International Auxiliary Language\n  Association)\nComments: first\n\t  second \n   \n  third\nAdded: 2005-10-16";

            // act
            var registry = await CreateFromContentAsync(content, readAsync);

            // assert
            var record = registry.OfType<SubtagRegistry.SubtagRegistrySubtagRecord>().Single();
            record.Description.Should().Equal(new[] { "Interlingua (International Auxiliary Language Association)" }, "a folded line is unfolded with one space (ref. RFC 5646 §3.1.1)");
            record.Comments.Should().Equal(new[] { "first second third" }, "each folded line is trimmed, and a blank one adds nothing");
            record.Added.Should().Be(new DateTime(2005, 10, 16));
        }

        [Fact]
        public void CreateFromContent_ForEveryFieldInRegistry_HasNoDoubledOrSurroundingSpaces()
        {
            // arrange
            var registry = SubtagRegistry.SubtagRegistry.CreateFromContent(_fixture.IanaSubtagRegistry.ToString());

            // act
            var paddedFields = registry
                .SelectMany(record => record.Fields)
                .Where(field => field.Value.Contains("  ") || field.Value.Trim() != field.Value)
                .ToList();

            // assert
            paddedFields.Should().BeEmpty("folded lines in the registry fixture are unfolded with one space");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CreateFromContent_WhenFieldNameIsUnknown_ReadsItAsSeparateField(bool readAsync)
        {
            // arrange
            var content = "Type: language\nSubtag: qaa\nDescription: Private use\nFuture-Field: some value\nAdded: 2005-10-16";

            // act
            var registry = await CreateFromContentAsync(content, readAsync);

            // assert
            var record = registry.OfType<SubtagRegistry.SubtagRegistrySubtagRecord>().Single();
            record.Description.Should().Equal(new[] { "Private use" }, "a field this version doesn't know is not part of the field before it");
            GetFieldValue(record, "Future-Field").Should().Be("some value");
        }

        [Theory]
        [InlineData("File-Date: 2025-01-01\n%%\n%%\nType: language\nSubtag: aa\n", false)]
        [InlineData("File-Date: 2025-01-01\n%%\n%%\nType: language\nSubtag: aa\n", true)]
        [InlineData("File-Date: 2025-01-01\n%%\nType: language\nDescription: No subtag or tag\n%%\nType: language\nSubtag: aa\n", false)]
        [InlineData("File-Date: 2025-01-01\n%%\nType: language\nDescription: No subtag or tag\n%%\nType: language\nSubtag: aa\n", true)]
        public async Task CreateFromContent_WhenRecordIsEmptyOrUnclassifiable_SkipsItAndReadsTheRest(string content, bool readAsync)
        {
            // act
            var registry = await CreateFromContentAsync(content, readAsync);

            // assert
            registry.Count.Should().Be(2, "a record that is empty or has no File-Date, Subtag or Tag field is skipped, not taken as the end of the registry");
            registry.OfType<SubtagRegistry.SubtagRegistrySubtagRecord>().Should().ContainSingle(record => record.Subtag == "aa");
        }

        private static async Task<SubtagRegistry.SubtagRegistry> CreateFromContentAsync(string content, bool readAsync)
        {
            // ReSharper disable once MethodHasAsyncOverload
            return readAsync ? await SubtagRegistry.SubtagRegistry.CreateFromContentAsync(content) : SubtagRegistry.SubtagRegistry.CreateFromContent(content);
        }

        private static string GetFieldValue(SubtagRegistry.ISubtagRegistryRecord record, string fieldName)
        {
            return record.Fields.Where(field => field.Key == fieldName).Select(field => field.Value).Single();
        }

        private SubtagRegistry.SubtagRegistrySubtagRecord GetSubtagRecord(SubtagRegistry.SubtagRegistryRecordType type, string subtag)
        {
            var registry = SubtagRegistry.SubtagRegistry.CreateFromContent(_fixture.IanaSubtagRegistry.ToString());
            return registry
                .OfType<SubtagRegistry.SubtagRegistrySubtagRecord>()
                .Single(record => record.Type == type && record.Subtag == subtag);
        }

        private SubtagRegistry.SubtagRegistryTagRecord GetTagRecord(SubtagRegistry.SubtagRegistryRecordType type, string tag)
        {
            var registry = SubtagRegistry.SubtagRegistry.CreateFromContent(_fixture.IanaSubtagRegistry.ToString());
            return registry
                .OfType<SubtagRegistry.SubtagRegistryTagRecord>()
                .Single(record => record.Type == type && record.Tag == tag);
        }

        private async Task<string> CreateRegistryFileAsync(string registryContent)
        {
            var tempFilePath = Path.GetTempFileName();
            using (var writer = new StreamWriter(tempFilePath, false, Encoding.UTF8))
            {
                await writer.WriteAsync(registryContent);
            }

            return tempFilePath;
        }

        private async Task<int> GetRegistryRecordCountAsync(string registryContent)
        {
            var recordCount = 0;
            using (var reader = new StringReader(registryContent))
            {
                var line = await reader.ReadLineAsync();
                while (line != null)
                {
                    if (line == "%%") recordCount++;

                    line = await reader.ReadLineAsync();
                }
            }

            return recordCount + 1;
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }
    }
}
