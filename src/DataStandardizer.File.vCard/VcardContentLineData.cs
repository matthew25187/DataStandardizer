using System;
using System.Text;
using DataStandardizer.File.ContentLine;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Gives <see cref="ContentLineFormatter"/> its view of a <see cref="VcardContentLine"/>, so that the public type doesn't implement an internal
    /// interface.
    /// </summary>
    internal readonly struct VcardContentLineData : IContentLineData
    {
        private readonly VcardContentLine _line;

        internal VcardContentLineData(VcardContentLine line) : this(line, false)
        {
        }

        /// <summary>
        /// Creates the view of a line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="writesBareTypes"><c>true</c> to write each <c>TYPE</c> parameter whose one value is a <c>knowntype</c> of vCard 2.1 §2.9 as that value alone.</param>
        internal VcardContentLineData(VcardContentLine line, bool writesBareTypes)
        {
            _line = line;
            RawParameters = writesBareTypes ? FormatWithBareTypes(line) : null;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Group => _line.Group;

        // A line keeps only its decoded parameter values, so they are formatted again, other than where a bare type is written.
        public string? RawParameters { get; }
#else
        public string Group => _line.Group;

        // A line keeps only its decoded parameter values, so they are formatted again, other than where a bare type is written.
        public string RawParameters { get; }
#endif

        public string Name => _line.Name;

        public int ParameterCount => _line.ParameterArray.Length;

        public string Value => _line.Value;

        public string GetParameterName(int parameterIndex)
        {
            return _line.ParameterArray[parameterIndex].Name;
        }

        public string GetParameterValue(int parameterIndex, int valueIndex)
        {
            return _line.ParameterArray[parameterIndex].ValueArray[valueIndex];
        }

        public int GetParameterValueCount(int parameterIndex)
        {
            return _line.ParameterArray[parameterIndex].ValueArray.Length;
        }

        /// <summary>
        /// Gets the parameters of a line with each <c>knowntype</c> alone, as <c>;WORK;VOICE</c>; or <c>null</c> if the line has none, or has a parameter that the formatter has to
        /// check or quote, which it then writes with every name.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static string? FormatWithBareTypes(VcardContentLine line)
#else
        internal static string FormatWithBareTypes(VcardContentLine line)
#endif
        {
            var hasBareType = false;
            foreach (var parameter in line.ParameterArray)
            {
                if (!IsPlainParameter(parameter)) return null;

                hasBareType |= IsBareType(parameter);
            }

            if (!hasBareType) return null;

            var builder = new StringBuilder();
            foreach (var parameter in line.ParameterArray)
            {
                builder.Append(';');
                if (!IsBareType(parameter))
                {
                    builder.Append(parameter.Name).Append('=');
                }

                builder.Append(parameter.ValueArray[0]);
            }

            return builder.ToString();
        }

        private static bool IsBareType(VcardContentLineParameter parameter)
        {
            return VcardLegacyTokens.EqualsIgnoreCase(parameter.Name.AsSpan(), VcardParameterNames.Type) && VcardLegacyTokens.IsKnownType(parameter.ValueArray[0].AsSpan());
        }

        /// <summary>
        /// Gets whether a parameter has one value of printable US-ASCII that needs no quoting, so that it can be written as it is.
        /// </summary>
        private static bool IsPlainParameter(VcardContentLineParameter parameter)
        {
            if (parameter.ValueArray.Length != 1 || parameter.Name.Length == 0) return false;

            foreach (var character in parameter.Name)
            {
                if (!(character == '-' || (character >= '0' && character <= '9') || (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z'))) return false;
            }

            foreach (var character in parameter.ValueArray[0])
            {
                if (character <= ' ' || character >= '\x7F' || character == '"' || character == ';' || character == ':' || character == ',') return false;
            }

            return true;
        }
    }
}
