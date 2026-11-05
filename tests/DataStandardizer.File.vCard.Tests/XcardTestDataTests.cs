using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the helpers in <see cref="XcardTestData"/> that have logic of their own.
    /// </summary>
    public class XcardTestDataTests
    {
        [Fact]
        public void Canonical_IndentedDocumentWithADeclaration_DropsTheWhiteSpaceBetweenElementsAndTheDeclaration()
        {
            // act
            var testResult = XcardTestData.Canonical("<?xml version=\"1.0\"?>\n<a>\n  <b> x </b>\n  <c></c>\n  <d/>\n</a>\n");

            // assert
            testResult.Should().Be("<a><b> x </b><c></c><d /></a>");
        }

        [Fact]
        public void ReadLastXcardProperty_CardOfTwoProperties_DescribesTheSecond()
        {
            // act
            var testResult = XcardTestData.ReadLastXcardProperty("<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>A</text></fn><note><text>B</text></note></vcard></vcards>");

            // assert
            testResult.Should().Be("NOTE:B");
        }
    }
}
