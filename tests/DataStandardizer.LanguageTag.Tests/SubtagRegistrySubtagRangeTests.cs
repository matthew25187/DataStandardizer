using System;
using System.Linq;
using DataStandardizer.LanguageTag.SubtagRegistry;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    public class SubtagRegistrySubtagRangeTests
    {
        /// <summary>
        /// The range of Script subtags reserved for private use (RFC 5646 §2.2.3¶3), as the IANA registry records it (§3.1.1).
        /// </summary>
        private const string PrivateUseScriptRange = "Qaaa..Qabx";

        [Theory]
        [InlineData("qaa..qtz", 520, "qaa", "qtz"),
         InlineData(PrivateUseScriptRange, 50, "Qaaa", "Qabx"),
         InlineData("QM..QZ", 14, "QM", "QZ"),
         InlineData("XA..XZ", 26, "XA", "XZ")]
        public void Expand_WhenSubtagIsRange_ReturnsEverySubtagInRange(string testSubtag, int expectedCount, string expectedFirst, string expectedLast)
        {
            // act
            var testResult = SubtagRegistrySubtagRange.Expand(testSubtag).ToList();

            // assert
            testResult.Should().HaveCount(expectedCount, "the range '{0}' denotes {1} subtags", testSubtag, expectedCount);
            testResult.Should().OnlyHaveUniqueItems().And.BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
            testResult.First().Should().Be(expectedFirst);
            testResult.Last().Should().Be(expectedLast);
        }

        [Fact]
        public void Expand_WhenRangeSpansPositions_RunsEachPositionThroughTheLetters()
        {
            // act
            var testResult = SubtagRegistrySubtagRange.Expand(PrivateUseScriptRange).ToList();

            // assert
            testResult.Should().Contain(new[] { "Qaaz", "Qaba" }).And.NotContain(new[] { "Qaby", "Qabz", "Qaca" });
        }

        [Theory]
        [InlineData("en"), InlineData("Latn"), InlineData("419"), InlineData("1901")]
        public void Expand_WhenSubtagIsNotRange_ReturnsSubtag(string testSubtag)
        {
            // act
            var testResult = SubtagRegistrySubtagRange.Expand(testSubtag);

            // assert
            testResult.Should().Equal(testSubtag);
        }

        [Theory]
        [InlineData("qtz..qaa"), InlineData("qaa..qtzz"), InlineData("..qaa"), InlineData("qaa.."), InlineData("q-a..q-z"), InlineData("a1..z9a")]
        public void Expand_WhenRangeIsMalformed_ReturnsSubtag(string testSubtag)
        {
            // act
            var testResult = SubtagRegistrySubtagRange.Expand(testSubtag);

            // assert
            testResult.Should().Equal(testSubtag);
        }
    }
}
