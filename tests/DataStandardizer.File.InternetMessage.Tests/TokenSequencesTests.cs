using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class TokenSequencesTests
    {
        [Fact]
        public void Read_Tokens_NamesEach()
        {
            // act
            var testResult = TokenSequences.Read("a (b) c");

            // assert
            testResult.Should().Be("Atom +Atom");
        }

        [Fact]
        public void Read_InvalidInput_GivesInvalid()
        {
            // act
            var testResult = TokenSequences.Read("(");

            // assert
            testResult.Should().Be(TokenSequences.Invalid);
        }

        [Fact]
        public void ReadWithComments_Tokens_NamesEachWithItsBytes()
        {
            // act
            var testResult = TokenSequences.ReadWithComments("a (b)");

            // assert
            testResult.Should().Be("Atom[a] Comment[(b)]");
        }

        [Fact]
        public void GetOnlyText_QuotedString_GivesItsContent()
        {
            // act
            var testResult = TokenSequences.GetOnlyText("\"a\"");

            // assert
            testResult.Should().Be("a");
        }

        [Fact]
        public void PeekThenRead_TwoTokens_GivesTheFirstTwice()
        {
            // act
            var testResult = TokenSequences.PeekThenRead("a,");

            // assert
            testResult.Should().Be("Atom Atom");
        }

        [Fact]
        public void ReadPhrase_Phrase_GivesItsParts()
        {
            // act
            var testResult = TokenSequences.ReadPhrase("a ,");

            // assert
            testResult.Should().Be("a|False|Comma");
        }
    }
}
