using System.Collections.Generic;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// The mappings that share a field with another mapping, for a mapper and a number of fields: the slot in which a line keeps the value each
    /// one's property reads, and the field each slot belongs to.
    /// </summary>
    /// <remarks>
    /// A reader's field plan builds one, and every line it reads with that number of fields shares it, so it is never changed.
    /// </remarks>
    internal sealed class CsvSharedFieldLayout
    {
        internal CsvSharedFieldLayout(Dictionary<string, int> slotsByPropertyKey, string[] fieldNames)
        {
            SlotsByPropertyKey = slotsByPropertyKey;
            FieldNames = fieldNames;
        }

        /// <summary>
        /// Gets the slot of each mapping, keyed by the mapping's key, which is the key of its property.
        /// </summary>
        internal Dictionary<string, int> SlotsByPropertyKey { get; }

        /// <summary>
        /// Gets the name of the field that each slot's mapping resolved to, by slot.
        /// </summary>
        internal string[] FieldNames { get; }
    }
}
