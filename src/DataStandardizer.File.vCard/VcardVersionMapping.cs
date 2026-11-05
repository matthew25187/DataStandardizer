using System;
using System.Collections.Generic;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Converts the lines of one version to and from the canonical form of the model, which is the form of vCard 4.0 wherever 4.0 has an equivalent, and checks them by that version's rules.
    /// </summary>
    /// <remarks>
    /// Each conversion rule is defined here once, and readers, writers and the versioned interfaces share it.  A version the model can read or write has one mapping, which
    /// <see cref="Find"/> gives from its <c>VERSION</c> value.  A conversion works on a whole card, since one property of a version can become part of another in canonical form.
    /// </remarks>
    internal abstract class VcardVersionMapping
    {
        private static readonly VcardVersionMapping Version40Mapping = new Version40();
        private static readonly VcardVersionMapping Version30Mapping = new VcardVersion30Mapping();
        private static readonly VcardVersionMapping Version21Mapping = new VcardVersion21Mapping();

        /// <summary>
        /// Gets the mapping of vCard 4.0.
        /// </summary>
        internal static VcardVersionMapping Canonical => Version40Mapping;

        /// <summary>
        /// Gets the mapping of vCard 3.0.
        /// </summary>
        internal static VcardVersionMapping Version30 => Version30Mapping;

        /// <summary>
        /// Gets the mapping of vCard 2.1.
        /// </summary>
        internal static VcardVersionMapping Version21 => Version21Mapping;

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
            if (string.Equals(versionText, Version40Mapping.VersionText, StringComparison.Ordinal)) return Version40Mapping;

            if (string.Equals(versionText, Version30Mapping.VersionText, StringComparison.Ordinal)) return Version30Mapping;

            return string.Equals(versionText, Version21Mapping.VersionText, StringComparison.Ordinal) ? Version21Mapping : null;
        }

        /// <summary>
        /// Gets the mapping of a version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="parameterName">The name of the parameter that gave the version.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>

        internal static VcardVersionMapping For(VcardVersion version, string parameterName)
        {
            switch (version)
            {
                case VcardVersion.V4_0:
                    return Version40Mapping;
                case VcardVersion.V3_0:
                    return Version30Mapping;
                case VcardVersion.V2_1:
                    return Version21Mapping;
                default:
                    throw new ArgumentOutOfRangeException(parameterName, version, "The version is not recognised.");
            }
        }

        /// <summary>
        /// Checks lines of this version by its rules.
        /// </summary>
        /// <param name="lines">The lines, in the form of this version.</param>
        /// <param name="card">The card they are of.</param>
        /// <param name="options">The options, which also apply to a card that a line holds.</param>
        /// <param name="depth">How many cards this one is nested in.</param>
        /// <param name="found">Receives the problems found.</param>
        internal abstract void Validate(IList<VcardContentLine> lines, VcardCard card, VcardOptions options, int depth, List<VcardDiagnostic> found);

        /// <summary>
        /// Changes the properties of a card, which are in the form of this version, to their canonical form.  A line that isn't valid is left as it is.
        /// </summary>
        /// <param name="card">The card.</param>
        /// <param name="options">The options, which also apply to a card that a line holds.</param>
        /// <param name="depth">How many cards this one is nested in.</param>
        internal abstract void ToCanonical(VcardCard card, VcardOptions options, int depth);

        /// <summary>
        /// Gets the lines of this version for the properties of a card, which are in canonical form.
        /// </summary>
        /// <param name="card">The card, which isn't changed.</param>
        /// <param name="options">The options.</param>
        /// <param name="found">Receives what the version can't hold, each as a <see cref="VcardDiagnosticKind.Lossy"/> diagnostic, and what a card held by a line lacks.</param>
        /// <returns>The lines, without what the version can't hold.</returns>
        internal abstract List<VcardContentLine> FromCanonical(VcardCard card, VcardOptions options, List<VcardDiagnostic> found);

        /// <summary>
        /// vCard 4.0, whose lines are in the canonical form already.
        /// </summary>
        private sealed class Version40 : VcardVersionMapping
        {
            // ref. RFC 6350 §6.7.9: VERSION-value = "4.0"
            internal override string VersionText => "4.0";

            internal override VcardVersion Version => VcardVersion.V4_0;

            internal override void Validate(IList<VcardContentLine> lines, VcardCard card, VcardOptions options, int depth, List<VcardDiagnostic> found)
            {
                VcardConformance.Validate(card, found);
            }

            internal override void ToCanonical(VcardCard card, VcardOptions options, int depth)
            {
            }

            internal override List<VcardContentLine> FromCanonical(VcardCard card, VcardOptions options, List<VcardDiagnostic> found)
            {
                return new List<VcardContentLine>(card.Properties);
            }
        }
    }
}
