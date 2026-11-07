using System;

namespace DataStandardizer.Core
{
    /// <summary>
    /// An enumeration of string constants.
    /// </summary>
    /// <remarks>
    /// The members of a string enumeration are its public static fields of its own type, declared on the struct or on a public nested class at any depth. Each member's name is
    /// its field name, which has to be unique across the struct and its nested classes. <see cref="StringEnum"/> gives the full rule.
    /// </remarks>
    public interface IStringEnum : IComparable
#if NETSTANDARD1_3_OR_GREATER||NET
        , IConvertible
#endif
    {

    }
}