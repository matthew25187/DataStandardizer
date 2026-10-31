using System.Collections.Generic;

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    /// <summary>
    /// A record of the Subtag Registry.
    /// </summary>
    /// <remarks>
    /// Records are immutable. The typed properties of each record type are parsed from its fields once, when the registry is loaded.
    /// </remarks>
    public interface ISubtagRegistryRecord
    {
        /// <summary>
        /// Gets the fields of the record, in the order they appear in the registry, as field name and field body.
        /// </summary>
        /// <remarks>
        /// A folded field body is unfolded, and a field this version doesn't know is kept under its own name, so every field of the record can be read here.
        /// </remarks>
        IReadOnlyList<KeyValuePair<string, string>> Fields { get; }
    }
}
