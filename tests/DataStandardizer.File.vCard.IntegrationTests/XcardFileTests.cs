using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="XcardReader"/> and <see cref="XcardWriter"/>.
    /// </summary>
    public class XcardFileTests
    {
        private const string FormattedName = "Jane Doe";

        private const string MinimalXcard =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\">\r\n" +
            "  <vcard>\r\n" +
            "    <fn>\r\n" +
            "      <text>Jane Doe</text>\r\n" +
            "    </fn>\r\n" +
            "  </vcard>\r\n" +
            "</vcards>";

        [Fact]
        public void ReadCard_File_ReadsTheCard()
        {
            // arrange
            using (var file = TemporaryFile.Create(Encoding.UTF8.GetBytes(MinimalXcard)))
            using (var reader = new XcardReader(file.Path))
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
            using (var file = TemporaryFile.Create(Encoding.UTF8.GetBytes(MinimalXcard)))
            using (var reader = new XcardReader(file.Path))
            {
                // act
                var testResult = await reader.ReadCardAsync();

                // assert
                testResult.FormattedNames[0].Value.Should().Be(FormattedName);
            }
        }

        [Fact]
        public void Dispose_ReaderBeforeTheFirstRead_ClosesTheFile()
        {
            // arrange
            using (var file = TemporaryFile.Create(Encoding.UTF8.GetBytes(MinimalXcard)))
            {
                var reader = new XcardReader(file.Path);

                // act
                reader.Dispose();

                // assert
                Action open = file.OpenExclusively;
                open.Should().NotThrow();
            }
        }

        [Fact]
        public void WriteCard_File_WritesTheXcard()
        {
            // arrange
            var card = new VcardCard();
            card.FormattedNames.Add(FormattedName);
            using (var file = TemporaryFile.Create())
            {
                using (var writer = new XcardWriter(file.Path))
                {
                    // act
                    writer.WriteCard(card);
                }

                // assert
                Encoding.UTF8.GetString(file.ReadAllBytes()).Should().Be(MinimalXcard);
            }
        }

        [Fact]
        public async Task DisposeAsync_WriterOfAFile_WritesTheCardAndClosesTheFile()
        {
            // arrange
            var card = new VcardCard();
            card.FormattedNames.Add(FormattedName);
            using (var file = TemporaryFile.Create())
            {
                var writer = new XcardWriter(file.Path);
                await writer.WriteCardAsync(card);

                // act
                await writer.DisposeAsync();

                // assert
                Encoding.UTF8.GetString(file.ReadAllBytes()).Should().Be(MinimalXcard);
                Action open = file.OpenExclusively;
                open.Should().NotThrow();
            }
        }
    }
}
