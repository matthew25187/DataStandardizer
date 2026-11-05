using System;
using DataStandardizer.File.ContentLine;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Turns the problems the content line engine finds into <see cref="IcalFileException"/>s.
    /// </summary>
    internal sealed class IcalContentLineErrorFactory : IContentLineErrorFactory
    {
        private static class ErrorMessage
        {
            internal const string BareLineFeed = "Found a line feed without a carriage return.  RFC 5545 requires a CRLF line break.";
            internal const string EmptyLine = "Found an empty content line.";
            internal const string ExpectedNameEnd = "Expected ';' or ':' after the property name.";
            internal const string ExpectedParameterEquals = "Expected '=' after the parameter name.";
            internal const string ExpectedParameterName = "Expected a parameter name after ';'.";
            internal const string ExpectedParameterValueEnd = "Expected ',', ';' or ':' after the parameter value.";
            internal const string ExpectedPropertyName = "Expected a property name at the start of the content line.";
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
            internal const string UnterminatedQuotedString = "Expected a closing '\"' for the quoted parameter value.";
        }

        /// <summary>
        /// Gets the factory, which holds no state.
        /// </summary>
        internal static readonly IcalContentLineErrorFactory Instance = new IcalContentLineErrorFactory();

        private IcalContentLineErrorFactory()
        {
        }

        /// <inheritdoc/>
        public Exception CreateException(ContentLineError error)
        {
            var exception = new IcalFileException(GetMessage(error));
            exception.Data.Add(IcalFileException.DataItemName.LineNumber, error.LineNumber);
            if (error.PhysicalLineNumber.HasValue)
            {
                exception.Data.Add(IcalFileException.DataItemName.PhysicalLineNumber, error.PhysicalLineNumber.Value);
            }

            if (error.PropertyName != null)
            {
                exception.Data.Add(IcalFileException.DataItemName.PropertyName, error.PropertyName);
            }

            return exception;
        }

        /// <summary>
        /// Builds an exception for a problem on a line that was read, or at a property of one.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static IcalFileException BuildException(string message, long lineNumber, long physicalLineNumber, string? propertyName)
#else
        internal static IcalFileException BuildException(string message, long lineNumber, long physicalLineNumber, string propertyName)
#endif
        {
            var exception = new IcalFileException(message);
            exception.Data.Add(IcalFileException.DataItemName.LineNumber, lineNumber);
            exception.Data.Add(IcalFileException.DataItemName.PhysicalLineNumber, physicalLineNumber);
            if (propertyName != null)
            {
                exception.Data.Add(IcalFileException.DataItemName.PropertyName, propertyName);
            }

            return exception;
        }

        internal static IcalFileException BuildInvalidTextEscapeException(long lineNumber, long physicalLineNumber, string propertyName)
        {
            return BuildException(ErrorMessage.InvalidTextEscape, lineNumber, physicalLineNumber, propertyName);
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
                case ContentLineErrorKind.UnterminatedQuotedString:
                    return ErrorMessage.UnterminatedQuotedString;
                default:
                    throw new ArgumentOutOfRangeException(nameof(error));
            }
        }
    }
}
