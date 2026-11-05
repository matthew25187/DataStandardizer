using System;
using System.Collections;
using DataStandardizer.File.ContentLine;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Pins the message and <see cref="Exception.Data"/> items File.vCard gives each kind of problem the shared content line engine reports.
    /// </summary>
    public class VcardContentLineErrorFactoryTests
    {
        private const long LineNumber = 7L;
        private const long PhysicalLineNumber = 9L;
        private const string PropertyName = "NOTE";
        private const string SubjectName = "X Y";
        private const string Group = "item1";
        private const int ControlCharacter = 0x01;
        private const int MaximumLineLength = 75;

        // A value no kind has.
        private const ContentLineErrorKind UnknownKind = (ContentLineErrorKind)int.MaxValue;

        // The error is passed as an object because a test method is public and ContentLineError is internal.
        public static TheoryData<object, string> EveryKindData()
        {
            return new TheoryData<object, string>
            {
                { Error(ContentLineErrorKind.BareLineFeed), "Found a line feed without a carriage return.  RFC 6350 requires a CRLF line break." },
                { Error(ContentLineErrorKind.EmptyLine), "Found an empty content line." },
                { Error(ContentLineErrorKind.ExpectedNameEnd), "Expected ';' or ':' after the property name." },
                { Error(ContentLineErrorKind.ExpectedParameterEquals), "Expected '=' after the parameter name." },
                { Error(ContentLineErrorKind.ExpectedParameterName), "Expected a parameter name after ';'." },
                { Error(ContentLineErrorKind.ExpectedParameterValueEnd), "Expected ',', ';' or ':' after the parameter value." },
                { Error(ContentLineErrorKind.ExpectedPropertyName), "Expected a property name at the start of the content line." },
                { Error(ContentLineErrorKind.InvalidParameterValueCharacter), "Found an invalid character U+0001 in a parameter value." },
                { Error(ContentLineErrorKind.InvalidParameterValueCharacterToWrite), "Found an invalid character U+0001 in a value of parameter X Y." },
                { Error(ContentLineErrorKind.InvalidGroupToWrite), "The group \"X Y\" is not valid.  A group has one or more letters, digits or '-'." },
                { Error(ContentLineErrorKind.ParameterValueNeedsCaretEncoding), "The character U+0001 in a value of parameter X Y can only be written with RFC 6868 caret encoding, which only a vCard 4.0 card has." },
                { Error(ContentLineErrorKind.InvalidParameterNameToWrite), "The parameter name \"X Y\" is not valid.  A name has one or more letters, digits or '-'." },
                { Error(ContentLineErrorKind.InvalidPropertyNameToWrite), "The property name \"X Y\" is not valid.  A name has one or more letters, digits or '-'." },
                { Error(ContentLineErrorKind.InvalidTextEscape), "Found a backslash that does not start a valid TEXT escape sequence." },
                { Error(ContentLineErrorKind.InvalidValueCharacter), "Found an invalid character U+0001 in the property value." },
                { Error(ContentLineErrorKind.LineTooLong), "The content line is longer than the maximum of 75 characters." },
                { Error(ContentLineErrorKind.LineTooLongToWrite), "The content line is too long to write." },
                { Error(ContentLineErrorKind.MissingLineBreak), "Expected a CRLF line break at the end of the last content line." },
                { Error(ContentLineErrorKind.MissingValue), "Expected ':' and a property value." },
                { Error(ContentLineErrorKind.UnterminatedQuotedString), "Expected a closing '\"' for the quoted parameter value." }
            };
        }

        // The kinds, so that one added later without a message fails here.
        public static TheoryData<object> DeclaredKindData()
        {
            var data = new TheoryData<object>();
            foreach (ContentLineErrorKind kind in Enum.GetValues(typeof(ContentLineErrorKind)))
            {
                data.Add(Error(kind));
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(EveryKindData))]
        public void CreateException_Kind_HasMessage(object error, string expectedMessage)
        {
            // arrange
            var factory = new VcardContentLineErrorFactory(new VcardLexicalState());

            // act
            var exception = factory.CreateException((ContentLineError)error);

            // assert
            exception.Should().BeOfType<VcardFileException>().Which.Message.Should().Be(expectedMessage);
        }

        [Theory]
        [MemberData(nameof(DeclaredKindData))]
        public void CreateException_DeclaredKind_ReturnsVcardFileException(object error)
        {
            // arrange
            var factory = new VcardContentLineErrorFactory(new VcardLexicalState());

            // act
            var exception = factory.CreateException((ContentLineError)error);

            // assert
            exception.Should().BeOfType<VcardFileException>();
        }

        [Fact]
        public void CreateException_LineOutsideACard_HasLineDetailsWithoutCardIndex()
        {
            // arrange
            var factory = new VcardContentLineErrorFactory(new VcardLexicalState());
            var expected = new Hashtable
            {
                { VcardFileException.DataItemName.LineNumber, LineNumber },
                { VcardFileException.DataItemName.PhysicalLineNumber, PhysicalLineNumber },
                { VcardFileException.DataItemName.PropertyName, PropertyName },
                { VcardFileException.DataItemName.Group, Group }
            };

            // act
            var exception = factory.CreateException(Error(ContentLineErrorKind.EmptyLine));

            // assert
            exception.Data.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void CreateException_NoPropertyNameOrGroup_LeavesThemOut()
        {
            // arrange
            var factory = new VcardContentLineErrorFactory(new VcardLexicalState());
            var error = new ContentLineError(ContentLineErrorKind.EmptyLine, LineNumber, PhysicalLineNumber, null);
            var expected = new Hashtable
            {
                { VcardFileException.DataItemName.LineNumber, LineNumber },
                { VcardFileException.DataItemName.PhysicalLineNumber, PhysicalLineNumber }
            };

            // act
            var exception = factory.CreateException(error);

            // assert
            exception.Data.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void CreateException_NoPhysicalLineNumber_LeavesItOut()
        {
            // arrange
            var factory = new VcardContentLineErrorFactory(new VcardLexicalState());
            var error = new ContentLineError(ContentLineErrorKind.LineTooLongToWrite, LineNumber, null, null);
            var expected = new Hashtable
            {
                { VcardFileException.DataItemName.LineNumber, LineNumber }
            };

            // act
            var exception = factory.CreateException(error);

            // assert
            exception.Data.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void CreateException_UnknownKind_Throws()
        {
            // arrange
            var factory = new VcardContentLineErrorFactory(new VcardLexicalState());
            var error = new ContentLineError(UnknownKind, LineNumber, PhysicalLineNumber, PropertyName);

            // act
            Action testAction = () => factory.CreateException(error);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        private static ContentLineError Error(ContentLineErrorKind kind)
        {
            return new ContentLineError(kind, LineNumber, PhysicalLineNumber, PropertyName, SubjectName, ControlCharacter, MaximumLineLength, Group);
        }
    }
}
