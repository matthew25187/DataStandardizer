using System;
using System.Collections;
using DataStandardizer.File.ContentLine;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Pins the message and <see cref="Exception.Data"/> items File.iCal gives each kind of problem the shared content line engine reports.
    /// </summary>
    public class IcalContentLineErrorFactoryTests
    {
        private const long LineNumber = 7L;
        private const long PhysicalLineNumber = 9L;
        private const string PropertyName = "SUMMARY";
        private const string SubjectName = "X Y";
        private const int ControlCharacter = 0x01;
        private const int MaximumLineLength = 75;

        private const string InvalidValueCharacterMessage = "Found an invalid character U+0001 in the property value.";

        // A value no kind has.
        private const ContentLineErrorKind UnknownKind = (ContentLineErrorKind)int.MaxValue;

        // The error is passed as an object because a test method is public and ContentLineError is internal.
        public static TheoryData<object, string> EveryKindData()
        {
            return new TheoryData<object, string>
            {
                { ReadError(ContentLineErrorKind.BareLineFeed), "Found a line feed without a carriage return.  RFC 5545 requires a CRLF line break." },
                { ReadError(ContentLineErrorKind.EmptyLine), "Found an empty content line." },
                { ReadError(ContentLineErrorKind.ExpectedNameEnd), "Expected ';' or ':' after the property name." },
                { ReadError(ContentLineErrorKind.ExpectedParameterEquals), "Expected '=' after the parameter name." },
                { ReadError(ContentLineErrorKind.ExpectedParameterName), "Expected a parameter name after ';'." },
                { ReadError(ContentLineErrorKind.ExpectedParameterValueEnd), "Expected ',', ';' or ':' after the parameter value." },
                { ReadError(ContentLineErrorKind.ExpectedPropertyName), "Expected a property name at the start of the content line." },
                { ReadError(ContentLineErrorKind.InvalidParameterValueCharacter), "Found an invalid character U+0001 in a parameter value." },
                { ReadError(ContentLineErrorKind.InvalidTextEscape), "Found a backslash that does not start a valid TEXT escape sequence." },
                { ReadError(ContentLineErrorKind.InvalidValueCharacter), InvalidValueCharacterMessage },
                { ReadError(ContentLineErrorKind.LineTooLong), "The content line is longer than the maximum of 75 characters." },
                { ReadError(ContentLineErrorKind.MissingLineBreak), "Expected a CRLF line break at the end of the last content line." },
                { ReadError(ContentLineErrorKind.MissingValue), "Expected ':' and a property value." },
                { ReadError(ContentLineErrorKind.UnterminatedQuotedString), "Expected a closing '\"' for the quoted parameter value." },
                { WriteError(ContentLineErrorKind.InvalidParameterNameToWrite), "The parameter name \"X Y\" is not valid.  A name has one or more letters, digits or '-'." },
                { WriteError(ContentLineErrorKind.InvalidParameterValueCharacterToWrite), "Found an invalid character U+0001 in a value of parameter X Y." },
                { WriteError(ContentLineErrorKind.InvalidPropertyNameToWrite), "The property name \"X Y\" is not valid.  A name has one or more letters, digits or '-'." },
                { WriteError(ContentLineErrorKind.InvalidValueCharacter), InvalidValueCharacterMessage },
                { WriteError(ContentLineErrorKind.LineTooLongToWrite), "The content line is too long to write." }
            };
        }

        // The kinds, so that one added later without a message fails here.
        public static TheoryData<object> DeclaredKindData()
        {
            var data = new TheoryData<object>();
            foreach (ContentLineErrorKind kind in Enum.GetValues(typeof(ContentLineErrorKind)))
            {
                data.Add(ReadError(kind));
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(EveryKindData))]
        public void CreateException_Kind_HasMessage(object error, string expectedMessage)
        {
            // arrange
            var factory = IcalContentLineErrorFactory.Instance;

            // act
            var exception = factory.CreateException((ContentLineError)error);

            // assert
            exception.Should().BeOfType<IcalFileException>().Which.Message.Should().Be(expectedMessage);
        }

        [Theory]
        [MemberData(nameof(DeclaredKindData))]
        public void CreateException_DeclaredKind_ReturnsIcalFileException(object error)
        {
            // arrange
            var factory = IcalContentLineErrorFactory.Instance;

            // act
            var exception = factory.CreateException((ContentLineError)error);

            // assert
            exception.Should().BeOfType<IcalFileException>();
        }

        [Fact]
        public void CreateException_ReadError_HasBothLineNumbersAndPropertyName()
        {
            // arrange
            var factory = IcalContentLineErrorFactory.Instance;
            var expected = new Hashtable
            {
                { IcalFileException.DataItemName.LineNumber, LineNumber },
                { IcalFileException.DataItemName.PhysicalLineNumber, PhysicalLineNumber },
                { IcalFileException.DataItemName.PropertyName, PropertyName }
            };

            // act
            var exception = factory.CreateException(ReadError(ContentLineErrorKind.EmptyLine));

            // assert
            exception.Data.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void CreateException_NoPropertyName_LeavesItOut()
        {
            // arrange
            var factory = IcalContentLineErrorFactory.Instance;
            var error = new ContentLineError(ContentLineErrorKind.EmptyLine, LineNumber, PhysicalLineNumber, null);
            var expected = new Hashtable
            {
                { IcalFileException.DataItemName.LineNumber, LineNumber },
                { IcalFileException.DataItemName.PhysicalLineNumber, PhysicalLineNumber }
            };

            // act
            var exception = factory.CreateException(error);

            // assert
            exception.Data.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void CreateException_WriteError_LeavesOutPhysicalLineNumber()
        {
            // arrange
            var factory = IcalContentLineErrorFactory.Instance;
            var expected = new Hashtable
            {
                { IcalFileException.DataItemName.LineNumber, LineNumber },
                { IcalFileException.DataItemName.PropertyName, PropertyName }
            };

            // act
            var exception = factory.CreateException(WriteError(ContentLineErrorKind.LineTooLongToWrite));

            // assert
            exception.Data.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void CreateException_UnknownKind_Throws()
        {
            // arrange
            var factory = IcalContentLineErrorFactory.Instance;
            var error = new ContentLineError(UnknownKind, LineNumber, PhysicalLineNumber, PropertyName);

            // act
            Action testAction = () => factory.CreateException(error);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        private static ContentLineError ReadError(ContentLineErrorKind kind)
        {
            return new ContentLineError(kind, LineNumber, PhysicalLineNumber, PropertyName, SubjectName, ControlCharacter, MaximumLineLength);
        }

        private static ContentLineError WriteError(ContentLineErrorKind kind)
        {
            return new ContentLineError(kind, LineNumber, null, PropertyName, SubjectName, ControlCharacter, MaximumLineLength);
        }
    }
}
