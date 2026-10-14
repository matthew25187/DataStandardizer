using System.Text;
using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests;

public class Utf8FoldRealigningStreamTests
{
    public static TheoryData<int> BufferSizeData()
    {
        return new TheoryData<int>(1, 2, 3, 4, 5, 6, 7, 16 * 1024);
    }

    [Theory]
    [MemberData(nameof(BufferSizeData))]
    public void Read_AsciiAndWholeSequences_PassesThroughUnchanged(int bufferSize)
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("SUMMARY:a\r\n b €\r\n\t😀 é\r\nX:\n y\r\n");

        // act
        var testResult = ReadThrough(input, bufferSize);

        // assert
        testResult.Should().Equal(input);
    }

    [Theory]
    [MemberData(nameof(BufferSizeData))]
    public void Read_FoldInsideSequence_MovesFoldAfterSequence(int bufferSize)
    {
        // arrange
        byte[] input = [0x41, 0xE2, 0x0D, 0x0A, 0x20, 0x82, 0xAC, 0x42];

        // act
        var testResult = ReadThrough(input, bufferSize);

        // assert
        testResult.Should().Equal(0x41, 0xE2, 0x82, 0xAC, 0x0D, 0x0A, 0x20, 0x42);
    }

    [Theory]
    [MemberData(nameof(BufferSizeData))]
    public void Read_SeveralFoldsInsideSequence_MovesAllInOrder(int bufferSize)
    {
        // arrange
        byte[] input = [0xF0, 0x0D, 0x0A, 0x20, 0x9F, 0x0A, 0x09, 0x98, 0x0D, 0x0A, 0x09, 0x80, 0x41];

        // act
        var testResult = ReadThrough(input, bufferSize);

        // assert
        testResult.Should().Equal(0xF0, 0x9F, 0x98, 0x80, 0x0D, 0x0A, 0x20, 0x0A, 0x09, 0x0D, 0x0A, 0x09, 0x41);
    }

    [Theory]
    [MemberData(nameof(BufferSizeData))]
    public void Read_LineBreakWithoutWhiteSpaceInsideSequence_PassesThroughUnchanged(int bufferSize)
    {
        // arrange
        byte[] input = [0xE2, 0x0D, 0x0A, 0x41, 0x82, 0xAC];

        // act
        var testResult = ReadThrough(input, bufferSize);

        // assert
        testResult.Should().Equal(input);
    }

    [Theory]
    [MemberData(nameof(BufferSizeData))]
    public void Read_BareCarriageReturnInsideSequence_PassesThroughUnchanged(int bufferSize)
    {
        // arrange
        byte[] input = [0xE2, 0x0D, 0x20, 0x82, 0xAC];

        // act
        var testResult = ReadThrough(input, bufferSize);

        // assert
        testResult.Should().Equal(input);
    }

    [Theory]
    [MemberData(nameof(BufferSizeData))]
    public void Read_SequenceBrokenAfterFold_KeepsFoldBeforeBreakingByte(int bufferSize)
    {
        // arrange
        byte[] input = [0xE2, 0x82, 0x0D, 0x0A, 0x20, 0x41];

        // act
        var testResult = ReadThrough(input, bufferSize);

        // assert
        testResult.Should().Equal(0xE2, 0x82, 0x0D, 0x0A, 0x20, 0x41);
    }

    [Theory]
    [MemberData(nameof(BufferSizeData))]
    public void Read_DataEndsInsideSequenceAfterFold_KeepsFold(int bufferSize)
    {
        // arrange
        byte[] input = [0x41, 0xE2, 0x82, 0x0D, 0x0A, 0x20];

        // act
        var testResult = ReadThrough(input, bufferSize);

        // assert
        testResult.Should().Equal(input);
    }

    [Theory]
    [MemberData(nameof(BufferSizeData))]
    public void Read_LineBreakAtEndInsideSequence_PassesThroughUnchanged(int bufferSize)
    {
        // arrange
        byte[] input = [0xE2, 0x82, 0x0D, 0x0A];

        // act
        var testResult = ReadThrough(input, bufferSize);

        // assert
        testResult.Should().Equal(input);
    }

    [Theory]
    [MemberData(nameof(BufferSizeData))]
    public void Read_WideByteOrderMark_PassesThroughUnchanged(int bufferSize)
    {
        // arrange
        // In UTF-16, U+0DE2 U+200A is E2 0D 0A 20, which would otherwise look like a fold inside a UTF-8 sequence.
        var input = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("෢ ං")).ToArray();

        // act
        var testResult = ReadThrough(input, bufferSize);

        // assert
        testResult.Should().Equal(input);
    }

    [Fact]
    public void Read_EmptyStream_ReturnsZero()
    {
        // arrange
        using var stream = new Utf8FoldRealigningStream(new MemoryStream(), false, 16);

        // act
        var testResult = stream.Read(new byte[4], 0, 4);

        // assert
        testResult.Should().Be(0);
    }

    [Fact]
    public void Dispose_LeaveOpen_LeavesInnerStreamOpen()
    {
        // arrange
        var inner = new MemoryStream();
        var stream = new Utf8FoldRealigningStream(inner, true, 16);

        // act
        stream.Dispose();

        // assert
        inner.CanRead.Should().BeTrue();
        stream.CanRead.Should().BeFalse();
    }

    [Fact]
    public void Dispose_NotLeaveOpen_DisposesInnerStream()
    {
        // arrange
        var inner = new MemoryStream();
        var stream = new Utf8FoldRealigningStream(inner, false, 16);

        // act
        stream.Dispose();

        // assert
        inner.CanRead.Should().BeFalse();
    }

    /// <summary>
    /// Reads all of the input through the stream, a few bytes at a time.
    /// </summary>
    private static byte[] ReadThrough(byte[] input, int bufferSize)
    {
        using var stream = new Utf8FoldRealigningStream(new MemoryStream(input), false, bufferSize);
        using var output = new MemoryStream();
        var buffer = new byte[3];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            output.Write(buffer, 0, read);
        }

        return output.ToArray();
    }
}
