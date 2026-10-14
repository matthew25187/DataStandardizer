using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// Tests of the asynchronous members of <see cref="IcalContentLineWriter"/>.  The targets used can only be written asynchronously, so these
/// tests also show that the async path does no synchronous I/O.
/// </summary>
public class IcalContentLineWriterAsyncTests
{
    public enum AsyncWriteMode
    {
        WriteLineAsync,
        WriteLinesAsync,
        WriteLinesAsyncFromAsyncEnumerable
    }

    #region Sync and async parity

    public static TheoryData<string, AsyncWriteMode, int> ParityData()
    {
        var data = new TheoryData<string, AsyncWriteMode, int>();
        foreach (var fixtureName in RfcFixtures.All.Keys)
        {
            foreach (var mode in Enum.GetValues<AsyncWriteMode>())
            {
                // The stream writer's buffer is at least 128 characters, so the smaller size makes it write through every few lines.
                foreach (var streamBufferSize in new[] { 1, 16 * 1024 })
                {
                    data.Add(fixtureName, mode, streamBufferSize);
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ParityData))]
    public async Task Write_EveryFixtureToStream_MatchesSync(string fixtureName, AsyncWriteMode mode, int streamBufferSize)
    {
        // arrange
        var lines = ReadFixtureLines(fixtureName);
        using var expectedStream = new MemoryStream();
        using (var syncWriter = new IcalContentLineWriter(expectedStream, new IcalOptions(), streamBufferSize))
        {
            syncWriter.WriteLines(lines);
        }

        var stream = new AsyncOnlyWriteStream();
        var asyncWriter = new IcalContentLineWriter(stream, new IcalOptions(), streamBufferSize);

        // act
        await WriteAsync(asyncWriter, lines, mode);
        var linesWritten = asyncWriter.LinesWritten;
        await asyncWriter.DisposeAsync();

        // assert
        stream.ToArray().Should().Equal(expectedStream.ToArray());
        linesWritten.Should().Be(lines.Count);
        stream.IsDisposed.Should().BeFalse();
    }

    [Theory]
    [InlineData(AsyncWriteMode.WriteLineAsync)]
    [InlineData(AsyncWriteMode.WriteLinesAsync)]
    [InlineData(AsyncWriteMode.WriteLinesAsyncFromAsyncEnumerable)]
    public async Task Write_FoldedLinesToTextWriter_MatchesSync(AsyncWriteMode mode)
    {
        // arrange
        var options = new IcalOptions { FoldLength = 7, FoldWhitespace = '\t', LineBreak = "\n" };
        var lines = new List<IcalContentLine>
        {
            new("DESCRIPTION", string.Concat(Enumerable.Repeat("Café 😀 会議 ", 20))),
            new("ATTENDEE", [new IcalContentLineParameter("CN", "Doe, \"Jane\"\n^"), new IcalContentLineParameter("X-P", "a", "b:c")], "mailto:x")
        };
        using var expected = new StringWriter();
        using (var syncWriter = new IcalContentLineWriter(expected, options))
        {
            syncWriter.WriteLines(lines);
        }

        var text = new AsyncOnlyTextWriter();
        await using var asyncWriter = new IcalContentLineWriter(text, options);

        // act
        await WriteAsync(asyncWriter, lines, mode);

        // assert
        text.ToString().Should().Be(expected.ToString());
    }

    [Fact]
    public async Task Write_SyncAndAsyncInterleaved_WritesInOrder()
    {
        // arrange
        using var text = new StringWriter();
        await using var writer = new IcalContentLineWriter(text);

        // act
        writer.WriteLine(new IcalContentLine("A", "1"));
        await writer.WriteLineAsync(new IcalContentLine("B", "2"));
        writer.WriteLines([new IcalContentLine("C", "3")]);
        await writer.WriteLinesAsync([new IcalContentLine("D", "4")]);
        writer.Flush();
        await writer.FlushAsync();

        // assert
        text.ToString().Should().Be(Crlf("A:1", "B:2", "C:3", "D:4"));
        writer.LinesWritten.Should().Be(4);
    }

    [Fact]
    public void WriteLineAsync_TextWriterTakesLineWithoutWaiting_CompletesSynchronously()
    {
        // arrange
        using var text = new StringWriter();
        using var writer = new IcalContentLineWriter(text);

        // act
        var testResult = writer.WriteLineAsync(new IcalContentLine("SUMMARY", "a"));

        // assert
        testResult.IsCompletedSuccessfully.Should().BeTrue();
        text.ToString().Should().Be(Crlf("SUMMARY:a"));
        writer.LinesWritten.Should().Be(1);
    }

    #endregion

    #region Invalid lines

    [Fact]
    public async Task WriteLineAsync_InvalidLine_FaultsTaskAndWriterContinues()
    {
        // arrange
        var text = new AsyncOnlyTextWriter();
        await using var writer = new IcalContentLineWriter(text);

        // act
        var task = writer.WriteLineAsync(new IcalContentLine("X_Y", "1"));
        var testAction = async () => await task;

        // assert
        await testAction.Should().ThrowAsync<IcalFileException>();
        await writer.WriteLineAsync(new IcalContentLine("A", "1"));
        text.ToString().Should().Be(Crlf("A:1"));
    }

    [Fact]
    public async Task WriteLinesAsync_InvalidLine_FaultsTaskAfterWritingLinesBeforeIt()
    {
        // arrange
        var text = new AsyncOnlyTextWriter();
        await using var writer = new IcalContentLineWriter(text);
        var lines = new[] { new IcalContentLine("A", "1"), new IcalContentLine("B", "\n") };

        // act
        var task = writer.WriteLinesAsync(lines);
        var testAction = async () => await task;

        // assert
        (await testAction.Should().ThrowAsync<IcalFileException>()).Which.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
        text.ToString().Should().Be(Crlf("A:1"));
    }

    [Fact]
    public async Task WriteLinesAsync_NullLineFromAsyncEnumerable_FaultsTask()
    {
        // arrange
        await using var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());

        // act
        var testAction = async () => await writer.WriteLinesAsync(ToAsyncEnumerable([new IcalContentLine("A", "1"), null!]));

        // assert
        (await testAction.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("lines");
        writer.LinesWritten.Should().Be(1);
    }

    [Fact]
    public async Task WriteAsync_NullArguments_ThrowSynchronously()
    {
        // arrange
        await using var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());

        // act
        var testActions = new Action[]
        {
            () => _ = writer.WriteLineAsync(null!),
            () => _ = writer.WriteLinesAsync((IEnumerable<IcalContentLine>)null!),
            () => _ = writer.WriteLinesAsync((IAsyncEnumerable<IcalContentLine>)null!)
        };

        // assert
        foreach (var testAction in testActions)
        {
            testAction.Should().Throw<ArgumentNullException>();
        }
    }

