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

        internal VcardContentLineData(VcardContentLine line)
        {
            _line = line;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Group => _line.Group;

        // A line keeps only its decoded parameter values, so they are always formatted again.
        public string? RawParameters => null;
#else
        public string Group => _line.Group;

        // A line keeps only its decoded parameter values, so they are always formatted again.
        public string RawParameters => null;
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
    }
}
