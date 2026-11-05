using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.IntegrationTests
{
    /// <summary>
    /// Tests that a <c>CHARSET</c> resolves to the code pages that the runtime has.
    /// </summary>
    /// <remarks>
    /// On .NET Framework the code pages come from the operating system, and on .NET from registering <c>CodePagesEncodingProvider</c>, which changes the whole
    /// process and can't be undone.  Either way they come from outside the project, so these are integration tests, and the unit tests check that .NET doesn't
    /// resolve them without the provider.
    /// </remarks>
    public class VcardCharsetCodePageTests
    {
        private const string BeginCard = "BEGIN:VCARD\r\nVERSION:2.1\r\n";

        // ref. IANA Character Sets registry.  The euro sign is 0x80 in Windows-1252.
        private const string EuroSign = "\x20AC";

        // "Japan", which is 0x93FA 0x967B in Shift_JIS.
        private const string Japan = "\x65E5\x672C";

        public static TheoryData<string, string> QuotedPrintableData => new TheoryData<string, string>
        {
            { BeginCard + "NOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=windows-1252:=80\r\n", EuroSign },
            { BeginCard + "NOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=Shift_JIS:=93=FA=96=7B\r\n", Japan }
        };

        // The card, with a raw 8-bit value in Shift_JIS.
        private static readonly byte[] RawShiftJisCard = Encoding.ASCII.GetBytes(BeginCard + "NOTE;ENCODING=8BIT;CHARSET=Shift_JIS:")
            .Concat(new byte[] { 0x93, 0xFA, 0x96, 0x7B, 0x0D, 0x0A })
            .ToArray();

        [Theory]
        [MemberData(nameof(QuotedPrintableData))]
        public void GetDecodedText_QuotedPrintableInCodePage_GivesTheText(string text, string expected)
        {
            // arrange
            CodePageProvider.Register();
            using (var reader = new VcardContentLineReader(new StringReader(text)))
            {
                var line = reader.ReadLines().Last();

                // act
                var testResult = line.GetDecodedText();

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Fact]
        public void GetDecodedText_RawShiftJisValueReadByteTransparently_GivesTheText()
        {
            // arrange
            CodePageProvider.Register();
            var options = new VcardOptions { Encoding = Encoding.GetEncoding("iso-8859-1") };
            using (var reader = new VcardContentLineReader(new MemoryStream(RawShiftJisCard), options))
            {
                var line = reader.ReadLines().Last();

                // act
                var testResult = line.GetDecodedText();

                // assert
                testResult.Should().Be(Japan);
            }
        }

        private static class CodePageProvider
        {
            /// <summary>
            /// Makes the runtime's code pages available: on .NET, registers <c>CodePagesEncodingProvider</c>, which the runtime registers only once however often it
            /// is called.  .NET Framework has them already.
            /// </summary>
            public static void Register()
            {
#if !NETFRAMEWORK
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            }
        }
    }
}
