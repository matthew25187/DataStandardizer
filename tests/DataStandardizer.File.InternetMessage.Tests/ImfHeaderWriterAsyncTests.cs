using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="ImfHeaderWriter"/>.  The streams used can only be written asynchronously, so these tests also show
    /// that the async path does no synchronous I/O.
    /// </summary>
    public class ImfHeaderWriterAsyncTests
    {
        private const string FieldName = "Subject";
        private const string ShortValue = "Hi";
        private const string ShortField = "Subject: Hi\r\n";

        public static TheoryData<string, string, ImfHeaderFieldKind, string> Formatted => ImfHeaderWriterTests.Formatted;

        public static TheoryData<string, string> RefusedFields => ImfHeaderWriterTests.RefusedFields;

        public static TheoryData<string> RefusedRawFields => ImfHeaderWriterTests.RefusedRawFields;

        public static TheoryData<string> FieldsToCopy => ImfHeaderWriterTests.FieldsToCopy;

        [Theory]
        [MemberData(nameof(Formatted))]
        public async Task WriteFieldAsync_NameAndValue_GivesTheSameBytesAsTheSynchronousWrite(string name, string value, ImfHeaderFieldKind kind, string expected)
        {
            // arrange
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var writer = new ImfHeaderWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteFieldAsync(name, value, kind).AsTask());

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Formatted))]
        public async Task WriteFieldAsync_Bytes_GivesTheSameBytesAsTheSynchronousWrite(string name, string value, ImfHeaderFieldKind kind, string expected)
        {
            // arrange
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var writer = new ImfHeaderWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteFieldAsync(Octets.Of(name), Octets.Of(value), kind).AsTask());

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(RefusedFields))]
        public async Task WriteFieldAsync_NameOrValueNotAllowed_FailsThroughTheTask(string name, string value)
        {
            // arrange
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var writer = new ImfHeaderWriter(stream);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                var task = writer.WriteFieldAsync(name, value, ImfHeaderFieldKind.Unstructured).AsTask();
                Func<Task> testAction = () => task;

                // assert
                await testAction.Should().ThrowAsync<ImfFileException>();
                stream.Length.Should().Be(0);
            });
        }

        [Theory]
        [MemberData(nameof(FieldsToCopy))]
        public async Task WriteFieldAsync_View_CopiesTheBytes(string field)
        {
            // arrange
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var reader = HeaderSections.CreateReader(field);
            var view = reader.ReadField().Value;
            var writer = new ImfHeaderWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteFieldAsync(view).AsTask());

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(field);
        }

        [Fact]
        public async Task WriteFieldAsync_ViewFromNoReader_FailsThroughTheTask()
        {
            // arrange
            var writer = new ImfHeaderWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                var task = writer.WriteFieldAsync(default(ImfHeaderFieldView)).AsTask();
                Func<Task> testAction = () => task;

                // assert
                await testAction.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Theory]
        [MemberData(nameof(FieldsToCopy))]
        public async Task WriteRawFieldAsync_OneWholeField_CopiesTheBytes(string field)
        {
            // arrange
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var writer = new ImfHeaderWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteRawFieldAsync(Octets.Of(field)).AsTask());

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(field);
        }

        [Theory]
        [MemberData(nameof(RefusedRawFields))]
        public async Task WriteRawFieldAsync_NotOneWholeField_FailsThroughTheTask(string field)
        {
            // arrange
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var writer = new ImfHeaderWriter(stream);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                var task = writer.WriteRawFieldAsync(Octets.Of(field)).AsTask();
                Func<Task> testAction = () => task;

                // assert
                await testAction.Should().ThrowAsync<ImfFileException>();
                stream.Length.Should().Be(0);
            });
        }

        [Fact]
        public async Task WriteFieldsAsync_Reader_CopiesTheFieldsAndReportsEach()
        {
            // arrange
            const string section = "A: 1\r\nB:\r\n 2\r\n";
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var progress = new RecordingProgress();
            var reader = new ImfHeaderReader(new BlockStream(Octets.Of(section), BlockStreamMode.AsynchronousOnly));
            var writer = new ImfHeaderWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteFieldsAsync(reader, progress));

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(section);
            progress.Last.Should().Be(new ImfWriteProgress(2, 0, 14));
        }

        [Fact]
        public async Task WriteEndOfHeaderAsync_AfterAField_WritesAnEmptyLine()
        {
            // arrange
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var writer = new ImfHeaderWriter(stream);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await writer.WriteFieldAsync(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured);

                // act
                await writer.WriteEndOfHeaderAsync();
            });

            // assert
            Octets.ToText(stream.ToArray()).Should().Be("Subject: Hi\r\n\r\n");
        }

        [Fact]
        public async Task WriteFieldAsync_AfterEndOfHeader_FailsThroughTheTask()
        {
            // arrange
            var writer = new ImfHeaderWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await writer.WriteEndOfHeaderAsync();

                // act
                var task = writer.WriteFieldAsync(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured).AsTask();
                Func<Task> testAction = () => task;

                // assert
                await testAction.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task FlushAsync_Stream_FlushesAsynchronously()
        {
            // arrange
            var writer = new ImfHeaderWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                Func<Task> testAction = () => writer.FlushAsync();

                // assert
                await testAction.Should().NotThrowAsync();
            });
        }

        [Fact]
        public async Task WriteFieldAsync_CanceledToken_FailsThroughTheTask()
        {
            // arrange
            var canceled = new CancellationToken(true);
            var writer = new ImfHeaderWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                var task = writer.WriteFieldAsync(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured, canceled).AsTask();
                Func<Task> testAction = () => task;

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            });
        }

        [Fact]
        public async Task WriteField_AfterACanceledWrite_ThrowsThatTheWriterCannotContinue()
        {
            // arrange
            var canceled = new CancellationToken(true);
            var writer = new ImfHeaderWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await MessageWrites.IgnoreFailureAsync(() => writer.WriteFieldAsync(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured, canceled).AsTask());

                // act
                Action testAction = () => writer.WriteField(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task WriteFieldAsync_NullName_ThrowsFromTheCall()
        {
            // arrange
            var writer = new ImfHeaderWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, () =>
            {
                // act
                Action testAction = () => _ = writer.WriteFieldAsync(null, ShortValue, ImfHeaderFieldKind.Unstructured);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("name");
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task WriteFieldAsync_StreamFails_FailsThroughTheTask()
        {
            // arrange
            var writer = new ImfHeaderWriter(new FailingWriteStream());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                var task = writer.WriteFieldAsync(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured).AsTask();
                Func<Task> testAction = () => task;

                // assert
                await testAction.Should().ThrowAsync<System.IO.IOException>().WithMessage(FailingWriteStream.FailureMessage);
            });
        }

        [Fact]
        public async Task WriteField_WhileAnAsynchronousWriteIsInProgress_Throws()
        {
            // arrange
            var stream = new GatedWriteStream();
            var writer = new ImfHeaderWriter(stream);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                var pending = writer.WriteFieldAsync(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured).AsTask();

                // act
                Action testAction = () => writer.WriteRawField(Octets.Of(ShortField));

                // assert
                testAction.Should().Throw<InvalidOperationException>();
                stream.OpenGate();
                await pending;
            });
        }

        [Fact]
        public async Task DisposeAsync_WhileAWriteIsInProgress_TheWriteFailsWithObjectDisposed()
        {
            // arrange
            var stream = new GatedWriteStream();
            var writer = new ImfHeaderWriter(stream);
            var pending = writer.WriteFieldAsync(FieldName, ShortValue, ImfHeaderFieldKind.Unstructured).AsTask();
            await writer.DisposeAsync();
            stream.OpenGate();

            // act
            Func<Task> testAction = () => pending;

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public async Task DisposeAsync_Twice_DoesNotThrow()
        {
            // arrange
            var writer = new ImfHeaderWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await writer.DisposeAsync();

            // act
            Func<Task> testAction = () => writer.DisposeAsync().AsTask();

            // assert
            await testAction.Should().NotThrowAsync();
        }
    }
}
