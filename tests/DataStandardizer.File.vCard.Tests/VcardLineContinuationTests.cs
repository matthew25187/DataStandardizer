using System;
using DataStandardizer.File.ContentLine;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of how <see cref="VcardLineContinuation"/> measures the physical lines of a quoted-printable value, which the scanner can ask about more than once.
    /// </summary>
    public class VcardLineContinuationTests
    {
        // The first physical line, which ends with a soft line break.
        private const string FirstLine = "NOTE;ENCODING=QUOTED-PRINTABLE:a=";

        // The logical line once the second physical line, of 75 characters, is joined on.  The soft line break's = is gone, so it starts at offset 32.
        private const string JoinedLine = "NOTE;ENCODING=QUOTED-PRINTABLE:abbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        private const int SecondLineStart = 32;

        // ref. vCard 2.1 §2.1.3: the longest line allowed, "less than 76 characters".
        private const int SecondLineLength = 75;

        [Fact]
        public void LongestQuotedPrintableLine_PhysicalLineAskedAboutTwice_CountsItOnce()
        {
            // arrange
            var continuation = new VcardLineContinuation(new VcardLexicalState(), acceptLegacySyntax: true, acceptRfc6350FoldsInVersion21: false);

            // act
            continuation.GetContinuation(FirstLine.AsSpan(), 0, out _);
            continuation.GetContinuation(JoinedLine.AsSpan(), SecondLineStart, out _);
            continuation.GetContinuation(JoinedLine.AsSpan(), SecondLineStart, out _);
            var testResult = continuation.LongestQuotedPrintableLine;

            // assert
            testResult.Should().Be(SecondLineLength);
        }
    }
}
