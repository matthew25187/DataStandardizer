using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the helpers in <see cref="JcardTestData"/> that have logic of their own.
    /// </summary>
    public class JcardTestDataTests
    {
        [Fact]
        public void Describe_LineWithAGroupAndParameters_GivesEachPart()
        {
            // arrange
            var line = new VcardContentLine("item1", "TEL", new[] { new VcardContentLineParameter("TYPE", "work", "voice"), new VcardContentLineParameter("PREF", "1") }, "123");

            // act
            var testResult = JcardTestData.Describe(line);

            // assert
            testResult.Should().Be("item1.TEL;TYPE=work|voice;PREF=1:123");
        }

        [Fact]
        public void Describe_LineWithNoGroupOrParameters_GivesTheNameAndValue()
        {
            // arrange
            var line = new VcardContentLine("FN", "A");

            // act
            var testResult = JcardTestData.Describe(line);

            // assert
            testResult.Should().Be("FN:A");
        }

        [Fact]
        public void ReadLastProperty_CardOfTwoProperties_DescribesTheSecond()
        {
            // act
            var testResult = JcardTestData.ReadLastProperty("[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"note\",{},\"text\",\"B\"]]]");

            // assert
            testResult.Should().Be("NOTE:B");
        }
    }
}
