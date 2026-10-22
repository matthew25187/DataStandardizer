using System;
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
        [InlineData("region", "BU", 2005, 10, 16)]
        [InlineData("language", "en", 2005, 10, 16)]
        [InlineData("extlang", "ase", 2009, 7, 29)]
        public void SubtagRecordAdded_WhenRecordHasAddedField_ReturnsAddedDate(string type, string subtag, int year, int month, int day)
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
            var record = this.GetSubtagRecord("region", "BU");

            // act
            var testResult = record.Deprecated;

            // assert
            testResult.Should().Be(new DateTime(1989, 12, 5), "the region subtag BU was deprecated on that date");
        }

        [Fact]
        public void SubtagRecordDeprecated_WhenRecordHasNoDeprecatedField_ReturnsNull()
        {
            // arrange
            var record = this.GetSubtagRecord("language", "en");

            // act
            var testResult = record.Deprecated;

            // assert
            testResult.Should().BeNull("the language subtag en is not deprecated");
        }

        [Theory]
        [InlineData("region", "BU", "MM")]
        [InlineData("extlang", "ase", "ase")]
        public void SubtagRecordPreferredValue_WhenRecordHasPreferredValueField_ReturnsPreferredValue(string type, string subtag, string expectedPreferredValue)
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
            var record = this.GetSubtagRecord("language", "en");

            // act
            var testResult = record.PreferredValue;

            // assert
            testResult.Should().BeNull("the language subtag en has no preferred value");
        }

        [Fact]
        public void SubtagRecordSuppressScript_WhenRecordHasSuppressScriptField_ReturnsSuppressScript()
        {
            // arrange
            var record = this.GetSubtagRecord("language", "en");

            // act
            var testResult = record.SuppressScript;

            // assert
            testResult.Should().Be("Latn", "the language subtag en has a Suppress-Script of Latn");
        }

        [Fact]
        public void SubtagRecordSuppressScript_WhenRecordHasNoSuppressScriptField_ReturnsNull()
        {
            // arrange
            var record = this.GetSubtagRecord("language", "jbo");

            // act
            var testResult = record.SuppressScript;

            // assert
            testResult.Should().BeNull("the language subtag jbo has no Suppress-Script");
        }

        [Fact]
        public void TagRecord_WhenGrandfatheredTagHasDateAndPreferredValueFields_ReturnsFieldValues()
        {
            // arrange
            var record = this.GetTagRecord("grandfathered", "zh-guoyu");

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
            GetFieldValue(record, "Comments").Should().Be("first second third", "each folded line is trimmed, and a blank one adds nothing");
            record.Added.Should().Be(new DateTime(2005, 10, 16));
        }

        [Fact]
        public void CreateFromContent_ForEveryFieldInRegistry_HasNoDoubledOrSurroundingSpaces()
        {
            // arrange
            var registry = SubtagRegistry.SubtagRegistry.CreateFromContent(_fixture.IanaSubtagRegistry.ToString());

            // act
            var paddedFields = registry
                .SelectMany(record => record)
                .Where(field => field.Item2 is string value && (value.Contains("  ") || value.Trim() != value))
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
            return record.Where(field => field.Item1 == fieldName).Select(field => field.Item2 as string).Single();
        }

        private SubtagRegistry.SubtagRegistrySubtagRecord GetSubtagRecord(string type, string subtag)
        {
            var registry = SubtagRegistry.SubtagRegistry.CreateFromContent(_fixture.IanaSubtagRegistry.ToString());
            return registry
                .OfType<SubtagRegistry.SubtagRegistrySubtagRecord>()
                .Single(record => record.Type == type && record.Subtag == subtag);
        }

        private SubtagRegistry.SubtagRegistryTagRecord GetTagRecord(string type, string tag)
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
