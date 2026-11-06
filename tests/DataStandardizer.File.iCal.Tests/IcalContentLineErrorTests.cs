using System;
using System.Collections;
using System.IO;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Pins the message and <see cref="Exception.Data"/> items of each content line problem, as seen through the public API.
    /// </summary>
    public class IcalContentLineErrorTests
    {
        private const string PropertyName = "A";
        private const string ParameterName = "P";

        // ref. RFC 5545 §3.1: U+0001 is a CONTROL character, which no value may hold.
        private const string ControlCharacter = "\x01";
        private const string InvalidName = "A B";
        private const string MalformedTextValue = "A:\\x\r\n";

        private const int MaxLineLength = 1;
        private const long FirstLine = 1L;

        private const string ExpectedNameEndMessage = "Expected ';' or ':' after the property name.";
        private const string InvalidParameterValueCharacterMessage = "Found an invalid character U+0001 in a parameter value.";
        private const string MissingValueMessage = "Expected ':' and a property value.";
        private const string InvalidTextEscapeMessage = "Found a backslash that does not start a valid TEXT escape sequence.";
        private const string InvalidValueCharacterMessage = "Found an invalid character U+0001 in the property value.";

        public static TheoryData<string, string, Hashtable> MalformedLineData()
        {
            return new TheoryData<string, string, Hashtable>
            {
                { "A:1\n", "Found a line feed without a carriage return.  RFC 5545 requires a CRLF line break.", ReadData() },
                { "\r\n", "Found an empty content line.", ReadData() },
                { "A.B:1\r\n", ExpectedNameEndMessage, ReadData(PropertyName) },
                { "A;P:1\r\n", "Expected '=' after the parameter name.", ReadData(PropertyName) },
                { "A;=1:2\r\n", "Expected a parameter name after ';'.", ReadData(PropertyName) },
                { "A;P=\"x\"y:1\r\n", "Expected ',', ';' or ':' after the parameter value.", ReadData(PropertyName) },
                { ":1\r\n", "Expected a property name at the start of the content line.", ReadData() },
                { "A;P=\x01:1\r\n", InvalidParameterValueCharacterMessage, ReadData(PropertyName) },
                { "A;P=\"\x01\":1\r\n", InvalidParameterValueCharacterMessage, ReadData(PropertyName) },
                { "A:\x01\r\n", InvalidValueCharacterMessage, ReadData(PropertyName) },
                { "A\r\n", MissingValueMessage, ReadData(PropertyName) },
                { "A;P=x\r\n", MissingValueMessage, ReadData(PropertyName) },
                { "A;P=\"x\r\n", "Expected a closing '\"' for the quoted parameter value.", ReadData(PropertyName) },
                { "item1.SUMMARY:x\r\n", ExpectedNameEndMessage, ReadData("item1") }
            };
        }

        public static TheoryData<IcalContentLine, string, Hashtable> UnwritableLineData()
        {
            return new TheoryData<IcalContentLine, string, Hashtable>
            {
                {
                    new IcalContentLine(InvalidName, string.Empty),
                    "The property name \"A B\" is not valid.  A name has one or more letters, digits or '-'.",
                    WriteData()
                },
                {
                    new IcalContentLine(PropertyName, new[] { new IcalContentLineParameter(InvalidName, "x") }, string.Empty),
                    "The parameter name \"A B\" is not valid.  A name has one or more letters, digits or '-'.",
                    WriteData(PropertyName)
                },
                {
                    new IcalContentLine(PropertyName, new[] { new IcalContentLineParameter(ParameterName, ControlCharacter) }, string.Empty),
                    "Found an invalid character U+0001 in a value of parameter P.",
                    WriteData(PropertyName)
                },
                {
                    new IcalContentLine(PropertyName, ControlCharacter),
                    InvalidValueCharacterMessage,
                    WriteData(PropertyName)
                }
            };
        }

        [Theory]
        [MemberData(nameof(MalformedLineData))]
        public void ReadLine_MalformedLine_ThrowsWithMessageAndData(string text, string expectedMessage, Hashtable expectedData)
        {
            // arrange
            using (var reader = new IcalContentLineReader(new StringReader(text)))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Be(expectedMessage);
                exception.Data.Should().BeEquivalentTo(expectedData);
            }
        }

        [Fact]
        public void ReadLine_LineLongerThanMaximum_ThrowsWithMessageAndData()
        {
            // arrange
            var options = new IcalOptions { MaxLineLength = MaxLineLength };
            using (var reader = new IcalContentLineReader(new StringReader("A:1\r\n"), options))
            {
                // act
                Action testAction = () => reader.ReadLine();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Be("The content line is longer than the maximum of 1 characters.");
                exception.Data.Should().BeEquivalentTo(ReadData());
            }
        }

        [Fact]
        public void GetTextValue_InvalidEscapeInView_ThrowsWithMessageAndData()
        {
            // arrange
            using (var reader = new IcalContentLineReader(new StringReader(MalformedTextValue)))
            {
                reader.ReadContentLine();
                var view = reader.CurrentLine;

                // act
                Action testAction = () => view.GetTextValue().ToString();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Be(InvalidTextEscapeMessage);
                exception.Data.Should().BeEquivalentTo(ReadData(PropertyName));
            }
        }

        [Fact]
        public void GetTextValue_InvalidEscapeInLine_ThrowsWithMessageAndData()
        {
            // arrange
            using (var reader = new IcalContentLineReader(new StringReader(MalformedTextValue)))
            {
                var line = reader.ReadLine();

                // act
                Action testAction = () => line.GetTextValue();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Be(InvalidTextEscapeMessage);
                exception.Data.Should().BeEquivalentTo(ReadData(PropertyName));
            }
        }

        [Theory]
        [MemberData(nameof(UnwritableLineData))]
        public void WriteLine_UnwritableLine_ThrowsWithMessageAndData(IcalContentLine line, string expectedMessage, Hashtable expectedData)
        {
            // arrange
            using (var writer = new IcalContentLineWriter(new StringWriter()))
            {
                // act
                Action testAction = () => writer.WriteLine(line);

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Message.Should().Be(expectedMessage);
                exception.Data.Should().BeEquivalentTo(expectedData);
            }
        }

        /// <summary>
        /// The items of a problem found on the first line read, before its property name was read.
        /// </summary>
        private static Hashtable ReadData()
        {
            return new Hashtable
            {
                { IcalFileException.DataItemName.LineNumber, FirstLine },
                { IcalFileException.DataItemName.PhysicalLineNumber, FirstLine }
            };
        }

        /// <summary>
        /// The items of a problem found on the first line read, after its property name was read.
        /// </summary>
        private static Hashtable ReadData(string propertyName)
        {
            return new Hashtable
            {
                { IcalFileException.DataItemName.LineNumber, FirstLine },
                { IcalFileException.DataItemName.PhysicalLineNumber, FirstLine },
                { IcalFileException.DataItemName.PropertyName, propertyName }
            };
        }

        /// <summary>
        /// The items of a problem found on the first line written, whose property name is not valid.
        /// </summary>
        private static Hashtable WriteData()
        {
            return new Hashtable { { IcalFileException.DataItemName.LineNumber, FirstLine } };
        }

        /// <summary>
        /// The items of a problem found on the first line written, whose property name is valid.
        /// </summary>
        private static Hashtable WriteData(string propertyName)
        {
            return new Hashtable
            {
                { IcalFileException.DataItemName.LineNumber, FirstLine },
                { IcalFileException.DataItemName.PropertyName, propertyName }
            };
        }
    }
}
