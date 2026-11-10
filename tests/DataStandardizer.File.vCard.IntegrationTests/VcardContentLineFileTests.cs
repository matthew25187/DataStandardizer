using System;
using System.IO;
using System.Threading.Tasks;
using DataStandardizer.File.ContentLine;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="VcardContentLineReader"/> and <see cref="VcardContentLineWriter"/>.
    /// </summary>
    public class VcardContentLineFileTests
    {
        private const string EuroSign = "\x20AC";

        // "A:" and the euro sign in UTF-8, E2 82 AC, with a CRLF and a fold's space between its second and third octets; then a line break.
        private static readonly byte[] FoldInsideEuroSign = { 0x41, 0x3A, 0xE2, 0x82, 0x0D, 0x0A, 0x20, 0xAC, 0x0D, 0x0A };

        // "A:1" and a line break.
        private static readonly byte[] SimpleLine = { 0x41, 0x3A, 0x31, 0x0D, 0x0A };

        // "A:1" and a line break, after a UTF-8 byte order mark, EF BB BF.
        private static readonly byte[] ByteOrderMarkLine = { 0xEF, 0xBB, 0xBF, 0x41, 0x3A, 0x31, 0x0D, 0x0A };

        // "A:" and the euro sign in UTF-8, E2 82 AC, and a line break, with no byte order mark.
        private static readonly byte[] EuroSignLine = { 0x41, 0x3A, 0xE2, 0x82, 0xAC, 0x0D, 0x0A };

        // More bytes than the line the writer writes, so that any left over would show.
        private static readonly byte[] ThousandBytes = new byte[1000];

        // "A:" and the byte FF, which never appears in UTF-8 (RFC 3629 §1), and a line break.
        private static readonly byte[] InvalidUtf8Line = { 0x41, 0x3A, 0xFF, 0x0D, 0x0A };

        [Fact]
        public void ReadLine_FoldInsideUtf8SequenceFromFile_RestoresCharacter()
        {
            // arrange
            using (var file = TemporaryFile.Create(FoldInsideEuroSign))
            using (var reader = new VcardContentLineReader(file.Path))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be(EuroSign);
            }
        }

        [Fact]
        public async Task ReadLineAsync_File_ReadsLine()
        {
            // arrange
            using (var file = TemporaryFile.Create(SimpleLine))
            using (var reader = new VcardContentLineReader(file.Path))
            {
                // act
                var testResult = await reader.ReadLineAsync();

                // assert
                testResult.Value.Should().Be("1");
            }
        }

        [Fact]
        public void ReadLine_FileWithByteOrderMark_Throws()
        {
            // arrange
            using (var file = TemporaryFile.Create(ByteOrderMarkLine))
            using (var reader = new VcardContentLineReader(file.Path))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadLine_FileWithByteOrderMarkAndAcceptByteOrderMark_ReadsLine()
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = null, AcceptByteOrderMark = true };
            using (var file = TemporaryFile.Create(ByteOrderMarkLine))
            using (var reader = new VcardContentLineReader(file.Path, options))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be("1");
            }
        }

        [Fact]
        public void ReadLine_FileWithInvalidUtf8_Throws()
        {
            // arrange
            using (var file = TemporaryFile.Create(InvalidUtf8Line))
            using (var reader = new VcardContentLineReader(file.Path))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void Dispose_AfterRead_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(SimpleLine))
            {
                var reader = new VcardContentLineReader(file.Path);
                reader.ReadLine();

                // act
                reader.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        // ref. #292: the file is closed even when it was never read.
        [Fact]
        public void Dispose_BeforeFirstRead_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(SimpleLine))
            {
                var reader = new VcardContentLineReader(file.Path);

                // act
                reader.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        [Fact]
        public async Task DisposeAsync_BeforeFirstRead_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(SimpleLine))
            {
                var reader = new VcardContentLineReader(file.Path);

                // act
                await reader.DisposeAsync();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        [Fact]
        public void Constructor_FilePath_ReadsThroughAFoldRealigningStreamOverTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(SimpleLine))
            using (var reader = new VcardContentLineReader(file.Path))
            {
                // act
                var testResult = reader.UnderlyingReader;

                // assert
                var streamReader = testResult.Should().BeOfType<StreamReader>().Subject;
                var foldRealigningStream = streamReader.BaseStream.Should().BeOfType<Utf8FoldRealigningStream>().Subject;
                foldRealigningStream.InnerStream.Should().BeOfType<FileStream>();
            }
        }

        // ref. #268: a file-path constructor opens the file for asynchronous I/O.
        [Fact]
        public void Constructor_FilePath_OpensFileForAsynchronousIo()
        {
            // arrange
            using (var file = TemporaryFile.Create(SimpleLine))
            using (var reader = new VcardContentLineReader(file.Path))
            {
                // act
                var testResult = (FileStream)((Utf8FoldRealigningStream)((StreamReader)reader.UnderlyingReader).BaseStream).InnerStream;

                // assert
                testResult.IsAsync.Should().BeTrue();
            }
        }

        [Fact]
        public void Constructor_MissingFile_Throws()
        {
            // arrange
            using (var file = TemporaryFile.Create())
            {
                var path = file.Path;
                file.Dispose();

                // act
                Action testAction = () => new VcardContentLineReader(path);

                // assert
                testAction.Should().Throw<FileNotFoundException>();
            }
        }

        [Fact]
        public void WriteLine_FilePath_WritesUtf8WithoutByteOrderMark()
        {
            // arrange
            using (var file = TemporaryFile.Create())
            {
                // act
                using (var writer = new VcardContentLineWriter(file.Path))
                {
                    writer.WriteLine(new VcardContentLine("A", EuroSign));
                }

                // assert
                file.ReadAllBytes().Should().Equal(EuroSignLine);
            }
        }

        [Fact]
        public async Task WriteLineAsync_FilePath_WritesTheLine()
        {
            // arrange
            using (var file = TemporaryFile.Create())
            {
                var writer = new VcardContentLineWriter(file.Path);

                // act
                await writer.WriteLineAsync(new VcardContentLine("A", "1"));
                await writer.DisposeAsync();

                // assert
                file.ReadAllBytes().Should().Equal(SimpleLine);
            }
        }

        [Fact]
        public void Constructor_WriterOverExistingFile_OverwritesIt()
        {
            // arrange
            using (var file = TemporaryFile.Create(ThousandBytes))
            {
                // act
                using (var writer = new VcardContentLineWriter(file.Path))
                {
                    writer.WriteLine(new VcardContentLine("A", "1"));
                }

                // assert
                file.ReadAllBytes().Should().Equal(SimpleLine);
            }
        }

        // ref. #268: a file-path constructor opens the file for asynchronous I/O.
        [Fact]
        public void Constructor_WriterOverFilePath_OpensFileForAsynchronousIo()
        {
            // arrange
            using (var file = TemporaryFile.Create())
            using (var writer = new VcardContentLineWriter(file.Path))
            {
                // act
                var testResult = (FileStream)((StreamWriter)writer.UnderlyingWriter).BaseStream;

                // assert
                testResult.IsAsync.Should().BeTrue();
            }
        }

        [Fact]
        public void Dispose_WriterOverFilePath_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create())
            {
                var writer = new VcardContentLineWriter(file.Path);
                writer.WriteLine(new VcardContentLine("A", "1"));

                // act
                writer.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        [Fact]
        public async Task DisposeAsync_WriterOverFilePath_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create())
            {
                var writer = new VcardContentLineWriter(file.Path);
                await writer.WriteLineAsync(new VcardContentLine("A", "1"));

                // act
                await writer.DisposeAsync();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }
    }
}
