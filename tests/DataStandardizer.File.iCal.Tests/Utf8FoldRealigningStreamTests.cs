using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    public class Utf8FoldRealigningStreamTests
    {
        // Content lines with folds before and inside multi-octet characters: the euro sign, a grinning face and e-acute.
        private const string FoldedUtf8Text = "SUMMARY:a\r\n b \x20AC\r\n\t\xD83D\xDE00 \xE9\r\nX:\n y\r\n";

        // A UTF-16 byte order mark and U+0DE2 U+200A U+0D82, which in UTF-16 is E2 0D 0A 20 82 0D.
        private static readonly byte[] WideByteOrderMarkInput = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("\x0DE2\x200A\x0D82")).ToArray();

        public static TheoryData<int> BufferSizeData()
        {
            return new TheoryData<int>(1, 2, 3, 4, 5, 6, 7, 16 * 1024);
        }

        [Theory]
        [MemberData(nameof(BufferSizeData))]
        public void Read_AsciiAndWholeSequences_PassesThroughUnchanged(int bufferSize)
        {
            // arrange
            var input = Encoding.UTF8.GetBytes(FoldedUtf8Text);

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
            byte[] input = new byte[] { 0x41, 0xE2, 0x0D, 0x0A, 0x20, 0x82, 0xAC, 0x42 };

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
            byte[] input = new byte[] { 0xF0, 0x0D, 0x0A, 0x20, 0x9F, 0x0A, 0x09, 0x98, 0x0D, 0x0A, 0x09, 0x80, 0x41 };

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
            byte[] input = new byte[] { 0xE2, 0x0D, 0x0A, 0x41, 0x82, 0xAC };

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
            byte[] input = new byte[] { 0xE2, 0x0D, 0x20, 0x82, 0xAC };

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
            byte[] input = new byte[] { 0xE2, 0x82, 0x0D, 0x0A, 0x20, 0x41 };

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
            byte[] input = new byte[] { 0x41, 0xE2, 0x82, 0x0D, 0x0A, 0x20 };

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
            byte[] input = new byte[] { 0xE2, 0x82, 0x0D, 0x0A };

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
            var input = WideByteOrderMarkInput;

            // act
            var testResult = ReadThrough(input, bufferSize);

            // assert
            testResult.Should().Equal(input);
        }

        [Fact]
        public void Read_EmptyStream_ReturnsZero()
        {
            // arrange
            using (var stream = new Utf8FoldRealigningStream(new MemoryStream(), false, 16))
            {
                // act
                var testResult = stream.Read(new byte[4], 0, 4);

                // assert
                testResult.Should().Be(0);
            }
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

        // Inputs of the synchronous tests, each with what the stream gives for it, and each buffer size and overload it is read with.
        public static TheoryData<byte[], byte[], int, bool> AsyncCases()
        {
            var cases = new[]
            {
                (Encoding.UTF8.GetBytes(FoldedUtf8Text), Encoding.UTF8.GetBytes(FoldedUtf8Text)),
                (new byte[] { 0x41, 0xE2, 0x0D, 0x0A, 0x20, 0x82, 0xAC, 0x42 }, new byte[] { 0x41, 0xE2, 0x82, 0xAC, 0x0D, 0x0A, 0x20, 0x42 }),
                (new byte[] { 0xF0, 0x0D, 0x0A, 0x20, 0x9F, 0x0A, 0x09, 0x98, 0x0D, 0x0A, 0x09, 0x80, 0x41 },
                    new byte[] { 0xF0, 0x9F, 0x98, 0x80, 0x0D, 0x0A, 0x20, 0x0A, 0x09, 0x0D, 0x0A, 0x09, 0x41 }),
                (new byte[] { 0xE2, 0x82, 0x0D }, new byte[] { 0xE2, 0x82, 0x0D }),
                (new byte[] { 0xFF, 0xFE, 0x41, 0x00, 0x0D, 0x00 }, new byte[] { 0xFF, 0xFE, 0x41, 0x00, 0x0D, 0x00 }),
                (new byte[0], new byte[0]),
            };
            var data = new TheoryData<byte[], byte[], int, bool>();
            foreach (var (input, expected) in cases)
            {
                foreach (var row in AsyncBufferSizeData())
                {
                    data.Add(input, expected, (int)row[0], (bool)row[1]);
                }
            }

            return data;
        }

        public static TheoryData<int, bool> AsyncBufferSizeData()
        {
            var data = new TheoryData<int, bool>();
            foreach (var bufferSize in BufferSizeData())
            {
                data.Add(bufferSize, false);
#if NETCOREAPP2_1_OR_GREATER
                data.Add(bufferSize, true);
#endif
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(AsyncCases))]
        public async Task ReadAsync_AsyncOnlyInnerStream_RealignsFolds(byte[] input, byte[] expected, int bufferSize, bool useMemoryOverload)
        {
            // act
            var testResult = await ReadThroughAsync(input, bufferSize, useMemoryOverload);

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public async Task ReadAsync_AfterDispose_ThrowsObjectDisposed()
        {
            // arrange
            var stream = new Utf8FoldRealigningStream(new AsyncOnlyStream(new byte[] { 0x41 }), false, 16);
            stream.Dispose();

            // act
            Func<Task<int>> testAction = () => stream.ReadAsync(new byte[1], 0, 1);

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

#if NETCOREAPP3_0_OR_GREATER
        [Fact]
        public async Task ReadAsync_MemoryAfterDisposeAsync_ThrowsObjectDisposed()
        {
            // arrange
            var stream = new Utf8FoldRealigningStream(new AsyncOnlyStream(new byte[] { 0x41 }), false, 16);
            await stream.DisposeAsync();

            // act
            Func<Task<int>> testAction = () => stream.ReadAsync(new byte[1]).AsTask();

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }
#endif

        /// <summary>
        /// Reads all of the input asynchronously through the stream, a few bytes at a time, from an inner stream that can only be read
        /// asynchronously.
        /// </summary>
        private static async Task<byte[]> ReadThroughAsync(byte[] input, int bufferSize, bool useMemoryOverload)
        {
            var stream = new Utf8FoldRealigningStream(new AsyncOnlyStream(input), false, bufferSize);
            try
            {
                using (var output = new MemoryStream())
                {
                    var buffer = new byte[3];
                    int read;
#if NETCOREAPP2_1_OR_GREATER
                    while ((read = useMemoryOverload ? await stream.ReadAsync(buffer.AsMemory()) : await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
#else
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
#endif
                    {
                        output.Write(buffer, 0, read);
                    }

                    return output.ToArray();
                }
            }
            finally
            {
#if NETCOREAPP3_0_OR_GREATER
                await stream.DisposeAsync();
#else
                stream.Dispose();
#endif
            }
        }

        /// <summary>
        /// Reads all of the input through the stream, a few bytes at a time.
        /// </summary>
        private static byte[] ReadThrough(byte[] input, int bufferSize)
        {
            using (var stream = new Utf8FoldRealigningStream(new MemoryStream(input), false, bufferSize))
            using (var output = new MemoryStream())
            {
                var buffer = new byte[3];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                }

                return output.ToArray();
            }
        }
    }
}
