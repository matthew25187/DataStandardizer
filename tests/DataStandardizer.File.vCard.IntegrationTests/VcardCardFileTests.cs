using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="VcardReader"/> and <see cref="VcardWriter"/>.
    /// </summary>
    public class VcardCardFileTests
    {
        private const string FormattedName = "Jane Doe";

        // "BEGIN:VCARD", "VERSION:4.0", "FN:Jane Doe" and "END:VCARD", each ending with CRLF.
        private static readonly byte[] MinimalCard =
        {
            0x42, 0x45, 0x47, 0x49, 0x4E, 0x3A, 0x56, 0x43, 0x41, 0x52, 0x44, 0x0D, 0x0A,
            0x56, 0x45, 0x52, 0x53, 0x49, 0x4F, 0x4E, 0x3A, 0x34, 0x2E, 0x30, 0x0D, 0x0A,
            0x46, 0x4E, 0x3A, 0x4A, 0x61, 0x6E, 0x65, 0x20, 0x44, 0x6F, 0x65, 0x0D, 0x0A,
            0x45, 0x4E, 0x44, 0x3A, 0x56, 0x43, 0x41, 0x52, 0x44, 0x0D, 0x0A
        };

        [Fact]
        public void Constructor_FilePathAndVersionThatCanNotBeWritten_LeavesTheFileAsItWas()
        {
            // arrange
            using (var file = TemporaryFile.Create(MinimalCard))
            {
                var options = new VcardOptions { WriteVersion = VcardVersion.V2_1 };

                // act
                Action testAction = () => new VcardWriter(file.Path, options);

                // assert
                testAction.Should().Throw<ArgumentException>();
                System.IO.File.ReadAllBytes(file.Path).Should().Equal(MinimalCard);
            }
        }

        [Fact]
        public void ReadCard_File_ReadsTheCard()
        {
            // arrange
            using (var file = TemporaryFile.Create(MinimalCard))
            using (var reader = new VcardReader(file.Path))
            {
                // act
                var testResult = reader.ReadCard();

                // assert
                testResult.FormattedNames[0].Value.Should().Be(FormattedName);
            }
        }

        [Fact]
        public async Task ReadCardAsync_File_ReadsTheCard()
        {
            // arrange
            using (var file = TemporaryFile.Create(MinimalCard))
            using (var reader = new VcardReader(file.Path))
            {
                // act
                var testResult = await reader.ReadCardAsync();

                // assert
                testResult.FormattedNames[0].Value.Should().Be(FormattedName);
            }
        }

        [Fact]
        public void Dispose_BeforeTheFirstRead_ClosesTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(MinimalCard))
            {
                var reader = new VcardReader(file.Path);

                // act
                reader.Dispose();

                // assert
                Action openForWriting = () => new FileStream(file.Path, FileMode.Open, FileAccess.Write, FileShare.None).Dispose();
                openForWriting.Should().NotThrow();
            }
        }

        [Fact]
        public void WriteCard_File_WritesTheCard()
        {
            // arrange
            using (var file = TemporaryFile.Create(new byte[0]))
            {
                var card = new VcardCard();
                card.FormattedNames.Add(FormattedName);

                // act
                using (var writer = new VcardWriter(file.Path))
                {
                    writer.WriteCard(card);
                }

                // assert
                System.IO.File.ReadAllBytes(file.Path).Should().Equal(MinimalCard);
            }
        }

        [Fact]
        public async Task WriteCardAsync_File_WritesTheCard()
        {
            // arrange
            using (var file = TemporaryFile.Create(new byte[0]))
            {
                var card = new VcardCard();
                card.FormattedNames.Add(FormattedName);
                var writer = new VcardWriter(file.Path, new VcardOptions());

                // act
                await writer.WriteCardAsync(card);
                await writer.DisposeAsync();

                // assert
                System.IO.File.ReadAllBytes(file.Path).Should().Equal(MinimalCard);
            }
        }
    }
}