    #endregion

    #region Progress

    [Fact]
    public async Task WriteLinesAsync_Progress_IsCumulativeAcrossCallsAndOverloads()
    {
        // arrange
        await using var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());
        var progress = new RecordingProgress();
        await writer.WriteLineAsync(new IcalContentLine("A", "0"));

        // act
        await writer.WriteLinesAsync([new IcalContentLine("A", "1"), new IcalContentLine("A", "2")], progress);
        await writer.WriteLinesAsync(ToAsyncEnumerable([new IcalContentLine("A", "3")]), progress);

        // assert
        progress.Reports.Should().Equal(new IcalWriteProgress(2), new IcalWriteProgress(3), new IcalWriteProgress(4));
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task WriteLineAsync_CanceledToken_CancelsAndFaultsWriter()
    {
        // arrange
        var text = new AsyncOnlyTextWriter();
        var writer = new IcalContentLineWriter(text);
        var canceledToken = new CancellationToken(true);

        // act
        var testAction = async () => await writer.WriteLineAsync(new IcalContentLine("A", "1"), canceledToken);

        // assert
        await testAction.Should().ThrowAsync<OperationCanceledException>();
        text.ToString().Should().BeEmpty();
        AssertFaulted(writer);
        await writer.DisposeAsync();
    }

    [Fact]
    public async Task WriteLinesAsync_CanceledToken_CancelsAndFaultsWriter()
    {
        // arrange
        var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());

        // act
        var testAction = async () => await writer.WriteLinesAsync([new IcalContentLine("A", "1")], cancellationToken: new CancellationToken(true));

