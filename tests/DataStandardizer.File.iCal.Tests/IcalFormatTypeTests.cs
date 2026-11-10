using System;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalFormatTypeTests
    {
        private const string Attach = "ATTACH";
        private const string Uri = "http://example.com/a";
        private const string LineWithInvalidFormatType = "ATTACH;FMTTYPE=audio:http://example.com/a";
        private const string NamesThatStartWithSymbols = "!a/.b";
        private const string QuotedFormatTypeLine = "ATTACH;FMTTYPE=\"audio/basic\":http://example.com/a";
        private const string Audio = "audio";
        private const string Basic = "basic";

        // ref. RFC 5545 §3.2.8: the example on the ATTACH property.
        private const string Rfc5545Example = "ATTACH;FMTTYPE=application/msword:ftp://example.com/pub/docs/agenda.doc";
        private const string Application = "application";
        private const string MsWord = "msword";

        // ref. RFC 4288 §4.2: reg-name = 1*127reg-name-chars
        private const int MaxRegNameLength = 127;

        private static readonly Func<string, MimeMediaType?>[] Accessors =
        {
            GetFormatTypeOfLine,
            GetFormatTypeOfView,
        };

        private static readonly string LongestName = new string('a', MaxRegNameLength);
        private static readonly string TooLongName = new string('a', MaxRegNameLength + 1);

        public static TheoryData<Func<string, MimeMediaType?>, string> LinesWithoutFormatType()
        {
            var data = new TheoryData<Func<string, MimeMediaType?>, string>();
            foreach (var accessor in Accessors)
            {
                data.Add(accessor, "ATTACH:" + Uri);
                data.Add(accessor, "ATTACH;VALUE=URI:" + Uri);
            }

            return data;
        }

        public static TheoryData<Func<string, MimeMediaType?>, string, string, string> LinesWithValidFormatType()
        {
            var data = new TheoryData<Func<string, MimeMediaType?>, string, string, string>();
            foreach (var accessor in Accessors)
            {
                data.Add(accessor, Rfc5545Example, Application, MsWord);
                data.Add(accessor, "ATTACH;VALUE=URI;FMTTYPE=audio/basic:" + Uri, "audio", "basic");
                data.Add(accessor, "ATTACH;fmttype=audio/basic:" + Uri, "audio", "basic");
                data.Add(accessor, "ATTACH;FMTTYPE=IMAGE/SVG+XML:" + Uri, "IMAGE", "SVG+XML");
                data.Add(accessor, "ATTACH;FMTTYPE=a0!#$&.+-^^_/z9!#$&.+-^^_:" + Uri, "a0!#$&.+-^_", "z9!#$&.+-^_");
                data.Add(accessor, "ATTACH;FMTTYPE=!a/.b:" + Uri, "!a", ".b");
                data.Add(accessor, "ATTACH;FMTTYPE=" + LongestName + "/" + LongestName + ":" + Uri, LongestName, LongestName);
            }

            return data;
        }

        public static TheoryData<Func<string, MimeMediaType?>> AccessorCases()
        {
            var data = new TheoryData<Func<string, MimeMediaType?>>();
            foreach (var accessor in Accessors)
            {
                data.Add(accessor);
            }

            return data;
        }

        public static TheoryData<Func<string, MimeMediaType?>, string> LinesWithInvalidFormatType()
        {
            var data = new TheoryData<Func<string, MimeMediaType?>, string>();
            foreach (var accessor in Accessors)
            {
                data.Add(accessor, "ATTACH;FMTTYPE=:" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=audio:" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=audio/:" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=/basic:" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=audio/basic/x:" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=\"audio/basic \":" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=\"text/plain;charset=us-ascii\":" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=te*t/plain:" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=text/pl~in:" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=t\xE9xt/plain:" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=" + TooLongName + "/basic:" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=audio/" + TooLongName + ":" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=audio/basic,image/png:" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=audio/basic;FMTTYPE=image/png:" + Uri);
                data.Add(accessor, "ATTACH;FMTTYPE=audio/basic;fmttype=audio/basic:" + Uri);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(LinesWithoutFormatType))]
        public void GetFormatType_NoFormatTypeParameter_ReturnsNull(Func<string, MimeMediaType?> accessor, string line)
        {
            // act
            var testResult = accessor(line);

            // assert
            testResult.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(LinesWithValidFormatType))]
        public void GetFormatType_ValidValue_GivesTypeAndSubtype(Func<string, MimeMediaType?> accessor, string line, string expectedType, string expectedSubtype)
        {
            // act
            var testResult = accessor(line);

            // assert
            testResult.Should().NotBeNull();
            testResult.Value.Type.Should().Be(expectedType);
            testResult.Value.Subtype.Should().Be(expectedSubtype);
            testResult.Value.Parameters.Should().BeEmpty();
        }

        // RFC 5545 §3.2.8 has no quoted form, which IcalReader and IcalWriter check.  The quotes don't change the media type, so the accessors
        // give it, as they do for a line read with IcalOptions.AllowQuotedParameterValues.
        [Theory]
        [MemberData(nameof(AccessorCases))]
        public void GetFormatType_QuotedValue_GivesTypeAndSubtype(Func<string, MimeMediaType?> accessor)
        {
            // act
            var testResult = accessor(QuotedFormatTypeLine);

            // assert
            testResult.Should().Be(new MimeMediaType(Audio, Basic));
        }

        [Theory]
        [MemberData(nameof(LinesWithInvalidFormatType))]
        public void GetFormatType_InvalidValue_ThrowsIcalFileException(Func<string, MimeMediaType?> accessor, string line)
        {
            // act
            Action testAction = () => accessor(line);

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        [Fact]
        public void GetFormatType_LineBuiltInCode_GivesMediaType()
        {
            // arrange
            var line = new IcalContentLine(Attach, new[] { new IcalContentLineParameter(IcalParameterNames.FormatType, "application/msword") }, Uri);

            // act
            var testResult = line.GetFormatType();

            // assert
            testResult.Should().Be(new MimeMediaType(Application, MsWord));
        }

        [Fact]
        public void GetFormatType_NameThatStartsWithSymbol_WritesBackAsRead()
        {
            // arrange
            var line = new IcalContentLine(Attach, new[] { new IcalContentLineParameter(IcalParameterNames.FormatType, NamesThatStartWithSymbols) }, Uri);

            // act
            var testResult = line.GetFormatType();

            // assert
            testResult.Value.ToString().Should().Be(NamesThatStartWithSymbols);
        }

        [Fact]
        public void GetFormatType_InvalidValueOfLine_ExceptionSaysWhere()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a", " b", LineWithInvalidFormatType)))
            {
                reader.ReadLine();
                var line = reader.ReadLine();

                // act
                Action testAction = () => line.GetFormatType();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
                exception.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(3L);
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(Attach);
            }
        }

        [Fact]
        public void GetFormatType_InvalidValueOfView_ExceptionSaysWhere()
        {
            // arrange
            using (var reader = CreateReader(Crlf("SUMMARY:a", " b", LineWithInvalidFormatType)))
            {
                reader.ReadContentLine();
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                Action testAction = () => view.GetFormatType();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
                exception.Data[IcalFileException.DataItemName.PhysicalLineNumber].Should().Be(3L);
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(Attach);
            }
        }

        private static MimeMediaType? GetFormatTypeOfLine(string line)
        {
            using (var reader = CreateReader(Crlf(line)))
            {
                return reader.ReadLine().GetFormatType();
            }
        }

        private static MimeMediaType? GetFormatTypeOfView(string line)
        {
            using (var reader = CreateReader(Crlf(line)))
            {
                reader.ReadContentLine();
                return reader.CurrentLine.GetFormatType();
            }
        }
    }
}
