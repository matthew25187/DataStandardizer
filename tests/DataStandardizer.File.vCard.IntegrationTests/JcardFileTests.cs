using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.IntegrationTests
{
    /// <summary>
    /// Tests of the file-path constructors of <see cref="JcardReader"/> and <see cref="JcardWriter"/>.
    /// </summary>
    public class JcardFileTests
    {
        private const string FormattedName = "Jane Doe";
        private const string MinimalJcard = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"Jane Doe\"]]]";

        [Fact]
        public void ReadCard_File_ReadsTheCard()
        {
            // arrange
            using (var file = TemporaryFile.Create(Encoding.UTF8.GetBytes(MinimalJcard)))
            using (var reader = new JcardReader(file.Path))
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
            using (var file = TemporaryFile.Create(Encoding.UTF8.GetBytes(MinimalJcard)))
            using (var reader = new JcardReader(file.Path))
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
            using (var file = TemporaryFile.Create(Encoding.UTF8.GetBytes(MinimalJcard)))
            {
                var reader = new JcardReader(file.Path);

                // act
                reader.Dispose();

                // assert
                Action open = file.OpenExclusively;
                open.Should().NotThrow();
            }
        }

        [Fact]
        public void WriteCard_File_WritesTheJcard()
        {
            // arrange
            var card = new VcardCard();
            card.FormattedNames.Add(FormattedName);
            using (var file = TemporaryFile.Create())
            {
                using (var writer = new JcardWriter(file.Path))
                {
                    // act
                    writer.WriteCard(card);
                }

                // assert
                Encoding.UTF8.GetString(file.ReadAllBytes()).Should().Be(MinimalJcard);
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
                var writer = new JcardWriter(file.Path);
                await writer.WriteCardAsync(card);

                // act
                await writer.DisposeAsync();

                // assert
                Encoding.UTF8.GetString(file.ReadAllBytes()).Should().Be(MinimalJcard);
                Action open = file.OpenExclusively;
                open.Should().NotThrow();
            }
        }

        [Fact]
        public void Constructor_FilePathAndEncodingThatIsNotUtf8_LeavesTheFileAsItWas()
        {
            // arrange
            var content = Encoding.UTF8.GetBytes(MinimalJcard);
            using (var file = TemporaryFile.Create(content))
            {
                var options = new VcardOptions { MaxLineLength = null, Encoding = Encoding.Unicode };

                // act
                Action testAction = () => new JcardWriter(file.Path, options);

                // assert
                testAction.Should().Throw<ArgumentException>();
                file.ReadAllBytes().Should().Equal(content);
            }
        }
    }
}
