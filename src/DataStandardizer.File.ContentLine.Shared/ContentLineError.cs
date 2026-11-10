namespace DataStandardizer.File.ContentLine
{
    /// <summary>
    /// A problem the content line engine found, with what an <see cref="IContentLineErrorFactory"/> needs to describe it.
    /// </summary>
    internal readonly struct ContentLineError
    {
        /// <param name="kind">The kind of problem.</param>
        /// <param name="lineNumber">The 1-based number of the logical line.</param>
        /// <param name="physicalLineNumber">The 1-based number of the physical line; or <c>null</c> for a line being written, which has none yet.</param>
        /// <param name="propertyName">The name of the property on the line; or <c>null</c> if it has not been read or is not valid.</param>
        /// <param name="subjectName">The name that <paramref name="kind"/> is about, if it is about one.</param>
        /// <param name="character">The character that <paramref name="kind"/> is about, if it is about one.</param>
        /// <param name="maximumLineLength">The limit that <paramref name="kind"/> is about, if it is about one.</param>
        /// <param name="group">The group of the line; or <c>null</c> if it has none or has not been read.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal ContentLineError(ContentLineErrorKind kind, long lineNumber, long? physicalLineNumber, string? propertyName, string? subjectName = null,
            int character = 0, int maximumLineLength = 0, string? group = null)
#else
        internal ContentLineError(ContentLineErrorKind kind, long lineNumber, long? physicalLineNumber, string propertyName, string subjectName = null,
            int character = 0, int maximumLineLength = 0, string group = null)
#endif
        {
            Kind = kind;
            LineNumber = lineNumber;
            PhysicalLineNumber = physicalLineNumber;
            PropertyName = propertyName;
            SubjectName = subjectName;
            Character = character;
            MaximumLineLength = maximumLineLength;
            Group = group;
        }

        /// <summary>
        /// Gets the character the problem is about, as its UTF-16 code unit.
        /// </summary>
        internal int Character { get; }

        /// <summary>
        /// Gets the kind of problem.
        /// </summary>
        internal ContentLineErrorKind Kind { get; }

        /// <summary>
        /// Gets the 1-based number of the logical line.
        /// </summary>
        internal long LineNumber { get; }

        /// <summary>
        /// Gets the limit the problem is about.
        /// </summary>
        internal int MaximumLineLength { get; }

        /// <summary>
        /// Gets the 1-based number of the physical line; or <c>null</c> for a line being written, which has none yet.
        /// </summary>
        internal long? PhysicalLineNumber { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the group of the line; or <c>null</c> if it has none or has not been read.
        /// </summary>
        internal string? Group { get; }

        /// <summary>
        /// Gets the name of the property on the line; or <c>null</c> if it has not been read or is not valid.
        /// </summary>
        internal string? PropertyName { get; }

        /// <summary>
        /// Gets the name the problem is about: a property or parameter name that is not valid, or the parameter whose value is at fault.
        /// </summary>
        internal string? SubjectName { get; }
#else
        /// <summary>
        /// Gets the group of the line; or <c>null</c> if it has none or has not been read.
        /// </summary>
        internal string Group { get; }

        /// <summary>
        /// Gets the name of the property on the line; or <c>null</c> if it has not been read or is not valid.
        /// </summary>
        internal string PropertyName { get; }

        /// <summary>
        /// Gets the name the problem is about: a property or parameter name that is not valid, or the parameter whose value is at fault.
        /// </summary>
        internal string SubjectName { get; }
#endif
    }
}
