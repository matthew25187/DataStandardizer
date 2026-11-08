using System;

namespace DataStandardizer.Chronology
{
    /// <summary>
    /// A system time that knows the <see cref="System.DateTimeKind"/> of the <see cref="DateTime"/> it converts to.
    /// </summary>
    /// <remarks>
    /// A system time that doesn't implement this interface converts to a <see cref="DateTime"/> of kind
    /// <see cref="DateTimeKind.Unspecified"/>.
    /// </remarks>
    internal interface ISystemTimeKind
    {
        DateTimeKind DateTimeKind { get; }
    }
}
