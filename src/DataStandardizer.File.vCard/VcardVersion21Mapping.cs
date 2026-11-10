#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#nullable disable
#endif
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The mapping of vCard 2.1, the versit specification of 1996.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A vCard 2.1 value has no escaping, other than <c>\;</c> in a compound value, and is decoded by its <c>ENCODING</c> and <c>CHARSET</c> before it is converted.  A <c>knowntype</c> is a
    /// <c>TYPE</c> value in lower case, as RFC 6350 writes them, and <c>PREF</c> is <c>PREF=1</c>.  An <c>AGENT</c> holds the card that follows its line, which is held as
    /// <c>RELATED;VALUE=text;TYPE=agent</c>, as an <c>AGENT</c> of vCard 3.0 is.
    /// </para>
    /// <para>
    /// A value is written in quoted-printable where it holds a character that isn't printable US-ASCII, with <c>CHARSET=UTF-8</c> if it holds one outside US-ASCII, or where its line would
    /// be longer than 75 characters.  A writer never folds a vCard 2.1 line, since vCard 2.1 and later versions unfold differently, and breaks a quoted-printable value with soft line
    /// breaks instead.  Binary data is written in <c>BASE64</c>.
    /// </para>
    /// </remarks>
    internal sealed class VcardVersion21Mapping : VcardVersionMapping
    {
        // ref. vCard 2.1 §2.4.1: the type that says a telephone number is preferred, which the grammar of §2.9 allows on any property.
        private const string PreferredType = "PREF";

        // ref. RFC 6350 §5.3: 1 is the most preferred.
        private const string MostPreferred = "1";

        // ref. RFC 6350 §6.6.6: type-param-related.
        private const string AgentType = "agent";

        // ref. RFC 2392 §2: a "cid" URL of a Content-ID.
        private const string ContentIdPrefix = "cid:";

        private const string TelPrefix = "tel:";
        private const string XNamePrefix = "X-";
        private const string Utf8Charset = "UTF-8";

        // ref. vCard 2.1 §2.1.3: "Quoted-Printable lines of text must also be limited to less than 76 characters", which a longer line is broken into instead of folded.
        private const int MaxPlainLineLength = 75;

        // Nothing sets a limit.  It matches that of a nested card read from text.
        private const int MaxNestingDepth = 8;

        private const string HexDigits = "0123456789ABCDEF";

        private static readonly MimeMediaType OctetStream = new MimeMediaType("application", "octet-stream");

        // ref. vCard 2.1 §2.6.6: "The value of this property must be 2.1".
        internal override string VersionText => "2.1";

        internal override VcardVersion Version => VcardVersion.V2_1;

        internal override void Validate(IList<VcardContentLine> lines, VcardCard card, VcardOptions options, int depth, List<VcardDiagnostic> found)
        {
            Vcard21Conformance.Validate(lines, card, options, depth, found);
        }

        internal override void ToCanonical(VcardCard card, VcardOptions options, int depth)
        {
            for (var index = 0; index < card.Properties.Count; index++)
            {
                card.Properties[index] = ToCanonicalLine(card.Properties[index]);
            }

            VcardVersion30Mapping.AttachLabels(card);
        }

        internal override List<VcardContentLine> FromCanonical(VcardCard card, VcardOptions options, List<VcardDiagnostic> found)
        {
            return new Downgrade(card, options, found, 0).Run();
        }

        /// <summary>
        /// Gets the lines to write for lines of vCard 2.1, with the lines of each card an <c>AGENT</c> holds after it, ending with its <c>END:VCARD</c>.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="options">The options.</param>
        internal static List<VcardContentLine> ExpandHeldCards(List<VcardContentLine> lines, VcardOptions options)
        {
            if (!lines.Exists(line => line.HeldCard != null)) return lines;

            var result = new List<VcardContentLine>(lines.Count);
            foreach (var line in lines)
            {
                result.Add(line);
                if (line.HeldCard is null) continue;

                // What it can't hold has been reported on the AGENT, so it isn't reported again.
                var held = new Downgrade(line.HeldCard, options, new List<VcardDiagnostic>(), 1).Run();
                result.Add(new VcardContentLine(VcardPropertyNames.Version, VcardVersionMapping.Version21.VersionText));
                result.AddRange(ExpandHeldCards(held, options));
                result.Add(new VcardContentLine(VcardPropertyNames.End, VcardCardBuilder.CardValue));
            }

            return result;
        }

        /// <summary>
        /// Gets the <c>cid</c> URI (RFC 2392 §2) of a Content-ID, with or without its angle brackets.
        /// </summary>
        /// <returns>The URI; or <c>null</c> if the Content-ID is empty.</returns>
        internal static string ContentIdToUri(string contentId)
        {
            var id = contentId;
            if (id.Length >= 2 && id[0] == '<' && id[id.Length - 1] == '>')
            {
                id = id.Substring(1, id.Length - 2);
            }

            if (id.Length == 0) return null;

            // ref. RFC 2392 §2: "%" hex-encodes what a URL can't hold, and is the reverse of how a Content-ID is got from one.
            var builder = new StringBuilder(ContentIdPrefix, ContentIdPrefix.Length + id.Length);
            foreach (var octet in Encoding.UTF8.GetBytes(id))
            {
                var character = (char)octet;
                // A '#' or '?' would start the fragment or query of the URI (RFC 3986 §3), so it is encoded too.
                if (octet > ' ' && octet < 0x7F && "\"#%<>?\\^`{|}".IndexOf(character) < 0)
                {
                    builder.Append(character);
                }
                else
                {
                    builder.Append('%').Append(HexDigits[octet >> 4]).Append(HexDigits[octet & 0xF]);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets the Content-ID, in angle brackets, of a <c>cid</c> URI.
        /// </summary>
        /// <returns>The Content-ID; or <c>null</c> if the text isn't a <c>cid</c> URI.</returns>
        internal static string UriToContentId(string uriText)
        {
            if (!uriText.StartsWith(ContentIdPrefix, StringComparison.OrdinalIgnoreCase) || uriText.Length == ContentIdPrefix.Length) return null;

            // ref. RFC 2392 §2: the reverse of ContentIdToUri.
            return "<" + Uri.UnescapeDataString(uriText.Substring(ContentIdPrefix.Length)) + ">";
        }

        private static bool IsName(VcardContentLine line, string name)
        {
            return string.Equals(line.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsParameterName(VcardContentLineParameter parameter, string name)
        {
            return string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStructural(VcardContentLine line)
        {
            return IsName(line, VcardPropertyNames.Begin) || IsName(line, VcardPropertyNames.End) || IsName(line, VcardPropertyNames.Version);
        }

        private static VcardContentLine ToCanonicalLine(VcardContentLine line)
        {
            if (IsStructural(line)) return line;
            if (IsName(line, VcardPropertyNames.Agent)) return AgentToCanonical(line);

            var parameters = new List<VcardContentLineParameter>(line.ParameterArray);
            var location = Vcard21Conformance.GetValueLocation(line);
            if (Vcard21Conformance.IsMediaProperty(line.Name)) return MediaToCanonical(line, parameters, location);

            string text;
            if (!Vcard21Conformance.TryGetText(line, out text)) return line;

            // A VALUE=URL or VALUE=CID on a property vCard 4.0 has no URI for stays in the form of vCard 2.1, since §2.1.8 lets it be "used on any property".
            if (location != null && !IsName(line, VcardPropertyNames.Url) && !IsName(line, VcardPropertyNames.Uid)) return line;

            RemoveEncoding(parameters);
            VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
            ConvertTypes(line.Name, parameters);

            var value = ValueToCanonical(line, parameters, text, location);
            return value is null ? line : line.With(line.Name, parameters.ToArray(), value);
        }

        /// <summary>
        /// Gets the canonical value of a property that isn't a picture, a sound, a key or an agent.
        /// </summary>
        /// <returns>The value; or <c>null</c> if it isn't valid, so the line stays as it is.</returns>
        private static string ValueToCanonical(VcardContentLine line, List<VcardContentLineParameter> parameters, string text, string location)
        {
            var value = text.AsSpan();
            switch (line.Name.ToUpperInvariant())
            {
                case VcardPropertyNames.Name:
                    VcardName name;
                    return VcardName.TryParse(value, VcardValueFormat.Text, VcardVersion.V2_1, out name) ? name.ToString(VcardValueFormat.Text, VcardVersion.V4_0) : null;
                case VcardPropertyNames.Address:
                    VcardAddress address;
                    return VcardAddress.TryParse(value, VcardValueFormat.Text, VcardVersion.V2_1, out address) ? address.ToString(VcardValueFormat.Text, VcardVersion.V4_0) : null;
                case VcardPropertyNames.Organization:
                    VcardOrganization organization;
                    return VcardOrganization.TryParse(value, VcardValueFormat.Text, VcardVersion.V2_1, out organization)
                        ? organization.ToString(VcardValueFormat.Text, VcardVersion.V4_0)
                        : null;
                case VcardPropertyNames.Birthday:
                    VcardDate date;
                    return VcardDate.TryParse(value, VcardValueFormat.Text, VcardVersion.V2_1, out date) ? date.ToString(VcardValueFormat.Text, VcardVersion.V4_0) : null;
                case VcardPropertyNames.Revision:
                    VcardTimestamp timestamp;
                    return VcardTimestamp.TryParse(value, VcardValueFormat.Text, VcardVersion.V2_1, out timestamp) ? timestamp.ToString(VcardValueFormat.Text, VcardVersion.V4_0) : null;
                case VcardPropertyNames.TimeZone:
                    // ref. vCard 2.1 §2.4.5: an offset from UTC; RFC 6350 §6.5.1: 'The default is a single text value', so an offset says what it is.
                    VcardUtcOffset offset;
                    if (!VcardUtcOffset.TryParse(value, VcardValueFormat.Text, VcardVersion.V2_1, out offset)) return null;

                    parameters.Insert(0, new VcardContentLineParameter(VcardParameterNames.Value, VcardValueTypeRegistry.GetValueTypeName(VcardValueType.UtcOffset)));
                    return offset.ToString(VcardValueFormat.Text, VcardVersion.V4_0);
                case VcardPropertyNames.Geo:
                    VcardGeo geo;
                    return VcardGeo.TryParse(value, VcardValueFormat.Text, VcardVersion.V2_1, out geo) ? geo.ToString(VcardValueFormat.Text, VcardVersion.V4_0) : null;
                case VcardPropertyNames.Url:
                    // ref. RFC 6350 §3.4 and its errata 3845 and 3846: a comma in a URI value is escaped in vCard 4.0.
                    return VcardValue.IsValidUriText(value) ? VcardValue.EscapeUri(text) : null;
                case VcardPropertyNames.Uid:
                    return UidToCanonical(parameters, text, location);
                default:
                    if (!VcardText.IsValidText(value, VcardVersion.V2_1)) return null;

                    // An extension's value has no known type, so it is kept as it was.
                    return Vcard21Conformance.IsDefinedProperty(line.Name) ? VcardValue.FormatText(text, VcardValueFormat.Text, VcardVersion.V4_0) : text;
            }
        }

        private static string UidToCanonical(List<VcardContentLineParameter> parameters, string text, string location)
        {
            if (location != null)
            {
                if (Vcard21Conformance.IsUrl(location)) return VcardValue.IsValidUriText(text.AsSpan()) ? VcardValue.EscapeUri(text) : null;

                // An X- location says nothing vCard 4.0 can say, so the line stays as it is.
                return Vcard21Conformance.IsContentId(location) ? ContentIdToUri(text) : null;
            }

            // ref. vCard 2.1 §2.6.5: "a unique character string"; RFC 6350 §6.7.6: 'The default is a single URI value.  It can also be reset to free-form text'.
            if (!VcardText.IsValidText(text.AsSpan(), VcardVersion.V2_1)) return null;
            if (VcardValue.IsUriText(text.AsSpan())) return VcardValue.EscapeUri(text);

            parameters.Insert(0, new VcardContentLineParameter(VcardParameterNames.Value, Vcard30Conformance.TextValueName));
            return VcardValue.FormatText(text, VcardValueFormat.Text, VcardVersion.V4_0);
        }

        private static VcardContentLine MediaToCanonical(VcardContentLine line, List<VcardContentLineParameter> parameters, string location)
        {
            var types = VcardVersion30Mapping.GetTypes(parameters);
            var mediaTypeIndex = types.FindIndex(type => VcardLegacyMediaTypes.TryGetMediaType(type.AsSpan(), out _));
            var mediaType = default(MimeMediaType);
            if (mediaTypeIndex >= 0)
            {
                VcardLegacyMediaTypes.TryGetMediaType(types[mediaTypeIndex].AsSpan(), out mediaType);
                types.RemoveAt(mediaTypeIndex);
            }

            string value;
            if (location is null && Vcard21Conformance.IsBinary(line))
            {
                byte[] data;
                if (!Vcard21Conformance.TryGetBinary(line, out data)) return line;

                value = VcardVersion30Mapping.FormatDataUri(mediaTypeIndex >= 0 ? mediaType : OctetStream, Convert.ToBase64String(data));
            }
            else
            {
                string text;
                if (!Vcard21Conformance.TryGetText(line, out text)) return line;

                if (location is null)
                {
                    // ref. vCard 2.1 §2.6.3 and §2.7.1: a sound can be "a string representation", and a key's "default format is clear-text".  RFC 6350 has neither, so the TYPE stays.
                    if (IsName(line, VcardPropertyNames.Photo) || IsName(line, VcardPropertyNames.Logo) || !VcardText.IsValidText(text.AsSpan(), VcardVersion.V2_1)) return line;

                    RemoveEncoding(parameters);
                    VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
                    ConvertTypes(line.Name, parameters);
                    parameters.Insert(0, new VcardContentLineParameter(VcardParameterNames.Value, Vcard30Conformance.TextValueName));
                    return line.With(line.Name, parameters.ToArray(), VcardValue.FormatText(text, VcardValueFormat.Text, VcardVersion.V4_0));
                }

                if (Vcard21Conformance.IsUrl(location))
                {
                    if (!VcardValue.IsValidUriText(text.AsSpan())) return line;

                    value = VcardValue.EscapeUri(text);
                }
                else if (Vcard21Conformance.IsContentId(location))
                {
                    value = ContentIdToUri(text);
                    if (value is null) return line;

                    value = VcardValue.EscapeUri(value);
                }
                else
                {
                    return line;
                }

                if (mediaTypeIndex >= 0)
                {
                    parameters.Add(new VcardContentLineParameter(VcardParameterNames.MediaType, mediaType.ToString()));
                }
            }

            RemoveEncoding(parameters);
            VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
            VcardVersion30Mapping.SetTypes(parameters, types);
            ConvertTypes(line.Name, parameters);
            return line.With(line.Name, parameters.ToArray(), value);
        }

        private static VcardContentLine AgentToCanonical(VcardContentLine line)
        {
            var parameters = new List<VcardContentLineParameter>(line.ParameterArray);
            var location = Vcard21Conformance.GetValueLocation(line);
            string value;
            if (line.HeldCard != null)
            {
                value = VcardValue.FormatText(VcardNestedCards.Write(line.HeldCard, VcardVersion.V4_0), VcardValueFormat.Text, VcardVersion.V4_0);
            }
            else if (location is null)
            {
                return line;
            }
            else
            {
                string text;
                if (!Vcard21Conformance.TryGetText(line, out text)) return line;

                value = Vcard21Conformance.IsUrl(location) ? (VcardValue.IsValidUriText(text.AsSpan()) ? text : null) : Vcard21Conformance.IsContentId(location) ? ContentIdToUri(text) : null;
                if (value is null) return line;

                value = VcardValue.EscapeUri(value);
            }

            RemoveEncoding(parameters);
            VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
            ConvertTypes(VcardPropertyNames.Related, parameters);
            if (line.HeldCard != null)
            {
                // ref. RFC 6350 §6.6.6: 'The default is a single URI.  It can also be reset to a single text value.'
                parameters.Insert(0, new VcardContentLineParameter(VcardParameterNames.Value, Vcard30Conformance.TextValueName));
            }

            var types = VcardVersion30Mapping.GetTypes(parameters);
            types.Add(AgentType);
            VcardVersion30Mapping.SetTypes(parameters, types);
            return line.With(VcardPropertyNames.Related, parameters.ToArray(), value);
        }

        private static void RemoveEncoding(List<VcardContentLineParameter> parameters)
        {
            VcardVersion30Mapping.Remove(parameters, VcardLegacyTokens.EncodingName);
            VcardVersion30Mapping.Remove(parameters, VcardLegacyTokens.CharsetName);
        }

        /// <summary>
        /// Gives each <c>TYPE</c> the form of vCard 4.0: a <c>knowntype</c> in lower case, and <c>PREF</c> as <c>PREF=1</c> where vCard 4.0 allows it (RFC 6350 §5.3).
        /// </summary>
        private static void ConvertTypes(string propertyName, List<VcardContentLineParameter> parameters)
        {
            var types = VcardVersion30Mapping.GetTypes(parameters);
            if (types.Count == 0) return;

            var isPreferred = false;
            var converted = new List<string>(types.Count);
            foreach (var type in types)
            {
                if (string.Equals(type, PreferredType, StringComparison.OrdinalIgnoreCase) && VcardPropertyParameterReader.IsAllowedOnProperty(VcardParameterSet.Preference, propertyName))
                {
                    isPreferred = true;
                }
                else
                {
                    converted.Add(VcardLegacyTokens.IsKnownType(type.AsSpan()) ? type.ToLowerInvariant() : type);
                }
            }

            VcardVersion30Mapping.SetTypes(parameters, converted);
            if (isPreferred)
            {
                parameters.Add(new VcardContentLineParameter(VcardParameterNames.Preference, MostPreferred));
            }
        }

        /// <summary>
        /// One conversion of a card from canonical form to vCard 2.1.
        /// </summary>
        private sealed class Downgrade
        {
            private readonly VcardCard _card;
            private readonly int _depth;
            private readonly List<VcardDiagnostic> _found;
            private readonly List<VcardContentLine> _lines;
            private readonly VcardOptions _options;

            internal Downgrade(VcardCard card, VcardOptions options, List<VcardDiagnostic> found, int depth)
            {
                _card = card;
                _options = options;
                _found = found;
                _depth = depth;
                _lines = new List<VcardContentLine>(card.Properties.Count);
            }

            internal List<VcardContentLine> Run()
            {
                foreach (var line in _card.Properties)
                {
                    Convert(line);
                }

                return _lines;
            }

            private void ReportLoss(VcardContentLine line, string what)
            {
                _found.Add(new VcardDiagnostic(VcardDiagnosticKind.Lossy, string.Format(CultureInfo.InvariantCulture, "vCard 2.1 can't hold {0} of {1}.", what, line.Name), line.Name, line.Group, line.LineNumber, _card.CardIndex));
            }

            private void ReportLostProperty(VcardContentLine line, string reason)
            {
                _found.Add(new VcardDiagnostic(VcardDiagnosticKind.Lossy, string.Format(CultureInfo.InvariantCulture, "vCard 2.1 can't hold this {0} property: {1}.", line.Name, reason), line.Name, line.Group, line.LineNumber, _card.CardIndex));
            }

            private void Convert(VcardContentLine line)
            {
                if (IsStructural(line))
                {
                    _lines.Add(line);
                    return;
                }

                if (IsName(line, VcardPropertyNames.Related))
                {
                    ConvertRelated(line);
                    return;
                }

                var isDefined = Vcard21Conformance.IsDefinedProperty(line.Name);
                if (!isDefined)
                {
                    if (VcardPropertyInfo.Find(line.Name) != null)
                    {
                        ReportLostProperty(line, "vCard 2.1 doesn't define it");
                        return;
                    }

                    if (!VcardLegacyTokens.IsExtensionWord(line.Name))
                    {
                        ReportLostProperty(line, "vCard 2.1 §2.9 has only its own properties and those whose names start with X-");
                        return;
                    }
                }

                if (line.GetParameter(VcardParameterNames.Phonetic) != null && (IsName(line, VcardPropertyNames.Name) || IsName(line, VcardPropertyNames.Address)))
                {
                    ReportLostProperty(line, "it is the pronunciation of another (RFC 9554 §4.6)");
                    return;
                }

                // A line still in the form of vCard 2.1, such as a NOTE;VALUE=URL, was read that way and is written that way.
                if (VcardConformance.HasEncodingParameter(line) || IsLegacyValueLocation(line))
                {
                    _lines.Add(line);
                    return;
                }

                var parameters = new List<VcardContentLineParameter>(line.ParameterArray);
                var label = IsName(line, VcardPropertyNames.Address) ? VcardVersion30Mapping.Find(parameters, VcardParameterNames.Label) : null;
                var mediaType = Vcard21Conformance.IsMediaProperty(line.Name) ? VcardVersion30Mapping.Find(parameters, VcardParameterNames.MediaType) : null;
                VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Label);
                if (mediaType != null)
                {
                    VcardVersion30Mapping.Remove(parameters, VcardParameterNames.MediaType);
                }

                if (VcardVersion30Mapping.Find(parameters, VcardParameterNames.SortAs) != null)
                {
                    // ref. vCard 2.1 has no SORT-STRING, which RFC 2426 §3.6.5 added.
                    ReportLoss(line, "the parameter SORT-AS");
                    VcardVersion30Mapping.Remove(parameters, VcardParameterNames.SortAs);
                }

                string value;
                bool isBinary;
                if (isDefined)
                {
                    if (!TryConvertValue(line, parameters, mediaType, out value, out isBinary)) return;
                }
                else
                {
                    // An extension's value has no known type, so it is kept as it was.
                    value = line.Value;
                    isBinary = false;
                }

                ConvertParameters(line, parameters);
                var converted = Encode(line, parameters, value, isBinary);
                _lines.Add(converted);
                if (label != null)
                {
                    AddLabel(converted, label);
                }
            }

            private static bool IsLegacyValueLocation(VcardContentLine line)
            {
                var location = VcardPropertyParameterReader.GetValue(line, VcardParameterNames.Value);
                return location != null && (VcardLegacyTokens.IsValueLocation(location.AsSpan()) || VcardLegacyTokens.IsExtensionWord(location));
            }

            /// <summary>
            /// Gets the text of vCard 2.1 for the value of a property it defines.
            /// </summary>
            /// <returns><c>false</c> if the property is lost.</returns>
            private bool TryConvertValue(VcardContentLine line, List<VcardContentLineParameter> parameters, string mediaType, out string value, out bool isBinary)
            {
                isBinary = false;
                value = line.Value;
                switch (line.Name.ToUpperInvariant())
                {
                    case VcardPropertyNames.Name:
                        return TryConvertName(line, ref value);
                    case VcardPropertyNames.Address:
                        return TryConvertAddress(line, parameters, ref value);
                    case VcardPropertyNames.Organization:
                        return TryConvertOrganization(line, ref value);
                    case VcardPropertyNames.Birthday:
                        return TryConvertBirthday(line, parameters, ref value);
                    case VcardPropertyNames.Revision:
                        return TryConvertRevision(line, ref value);
                    case VcardPropertyNames.TimeZone:
                        return TryConvertTimeZone(line, parameters, ref value);
                    case VcardPropertyNames.Geo:
                        return TryConvertGeo(line, ref value);
                    case VcardPropertyNames.Telephone:
                        return TryConvertTelephone(line, parameters, ref value);
                    case VcardPropertyNames.Uid:
                        return TryConvertUid(line, parameters, ref value);
                    case VcardPropertyNames.Url:
                        value = UnescapeUri(value);
                        VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
                        return true;
                    case VcardPropertyNames.Photo:
                    case VcardPropertyNames.Logo:
                    case VcardPropertyNames.Sound:
                    case VcardPropertyNames.Key:
                        return TryConvertMedia(line, parameters, mediaType, ref value, out isBinary);
                    default:
                        value = UnescapeText(value);
                        return true;
                }
            }

            private static string UnescapeText(string value)
            {
                string text;
                return VcardVersion30Mapping.TryUnescape(value, out text) ? text : value;
            }

            private static string UnescapeUri(string value)
            {
                string text;
                return VcardValue.TryGetUriText(value, out text) ? text : value;
            }

            private bool TryConvertName(VcardContentLine line, ref string value)
            {
                VcardName name;
                if (!VcardName.TryParse(value.AsSpan(), VcardValueFormat.Text, out name)) return true;

                if (name.Components.HasValueFrom(VcardName.SecondarySurnamesComponent))
                {
                    // ref. RFC 9554 §2.2: a line with PHONETIC, or added as it was, may not have them in the older components yet.
                    name = name.WithBackwardCompatibleComponents();
                    ReportLoss(line, "the components of RFC 9554");
                }

                name = name.WithoutRfc9554Components();
                if (!name.IsDefinedIn(VcardVersion.V2_1))
                {
                    // ref. vCard 2.1 §2.9: nameparts has one strnosemi in a component.
                    name = name.WithSingleItems();
                    ReportLoss(line, "several values in one component");
                    if (!name.IsDefinedIn(VcardVersion.V2_1))
                    {
                        ReportLostProperty(line, "a component ends with a backslash, which would escape the semicolon after it (§2.9)");
                        return false;
                    }
                }

                value = name.ToString(VcardValueFormat.Text, VcardVersion.V2_1);
                return true;
            }

            private bool TryConvertAddress(VcardContentLine line, List<VcardContentLineParameter> parameters, ref string value)
            {
                VcardAddress address;
                if (!VcardAddress.TryParse(value.AsSpan(), VcardValueFormat.Text, out address)) return true;

                if (address.HasDetailedComponents)
                {
                    address = address.WithBackwardCompatibleComponents();
                    ReportLoss(line, "the components of RFC 9554");
                }

                address = address.WithoutRfc9554Components();
                if (!address.IsDefinedIn(VcardVersion.V2_1))
                {
                    // ref. vCard 2.1 §2.9: addressparts has one strnosemi in a component.
                    address = address.WithSingleItems();
                    ReportLoss(line, "several values in one component");
                    if (!address.IsDefinedIn(VcardVersion.V2_1))
                    {
                        ReportLostProperty(line, "a component ends with a backslash, which would escape the semicolon after it (§2.9)");
                        return false;
                    }
                }

                value = address.ToString(VcardValueFormat.Text, VcardVersion.V2_1);
                var types = VcardVersion30Mapping.GetTypes(parameters);
                if (types.RemoveAll(type => string.Equals(type, "billing", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "delivery", StringComparison.OrdinalIgnoreCase)) != 0)
                {
                    // ref. RFC 9554 §5: the values of TYPE that it adds to ADR.
                    ReportLoss(line, "the TYPE values of RFC 9554");
                    VcardVersion30Mapping.SetTypes(parameters, types);
                }

                return true;
            }

            private bool TryConvertOrganization(VcardContentLine line, ref string value)
            {
                VcardOrganization organization;
                if (!VcardOrganization.TryParse(value.AsSpan(), VcardValueFormat.Text, out organization)) return true;

                if (!organization.IsDefinedIn(VcardVersion.V2_1))
                {
                    ReportLostProperty(line, "a component ends with a backslash, which would escape the semicolon after it (§2.9)");
                    return false;
                }

                value = organization.ToString(VcardValueFormat.Text, VcardVersion.V2_1);
                return true;
            }

            private bool TryConvertBirthday(VcardContentLine line, List<VcardContentLineParameter> parameters, ref string value)
            {
                if (VcardValueTypeRegistry.GetValueType(line) == VcardValueType.Text)
                {
                    ReportLostProperty(line, "its value is text");
                    return false;
                }

                // ref. vCard 2.1 §2.2.4: "a calendar date in a complete representation consistent with ISO 8601".
                VcardDate date;
                if (!VcardDate.TryParse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V4_0, out date) || !date.IsDefinedIn(VcardVersion.V2_1))
                {
                    VcardDateAndOrTime any;
                    if (!VcardDateAndOrTime.TryParse(value.AsSpan(), VcardValueFormat.Text, out any)) return true;

                    ReportLostProperty(line, "its value isn't a complete date");
                    return false;
                }

                value = date.ToString(VcardValueFormat.Text, VcardVersion.V2_1);
                VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
                return true;
            }

            private bool TryConvertRevision(VcardContentLine line, ref string value)
            {
                VcardTimestamp timestamp;
                if (!VcardTimestamp.TryParse(value.AsSpan(), VcardValueFormat.Text, out timestamp)) return true;

                if (!timestamp.IsDefinedIn(VcardVersion.V2_1))
                {
                    ReportLostProperty(line, "its time isn't one vCard 2.1 can write");
                    return false;
                }

                value = timestamp.ToString(VcardValueFormat.Text, VcardVersion.V2_1);
                return true;
            }

            private bool TryConvertTimeZone(VcardContentLine line, List<VcardContentLineParameter> parameters, ref string value)
            {
                // ref. vCard 2.1 §2.4.5: "It is an offset from Coordinated Universal Time (UTC)."
                VcardUtcOffset offset;
                if (VcardValueTypeRegistry.GetValueType(line) != VcardValueType.UtcOffset)
                {
                    ReportLostProperty(line, "its value isn't an offset from UTC");
                    return false;
                }

                if (!VcardUtcOffset.TryParse(value.AsSpan(), VcardValueFormat.Text, out offset)) return true;

                if (!offset.IsDefinedIn(VcardVersion.V2_1))
                {
                    ReportLostProperty(line, "its offset isn't one vCard 2.1 can write");
                    return false;
                }

                value = offset.ToString(VcardValueFormat.Text, VcardVersion.V2_1);
                VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
                return true;
            }

            private bool TryConvertGeo(VcardContentLine line, ref string value)
            {
                VcardGeo geo;
                if (VcardGeo.TryParse(value.AsSpan(), VcardValueFormat.Text, out geo) && geo.IsDefinedIn(VcardVersion.V2_1))
                {
                    value = geo.ToString(VcardValueFormat.Text, VcardVersion.V2_1);
                    return true;
                }

                Uri uri;
                if (!VcardValue.TryParseUri(value.AsSpan(), VcardValueFormat.Text, out uri)) return true;

                ReportLostProperty(line, "only a latitude and a longitude can be written");
                return false;
            }

            private bool TryConvertTelephone(VcardContentLine line, List<VcardContentLineParameter> parameters, ref string value)
            {
                if (VcardValueTypeRegistry.GetValueType(line) != VcardValueType.Uri)
                {
                    value = UnescapeText(value);
                    return true;
                }

                // ref. vCard 2.1 §2.4.1: "the canonical number string for a telephone number", which RFC 3966 §3 writes after its scheme.
                string text;
                if (!VcardValue.TryGetUriText(value, out text)) return true;

                if (!text.StartsWith(TelPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    ReportLostProperty(line, "its value is a URI other than a telephone number");
                    return false;
                }

                var number = text.Substring(TelPrefix.Length);
                var parametersIndex = number.IndexOf(';');
                if (parametersIndex >= 0)
                {
                    ReportLoss(line, "the parameters of a tel URI");
                    number = number.Substring(0, parametersIndex);
                }

                value = number;
                VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
                return true;
            }

            private static bool TryConvertUid(VcardContentLine line, List<VcardContentLineParameter> parameters, ref string value)
            {
                var isText = VcardValueTypeRegistry.GetValueType(line) == VcardValueType.Text;
                VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
                value = isText ? UnescapeText(value) : UnescapeUri(value);
                return true;
            }

            private bool TryConvertMedia(VcardContentLine line, List<VcardContentLineParameter> parameters, string mediaType, ref string value, out bool isBinary)
            {
                isBinary = false;
                if (VcardValueTypeRegistry.GetValueType(line) == VcardValueType.Text)
                {
                    // ref. vCard 2.1 §2.6.3 and §2.7.1: a sound or a key can be text.
                    if (!IsName(line, VcardPropertyNames.Sound) && !IsName(line, VcardPropertyNames.Key))
                    {
                        ReportLostProperty(line, "its value is text");
                        return false;
                    }

                    VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
                    value = UnescapeText(value);
                    return true;
                }

                string uriText;
                if (!VcardValue.TryGetUriText(value, out uriText)) return true;

                VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
                MimeDataUri data;
                if (MimeDataUri.TryParse(uriText, out data))
                {
                    // ref. vCard 2.1 §2.1.9: "with a Base 64 (default) or Quoted-Printable encoding".
                    value = System.Convert.ToBase64String(data.GetData());
                    isBinary = true;
                    if (!VcardVersion30Mapping.IsUntyped(data.MediaType))
                    {
                        AddMediaToken(line, parameters, data.MediaType);
                    }

                    return true;
                }

                var contentId = UriToContentId(uriText);
                parameters.Insert(0, new VcardContentLineParameter(VcardParameterNames.Value, contentId is null ? Vcard21Conformance.UrlValue : Vcard21Conformance.ContentIdValue));
                value = contentId ?? uriText;

                MimeMediaType parsedType;
                if (mediaType != null && MimeMediaType.TryParse(mediaType.AsSpan(), out parsedType))
                {
                    AddMediaToken(line, parameters, parsedType);
                }

                return true;
            }

            private void AddMediaToken(VcardContentLine line, List<VcardContentLineParameter> parameters, MimeMediaType mediaType)
            {
                string token;
                if (!VcardLegacyMediaTypes.TryGetToken(mediaType, VcardVersion.V2_1, out token))
                {
                    // ref. vCard 2.1 §2.9: knowntype has only the formats of 1996.
                    ReportLoss(line, "the media type " + mediaType.Type + "/" + mediaType.Subtype + " (§2.9 has no knowntype for it)");
                    return;
                }

                var types = VcardVersion30Mapping.GetTypes(parameters);
                types.Insert(0, token);
                VcardVersion30Mapping.SetTypes(parameters, types);
            }

            private void ConvertRelated(VcardContentLine line)
            {
                var types = VcardPropertyParameterReader.GetListValues(line, VcardParameterNames.Type);
                if (!types.Exists(type => string.Equals(type, AgentType, StringComparison.OrdinalIgnoreCase)))
                {
                    ReportLostProperty(line, "vCard 2.1 has only AGENT, which is its TYPE=agent");
                    return;
                }

                var parameters = new List<VcardContentLineParameter>(line.ParameterArray);
                var valueType = VcardValueTypeRegistry.GetValueType(line);
                types.RemoveAll(type => string.Equals(type, AgentType, StringComparison.OrdinalIgnoreCase));
                VcardVersion30Mapping.SetTypes(parameters, types);
                VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
                var agent = line.WithNameAndValue(VcardPropertyNames.Agent, line.Value);
                string value;
                VcardCard held = null;
                if (valueType == VcardValueType.Text)
                {
                    string text;
                    if (!VcardVersion30Mapping.TryUnescape(line.Value, out text) || !VcardNestedCards.TryReadHeld(text, out held))
                    {
                        // ref. vCard 2.1 §2.5.4: "The value of this property is a string containing another vCard object."
                        ReportLostProperty(agent, "its value is text that isn't a card");
                        return;
                    }

                    if (_depth + 1 >= MaxNestingDepth)
                    {
                        ReportLostProperty(agent, "its card is nested too deeply");
                        return;
                    }

                    ReportHeldCard(agent, held);

                    // ref. vCard 2.1 §2.9: "AGENT" [params] ":" vcard CRLF, so the card starts on this line.
                    value = "BEGIN:" + VcardCardBuilder.CardValue;
                }
                else
                {
                    var uriText = UnescapeUri(line.Value);
                    var contentId = UriToContentId(uriText);
                    parameters.Insert(0, new VcardContentLineParameter(VcardParameterNames.Value, contentId is null ? Vcard21Conformance.UrlValue : Vcard21Conformance.ContentIdValue));
                    value = contentId ?? uriText;
                }

                ConvertParameters(agent, parameters);

                // The value that starts a held card is never encoded, since a reader looks for it as it is, and it is printable US-ASCII whatever the length of its line.
                var converted = held is null ? Encode(agent, parameters, value, false) : agent.With(agent.Name, parameters.ToArray(), value).WithHeldCard(held);
                _lines.Add(converted);
            }

            /// <summary>
            /// Reports what a card held by an <c>AGENT</c> can't hold, and what it lacks.
            /// </summary>
            private void ReportHeldCard(VcardContentLine agent, VcardCard held)
            {
                var heldFound = new List<VcardDiagnostic>();
                var heldLines = new Downgrade(held, _options, heldFound, _depth + 1).Run();
                var heldChecks = new List<VcardDiagnostic>();
                Vcard21Conformance.Validate(heldLines, held, VcardNestedCards.HeldCardOptions, _depth + 1, heldChecks);
                heldFound.AddRange(heldChecks.FindAll(diagnostic => diagnostic.Kind == VcardDiagnosticKind.MissingRequiredProperty));
                foreach (var diagnostic in heldFound)
                {
                    _found.Add(new VcardDiagnostic(diagnostic.Kind, "In the card of an AGENT: " + diagnostic.Message, diagnostic.PropertyName, diagnostic.Group, agent.LineNumber, _card.CardIndex));
                }
            }

            /// <summary>
            /// Gives the parameters the form of vCard 2.1, and reports those it can't hold.
            /// </summary>
            private void ConvertParameters(VcardContentLine line, List<VcardContentLineParameter> parameters)
            {
                foreach (var name in VcardVersion30Mapping.Version40OnlyParameters)
                {
                    if (VcardVersion30Mapping.Find(parameters, name) != null)
                    {
                        ReportLoss(line, "the parameter " + name);
                        VcardVersion30Mapping.Remove(parameters, name);
                    }
                }

                var language = VcardVersion30Mapping.Find(parameters, VcardParameterNames.Language);
                if (language != null && !Vcard30Conformance.IsRfc1766Tag(language))
                {
                    // ref. vCard 2.1 §2.1.7: "a string consistent with RFC 1766".
                    ReportLoss(line, "the language tag " + language + ", which isn't one of RFC 1766");
                    VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Language);
                }

                ConvertPreference(line, parameters);
                ConvertTypeValues(line, parameters);

                for (var index = parameters.Count - 1; index >= 0; index--)
                {
                    var parameter = parameters[index];
                    if (IsParameterName(parameter, VcardParameterNames.Type) || IsParameterName(parameter, VcardParameterNames.Language) || IsParameterName(parameter, VcardParameterNames.Value)) continue;

                    // ref. vCard 2.1 §2.9: "X-" word [ws] "=" [ws] word.
                    if (VcardLegacyTokens.IsExtensionWord(parameter.Name) && parameter.ValueArray.Length == 1 && VcardLegacyTokens.IsWord(parameter.ValueArray[0])) continue;

                    ReportLoss(line, "the parameter " + parameter.Name);
                    parameters.RemoveAt(index);
                }

                var valueType = VcardVersion30Mapping.Find(parameters, VcardParameterNames.Value);
                if (valueType != null && !VcardLegacyTokens.IsValueLocation(valueType.AsSpan()))
                {
                    // A type of vCard 4.0 that the conversion of the value hasn't removed says nothing vCard 2.1 can say.
                    VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Value);
                }
            }

            private void ConvertPreference(VcardContentLine line, List<VcardContentLineParameter> parameters)
            {
                var text = VcardVersion30Mapping.Find(parameters, VcardParameterNames.Preference);
                if (text is null) return;

                VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Preference);

                // An AGENT is a RELATED in the card being converted.
                var canonicalName = IsName(line, VcardPropertyNames.Agent) ? VcardPropertyNames.Related : line.Name;
                int preference;
                var isLowest = VcardPropertyParameterReader.TryParsePreference(text, out preference) && preference == VcardVersion30Mapping.GetLowestPreference(_card.Properties, canonicalName);
                if (!isLowest)
                {
                    // ref. vCard 2.1 §2.4.1: PREF says only that an instance is preferred, so it has no order after the first.
                    ReportLoss(line, "the order of a PREF that isn't the lowest");
                    return;
                }

                var types = VcardVersion30Mapping.GetTypes(parameters);
                types.Add(PreferredType);
                VcardVersion30Mapping.SetTypes(parameters, types);
            }

            /// <summary>
            /// Writes each <c>TYPE</c> value as a parameter of its own, since a parameter of vCard 2.1 has one value (§2.9), with a <c>knowntype</c> in upper case.
            /// </summary>
            private void ConvertTypeValues(VcardContentLine line, List<VcardContentLineParameter> parameters)
            {
                var position = parameters.FindIndex(parameter => IsParameterName(parameter, VcardParameterNames.Type));
                if (position < 0) return;

                var types = VcardVersion30Mapping.GetTypes(parameters);
                VcardVersion30Mapping.Remove(parameters, VcardParameterNames.Type);
                var converted = new List<VcardContentLineParameter>(types.Count);
                foreach (var type in types)
                {
                    if (VcardLegacyTokens.IsKnownType(type.AsSpan()))
                    {
                        var token = type.ToUpperInvariant();
                        if (!converted.Exists(parameter => string.Equals(parameter.ValueArray[0], token, StringComparison.Ordinal)))
                        {
                            converted.Add(new VcardContentLineParameter(VcardParameterNames.Type, token));
                        }
                    }
                    else if (VcardLegacyTokens.IsExtensionWord(type))
                    {
                        converted.Add(new VcardContentLineParameter(VcardParameterNames.Type, type));
                    }
                    else
                    {
                        // ref. vCard 2.1 §2.9: ptypeval = knowntype / "X-" word.
                        ReportLoss(line, "the TYPE value " + type);
                    }
                }

                parameters.InsertRange(Math.Min(position, parameters.Count), converted);
            }

            /// <summary>
            /// Gets the line with its value in the encoding vCard 2.1 needs for it.
            /// </summary>
            private VcardContentLine Encode(VcardContentLine line, List<VcardContentLineParameter> parameters, string value, bool isBinary)
            {
                if (isBinary)
                {
                    parameters.Add(new VcardContentLineParameter(VcardLegacyTokens.EncodingName, VcardLegacyTokens.Base64));
                    return line.With(line.Name, parameters.ToArray(), value);
                }

                var plain = line.With(line.Name, parameters.ToArray(), value);
                if (IsPrintableAscii(value) && GetLineLength(plain) <= MaxPlainLineLength) return plain;

                // ref. vCard 2.1 §2.1.3 and §2.1.6: a line break is written =0D=0A, and text outside US-ASCII needs its character set.
                var text = value.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");
                var hasNonAscii = !IsAscii(text);
                var encoded = MimeQuotedPrintable.Encode(Encoding.UTF8.GetBytes(text), MimeQuotedPrintableMode.Body, MimeQuotedPrintableEncoderOptions.Binary).Replace("=\r\n", string.Empty);
                parameters.Add(new VcardContentLineParameter(VcardLegacyTokens.EncodingName, VcardLegacyTokens.QuotedPrintable));
                if (hasNonAscii)
                {
                    parameters.Add(new VcardContentLineParameter(VcardLegacyTokens.CharsetName, Utf8Charset));
                }

                return line.With(line.Name, parameters.ToArray(), encoded);
            }

            private static bool IsAscii(string value)
            {
                foreach (var character in value)
                {
                    if (character >= '\x80') return false;
                }

                return true;
            }

            private static bool IsPrintableAscii(string value)
            {
                foreach (var character in value)
                {
                    if (character < ' ' || character >= '\x7F') return false;
                }

                return true;
            }

            /// <summary>
            /// Gets the length of a line as the writer writes it, in characters, which are octets in printable US-ASCII.
            /// </summary>
            private int GetLineLength(VcardContentLine line)
            {
                var length = (line.Group is null ? 0 : line.Group.Length + 1) + line.Name.Length + 1 + line.Value.Length;
                var bareParameters = _options.WriteNamedTypesInVersion21 ? null : VcardContentLineData.FormatWithBareTypes(line);
                if (bareParameters != null) return length + bareParameters.Length;

                foreach (var parameter in line.ParameterArray)
                {
                    length += 1 + parameter.Name.Length + 1 + parameter.ValueArray.Length - 1;
                    foreach (var parameterValue in parameter.ValueArray)
                    {
                        length += parameterValue.Length;
                    }
                }

                return length;
            }

            private void AddLabel(VcardContentLine address, string label)
            {
                // ref. RFC 6350 §6.3.1: its example writes a line break in a LABEL as "\n", where RFC 6868 would write it "^n".
                var parameters = new List<VcardContentLineParameter>();
                foreach (var parameter in address.ParameterArray)
                {
                    if (IsParameterName(parameter, VcardParameterNames.Type) || IsParameterName(parameter, VcardParameterNames.Language))
                    {
                        parameters.Add(parameter);
                    }
                }

                var labelLine = address.With(VcardPropertyNames.PostalLabel, parameters.ToArray(), string.Empty);
                _lines.Add(Encode(labelLine, parameters, label.Replace("\\n", "\n"), false));
            }
        }
    }
}
