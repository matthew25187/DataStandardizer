using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Converts the lines of one version to and from the canonical form of the model, which is the form of vCard 4.0 wherever 4.0 has an equivalent.
    /// </summary>
    /// <remarks>
    /// Each conversion rule is defined here once, and readers, writers and the versioned interfaces share it.  A version the model can read or write has one mapping, which
    /// <see cref="Find"/> gives from its <c>VERSION</c> value.
    /// </remarks>
    internal abstract class VcardVersionMapping
    {
        private static readonly VcardVersionMapping Version40Mapping = new Version40();

        /// <summary>
        /// Gets the mapping of vCard 4.0.
        /// </summary>
        internal static VcardVersionMapping Canonical => Version40Mapping;

        /// <summary>
        /// Gets the version.
        /// </summary>
        internal abstract VcardVersion Version { get; }

        /// <summary>
        /// Gets the value of the version's <c>VERSION</c> property.
        /// </summary>
        internal abstract string VersionText { get; }

        /// <summary>
        /// Gets the mapping of a version.
        /// </summary>
        /// <param name="versionText">The value of a <c>VERSION</c> property.</param>
        /// <returns>The mapping; or <c>null</c> for a version that can't be read or written.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static VcardVersionMapping? Find(string versionText)
#else
        internal static VcardVersionMapping Find(string versionText)
#endif
        {
            return string.Equals(versionText, Version40Mapping.VersionText, StringComparison.Ordinal) ? Version40Mapping : null;
        }

        /// <summary>
        /// Gets the canonical form of a line of this version.
        /// </summary>
        internal abstract VcardContentLine ToCanonical(VcardContentLine line);

        /// <summary>
        /// Gets the line of this version for a line in canonical form.
        /// </summary>
        internal abstract VcardContentLine FromCanonical(VcardContentLine line);

        /// <summary>
        /// vCard 4.0, whose lines are in the canonical form already.
        /// </summary>
        private sealed class Version40 : VcardVersionMapping
        {
            // ref. RFC 6350 §6.7.9: VERSION-value = "4.0"
            internal override string VersionText => "4.0";

            internal override VcardVersion Version => VcardVersion.V4_0;

            internal override VcardContentLine ToCanonical(VcardContentLine line)
            {
                return line;
            }

            internal override VcardContentLine FromCanonical(VcardContentLine line)
            {
                return line;
            }
        }
    }
}
