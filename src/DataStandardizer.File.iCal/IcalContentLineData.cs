using DataStandardizer.File.ContentLine;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Gives <see cref="ContentLineFormatter"/> its view of an <see cref="IcalContentLine"/>, so that the public type doesn't implement an internal
    /// interface.
    /// </summary>
    internal readonly struct IcalContentLineData : IContentLineData
    {
        private readonly IcalContentLine _line;

        internal IcalContentLineData(IcalContentLine line)
        {
            _line = line;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Group => null;
#else
        public string Group => null;
#endif

        public string Name => _line.Name;

        public int ParameterCount => _line.ParameterArray.Length;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? RawParameters => _line.RawParameters;
#else
        public string RawParameters => _line.RawParameters;
#endif

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
    }
}
