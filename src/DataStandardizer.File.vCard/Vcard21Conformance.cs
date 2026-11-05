#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#nullable disable
#endif
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Checks the lines of a vCard 2.1 card, in the form of that version, against the vCard 2.1 specification of 1996.
    /// </summary>
    /// <remarks>
    /// The grammar of §2.9 has a closed list of property names, and an extension's name starts with <c>X-</c> (§2.8.1).  Its parameters are <c>TYPE</c>, <c>VALUE</c>, <c>ENCODING</c>,
    /// <c>CHARSET</c>, <c>LANGUAGE</c>, an <c>X-</c> parameter and a <c>knowntype</c> alone, each of which "may be used on any property".  Each parameter has one value.
    /// </remarks>
    internal static class Vcard21Conformance
    {
        // ref. vCard 2.1 §2.1.8 and §2.9: pvalueval.
        internal const string UrlValue = "URL";
        internal const string ContentIdValue = "CONTENT-ID";
        internal const string ShortContentIdValue = "CID";

        private static readonly string[] SingleParameters =
        {
            VcardParameterNames.Value, VcardLegacyTokens.EncodingName, VcardLegacyTokens.CharsetName, VcardParameterNames.Language
        };

        /// <summary>
        /// Checks the lines of a card.
        /// </summary>
        internal static void Validate(IList<VcardContentLine> lines, VcardCard card, VcardOptions options, int depth, List<VcardDiagnostic> found)
        {
            var hasName = false;
            foreach (var line in lines)
            {
                if (IsStructuralName(line.Name))
                {
                    Add(found, VcardDiagnosticKind.InvalidStructure, Format("A {0} line can't be in the properties of a card; a writer writes it.", line.Name), line, card);
                    continue;
                }

                hasName |= IsName(line, VcardPropertyNames.Name);
                var isDefined = IsDefinedProperty(line.Name);
                if (!isDefined && !VcardLegacyTokens.IsExtensionWord(line.Name))
                {
                    // ref. vCard 2.1 §2.9: name = "LOGO" / "PHOTO" / ... / "X-" word, and the items of ADR, ORG, N and AGENT.
                    Add(found, VcardDiagnosticKind.InvalidValue, Format("vCard 2.1 §2.9 doesn't define the property {0}, and an extension's name starts with X- (§2.8.1).", line.Name), line, card);
                }

                CheckParameters(line, card, found);
                CheckValue(line, isDefined, card, found);
            }

            // ref. vCard 2.1 §2.2.2: "Support for this property is mandatory", and "All vCard data streams should include this property".
            if (!hasName)
            {
                found.Add(new VcardDiagnostic(VcardDiagnosticKind.MissingRequiredProperty, "vCard 2.1 §2.2.2 requires an N property.", VcardPropertyNames.Name, null, card.LineNumber, card.CardIndex));
            }
        }

        /// <summary>
        /// Gets whether vCard 2.1 defines a property, other than the <c>BEGIN</c>, <c>VERSION</c> and <c>END</c> that a writer writes.
        /// </summary>
        internal static bool IsDefinedProperty(string name)
        {
            var info = VcardPropertyInfo.Find(name);
            return info != null && info.IsDefinedIn(VcardVersion.V2_1) && !IsStructuralName(name);
        }

        /// <summary>
        /// Gets whether a property's value is a picture, a sound or a key, which can be binary (vCard 2.1 §2.1.9).
        /// </summary>
        internal static bool IsMediaProperty(string name)
        {
            return string.Equals(name, VcardPropertyNames.Photo, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, VcardPropertyNames.Logo, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, VcardPropertyNames.Sound, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, VcardPropertyNames.Key, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets whether a value location is a Content-ID (vCard 2.1 §2.1.8: '"Content-ID" value, or "CID" for short').
        /// </summary>
        internal static bool IsContentId(string location)
        {
            return string.Equals(location, ContentIdValue, StringComparison.OrdinalIgnoreCase) || string.Equals(location, ShortContentIdValue, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets whether a value location is a URL.
        /// </summary>
        internal static bool IsUrl(string location)
        {
            return string.Equals(location, UrlValue, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the value location of a line; or <c>null</c> if it has none, or says <c>INLINE</c>, which is the default.
        /// </summary>
        internal static string GetValueLocation(VcardContentLine line)
        {
            var location = VcardPropertyParameterReader.GetValue(line, VcardParameterNames.Value);
            return location is null || string.Equals(location, VcardLegacyTokens.InlineValue, StringComparison.OrdinalIgnoreCase) ? null : location;
        }

        /// <summary>
        /// Gets the value of a line as text, decoded by its <c>ENCODING</c> and <c>CHARSET</c>, or by the defaults of vCard 2.1 (§2.1.5, §2.1.6).
        /// </summary>
        /// <returns><c>false</c> if the value can't be decoded.</returns>
        internal static bool TryGetText(VcardContentLine line, out string text)
        {
            try
            {
                text = line.GetDecodedText();
                return true;
            }
            catch (VcardFileException)
            {
                text = null;
                return false;
            }
        }

        /// <summary>
        /// Gets the octets of a line that is binary: a picture, a sound or a key encoded with <c>BASE64</c>, or with <c>QUOTED-PRINTABLE</c> and a <c>TYPE</c> that names a format.
        /// </summary>
        /// <returns><c>false</c> if the line isn't binary, or its value can't be decoded.</returns>
        internal static bool TryGetBinary(VcardContentLine line, out byte[] data)
        {
            data = null;
            if (!IsBinary(line)) return false;

            try
            {
                data = line.GetDecodedValue();
                return true;
            }
            catch (VcardFileException)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets whether a line holds binary data inline.  ref. vCard 2.1 §2.1.9: "Inline binary information is included as a property value after being encoded into clear-text with a Base 64
        /// (default) or Quoted-Printable encoding".  Quoted-printable is also how text is written, so it is binary only where a <c>TYPE</c> names a format.
        /// </summary>
        internal static bool IsBinary(VcardContentLine line)
        {
            if (!IsMediaProperty(line.Name) || GetValueLocation(line) != null) return false;

            var encoding = VcardPropertyParameterReader.GetValue(line, VcardLegacyTokens.EncodingName);
            if (string.Equals(encoding, VcardLegacyTokens.Base64, StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(encoding, VcardLegacyTokens.QuotedPrintable, StringComparison.OrdinalIgnoreCase)) return false;

            foreach (var type in VcardPropertyParameterReader.GetListValues(line, VcardParameterNames.Type))
            {
                Communication.Mime.MimeMediaType mediaType;
                if (VcardLegacyMediaTypes.TryGetMediaType(type.AsSpan(), out mediaType)) return true;
            }

            return false;
        }

        private static string Format(string format, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arguments);
        }

        private static void Add(List<VcardDiagnostic> found, VcardDiagnosticKind kind, string message, VcardContentLine line, VcardCard card)
        {
            found.Add(new VcardDiagnostic(kind, message, line.Name, line.Group, line.LineNumber, card.CardIndex));
        }

        private static bool IsName(VcardContentLine line, string name)
        {
            return string.Equals(line.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStructuralName(string name)
        {
            return string.Equals(name, VcardPropertyNames.Begin, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, VcardPropertyNames.End, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, VcardPropertyNames.Version, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsParameter(VcardContentLineParameter parameter, string name)
        {
            return string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        private static void CheckParameters(VcardContentLine line, VcardCard card, List<VcardDiagnostic> found)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var parameter in line.ParameterArray)
            {
                var isRepeated = !names.Add(parameter.Name);

                // ref. vCard 2.1 §2.9: each param has one value.
                if (parameter.ValueArray.Length > 1)
                {
                    Add(found, VcardDiagnosticKind.InvalidParameter, Format("vCard 2.1 §2.9 has one value in a parameter, and {0} has {1}.", parameter.Name, parameter.ValueArray.Length), line, card);
                    continue;
                }

                var value = parameter.ValueArray[0];
                if (!IsValidParameter(parameter, value))
                {
                    Add(found, VcardDiagnosticKind.InvalidParameter, Format("vCard 2.1 §2.9 doesn't allow the parameter {0}={1}.", parameter.Name, value), line, card);
                    continue;
                }

                if (isRepeated && Array.Exists(SingleParameters, name => IsParameter(parameter, name)))
                {
                    Add(found, VcardDiagnosticKind.InvalidParameter, Format("The parameter {0} can appear only once.", parameter.Name), line, card);
                }
            }
        }

        private static bool IsValidParameter(VcardContentLineParameter parameter, string value)
        {
            // ref. vCard 2.1 §2.9: ptypeval = knowntype / "X-" word, and the same for the other parameters, with the values of each.
            if (IsParameter(parameter, VcardParameterNames.Type)) return VcardLegacyTokens.IsKnownType(value.AsSpan()) || VcardLegacyTokens.IsExtensionWord(value);
            if (IsParameter(parameter, VcardParameterNames.Value)) return VcardLegacyTokens.IsValueLocation(value.AsSpan()) || VcardLegacyTokens.IsExtensionWord(value);
            if (IsParameter(parameter, VcardLegacyTokens.EncodingName)) return VcardLegacyTokens.IsEncodingName(value.AsSpan()) || VcardLegacyTokens.IsExtensionWord(value);

            // ref. vCard 2.1 §2.9: charsetval is "a character set string as defined in Section 7.1 of RFC 1521", whose charset is a token, and langval is of RFC 1766.
            if (IsParameter(parameter, VcardLegacyTokens.CharsetName)) return VcardLegacyTokens.IsWord(value);
            if (IsParameter(parameter, VcardParameterNames.Language)) return Vcard30Conformance.IsRfc1766Tag(value);

            // ref. vCard 2.1 §2.9: "X-" word [ws] "=" [ws] word.
            return VcardLegacyTokens.IsExtensionWord(parameter.Name) && VcardLegacyTokens.IsWord(value);
        }

        private static void CheckValue(VcardContentLine line, bool isDefined, VcardCard card, List<VcardDiagnostic> found)
        {
            if (!IsValidValue(line, isDefined))
            {
                Add(found, VcardDiagnosticKind.InvalidValue, Format("The value of {0} is not valid in vCard 2.1.", line.Name), line, card);
            }
        }

        private static bool IsValidValue(VcardContentLine line, bool isDefined)
        {
            if (IsName(line, VcardPropertyNames.Agent)) return IsValidAgent(line);

            var location = GetValueLocation(line);
            if (IsMediaProperty(line.Name) && location is null)
            {
                byte[] data;
                if (IsBinary(line)) return TryGetBinary(line, out data);

                // ref. vCard 2.1 §2.6.3 and §2.7.1: a sound can be "a string representation", and a key's "default format is clear-text".  A picture is an image.
                if (IsName(line, VcardPropertyNames.Photo) || IsName(line, VcardPropertyNames.Logo)) return false;
            }

            string text;
            if (!TryGetText(line, out text)) return false;

            // ref. vCard 2.1 §2.1.8: "the property value is the Uniform Resource Locator", or "the Content-ID for the MIME entity".
            if (location != null)
            {
                if (IsUrl(location)) return VcardValue.IsValidUriText(text.AsSpan());

                return !IsContentId(location) || text.Length != 0;
            }

            if (!isDefined) return VcardText.IsValidText(text.AsSpan(), VcardVersion.V2_1);

            var value = text.AsSpan();
            VcardStructuredValueView view;
            switch (line.Name.ToUpperInvariant())
            {
                case VcardPropertyNames.Name:
                    return VcardName.TryParseView(value, VcardValueFormat.Text, VcardVersion.V2_1, out view);
                case VcardPropertyNames.Address:
                    return VcardAddress.TryParseView(value, VcardValueFormat.Text, VcardVersion.V2_1, out view);
                case VcardPropertyNames.Organization:
                    return VcardOrganization.TryParseView(value, VcardValueFormat.Text, VcardVersion.V2_1, out view);
                case VcardPropertyNames.Birthday:
                    VcardDate date;
                    return VcardDate.TryParse(value, VcardValueFormat.Text, VcardVersion.V2_1, out date);
                case VcardPropertyNames.Revision:
                    VcardTimestamp timestamp;
                    return VcardTimestamp.TryParse(value, VcardValueFormat.Text, VcardVersion.V2_1, out timestamp);
                case VcardPropertyNames.TimeZone:
                    VcardUtcOffset offset;
                    return VcardUtcOffset.TryParse(value, VcardValueFormat.Text, VcardVersion.V2_1, out offset);
                case VcardPropertyNames.Geo:
                    VcardGeo geo;
                    return VcardGeo.TryParse(value, VcardValueFormat.Text, VcardVersion.V2_1, out geo);
                case VcardPropertyNames.Url:
                    // ref. vCard 2.1 §2.6.4: "a string conforming to the IETF RFC 1738".
                    return VcardValue.IsValidUriText(value);
                default:
                    return VcardText.IsValidText(value, VcardVersion.V2_1);
            }
        }

        private static bool IsValidAgent(VcardContentLine line)
        {
            // ref. vCard 2.1 §2.5.4: "The value of this property is a string containing another vCard object", which the reader has read as the lines after it.
            var location = GetValueLocation(line);
            if (location is null) return line.HeldCard != null;

            string text;
            if (!TryGetText(line, out text)) return false;

            if (IsUrl(location)) return VcardValue.IsValidUriText(text.AsSpan());

            return !IsContentId(location) || text.Length != 0;
        }
    }
}
