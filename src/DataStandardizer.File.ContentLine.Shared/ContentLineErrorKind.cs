namespace DataStandardizer.File.ContentLine
{
    /// <summary>
    /// The kinds of problem the content line engine reports to an <see cref="IContentLineErrorFactory"/>.
    /// </summary>
    /// <remarks>
    /// The members of <see cref="ContentLineError"/> a kind needs are named on it.  Every kind can also have
    /// <see cref="ContentLineError.PropertyName"/>, once the name has been read.
    /// </remarks>
    internal enum ContentLineErrorKind
    {
        /// <summary>A line feed without a carriage return, which is not accepted.</summary>
        BareLineFeed,

        /// <summary>A logical line with no characters.</summary>
        EmptyLine,

        /// <summary>The property name is followed by something other than ';' or ':'.</summary>
        ExpectedNameEnd,

        /// <summary>The parameter name is not followed by '='.</summary>
        ExpectedParameterEquals,

        /// <summary>A ';' is not followed by a parameter name.</summary>
        ExpectedParameterName,

        /// <summary>A quoted parameter value is followed by something other than ',', ';' or ':'.</summary>
        ExpectedParameterValueEnd,

        /// <summary>The line, or the part of it after a group's '.', does not start with a property name.</summary>
        ExpectedPropertyName,

        /// <summary>The group to write is not valid: <see cref="ContentLineError.SubjectName"/>.</summary>
        InvalidGroupToWrite,

        /// <summary>A parameter value read holds a character that is not allowed: <see cref="ContentLineError.Character"/>.</summary>
        InvalidParameterValueCharacter,

        /// <summary>A parameter value to write holds a character that is not allowed: <see cref="ContentLineError.Character"/>, in the parameter
        /// <see cref="ContentLineError.SubjectName"/>.</summary>
        InvalidParameterValueCharacterToWrite,

        /// <summary>A physical line can't be part of the value it continues.</summary>
        InvalidContinuationLine,

        /// <summary>The parameter name to write is not valid: <see cref="ContentLineError.SubjectName"/>.</summary>
        InvalidParameterNameToWrite,

        /// <summary>The property name to write is not valid: <see cref="ContentLineError.SubjectName"/>.</summary>
        InvalidPropertyNameToWrite,

        /// <summary>A backslash in a TEXT value does not start a valid escape sequence.</summary>
        InvalidTextEscape,

        /// <summary>The property value, read or to write, holds a character that is not allowed: <see cref="ContentLineError.Character"/>.</summary>
        InvalidValueCharacter,

        /// <summary>A logical line read is longer than the maximum: <see cref="ContentLineError.MaximumLineLength"/>.</summary>
        LineTooLong,

        /// <summary>A line is too long to fit in a buffer to write it.</summary>
        LineTooLongToWrite,

        /// <summary>The data ends inside a value that continues on later lines, before the line that ends it.</summary>
        MissingContinuationEnd,

        /// <summary>The last line of the data ends without a line break, which is not accepted.</summary>
        MissingLineBreak,

        /// <summary>The line ends before the ':' and the value.</summary>
        MissingValue,

        /// <summary>A parameter value to write holds a character that only caret encoding can represent, where caret encoding doesn't apply:
        /// <see cref="ContentLineError.Character"/>, in the parameter <see cref="ContentLineError.SubjectName"/>.</summary>
        ParameterValueNeedsCaretEncoding,

        /// <summary>A quoted parameter value has no closing quote.</summary>
        UnterminatedQuotedString
    }
}
