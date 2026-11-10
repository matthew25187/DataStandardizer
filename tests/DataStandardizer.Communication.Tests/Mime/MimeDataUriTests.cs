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

        // ref. RFC 2397 §2: the media type of a data URI that names none.
        private const string DefaultMediaType = "text/plain;charset=US-ASCII";

        // ref. RFC 2397 §3: the scheme.
        private const string DataScheme = "data";

        // ref. RFC 2397 §4: the brief note example, and its text.
        private const string Rfc2397BriefNote = "data:,A%20brief%20note";
        private const string Rfc2397BriefNoteText = "A brief note";

        // ref. RFC 2397 §4, with the line breaks that are there only for display removed.
        private const string Rfc2397Gif =
            "data:image/gif;base64,R0lGODdhMAAwAPAAAAAAAP///ywAAAAAMAAw" +
            "AAAC8IyPqcvt3wCcDkiLc7C0qwyGHhSWpjQu5yqmCYsapyuvUUlvONmOZtfzgFz" +
            "ByTB10QgxOR0TqBQejhRNzOfkVJ+5YiUqrXF5Y5lKh/DeuNcP5yLWGsEbtLiOSp" +
            "a/TPg7JpJHxyendzWTBfX0cxOnKPjgBzi4diinWGdkF8kjdfnycQZXZeYGejmJl" +
            "ZeGl9i2icVqaNVailT6F5iJ90m6mvuTS4OK05M0vDk0Q4XUtwvKOzrcd3iq9uis" +
            "F81M1OIcR7lEewwcLp7tuNNkM3uNna3F2JQFo97Vriy/Xl4/f1cf5VWzXyym7PH" +
            "hhx4dbgYKAAA7";

        // The GIF of Rfc2397Gif is 273 bytes, from the GIF87a signature to the ';' trailer.
        private const int Rfc2397GifLength = 273;
        private static readonly byte[] GifSignature = { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 };
        private const byte GifTrailer = 0x3B;

        // ref. RFC 2397 §4: the vendor query example, whose data has commas, and its data.
        private const string Rfc2397VendorQuery = "data:application/vnd-xxx-query,select_vcount,fcol_from_fieldtable/local";
        private const string Rfc2397VendorQueryData = "select_vcount,fcol_from_fieldtable/local";

        // ref. RFC 2397 §4: the Greek example, which has "%fg", an invalid escape, and the same with a valid escape in its place.
        private const string Rfc2397GreekWithInvalidEscape = "data:text/plain;charset=iso-8859-7,%be%fg%be";
        private const string Rfc2397GreekCorrected = "data:text/plain;charset=iso-8859-7,%be%d3%BE";

        // A data URI of UTF-8 text, the euro sign, with parameters but no type.
        private const string Utf8EuroSignWithoutType = "data:;charset=UTF-8,%E2%82%AC";
        private const string TextPlainUtf8 = "text/plain;charset=UTF-8";

        // A data URI with a percent-encoded quoted parameter value, "a b".
        private const string PercentEncodedQuotedParameter = "data:text/plain;name=%22a%20b%22,x";

        // Data URIs of the bytes 0, 1 and 2, in base64.
        private const string Base64OfThreeBytes = "data:;base64,AAEC";
        private const string PngOfThreeBytes = "data:image/png;base64,AAEC";
        private const string PngOfThreeBytesUpperCaseExtension = "data:image/png;BASE64,AAEC";
        private const string ImagePng = "image/png";
        private static readonly byte[] ThreeBytes = { 0, 1, 2 };

        // Data URIs of no data.
        private const string EmptyText = "data:,";
        private const string EmptyBase64 = "data:;base64,";

        // A data URI of the text "abc".
        private const string Abc = "data:,abc";

        // One character too short for Abc.
        private const int OneTooFewForAbc = 8;

        // ref. RFC 4648 §4: base64 of PNG's first four bytes, 0x89 "PNG".
        private static readonly byte[] PngSignatureStart = { 0x89, 0x50, 0x4E, 0x47 };
        private const string PngSignatureStartUri = "data:image/png;base64,iVBORw==";

        // Text with a space, a percent sign, a euro sign and a number sign, which a URI can't hold as themselves, and its data URI.
        private const string TextThatNeedsEscapes = "A brief note, 100% " + EuroSign + "#";
        private const string TextThatNeedsEscapesUri = "data:text/plain;charset=UTF-8,A%20brief%20note,%20100%25%20%E2%82%AC%23";

        // A media type with a quoted parameter value, "a, b", and the data URI of "x" in it.
        private const string MediaTypeWithQuotedValue = "text/plain;name=\"a, b\"";
        private const string QuotedValueUri = "data:text/plain;name=%22a%2C%20b%22,x";
        private static readonly byte[] LetterX = { 0x78 };

        // Room for the three bytes of Base64OfThreeBytes and one more, and too little for them.
        private const int RoomForThreeBytes = 4;
        private const int TooLittleForThreeBytes = 2;

        public static TheoryData<byte[]> EveryByte()
        {
            var data = new byte[256];
            for (var index = 0; index < data.Length; index++)
            {
                data[index] = (byte)index;
            }

            return new TheoryData<byte[]> { data };
        }

        public static TheoryData<string> InvalidUris => new TheoryData<string>
        {
            // Not data URIs.
            "",
            DataScheme,
            "data:",
            "data:text/plain",
            " data:,a",
            "http:,a",
            // Invalid media types.
            "data:text,a",
            "data:text/,a",
            "data:base64,a",
            "data:text/plain;charset,a",
            "data:text/plain;charset=a;charset=b,a",
            "data:text/plain;name=%E2%82%AC,a",
            "data:text plain,a",
            // Invalid percent-encoding.
            "data:,%",
            "data:,%4",
            "data:,%4G",
            "data:,%G4",
            "data:text/plain;name=%2,a",
            // Characters that can't appear in the data.
            "data:,a b",
            "data:,a#b",
            "data:,a\"b",
            "data:," + LatinSmallLetterEWithAcute,
            // Invalid base64.
            "data:;base64,AAE",
            "data:;base64,AA=A",
            "data:;base64,A===",
            "data:;base64,====",
            "data:;base64,AA%3D%3D",
            "data:;base64,AA\r\nEC",
            "data:;base64,AAEC====",
        };

        public static TheoryData<string> CanonicalUris => new TheoryData<string>
        {
            Rfc2397BriefNote,
            "DATA:Text/Plain;Charset=iso-8859-7,%be%d3%BE",
            EmptyBase64,
            Utf8EuroSignWithoutType,
            PercentEncodedQuotedParameter,
            Rfc2397VendorQuery,
            Rfc2397Gif,
        };

        public static TheoryData<string, string> EqualUris => new TheoryData<string, string>
        {
            { Abc, "data:text/plain;charset=us-ascii,abc" },
            { Abc, "data:;base64,YWJj" },
            { "data:,%41", "DATA:,A" },
            { PngOfThreeBytes, "data:IMAGE/PNG,%00%01%02" },
        };

        public static TheoryData<string, string> UnequalUris => new TheoryData<string, string>
        {
            { Abc, "data:,abd" },
            { Abc, "data:,ab" },
            { Abc, "data:text/plain,abc" },
            { Base64OfThreeBytes, "data:;base64,AAED" },
        };

        public static TheoryData<Action> MembersOfUninitialized => new TheoryData<Action>
        {
            () => _ = default(MimeDataUri).MediaType,
            () => _ = default(MimeDataUri).IsBase64,
            () => _ = default(MimeDataUri).DataLength,
            () => default(MimeDataUri).GetData(),
            () => default(MimeDataUri).ToUri(),
        };

        [Fact]
        public void Parse_Rfc2397BriefNote_HasDefaultMediaTypeAndDataLength()
        {
            // arrange
            var expectedMediaType = MimeMediaType.Parse(DefaultMediaType);

            // act
            var testResult = MimeDataUri.Parse(Rfc2397BriefNote);

            // assert
            testResult.MediaType.Should().Be(expectedMediaType);
            testResult.IsBase64.Should().BeFalse();
            testResult.DataLength.Should().Be(Rfc2397BriefNoteText.Length);
        }

        [Fact]
        public void GetData_Rfc2397BriefNote_ReturnsDecodedText()
        {
            // arrange
            var uri = MimeDataUri.Parse(Rfc2397BriefNote);

            // act
            var testResult = uri.GetData();

            // assert
            Encoding.ASCII.GetString(testResult).Should().Be(Rfc2397BriefNoteText);
        }

        [Fact]
        public void Parse_Rfc2397Gif_HasImageGifMediaTypeAndBase64Data()
        {
            // arrange
            var expectedMediaType = MimeMediaType.Parse("image/gif");

            // act
            var testResult = MimeDataUri.Parse(Rfc2397Gif);

            // assert
            testResult.MediaType.Should().Be(expectedMediaType);
            testResult.IsBase64.Should().BeTrue();
            testResult.DataLength.Should().Be(Rfc2397GifLength);
        }

        [Fact]
        public void GetData_Rfc2397Gif_DecodesBase64Image()
        {
            // arrange
            var uri = MimeDataUri.Parse(Rfc2397Gif);

            // act
            var testResult = uri.GetData();

            // assert
            testResult.Should().HaveCount(Rfc2397GifLength).And.StartWith(GifSignature).And.EndWith(GifTrailer);
        }

        [Fact]
        public void TryParse_Rfc2397GreekWithInvalidEscape_ReturnsFalse()
        {
            // act
            var testResult = MimeDataUri.TryParse(Rfc2397GreekWithInvalidEscape, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Parse_Rfc2397GreekCorrected_KeepsCharset()
        {
            // arrange
            var expectedMediaType = MimeMediaType.Parse("text/plain;charset=ISO-8859-7");

            // act
            var testResult = MimeDataUri.Parse(Rfc2397GreekCorrected);

            // assert
            testResult.MediaType.Should().Be(expectedMediaType);
        }

        [Fact]
        public void GetData_Rfc2397GreekCorrected_DecodesBytes()
        {
            // arrange
            var uri = MimeDataUri.Parse(Rfc2397GreekCorrected);

            // act
            var testResult = uri.GetData();

            // assert
            testResult.Should().Equal(0xBE, 0xD3, 0xBE);
        }

        [Fact]
        public void Parse_Rfc2397VendorQuery_HasVendorMediaType()
        {
            // arrange
            var expectedMediaType = new MimeMediaType("application", "vnd-xxx-query");

            // act
            var testResult = MimeDataUri.Parse(Rfc2397VendorQuery);

            // assert
            testResult.MediaType.Should().Be(expectedMediaType);
        }

        [Fact]
        public void GetData_Rfc2397VendorQuery_KeepsCommasInData()
        {
            // arrange
            var uri = MimeDataUri.Parse(Rfc2397VendorQuery);

            // act
            var testResult = uri.GetData();

            // assert
            Encoding.ASCII.GetString(testResult).Should().Be(Rfc2397VendorQueryData);
        }

        [Theory]
        [InlineData(EmptyText)]
        [InlineData(EmptyBase64)]
        [InlineData("DATA:,abc")]
        public void Parse_NoMediaType_UsesDefault(string value)
        {
            // arrange
            var expectedMediaType = MimeMediaType.Parse(DefaultMediaType);

            // act
            var testResult = MimeDataUri.Parse(value);

            // assert
            testResult.MediaType.Should().Be(expectedMediaType);
        }

        [Fact]
        public void Parse_ParametersWithoutType_UsesTextPlain()
        {
            // arrange
            var expectedMediaType = MimeMediaType.Parse(TextPlainUtf8);

            // act
            var testResult = MimeDataUri.Parse(Utf8EuroSignWithoutType);

            // assert
            testResult.MediaType.Should().Be(expectedMediaType);
        }

        [Fact]
        public void GetData_ParametersWithoutType_DecodesText()
        {
            // arrange
            var uri = MimeDataUri.Parse(Utf8EuroSignWithoutType);

            // act
            var testResult = uri.GetData();

            // assert
            Encoding.UTF8.GetString(testResult).Should().Be(EuroSign);
        }

        [Fact]
        public void Parse_PercentEncodedParameterValue_DecodesIt()
        {
            // act
            var testResult = MimeDataUri.Parse(PercentEncodedQuotedParameter);

            // assert
            testResult.MediaType.Parameters[0].Value.Should().Be("a b");
        }

        [Theory]
        [InlineData(PngOfThreeBytesUpperCaseExtension)]
        [InlineData(PngOfThreeBytes)]
        public void Parse_Base64ExtensionAnyCase_IsBase64(string value)
        {
            // arrange
            var expectedMediaType = MimeMediaType.Parse(ImagePng);

            // act
            var testResult = MimeDataUri.Parse(value);

            // assert
            testResult.IsBase64.Should().BeTrue();
            testResult.MediaType.Should().Be(expectedMediaType);
        }

        [Theory]
        [InlineData(PngOfThreeBytesUpperCaseExtension)]
        [InlineData(PngOfThreeBytes)]
        public void GetData_Base64ExtensionAnyCase_DecodesBase64(string value)
        {
            // arrange
            var uri = MimeDataUri.Parse(value);

            // act
            var testResult = uri.GetData();

            // assert
            testResult.Should().Equal(ThreeBytes);
        }

        public static TheoryData<string, byte[]> Base64Paddings => new TheoryData<string, byte[]>
        {
            { "data:application/octet-stream;base64,AA==", new byte[] { 0 } },
            { "data:application/octet-stream;base64,AAE=", new byte[] { 0, 1 } },
            { "data:application/octet-stream;base64,AAEC", ThreeBytes },
            { "data:application/octet-stream;base64,", new byte[0] },
        };

        [Theory]
        [MemberData(nameof(Base64Paddings))]
        public void GetData_Base64_DecodesPadding(string value, byte[] expected)
        {
            // arrange
            var uri = MimeDataUri.Parse(value);

            // act
            var testResult = uri.GetData();

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(Base64Paddings))]
        public void DataLength_Base64_CountsDecodedBytes(string value, byte[] expected)
        {
            // arrange
            var uri = MimeDataUri.Parse(value);

            // act
            var testResult = uri.DataLength;

            // assert
            testResult.Should().Be(expected.Length);
        }

        [Theory]
        [MemberData(nameof(InvalidUris))]
        public void TryParse_Invalid_ReturnsFalse(string value)
        {
            // act
            var testResult = MimeDataUri.TryParse(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().Be(default(MimeDataUri));
        }

        [Theory]
        [MemberData(nameof(InvalidUris))]
        public void TryParse_InvalidSpan_ReturnsFalse(string value)
        {
            // act
            var testResult = MimeDataUri.TryParse(value.AsSpan(), out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().Be(default(MimeDataUri));
        }

        [Theory]
        [MemberData(nameof(InvalidUris))]
        public void Parse_Invalid_ThrowsFormatException(string value)
        {
            // act
            Action testAction = () => MimeDataUri.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void TryParse_Null_ReturnsFalse()
        {
            // act
            var testResult = MimeDataUri.TryParse((string)null, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Parse_Null_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => MimeDataUri.Parse((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(CanonicalUris))]
        public void TryFormat_Parsed_WritesItAsParsed(string value)
        {
            // arrange
            var uri = MimeDataUri.Parse(value);
            var destination = new char[value.Length];

            // act
            var testResult = uri.TryFormat(destination, out var charsWritten);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(value.Length);
            new string(destination).Should().Be(value);
        }

        [Theory]
        [MemberData(nameof(CanonicalUris))]
        public void ToString_Parsed_ReturnsItAsParsed(string value)
        {
            // arrange
            var uri = MimeDataUri.Parse(value);

            // act
            var testResult = uri.ToString();

            // assert
            testResult.Should().Be(value);
        }

        [Theory]
        [MemberData(nameof(CanonicalUris))]
        public void Parse_SpanOfParsed_ReturnsEqualUri(string value)
        {
            // arrange
            var uri = MimeDataUri.Parse(value);

            // act
            var testResult = MimeDataUri.Parse(value.AsSpan());

            // assert
            testResult.Should().Be(uri);
        }

        [Fact]
        public void TryFormat_DestinationTooShort_ReturnsFalse()
        {
            // arrange
            var uri = MimeDataUri.Parse(Abc);

            // act
            var testResult = uri.TryFormat(new char[OneTooFewForAbc], out var charsWritten);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void Create_Base64_WritesBase64Uri()
        {
            // arrange
            var mediaType = MimeMediaType.Parse(ImagePng);

            // act
            var testResult = MimeDataUri.Create(mediaType, PngSignatureStart);

            // assert
            testResult.ToString().Should().Be(PngSignatureStartUri);
            testResult.IsBase64.Should().BeTrue();
        }

        [Fact]
        public void GetData_CreatedAsBase64_ReturnsData()
        {
            // arrange
            var uri = MimeDataUri.Create(MimeMediaType.Parse(ImagePng), PngSignatureStart);

            // act
            var testResult = uri.GetData();

            // assert
            testResult.Should().Equal(PngSignatureStart);
        }

        [Fact]
        public void Create_PercentEncoded_EncodesBytesThatCantAppearInAUri()
        {
            // arrange
            var mediaType = MimeMediaType.Parse(TextPlainUtf8);
            var data = Encoding.UTF8.GetBytes(TextThatNeedsEscapes);

            // act
            var testResult = MimeDataUri.Create(mediaType, data, false);

            // assert
            testResult.ToString().Should().Be(TextThatNeedsEscapesUri);
            testResult.IsBase64.Should().BeFalse();
        }

        [Fact]
        public void GetData_CreatedPercentEncoded_ReturnsData()
        {
            // arrange
            var data = Encoding.UTF8.GetBytes(TextThatNeedsEscapes);
            var uri = MimeDataUri.Create(MimeMediaType.Parse(TextPlainUtf8), data, false);

            // act
            var testResult = uri.GetData();

            // assert
            testResult.Should().Equal(data);
        }

        [Fact]
        public void Create_QuotedParameterValue_PercentEncodesIt()
        {
            // arrange
            var mediaType = MimeMediaType.Parse(MediaTypeWithQuotedValue);

            // act
            var testResult = MimeDataUri.Create(mediaType, LetterX, false);

            // assert
            testResult.ToString().Should().Be(QuotedValueUri);
        }

        [Fact]
        public void Parse_CreatedWithQuotedParameterValue_KeepsMediaType()
        {
            // arrange
            var mediaType = MimeMediaType.Parse(MediaTypeWithQuotedValue);

            // act
            var testResult = MimeDataUri.Parse(QuotedValueUri);

            // assert
            testResult.MediaType.Should().Be(mediaType);
        }

        [Theory]
        [MemberData(nameof(EveryByte))]
        public void Parse_CreatedAsBase64FromEveryByte_ReturnsEqualUri(byte[] data)
        {
            // arrange
            var uri = MimeDataUri.Create(MimeMediaType.ApplicationOctetStream, data, true);
            var formatted = uri.ToString();

            // act
            var testResult = MimeDataUri.Parse(formatted);

            // assert
            testResult.Should().Be(uri);
        }

        [Theory]
        [MemberData(nameof(EveryByte))]
        public void GetData_ParsedFromCreatedAsBase64FromEveryByte_ReturnsData(byte[] data)
        {
            // arrange
            var formatted = MimeDataUri.Create(MimeMediaType.ApplicationOctetStream, data, true).ToString();
            var uri = MimeDataUri.Parse(formatted);

            // act
            var testResult = uri.GetData();

            // assert
            testResult.Should().Equal(data);
        }

        [Theory]
        [MemberData(nameof(EveryByte))]
        public void Parse_CreatedPercentEncodedFromEveryByte_ReturnsEqualUri(byte[] data)
        {
            // arrange
            var uri = MimeDataUri.Create(MimeMediaType.ApplicationOctetStream, data, false);
            var formatted = uri.ToString();

            // act
            var testResult = MimeDataUri.Parse(formatted);

            // assert
            testResult.Should().Be(uri);
        }

        [Theory]
        [MemberData(nameof(EveryByte))]
        public void GetData_ParsedFromCreatedPercentEncodedFromEveryByte_ReturnsData(byte[] data)
        {
            // arrange
            var formatted = MimeDataUri.Create(MimeMediaType.ApplicationOctetStream, data, false).ToString();
            var uri = MimeDataUri.Parse(formatted);

            // act
            var testResult = uri.GetData();

            // assert
            testResult.Should().Equal(data);
        }

        [Fact]
        public void Create_UninitializedMediaType_ThrowsArgumentException()
        {
            // act
            Action testAction = () => MimeDataUri.Create(default(MimeMediaType), new byte[0]);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TryCopyData_LongEnough_WritesData()
        {
            // arrange
            var uri = MimeDataUri.Parse(Base64OfThreeBytes);
            var destination = new byte[RoomForThreeBytes];

            // act
            var testResult = uri.TryCopyData(destination, out var bytesWritten);

            // assert
            testResult.Should().BeTrue();
            bytesWritten.Should().Be(ThreeBytes.Length);
            destination.Should().Equal(0, 1, 2, 0);
        }

        [Theory]
        [InlineData(Base64OfThreeBytes)]
        [InlineData("data:,%00%01%02")]
        public void TryCopyData_TooShort_ReturnsFalse(string value)
        {
            // arrange
            var uri = MimeDataUri.Parse(value);

            // act
            var testResult = uri.TryCopyData(new byte[TooLittleForThreeBytes], out var bytesWritten);

            // assert
            testResult.Should().BeFalse();
            bytesWritten.Should().Be(0);
        }

        [Fact]
        public void ToUri_Parsed_ReturnsUriWithDataScheme()
        {
            // arrange
            var uri = MimeDataUri.Parse(Rfc2397BriefNote);

            // act
            var testResult = uri.ToUri();

            // assert
            testResult.Scheme.Should().Be(DataScheme);
            testResult.OriginalString.Should().Be(Rfc2397BriefNote);
        }

        [Theory]
        [MemberData(nameof(EqualUris))]
        public void Equals_SameMediaTypeAndData_ReturnsTrue(string left, string right)
        {
            // arrange
            var x = MimeDataUri.Parse(left);
            var y = MimeDataUri.Parse(right);

            // act
            var testResult = x.Equals(y);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(EqualUris))]
        public void EqualityOperator_SameMediaTypeAndData_ReturnsTrue(string left, string right)
        {
            // arrange
            var x = MimeDataUri.Parse(left);
            var y = MimeDataUri.Parse(right);

            // act
            var testResult = x == y;

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(EqualUris))]
        public void GetHashCode_SameMediaTypeAndData_ReturnsSameHashCode(string left, string right)
        {
            // arrange
            var x = MimeDataUri.Parse(left);
            var yHashCode = MimeDataUri.Parse(right).GetHashCode();

            // act
            var testResult = x.GetHashCode();

            // assert
            testResult.Should().Be(yHashCode);
        }

        [Theory]
        [MemberData(nameof(UnequalUris))]
        public void Equals_DifferentMediaTypeOrData_ReturnsFalse(string left, string right)
        {
            // arrange
            var x = MimeDataUri.Parse(left);
            var y = MimeDataUri.Parse(right);

            // act
            var testResult = x.Equals(y);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(UnequalUris))]
        public void InequalityOperator_DifferentMediaTypeOrData_ReturnsTrue(string left, string right)
        {
            // arrange
            var x = MimeDataUri.Parse(left);
            var y = MimeDataUri.Parse(right);

            // act
            var testResult = x != y;

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(MembersOfUninitialized))]
        public void Members_Uninitialized_ThrowInvalidOperationException(Action member)
        {
            // act
            var testAction = member;

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ToString_Uninitialized_ReturnsEmpty()
        {
            // act
            var testResult = default(MimeDataUri).ToString();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void TryFormat_Uninitialized_WritesNothing()
        {
            // act
            var testResult = default(MimeDataUri).TryFormat(new char[0], out var charsWritten);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void Equals_Uninitialized_EqualsUninitialized()
        {
            // act
            var testResult = default(MimeDataUri).Equals(default(MimeDataUri));

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_UninitializedAndEmptyData_ReturnsFalse()
        {
            // arrange
            var empty = MimeDataUri.Parse(EmptyText);

            // act
            var testResult = default(MimeDataUri).Equals(empty);

            // assert
            testResult.Should().BeFalse();
        }
    }
}
