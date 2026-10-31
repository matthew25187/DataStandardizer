using System;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeDataUriTests
    {
        private const string EuroSign = "\x20AC";
        private const string LatinSmallLetterEWithAcute = "\x00E9";

        // ref. RFC 2397 §4, with the line breaks that are there only for display removed.
        private const string Rfc2397Gif =
            "data:image/gif;base64,R0lGODdhMAAwAPAAAAAAAP///ywAAAAAMAAw" +
            "AAAC8IyPqcvt3wCcDkiLc7C0qwyGHhSWpjQu5yqmCYsapyuvUUlvONmOZtfzgFz" +
            "ByTB10QgxOR0TqBQejhRNzOfkVJ+5YiUqrXF5Y5lKh/DeuNcP5yLWGsEbtLiOSp" +
            "a/TPg7JpJHxyendzWTBfX0cxOnKPjgBzi4diinWGdkF8kjdfnycQZXZeYGejmJl" +
            "ZeGl9i2icVqaNVailT6F5iJ90m6mvuTS4OK05M0vDk0Q4XUtwvKOzrcd3iq9uis" +
            "F81M1OIcR7lEewwcLp7tuNNkM3uNna3F2JQFo97Vriy/Xl4/f1cf5VWzXyym7PH" +
            "hhx4dbgYKAAA7";

        [Fact]
        public void Parse_Rfc2397BriefNote_HasDefaultMediaTypeAndDecodedText()
        {
            var result = MimeDataUri.Parse("data:,A%20brief%20note");

            result.MediaType.Should().Be(MimeMediaType.Parse("text/plain;charset=US-ASCII"));
            result.IsBase64.Should().BeFalse();
            result.DataLength.Should().Be(12);
            Encoding.ASCII.GetString(result.GetData()).Should().Be("A brief note");
        }

        [Fact]
        public void Parse_Rfc2397Gif_DecodesBase64Image()
        {
            var result = MimeDataUri.Parse(Rfc2397Gif);

            result.MediaType.Should().Be(MimeMediaType.Parse("image/gif"));
            result.IsBase64.Should().BeTrue();
            var data = result.GetData();
            data.Should().HaveCount(result.DataLength);
            Encoding.ASCII.GetString(data, 0, 6).Should().Be("GIF87a");
            data[data.Length - 1].Should().Be((byte)';');
        }

        [Fact]
        public void TryParse_Rfc2397GreekWithInvalidEscape_ReturnsFalse()
        {
            // The RFC 2397 §4 example has "%fg", which is not a valid escape.
            MimeDataUri.TryParse("data:text/plain;charset=iso-8859-7,%be%fg%be", out _).Should().BeFalse();
        }

        [Fact]
        public void Parse_Rfc2397GreekCorrected_KeepsCharsetAndDecodesBytes()
        {
            var result = MimeDataUri.Parse("data:text/plain;charset=iso-8859-7,%be%d3%BE");

            result.MediaType.Should().Be(MimeMediaType.Parse("text/plain;charset=ISO-8859-7"));
            result.GetData().Should().Equal(0xBE, 0xD3, 0xBE);
        }

        [Fact]
        public void Parse_Rfc2397VendorQuery_KeepsCommasInData()
        {
            var result = MimeDataUri.Parse("data:application/vnd-xxx-query,select_vcount,fcol_from_fieldtable/local");

            result.MediaType.Should().Be(new MimeMediaType("application", "vnd-xxx-query"));
            Encoding.ASCII.GetString(result.GetData()).Should().Be("select_vcount,fcol_from_fieldtable/local");
        }

        [Theory]
        [InlineData("data:,")]
        [InlineData("data:;base64,")]
        [InlineData("DATA:,abc")]
        public void Parse_NoMediaType_UsesDefault(string value)
        {
            MimeDataUri.Parse(value).MediaType.Should().Be(MimeMediaType.Parse("text/plain;charset=US-ASCII"));
        }

        [Fact]
        public void Parse_ParametersWithoutType_UsesTextPlain()
        {
            var result = MimeDataUri.Parse("data:;charset=UTF-8,%E2%82%AC");

            result.MediaType.Should().Be(MimeMediaType.Parse("text/plain;charset=UTF-8"));
            Encoding.UTF8.GetString(result.GetData()).Should().Be(EuroSign);
        }

        [Fact]
        public void Parse_PercentEncodedParameterValue_DecodesIt()
        {
            var result = MimeDataUri.Parse("data:text/plain;name=%22a%20b%22,x");

            result.MediaType.Parameters[0].Value.Should().Be("a b");
        }

        [Theory]
        [InlineData("data:image/png;BASE64,AAEC")]
        [InlineData("data:image/png;base64,AAEC")]
        public void Parse_Base64ExtensionAnyCase_IsBase64(string value)
        {
            var result = MimeDataUri.Parse(value);

            result.IsBase64.Should().BeTrue();
            result.MediaType.Should().Be(MimeMediaType.Parse("image/png"));
            result.GetData().Should().Equal(0, 1, 2);
        }

        [Theory]
        [InlineData("AA==", new byte[] { 0 })]
        [InlineData("AAE=", new byte[] { 0, 1 })]
        [InlineData("AAEC", new byte[] { 0, 1, 2 })]
        [InlineData("", new byte[0])]
        public void GetData_Base64_DecodesPadding(string data, byte[] expected)
        {
            var result = MimeDataUri.Parse("data:application/octet-stream;base64," + data);

            result.DataLength.Should().Be(expected.Length);
            result.GetData().Should().Equal(expected);
        }

        [Theory]
        // Not data URIs.
        [InlineData("")]
        [InlineData("data")]
        [InlineData("data:")]
        [InlineData("data:text/plain")]
        [InlineData(" data:,a")]
        [InlineData("http:,a")]
        // Invalid media types.
        [InlineData("data:text,a")]
        [InlineData("data:text/,a")]
        [InlineData("data:base64,a")]
        [InlineData("data:text/plain;charset,a")]
        [InlineData("data:text/plain;charset=a;charset=b,a")]
        [InlineData("data:text/plain;name=%E2%82%AC,a")]
        [InlineData("data:text plain,a")]
        // Invalid percent-encoding.
        [InlineData("data:,%")]
        [InlineData("data:,%4")]
        [InlineData("data:,%4G")]
        [InlineData("data:,%G4")]
        [InlineData("data:text/plain;name=%2,a")]
        // Characters that can't appear in the data.
        [InlineData("data:,a b")]
        [InlineData("data:,a#b")]
        [InlineData("data:,a\"b")]
        [InlineData("data:," + LatinSmallLetterEWithAcute)]
        // Invalid base64.
        [InlineData("data:;base64,AAE")]
        [InlineData("data:;base64,AA=A")]
        [InlineData("data:;base64,A===")]
        [InlineData("data:;base64,====")]
        [InlineData("data:;base64,AA%3D%3D")]
        [InlineData("data:;base64,AA\r\nEC")]
        [InlineData("data:;base64,AAEC====")]
        public void TryParse_Invalid_ReturnsFalse(string value)
        {
            MimeDataUri.TryParse(value, out var result).Should().BeFalse();
            result.Should().Be(default(MimeDataUri));
            MimeDataUri.TryParse(value.AsSpan(), out _).Should().BeFalse();
            Action parse = () => MimeDataUri.Parse(value);
            parse.Should().Throw<FormatException>();
        }

        [Fact]
        public void TryParse_Null_ReturnsFalse()
        {
            MimeDataUri.TryParse((string)null, out _).Should().BeFalse();
        }

        [Fact]
        public void Parse_Null_Throws()
        {
            Action parse = () => MimeDataUri.Parse((string)null);

            parse.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("data:,A%20brief%20note")]
        [InlineData("DATA:Text/Plain;Charset=iso-8859-7,%be%d3%BE")]
        [InlineData("data:;base64,")]
        [InlineData("data:;charset=UTF-8,%E2%82%AC")]
        [InlineData("data:text/plain;name=%22a%20b%22,x")]
        [InlineData("data:application/vnd-xxx-query,select_vcount,fcol_from_fieldtable/local")]
        [InlineData(Rfc2397Gif)]
        public void TryFormat_Parsed_WritesItAsParsed(string value)
        {
            var uri = MimeDataUri.Parse(value);
            var buffer = new char[value.Length];

            uri.TryFormat(buffer, out var charsWritten).Should().BeTrue();
            new string(buffer, 0, charsWritten).Should().Be(value);
            uri.ToString().Should().Be(value);
            MimeDataUri.Parse(buffer.AsSpan(0, charsWritten)).Should().Be(uri);
        }

        [Fact]
        public void TryFormat_DestinationTooShort_ReturnsFalse()
        {
            var uri = MimeDataUri.Parse("data:,abc");

            uri.TryFormat(new char[8], out var charsWritten).Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void Create_Base64_WritesBase64Uri()
        {
            var uri = MimeDataUri.Create(MimeMediaType.Parse("image/png"), new byte[] { 0x89, 0x50, 0x4E, 0x47 });

            uri.ToString().Should().Be("data:image/png;base64,iVBORw==");
            uri.IsBase64.Should().BeTrue();
            uri.GetData().Should().Equal(0x89, 0x50, 0x4E, 0x47);
        }

        [Fact]
        public void Create_PercentEncoded_EncodesBytesThatCantAppearInAUri()
        {
            var data = Encoding.UTF8.GetBytes("A brief note, 100% " + EuroSign + "#");

            var uri = MimeDataUri.Create(MimeMediaType.Parse("text/plain;charset=UTF-8"), data, false);

            uri.ToString().Should().Be("data:text/plain;charset=UTF-8,A%20brief%20note,%20100%25%20%E2%82%AC%23");
            uri.IsBase64.Should().BeFalse();
            uri.GetData().Should().Equal(data);
        }

        [Fact]
        public void Create_QuotedParameterValue_PercentEncodesIt()
        {
            var mediaType = MimeMediaType.Parse("text/plain;name=\"a, b\"");

            var uri = MimeDataUri.Create(mediaType, new byte[] { 0x78 }, false);

            uri.ToString().Should().Be("data:text/plain;name=%22a%2C%20b%22,x");
            MimeDataUri.Parse(uri.ToString()).MediaType.Should().Be(mediaType);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Create_EveryByte_RoundTrips(bool base64)
        {
            var data = new byte[256];
            for (var index = 0; index < data.Length; index++)
            {
                data[index] = (byte)index;
            }

            var uri = MimeDataUri.Create(MimeMediaType.ApplicationOctetStream, data, base64);
            var parsed = MimeDataUri.Parse(uri.ToString());

            parsed.GetData().Should().Equal(data);
            parsed.Should().Be(uri);
        }

        [Fact]
        public void Create_UninitializedMediaType_Throws()
        {
            Action create = () => MimeDataUri.Create(default(MimeMediaType), new byte[0]);

            create.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TryCopyData_LongEnough_WritesData()
        {
            var uri = MimeDataUri.Parse("data:;base64,AAEC");
            var buffer = new byte[4];

            uri.TryCopyData(buffer, out var bytesWritten).Should().BeTrue();
            bytesWritten.Should().Be(3);
            buffer.Should().Equal(0, 1, 2, 0);
        }

        [Theory]
        [InlineData("data:;base64,AAEC")]
        [InlineData("data:,%00%01%02")]
        public void TryCopyData_TooShort_ReturnsFalse(string value)
        {
            var uri = MimeDataUri.Parse(value);

            uri.TryCopyData(new byte[2], out var bytesWritten).Should().BeFalse();
            bytesWritten.Should().Be(0);
        }

        [Fact]
        public void ToUri_ReturnsDataUri()
        {
            var uri = MimeDataUri.Parse("data:,A%20brief%20note").ToUri();

            uri.Scheme.Should().Be("data");
            uri.OriginalString.Should().Be("data:,A%20brief%20note");
        }

        [Theory]
        [InlineData("data:,abc", "data:text/plain;charset=us-ascii,abc")]
        [InlineData("data:,abc", "data:;base64,YWJj")]
        [InlineData("data:,%41", "DATA:,A")]
        [InlineData("data:image/png;base64,AAEC", "data:IMAGE/PNG,%00%01%02")]
        public void Equals_SameMediaTypeAndData_ReturnsTrue(string left, string right)
        {
            var x = MimeDataUri.Parse(left);
            var y = MimeDataUri.Parse(right);

            x.Equals(y).Should().BeTrue();
            (x == y).Should().BeTrue();
            x.GetHashCode().Should().Be(y.GetHashCode());
        }

        [Theory]
        [InlineData("data:,abc", "data:,abd")]
        [InlineData("data:,abc", "data:,ab")]
        [InlineData("data:,abc", "data:text/plain,abc")]
        [InlineData("data:;base64,AAEC", "data:;base64,AAED")]
        public void Equals_DifferentMediaTypeOrData_ReturnsFalse(string left, string right)
        {
            var x = MimeDataUri.Parse(left);
            var y = MimeDataUri.Parse(right);

            x.Equals(y).Should().BeFalse();
            (x != y).Should().BeTrue();
        }

        [Fact]
        public void Uninitialized_MembersThrowOrAreEmpty()
        {
            var uri = default(MimeDataUri);

            uri.Invoking(u => u.MediaType).Should().Throw<InvalidOperationException>();
            uri.Invoking(u => u.IsBase64).Should().Throw<InvalidOperationException>();
            uri.Invoking(u => u.DataLength).Should().Throw<InvalidOperationException>();
            uri.Invoking(u => u.GetData()).Should().Throw<InvalidOperationException>();
            uri.Invoking(u => u.ToUri()).Should().Throw<InvalidOperationException>();
            uri.ToString().Should().BeEmpty();
            uri.TryFormat(new char[0], out var charsWritten).Should().BeTrue();
            charsWritten.Should().Be(0);
            uri.Should().Be(default(MimeDataUri));
            uri.Should().NotBe(MimeDataUri.Parse("data:,"));
        }
    }
}
