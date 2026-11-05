using System;
using System.Collections.Generic;
using System.Globalization;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Builds cards from content lines, checking that <c>BEGIN</c> and <c>END</c> lines are paired.  Does no I/O, so that the synchronous and asynchronous readers share it.
    /// </summary>
    internal sealed class VcardCardBuilder
    {
        // ref. RFC 6350 §6.1.1 and §6.1.2: the value of BEGIN and END, which is case-insensitive.
        internal const string CardValue = "VCARD";

        // ref. RFC 6715 §3.1 and §5, as published: the name that errata 3340 and 3341 correct to ORG-DIRECTORY.
        private const string OrgUriName = "ORG-URI";

        private readonly VcardOptions _options;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardContentLine? _beginLine;
        private VcardCard? _card;
        private VcardVersionMapping? _mapping;
#else
        private VcardContentLine _beginLine;
        private VcardCard _card;
        private VcardVersionMapping _mapping;
#endif
        private readonly VcardVersionMapping _defaultMapping;
        private readonly int _depth;
        private List<VcardDiagnostic> _structureDiagnostics = new List<VcardDiagnostic>();

        internal VcardCardBuilder(VcardOptions options) : this(options, VcardVersionMapping.Canonical, 0)
        {
        }

        /// <summary>
        /// Creates a builder.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="defaultMapping">The mapping of a card with no <c>VERSION</c>.</param>
        /// <param name="depth">How many cards the cards are nested in.</param>
        internal VcardCardBuilder(VcardOptions options, VcardVersionMapping defaultMapping, int depth)
        {
            _options = options;
            _defaultMapping = defaultMapping;
            _depth = depth;
        }

        /// <summary>
        /// Gets the number of cards completed.
        /// </summary>
        internal long CardsRead { get; private set; }

        /// <summary>
        /// Adds a line to the card being built.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>The card, if the line ends it; otherwise <c>null</c>.  Its conformance has not been checked, and <see cref="TakeStructureDiagnostics"/> gives what was found
        /// in the lines that aren't in the card.</returns>
        /// <exception cref="VcardFileException">Where the card ends can't be known: a property outside a card, an <c>END</c> with no <c>BEGIN</c>, a nested <c>BEGIN</c>, or a
        /// version that can't be read.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal VcardCard? Add(VcardContentLine line)
#else
        internal VcardCard Add(VcardContentLine line)
#endif
        {
            if (IsName(line, VcardPropertyNames.Begin))
            {
                Begin(line);
                return null;
            }

            if (IsName(line, VcardPropertyNames.End)) return End(line);

            var card = _card;
            if (card is null)
                throw BuildStructureException(Format("Found a {0} property outside a card; expected BEGIN:VCARD.", line.Name.ToUpperInvariant()), line);

            if (IsName(line, VcardPropertyNames.Version))
            {
                AddVersion(line, card);
            }
            else
            {
                card.Properties.Add(ApplyAccommodations(line));
            }

            return null;
        }

        /// <summary>
        /// Checks that the data has ended where it may.
        /// </summary>
        /// <exception cref="VcardFileException">A card has not been ended, or the data held no card.</exception>
        internal void Complete()
        {
            var beginLine = _beginLine;
            if (beginLine != null)
                throw BuildStructureException(Format("The data ended before END:VCARD; the BEGIN:VCARD on line {0} is not closed.", beginLine.LineNumber), beginLine);

            // ref. RFC 6350 §3.3: vcard-entity = 1*vcard
            if (CardsRead == 0)
            {
                var exception = new VcardFileException("The data holds no card; RFC 6350 §3.3 requires at least one.");
                exception.Data.Add(VcardFileException.DataItemName.DiagnosticKind, VcardDiagnosticKind.InvalidStructure);
                throw exception;
            }
        }

        /// <summary>
        /// Gets the problems found in the lines of the last card that aren't in the card: its <c>BEGIN</c>, <c>VERSION</c> and <c>END</c> lines.
        /// </summary>
        internal List<VcardDiagnostic> TakeStructureDiagnostics()
        {
            var diagnostics = _structureDiagnostics;
            _structureDiagnostics = new List<VcardDiagnostic>();
            return diagnostics;
        }

        /// <summary>
        /// Gives a line the form it has in a card under the options that accept another form.
        /// </summary>
        private VcardContentLine ApplyAccommodations(VcardContentLine line)
        {
            if (_options.AcceptOrgUriProperty && IsName(line, OrgUriName))
            {
                line = line.WithNameAndValue(VcardPropertyNames.OrganizationDirectory, line.Value);
            }

            return line;
        }

        /// <summary>
        /// Gives a line of a vCard 4.0 card the form it has under the option that accepts a comma with no backslash.  It waits for the end of the card, since a <c>VERSION</c> needn't come first
        /// under <see cref="VcardOptions.AllowVersionNotFirst"/>, and the rule isn't one of other versions.
        /// </summary>
        private VcardContentLine ApplyCommaAccommodation(VcardContentLine line)
        {
            if (_options.AcceptUnescapedCommasInUris && !VcardConformance.HasEncodingParameter(line) &&
                (IsName(line, VcardPropertyNames.ClientPidMap) || VcardValueTypeRegistry.GetValueType(line) == VcardValueType.Uri))
            {
                var value = VcardValue.EscapeBareCommas(line.Value);
                if (!ReferenceEquals(value, line.Value))
                {
                    line = line.WithNameAndValue(line.Name, value);
                }
            }

            return line;
        }

        private static bool IsName(VcardContentLine line, string name)
        {
            return string.Equals(line.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCardValue(VcardContentLine line)
        {
            return string.Equals(line.Value, CardValue, StringComparison.OrdinalIgnoreCase);
        }

        private static string Format(string format, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arguments);
        }

        private static VcardFileException BuildStructureException(string message, VcardContentLine line)
        {
            return VcardConformance.BuildException(new VcardDiagnostic(VcardDiagnosticKind.InvalidStructure, message, line.Name, line.Group, line.LineNumber, line.CardIndex));
        }

        private void Begin(VcardContentLine line)
        {
            if (!IsCardValue(line))
                throw BuildStructureException(Format("Found BEGIN:{0}; expected BEGIN:VCARD.", line.Value), line);

            // The reader has counted this line as the start of another card, so it can't go on with the one before.
            // ref. RFC 6350 §3.3: a vcard holds contentlines, none of which can be another vcard.
            if (_card != null)
                throw BuildStructureException("Found a BEGIN:VCARD inside a card, where RFC 6350 §3.3 doesn't allow it.  Expected END:VCARD.", line);

            _beginLine = line;
            _card = new VcardCard { CardIndex = line.CardIndex, LineNumber = line.LineNumber };
            _mapping = null;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardCard? End(VcardContentLine line)
#else
        private VcardCard End(VcardContentLine line)
#endif
        {
            var card = _card;
            var beginLine = _beginLine;
            if (card is null || beginLine is null)
                throw BuildStructureException(Format("Found END:{0} with no BEGIN:VCARD before it.", line.Value), line);
            if (!IsCardValue(line))
                throw BuildStructureException(Format("Found END:{0}, but the BEGIN:VCARD on line {1} is not closed; expected END:VCARD.", line.Value, beginLine.LineNumber), line);

            var mapping = _mapping;
            if (mapping is null)
            {
                _structureDiagnostics.Add(new VcardDiagnostic(VcardDiagnosticKind.MissingRequiredProperty, "A card requires a VERSION property: RFC 6350 §6.7.9, RFC 2426 §5.", VcardPropertyNames.Version, null,
                    beginLine.LineNumber, card.CardIndex));
                mapping = _defaultMapping;
            }

            CheckDelimiterLine(beginLine, card, mapping);
            CheckDelimiterLine(line, card, mapping);

            if (mapping.Version == VcardVersion.V4_0)
            {
                for (var index = 0; index < card.Properties.Count; index++)
                {
                    card.Properties[index] = ApplyCommaAccommodation(card.Properties[index]);
                }
            }
            else
            {
                // A vCard 4.0 card is checked in canonical form, as it is.  Any other is checked in its own form before it stops being in that form.
                mapping.Validate(card.Properties, card, _structureDiagnostics);
            }

            mapping.ToCanonical(card, _depth);

            _card = null;
            _beginLine = null;
            _mapping = null;
            CardsRead++;
            return card;
        }

        private void CheckDelimiterLine(VcardContentLine line, VcardCard card, VcardVersionMapping mapping)
        {
            // ref. RFC 6350 §3.3: vcard = "BEGIN:VCARD" CRLF "VERSION:4.0" CRLF 1*contentline "END:VCARD" CRLF.  So neither has a group or a parameter.
            // ref. RFC 2426 §4: vcard = [group "."] "BEGIN" ":" "VCARD" 1*CRLF 1*(contentline) [group "."] "END" ":" "VCARD" 1*CRLF.  So they may have a group.
            if ((mapping.Version == VcardVersion.V4_0 && line.Group != null) || line.ParameterArray.Length != 0)
            {
                _structureDiagnostics.Add(new VcardDiagnostic(VcardDiagnosticKind.InvalidStructure,
                    Format("A {0} line of this version can't have that group or parameters: RFC 6350 §3.3, RFC 2426 §4.", line.Name.ToUpperInvariant()), line.Name, line.Group, line.LineNumber, card.CardIndex));
            }
        }

        private void AddVersion(VcardContentLine line, VcardCard card)
        {
            // ref. RFC 6350 §6.7.9: "The value MUST be '4.0' if the vCard corresponds to this specification."
            var mapping = VcardVersionMapping.Find(line.Value);
            if (mapping is null)
            {
                throw VcardConformance.BuildException(new VcardDiagnostic(VcardDiagnosticKind.UnsupportedVersion,
                    Format("The card has VERSION:{0}.  Only vCard 4.0 and 3.0 can be read so far.", line.Value), line.Name, line.Group, line.LineNumber, card.CardIndex));
            }

            if (mapping.Version == VcardVersion.V4_0)
            {
                VcardConformance.ValidateVersionLine(line, card, _structureDiagnostics);

                // ref. RFC 6350 §3.3 has no group here, though §6.7.9 does allow the VALUE=text and any-param that a line can have.
                if (line.Group != null)
                {
                    _structureDiagnostics.Add(new VcardDiagnostic(VcardDiagnosticKind.InvalidStructure, "RFC 6350 §3.3 doesn't allow a group on a VERSION line.", line.Name, line.Group,
                        line.LineNumber, card.CardIndex));
                }
            }
            else
            {
                Vcard30Conformance.ValidateVersionLine(line, card, _structureDiagnostics);
            }

            if (_mapping != null)
            {
                _structureDiagnostics.Add(new VcardDiagnostic(VcardDiagnosticKind.CardinalityViolation, "A card has exactly one VERSION property: RFC 6350 §6.7.9, RFC 2426 §5.", line.Name, line.Group,
                    line.LineNumber, card.CardIndex));
                return;
            }

            // ref. RFC 6350 §6.7.9: it "must appear immediately after BEGIN:VCARD".  RFC 2426 has no such rule, since §4 has it as one contentline among others.
            if (mapping.Version == VcardVersion.V4_0 && card.Properties.Count != 0 && !_options.AllowVersionNotFirst)
            {
                _structureDiagnostics.Add(new VcardDiagnostic(VcardDiagnosticKind.InvalidStructure, "RFC 6350 §6.7.9 requires the VERSION property immediately after BEGIN:VCARD.",
                    line.Name, line.Group, line.LineNumber, card.CardIndex));
            }

            _mapping = mapping;
            card.SourceVersion = mapping.Version;
        }
    }
}
