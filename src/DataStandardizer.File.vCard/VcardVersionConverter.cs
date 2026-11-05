using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Says what happens to a card in another version of vCard, and converts it.
    /// </summary>
    /// <remarks>
    /// A <see cref="VcardCard"/> holds each concept in one canonical form, whatever version it was read from, so a card doesn't have to be converted to write it as another version: set
    /// <see cref="VcardOptions.WriteVersion"/>.  Use this class to find out first what would be lost, or to get a card with only what a version can hold.
    /// </remarks>
    public static class VcardVersionConverter
    {
        /// <summary>
        /// Lists what a card can't keep in a version, without changing or writing anything.
        /// </summary>
        /// <remarks>
        /// For vCard 3.0, each item is <see cref="VcardDiagnosticKind.Lossy"/>: data that <see cref="VcardWriter"/> refuses to write unless <see cref="VcardOptions.AllowLossyVersionConversion"/> is set.
        /// vCard 4.0 is the form of the model, so nothing is lost, and each item is <see cref="VcardDiagnosticKind.VersionConversion"/>: a property of an older version, such as a <c>MAILER</c>
        /// or a <c>LABEL</c> that goes with no <c>ADR</c>, which stays as a property that vCard 4.0 doesn't define.
        /// </remarks>
        /// <param name="card">The card.</param>
        /// <param name="version">The version.</param>
        /// <returns>The items, in the order of the properties.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        /// <exception cref="ArgumentException">Cards of <paramref name="version"/> can't be written yet.</exception>
        public static IReadOnlyList<VcardDiagnostic> Analyze(VcardCard card, VcardVersion version)
        {
            if (card is null)
                throw new ArgumentNullException(nameof(card));

            var mapping = VcardVersionMapping.For(version, nameof(version));
            var found = new List<VcardDiagnostic>();
            mapping.FromCanonical(card, new VcardOptions(), found);
            if (version == VcardVersion.V4_0)
            {
                AddPropertiesKeptInVersion40(card, found);
            }

            return new ReadOnlyCollection<VcardDiagnostic>(found);
        }

        /// <summary>
        /// Gets a copy of a card with only what a version can hold, with <see cref="VcardCard.SourceVersion"/> set to that version.  What is lost is lost by this call, so it throws.
        /// </summary>
        /// <param name="card">The card.</param>
        /// <param name="version">The version.</param>
        /// <returns>The copy.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        /// <exception cref="ArgumentException">Cards of <paramref name="version"/> can't be written yet.</exception>
        /// <exception cref="VcardFileException"><see cref="Analyze"/> gives a <see cref="VcardDiagnosticKind.Lossy"/> item for the card.</exception>
        public static VcardCard Convert(VcardCard card, VcardVersion version)
        {
            return Convert(card, version, new VcardOptions());
        }

        /// <summary>
        /// Gets a copy of a card with only what a version can hold, with <see cref="VcardCard.SourceVersion"/> set to that version.
        /// </summary>
        /// <param name="card">The card.</param>
        /// <param name="version">The version.</param>
        /// <param name="options">The options.  <see cref="VcardOptions.AllowLossyVersionConversion"/> says if data can be left out.</param>
        /// <returns>The copy.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        /// <exception cref="ArgumentException">Cards of <paramref name="version"/> can't be written yet.</exception>
        /// <exception cref="VcardFileException"><see cref="Analyze"/> gives a <see cref="VcardDiagnosticKind.Lossy"/> item for the card, which the options don't accept.</exception>
        public static VcardCard Convert(VcardCard card, VcardVersion version, VcardOptions options)
        {
            if (card is null)
                throw new ArgumentNullException(nameof(card));
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            var mapping = VcardVersionMapping.For(version, nameof(version));
            var found = new List<VcardDiagnostic>();
            var lines = mapping.FromCanonical(card, options, found);
            foreach (var diagnostic in found)
            {
                if (!VcardConformance.IsAccepted(diagnostic.Kind, options, true))
                    throw VcardConformance.BuildException(diagnostic);
            }

            var result = new VcardCard { SourceVersion = version };
            foreach (var line in lines)
            {
                result.Properties.Add(line);
            }

            mapping.ToCanonical(result, 0);
            return result;
        }

        private static void AddPropertiesKeptInVersion40(VcardCard card, List<VcardDiagnostic> found)
        {
            foreach (var line in card.Properties)
            {
                var info = VcardPropertyInfo.Find(line.Name);
                if (info != null && !info.IsDefinedIn(VcardVersion.V4_0))
                {
                    found.Add(new VcardDiagnostic(VcardDiagnosticKind.VersionConversion, "vCard 4.0 has no equivalent of " + info.Name + ", so it is written as a property that RFC 6350 doesn't define.",
                        line.Name, line.Group, line.LineNumber, card.CardIndex));
                }
            }
        }
    }
}