        // assert
        await testAction.Should().ThrowAsync<OperationCanceledException>();
        AssertFaulted(writer);
        writer.Dispose();
    }

    [Fact]
    public async Task FlushAsync_CanceledToken_CancelsAndFaultsWriter()
    {
        // arrange
        var text = new AsyncOnlyTextWriter();
        var writer = new IcalContentLineWriter(text);

        // act
        var testAction = async () => await writer.FlushAsync(new CancellationToken(true));

        // assert
        await testAction.Should().ThrowAsync<OperationCanceledException>();
        text.FlushCount.Should().Be(0);
        AssertFaulted(writer);
        writer.Dispose();
    }

    [Fact]
    public async Task WriteLinesAsync_CanceledDuringWrite_CancelsAndFaultsWriter()
    {
        // arrange
        var text = new GatedTextWriter();
        var writer = new IcalContentLineWriter(text);
        using var cancellationTokenSource = new CancellationTokenSource();
        var task = writer.WriteLinesAsync([new IcalContentLine("A", "1"), new IcalContentLine("B", "2")], cancellationToken: cancellationTokenSource.Token);
        await text.WriteStarted;

        // act
        await cancellationTokenSource.CancelAsync();
        var testAction = async () => await task;

        // assert
        await testAction.Should().ThrowAsync<OperationCanceledException>();
        AssertFaulted(writer);
        writer.Dispose();
    }

    [Fact]
    public async Task WriteLinesAsync_AsyncEnumerable_ReceivesCancellationToken()
    {
        // arrange
        await using var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());
        using var cancellationTokenSource = new CancellationTokenSource();
        var observedTokens = new List<CancellationToken>();

        async IAsyncEnumerable<IcalContentLine> Lines([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            observedTokens.Add(cancellationToken);
            await Task.Yield();
            yield return new IcalContentLine("A", "1");
        }

        // act
        await writer.WriteLinesAsync(Lines(), cancellationToken: cancellationTokenSource.Token);

        // assert
        observedTokens.Should().Equal(cancellationTokenSource.Token);
    }

    #endregion

    #region Concurrency and disposal

    [Fact]
    public async Task Operations_WhileWriteInProgress_ThrowInvalidOperation()
    {
        // arrange
        var text = new GatedTextWriter();
        await using var writer = new IcalContentLineWriter(text);
        var line = new IcalContentLine("A", "1");
        var pending = writer.WriteLineAsync(line).AsTask();
        await text.WriteStarted;

        // act
        var testActions = new Action[]
        {
            () => writer.WriteLine(line),
            () => writer.WriteLines([line]),
            () => writer.Flush(),
            () => _ = writer.WriteLineAsync(line),
            () => _ = writer.WriteLinesAsync([line]),
            () => _ = writer.WriteLinesAsync(ToAsyncEnumerable([line])),
            () => _ = writer.FlushAsync()
        };

        // assert
        foreach (var testAction in testActions)
        {
            testAction.Should().Throw<InvalidOperationException>().WithMessage("*in progress*");
        }

        text.OpenGate();
        await pending;
        await writer.WriteLineAsync(new IcalContentLine("B", "2"));
        text.ToString().Should().Be(Crlf("A:1", "B:2"));
    }

    [Fact]
    public async Task Dispose_WhileWriteInProgress_CompletesWriteWithObjectDisposed()
    {
        // arrange
        var text = new GatedTextWriter();
        var writer = new IcalContentLineWriter(text);
        var pending = writer.WriteLineAsync(new IcalContentLine("A", "1")).AsTask();
        await text.WriteStarted;

        // act
        writer.Dispose();
        text.OpenGate();
        var testAction = async () => await pending;

        // assert
        await testAction.Should().ThrowAsync<ObjectDisposedException>();
        text.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_OwnedWriter_FlushesWithoutSynchronousIo()
    {
        // arrange
        var stream = new AsyncOnlyWriteStream();
        var writer = new IcalContentLineWriter(stream);
        await writer.WriteLineAsync(new IcalContentLine("A", "1"));

        // act
        await writer.DisposeAsync();

        // assert
        stream.ToArray().Should().Equal(Encoding.ASCII.GetBytes("A:1\r\n"));
        stream.FlushCount.Should().BeGreaterThan(0);
        stream.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_CallerSuppliedWriter_IsNeitherFlushedNorDisposed()
    {
        // arrange
        var text = new AsyncOnlyTextWriter();
        var writer = new IcalContentLineWriter(text);
        await writer.WriteLineAsync(new IcalContentLine("A", "1"));

        // act
        await writer.DisposeAsync();

        // assert
        text.FlushCount.Should().Be(0);
        text.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public async Task FlushAsync_CallerSuppliedWriter_IsFlushed()
    {
        // arrange
        var text = new AsyncOnlyTextWriter();
        await using var writer = new IcalContentLineWriter(text);

        // act
        await writer.FlushAsync();

        // assert
        text.FlushCount.Should().Be(1);
    }

    [Fact]
    public async Task FlushAsync_OwnedWriter_WritesThroughToStream()
    {
        // arrange
        var stream = new AsyncOnlyWriteStream();
        await using var writer = new IcalContentLineWriter(stream);
        await writer.WriteLineAsync(new IcalContentLine("A", "1"));

        // act
        await writer.FlushAsync();

        // assert
        stream.ToArray().Should().Equal(Encoding.ASCII.GetBytes("A:1\r\n"));
    }

    [Fact]
    public async Task AsyncMembers_AfterDispose_ThrowObjectDisposedSynchronously()
    {
        // arrange
        var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());
        await writer.DisposeAsync();
        var line = new IcalContentLine("A", "1");

        // act
        var testActions = new Action[]
        {
            () => _ = writer.WriteLineAsync(line),
            () => _ = writer.WriteLineAsync(null!),
            () => _ = writer.WriteLinesAsync([line]),
            () => _ = writer.WriteLinesAsync(ToAsyncEnumerable([line])),
            () => _ = writer.FlushAsync()
        };

        // assert
        foreach (var testAction in testActions)
        {
            testAction.Should().Throw<ObjectDisposedException>().Which.ObjectName.Should().Be(nameof(IcalContentLineWriter));
        }
    }

    [Fact]
    public async Task DisposeAndDisposeAsync_InAnyOrder_DoNotThrow()
    {
        // arrange
        var first = new IcalContentLineWriter(new AsyncOnlyWriteStream());
        var second = new IcalContentLineWriter(new MemoryStream());

        // act
        var testAction = async () =>
        {
            await first.DisposeAsync();
            first.Dispose();
            await first.DisposeAsync();
            second.Dispose();
            await second.DisposeAsync();
        };

        // assert
        await testAction.Should().NotThrowAsync();
    }

    #endregion

    private static void AssertFaulted(IcalContentLineWriter writer)
    {
        var line = new IcalContentLine("A", "1");
        var testActions = new Action[]
        {
            () => writer.WriteLine(line),
            () => writer.Flush(),
            () => _ = writer.WriteLineAsync(line),
            () => _ = writer.FlushAsync()
        };

        foreach (var testAction in testActions)
        {
            testAction.Should().Throw<InvalidOperationException>().WithMessage("*canceled*");
        }
    }

    private static List<IcalContentLine> ReadFixtureLines(string fixtureName)
    {
        using var reader = CreateReader(RfcFixtures.All[fixtureName]);
        var lines = ReadAll(reader);

        // Add lines long enough to fill the stream writer's buffer.
        lines.Add(new IcalContentLine("DESCRIPTION", string.Concat(Enumerable.Repeat("Café 😀 ", 100))));
        lines.Add(new IcalContentLine("X-LONG", [new IcalContentLineParameter("X-P", new string('p', 300))], new string('v', 300)));
        return lines;
    }

    private static async IAsyncEnumerable<IcalContentLine> ToAsyncEnumerable(IEnumerable<IcalContentLine> lines)
    {
        foreach (var line in lines)
        {
            await Task.Yield();
            yield return line;
        }
    }

    private static async Task WriteAsync(IcalContentLineWriter writer, IReadOnlyList<IcalContentLine> lines, AsyncWriteMode mode)
    {
        switch (mode)
        {
            case AsyncWriteMode.WriteLineAsync:
                foreach (var line in lines)
                {
                    await writer.WriteLineAsync(line);
                }

                break;
            case AsyncWriteMode.WriteLinesAsync:
                await writer.WriteLinesAsync(lines);
                break;
            default:
                await writer.WriteLinesAsync(ToAsyncEnumerable(lines));
                break;
        }
    }
}
