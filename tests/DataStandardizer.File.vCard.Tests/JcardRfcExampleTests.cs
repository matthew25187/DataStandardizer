using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.JcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests that the example of RFC 7095 Appendix B is read and written as the RFC gives it.
    /// </summary>
    public class JcardRfcExampleTests
    {
        [Fact]
        public void ReadCards_RfcJcardExample_GivesTheRfcProperties()
        {
            // act
            var testResult = Describe(ReadJcard(RfcExampleJcard));

            // assert
            testResult.Should().BeEquivalentTo(new[] { RfcExampleProperties }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void ReadCards_RfcJcardExample_ReadsTheSameCardAsTheVcardExampleWrittenAsJcard()
        {
            // arrange
            var expected = Describe(ReadJcard(WriteJcard(ReadVcards(RfcExampleVcard))));

            // act
            var testResult = Describe(ReadJcard(RfcExampleJcard));

            // assert
            testResult.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        [Fact]
        public void WriteCards_RfcVcardExample_WritesTheRfcJcardExample()
        {
            // arrange
            var cards = ReadVcards(RfcExampleVcard);
            var expected = JsonNode.Parse(RfcExampleJcard);

            // act
            var testResult = JsonNode.Parse(WriteJcard(cards));

            // assert: the same JSON, ignoring formatting and the order of the members of an object.
            JsonNode.DeepEquals(testResult, expected).Should().BeTrue();
        }

        [Fact]
        public void ReadCard_RfcJcardExample_IsVersion40()
        {
            // act
            var testResult = ReadJcard(RfcExampleJcard)[0].SourceVersion;

            // assert
            testResult.Should().Be(VcardVersion.V4_0);
        }
    }
}
