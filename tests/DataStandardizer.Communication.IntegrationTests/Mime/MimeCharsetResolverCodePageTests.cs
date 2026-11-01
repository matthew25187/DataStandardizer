using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.IntegrationTests.Mime
{
    /// <summary>
    /// Tests that <see cref="MimeCharsetResolver"/> resolves the code pages that the runtime has.
    /// </summary>
    /// <remarks>
    /// On .NET Framework the code pages come from the operating system, and on .NET from registering <c>CodePagesEncodingProvider</c>,
    /// which changes the whole process and can't be undone. Either way they come from outside the project, so these are integration tests,
    /// and the unit tests can check that .NET doesn't resolve them without the provider.
    /// </remarks>
    public class MimeCharsetResolverCodePageTests
    {
        // ref. IANA Character Sets registry; the Windows-1252 and Shift_JIS code pages.
        private const string Windows1252Name = "windows-1252";
        private const string ShiftJisName = "Shift_JIS";

        // The euro sign, left double quotation mark and right double quotation mark, at 0x80, 0x93 and 0x94 in Windows-1252.
        private const string Windows1252Text = "\x20AC\x201C\x201D";
        private static readonly byte[] Windows1252Bytes = { 0x80, 0x93, 0x94 };

        // "Japan", which is 0x93FA 0x967B in Shift_JIS.
        private const string ShiftJisText = "\x65E5\x672C";
        private static readonly byte[] ShiftJisBytes = { 0x93, 0xFA, 0x96, 0x7B };

        public static TheoryData<string, string, byte[]> CodePages => new TheoryData<string, string, byte[]>
        {
            { Windows1252Name, Windows1252Text, Windows1252Bytes },
            { "WINDOWS-1252", Windows1252Text, Windows1252Bytes },
            { ShiftJisName, ShiftJisText, ShiftJisBytes },
            { "shift_jis", ShiftJisText, ShiftJisBytes },
        };

        [Theory]
        [MemberData(nameof(CodePages))]
        public void GetString_CodePageTheRuntimeHas_ReturnsText(string name, string text, byte[] bytes)
        {
            // arrange
            CodePageProvider.Register();
            var encoding = Resolve(name);

            // act
            var testResult = encoding.GetString(bytes);

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(CodePages))]
        public void GetBytes_CodePageTheRuntimeHas_ReturnsBytes(string name, string text, byte[] bytes)
        {
            // arrange
            CodePageProvider.Register();
            var encoding = Resolve(name);

            // act
            var testResult = encoding.GetBytes(text);

            // assert
            testResult.Should().Equal(bytes);
        }

        private static Encoding Resolve(string name)
        {
            MimeCharsetResolver.Default.TryGetEncoding(MimeCharset.Parse(name), out var encoding);
            return encoding;
        }

        private static class CodePageProvider
        {
            /// <summary>
            /// Makes the runtime's code pages available: on .NET, registers <c>CodePagesEncodingProvider</c>, which the runtime registers only
            /// once however often it is called. .NET Framework has them already.
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
