using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="IcalContentLineReader"/> and <see cref="IcalContentLineWriter"/>.
    /// </summary>
    public class IcalContentLineFileTests
    {
        private const string Summary = "SUMMARY";
        private const string CafeWithAcute = "Caf\xE9";
        private const string EuroSign = "\x20AC";

        // ref. RFC 5545 §3.1: "SUMMARY:" and the euro sign in UTF-8, E2 82 AC, folded between its second and third octets.
        private static readonly byte[] FoldInsideEuroSign =
        {
            0x53, 0x55, 0x4D, 0x4D, 0x41, 0x52, 0x59, 0x3A, 0xE2, 0x82, 0x0D, 0x0A, 0x20, 0xAC
        };

        // "SUMMARY:A" and a line break.
        private static readonly byte[] SummaryLine = { 0x53, 0x55, 0x4D, 0x4D, 0x41, 0x52, 0x59, 0x3A, 0x41, 0x0D, 0x0A };

        // "SUMMARY:Caf" followed by U+00E9 in UTF-8, C3 A9, and a line break; with no byte order mark.
        private static readonly byte[] CafeSummaryLine =
        {
            0x53, 0x55, 0x4D, 0x4D, 0x41, 0x52, 0x59, 0x3A, 0x43, 0x61, 0x66, 0xC3, 0xA9, 0x0D, 0x0A
        };

        // More bytes than the line the writer writes, so that any left over would show.
        private static readonly byte[] ThousandBytes = new byte[1000];

        [Fact]
        public void ReadLine_FoldInsideUtf8SequenceFromFile_RestoresCharacter()
        {
            // arrange
            using (var file = TemporaryFile.Create(FoldInsideEuroSign))
            using (var reader = new IcalContentLineReader(file.Path))
            {
                // act
                var testResult = reader.ReadLine();

                // assert
                testResult.Value.Should().Be(EuroSign);
            }
        }

        [Fact]
        public void Dispose_ReaderOverFilePath_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(SummaryLine))
            {
                var reader = new IcalContentLineReader(file.Path);
                reader.ReadLine();

                // act
                reader.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }

        [Fact]
        public void Constructor_FilePath_ReadsThroughAFoldRealigningStreamOverTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(SummaryLine))
            using (var reader = new IcalContentLineReader(file.Path))
            {
                // act
                var testResult = reader.UnderlyingReader;

                // assert
                var streamReader = testResult.Should().BeOfType<StreamReader>().Subject;
                var foldRealigningStream = streamReader.BaseStream.Should().BeOfType<Utf8FoldRealigningStream>().Subject;
                foldRealigningStream.InnerStream.Should().BeOfType<FileStream>();
            }
        }

        [Fact]
        public void Constructor_ExistingFile_IsOverwritten()
        {
            // arrange
            using (var file = TemporaryFile.Create(ThousandBytes))
            {
                // act
                using (var writer = new IcalContentLineWriter(file.Path))
                {
                    writer.WriteLine(new IcalContentLine(Summary, CafeWithAcute));
                }

                // assert
                file.ReadAllBytes().Should().Equal(CafeSummaryLine);
            }
        }

        [Fact]
        public void Dispose_WriterOverFilePath_ClosesFile()
        {
            // arrange
            using (var file = TemporaryFile.Create())
            {
                var writer = new IcalContentLineWriter(file.Path);
                writer.WriteLine(new IcalContentLine(Summary, CafeWithAcute));

                // act
                writer.Dispose();

                // assert
                Action openExclusively = file.OpenExclusively;
                openExclusively.Should().NotThrow();
            }
        }
    }
}
