namespace DataStandardizer.File.ContentLine
{
    /// <summary>
    /// A content line for <see cref="ContentLineFormatter"/> to format.  A package implements it with a struct around its own line type, so that no
    /// internal interface appears on a public type.
    /// </summary>
    internal interface IContentLineData
    {
        /// <summary>
        /// Gets the group, without its '.'; or <c>null</c> if the line has none.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        string? Group { get; }
#else
        string Group { get; }
#endif

        /// <summary>
        /// Gets the property name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the number of parameters.  Not used when <see cref="RawParameters"/> is set.
        /// </summary>
        int ParameterCount { get; }

        /// <summary>
        /// Gets the parameters exactly as they appeared in the source, from the first ';' up to the ':' before the value; or <c>null</c> to format
        /// them from <see cref="ParameterCount"/> and the methods that take a parameter index.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        string? RawParameters { get; }
#else
        string RawParameters { get; }
#endif

        /// <summary>
        /// Gets the value, as it appears in the content line once unfolded.
        /// </summary>
        string Value { get; }

        /// <summary>
        /// Gets the name of a parameter.
        /// </summary>
        /// <param name="parameterIndex">The index of the parameter.</param>
        string GetParameterName(int parameterIndex);

        /// <summary>
        /// Gets a value of a parameter, without quoting or caret encoding.
        /// </summary>
        /// <param name="parameterIndex">The index of the parameter.</param>
        /// <param name="valueIndex">The index of the value.</param>
        string GetParameterValue(int parameterIndex, int valueIndex);

        /// <summary>
        /// Gets the number of values of a parameter.
        /// </summary>
        /// <param name="parameterIndex">The index of the parameter.</param>
        int GetParameterValueCount(int parameterIndex);
    }
}
