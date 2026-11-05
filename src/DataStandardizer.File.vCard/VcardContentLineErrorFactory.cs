﻿using System;
using DataStandardizer.File.ContentLine;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Turns the problems the content line engine finds into <see cref="VcardFileException"/>s.
    /// </summary>
    internal sealed class VcardContentLineErrorFactory : IContentLineErrorFactory
    {
        private static class ErrorMessage
        {
            internal const string DecodingFailed = "The data could not be decoded.  RFC 6350 requires UTF-8.";
            internal const string BareLineFeed = "Found a line feed without a carriage return.  RFC 6350 requires a CRLF line break.";
            internal const string EmptyLine = "Found an empty content line.";
            internal const string ExpectedNameEnd = "Expected ';' or ':' after the property name.";
            internal const string ExpectedParameterEquals = "Expected '=' after the parameter name.";
            internal const string ExpectedParameterName = "Expected a parameter name after ';'.";
            internal const string ExpectedParameterValueEnd = "Expected ',', ';' or ':' after the parameter value.";
            internal const string ExpectedPropertyName = "Expected a property name at the start of the content line.";
            internal const string InvalidGroupToWriteTemplate = "The group \"{0}\" is not valid.  A group has one or more letters, digits or '-'.";
            internal const string InvalidParameterNameToWriteTemplate = "The parameter name \"{0}\" is not valid.  A name has one or more letters, digits or '-'.";
            internal const string InvalidParameterValueCharacterTemplate = "Found an invalid character U+{0:X4} in a parameter value.";
            internal const string InvalidParameterValueCharacterToWriteTemplate = "Found an invalid character U+{0:X4} in a value of parameter {1}.";
            internal const string InvalidPropertyNameToWriteTemplate = "The property name \"{0}\" is not valid.  A name has one or more letters, digits or '-'.";
            internal const string InvalidTextEscape = "Found a backslash that does not start a valid TEXT escape sequence.";
            internal const string InvalidValueCharacterTemplate = "Found an invalid character U+{0:X4} in the property value.";
            internal const string LineTooLongTemplate = "The content line is longer than the maximum of {0} characters.";
            internal const string LineTooLongToWrite = "The content line is too long to write.";
            internal const string MissingLineBreak = "Expected a CRLF line break at the end of the last content line.";
            internal const string MissingValue = "Expected ':' and a property value.";
            internal const string ParameterValueNeedsCaretEncodingTemplate = "The character U+{0:X4} in a value of parameter {1} can only be written with RFC 6868 caret encoding, which only a vCard 4.0 card has.";
            internal const string UnterminatedQuotedString = "Expected a closing '\"' for the quoted parameter value.";
        }

        private readonly VcardLexicalState _state;

        /// <param name="state">Says which card a line that is being read is in.</param>
        internal VcardContentLineErrorFactory(VcardLexicalState state)
        {
            _state = state;
        }

        /// <inheritdoc/>
        public Exception CreateException(ContentLineError error)
        {
            var exception = new VcardFileException(GetMessage(error));
            exception.Data.Add(VcardFileException.DataItemName.LineNumber, error.LineNumber);
            if (error.PhysicalLineNumber.HasValue)
            {
                exception.Data.Add(VcardFileException.DataItemName.PhysicalLineNumber, error.PhysicalLineNumber.Value);
            }

            AddLineDetails(exception, error.PropertyName, error.Group, _state.CardIndexOfLine);
            return exception;
        }

        /// <summary>
        /// Builds the exception for data that a reader's stream reader can't decode.  The decoder works on whole blocks, ahead of the lines that have
        /// been returned, so there is no line to name.
        /// </summary>
        internal static VcardFileException BuildDecodingException(Exception innerException)
        {
            return new VcardFileException(ErrorMessage.DecodingFailed, innerException);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static VcardFileException BuildInvalidTextEscapeException(long lineNumber, long physicalLineNumber, string propertyName, string? group, int cardIndex)
#else
        internal static VcardFileException BuildInvalidTextEscapeException(long lineNumber, long physicalLineNumber, string propertyName, string group, int cardIndex)
#endif
        {
            var exception = new VcardFileException(ErrorMessage.InvalidTextEscape);
            exception.Data.Add(VcardFileException.DataItemName.LineNumber, lineNumber);
            exception.Data.Add(VcardFileException.DataItemName.PhysicalLineNumber, physicalLineNumber);
            AddLineDetails(exception, propertyName, group, cardIndex);
            return exception;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static void AddLineDetails(VcardFileException exception, string? propertyName, string? group, int cardIndex)
#else
        private static void AddLineDetails(VcardFileException exception, string propertyName, string group, int cardIndex)
#endif
        {
            if (propertyName != null)
            {
                exception.Data.Add(VcardFileException.DataItemName.PropertyName, propertyName);
            }

            if (group != null)
            {
                exception.Data.Add(VcardFileException.DataItemName.Group, group);
            }

            if (cardIndex >= 0)
            {
                exception.Data.Add(VcardFileException.DataItemName.CardIndex, cardIndex);
            }
        }

        private static string GetMessage(ContentLineError error)
        {
            switch (error.Kind)
            {
                case ContentLineErrorKind.BareLineFeed:
                    return ErrorMessage.BareLineFeed;
                case ContentLineErrorKind.EmptyLine:
                    return ErrorMessage.EmptyLine;
                case ContentLineErrorKind.ExpectedNameEnd:
                    return ErrorMessage.ExpectedNameEnd;
                case ContentLineErrorKind.ExpectedParameterEquals:
                    return ErrorMessage.ExpectedParameterEquals;
                case ContentLineErrorKind.ExpectedParameterName:
                    return ErrorMessage.ExpectedParameterName;
                case ContentLineErrorKind.ExpectedParameterValueEnd:
                    return ErrorMessage.ExpectedParameterValueEnd;
                case ContentLineErrorKind.ExpectedPropertyName:
                    return ErrorMessage.ExpectedPropertyName;
                case ContentLineErrorKind.InvalidGroupToWrite:
                    return string.Format(ErrorMessage.InvalidGroupToWriteTemplate, error.SubjectName);
                case ContentLineErrorKind.InvalidParameterNameToWrite:
                    return string.Format(ErrorMessage.InvalidParameterNameToWriteTemplate, error.SubjectName);
                case ContentLineErrorKind.InvalidParameterValueCharacter:
                    return string.Format(ErrorMessage.InvalidParameterValueCharacterTemplate, error.Character);
                case ContentLineErrorKind.InvalidParameterValueCharacterToWrite:
                    return string.Format(ErrorMessage.InvalidParameterValueCharacterToWriteTemplate, error.Character, error.SubjectName);
                case ContentLineErrorKind.InvalidPropertyNameToWrite:
                    return string.Format(ErrorMessage.InvalidPropertyNameToWriteTemplate, error.SubjectName);
                case ContentLineErrorKind.InvalidTextEscape:
                    return ErrorMessage.InvalidTextEscape;
                case ContentLineErrorKind.InvalidValueCharacter:
                    return string.Format(ErrorMessage.InvalidValueCharacterTemplate, error.Character);
                case ContentLineErrorKind.LineTooLong:
                    return string.Format(ErrorMessage.LineTooLongTemplate, error.MaximumLineLength);
                case ContentLineErrorKind.LineTooLongToWrite:
                    return ErrorMessage.LineTooLongToWrite;
                case ContentLineErrorKind.MissingLineBreak:
                    return ErrorMessage.MissingLineBreak;
                case ContentLineErrorKind.MissingValue:
                    return ErrorMessage.MissingValue;
                case ContentLineErrorKind.ParameterValueNeedsCaretEncoding:
                    return string.Format(ErrorMessage.ParameterValueNeedsCaretEncodingTemplate, error.Character, error.SubjectName);
                case ContentLineErrorKind.UnterminatedQuotedString:
                    return ErrorMessage.UnterminatedQuotedString;
                default:
                    throw new ArgumentOutOfRangeException(nameof(error));
            }
        }
    }
}
