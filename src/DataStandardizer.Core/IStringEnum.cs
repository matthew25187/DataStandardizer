using System;

namespace DataStandardizer.Core
{
    /// <summary>
    /// An enumeration of string constants.
    /// </summary>
    public interface IStringEnum : IComparable
#if NETSTANDARD1_3_OR_GREATER||NET
        , IConvertible
#endif
    {

    }
}