using System;

namespace DataStandardizer.Core
{
    /// <summary>
    /// An enumeration of string constants.
    /// </summary>
    /// <remarks>
    /// The members of a string enumeration are its public static fields of its own type, declared on the struct or on a public nested class at any depth. Each member's name is
    /// its field name, which has to be unique across the struct and its nested classes. <see cref="StringEnum"/> gives the full rule.
    /// <para>
    /// The interface extends <see cref="IComparable"/> only, on every target, so a struct built against any build of this library loads with every other build. A struct
    /// that also implements <c>IConvertible</c> declares it itself, on the targets that have it.
    /// </para>
    /// </remarks>
    public interface IStringEnum : IComparable
    {

    }
}