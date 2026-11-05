#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#nullable disable
#endif
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The mapping of vCard 3.0: RFC 2426 with its errata, RFC 2739 and RFC 4770.
    /// </summary>
    internal sealed class VcardVersion30Mapping : VcardVersionMapping
    {
        // ref. RFC 2426 §3.2.1, §3.3.1 and §3.3.2: the value of TYPE that says an instance is preferred.
        private const string PreferredType = "pref";

        // ref. RFC 6350 §5.3: 1 is the most preferred.
        private const string MostPreferred = "1";

        // ref. RFC 6350 §6.6.6: type-param-related.
        private const string AgentType = "agent";

        // ref. RFC 2426 §3.2.1 and §3.2.2: 'The default is "TYPE=intl,postal,parcel,work"'.
        private static readonly string[] DefaultAddressTypes = { "intl", "parcel", "postal", "work" };

        // ref. RFC 9554 §5: the values of TYPE that it adds to ADR.
        private static readonly string[] Rfc9554AddressTypes = { "billing", "delivery" };

        // ref. RFC 6350 §6.2.4, §6.6.3, §6.7.5 and §6.8.1: the type-value of each of these says what it is for, where in vCard 3.0 the same parameter says its format.
        private static readonly string[] Version40MediaUses = { "home", "work" };

        private const string DataPrefix = "data:";
        private const string TelPrefix = "tel:";
        private const string Base64Marker = ";base64,";
        private const string ImageType = "image";
        private const string AudioType = "audio";
        private const string ApplicationType = "application";

        private static readonly MimeMediaType OctetStream = new MimeMediaType(ApplicationType, "octet-stream");

        // ref. RFC 6350 and its extensions: the parameters vCard 4.0 has that RFC 2426 doesn't, other than those a line here converts.
        internal static readonly string[] Version40OnlyParameters =
        {
            VcardParameterNames.Pid, VcardParameterNames.AlternativeId, VcardParameterNames.CalendarScale, VcardParameterNames.Geo, VcardParameterNames.TimeZone,
            VcardParameterNames.Index, VcardParameterNames.Level, VcardParameterNames.CountryCode, VcardParameterNames.Author, VcardParameterNames.AuthorName,
            VcardParameterNames.Created, VcardParameterNames.Derived, VcardParameterNames.Phonetic, VcardParameterNames.PropertyId, VcardParameterNames.Script,
            VcardParameterNames.ServiceType, VcardParameterNames.Username, VcardParameterNames.MediaType, VcardParameterNames.SortAs, VcardParameterNames.Label
        };

        // ref. RFC 2426 §3.6.9: 'Value MUST be "3.0"'.
        internal override string VersionText => "3.0";

        internal override VcardVersion Version => VcardVersion.V3_0;

        internal override void Validate(IList<VcardContentLine> lines, VcardCard card, VcardOptions options, int depth, List<VcardDiagnostic> found)
        {
            Vcard30Conformance.Validate(lines, card, options, depth, found);
        }

        internal override void ToCanonical(VcardCard card, VcardOptions options, int depth)
        {
            for (var index = 0; index < card.Properties.Count; index++)
            {
                card.Properties[index] = ToCanonicalLine(card.Properties[index], options, depth);
            }

            AttachLabels(card);
            AttachSortStrings(card);
        }

        internal override List<VcardContentLine> FromCanonical(VcardCard card, VcardOptions options, List<VcardDiagnostic> found)
        {
            return new Downgrade(card, options, found).Run();
        }

        /// <summary>
        /// Puts a <c>LABEL</c> line on the <c>ADR</c> it goes with, if there is one.
        /// </summary>
        /// <param name="card">The card.</param>
        /// <param name="labelIndex">The index of the line.</param>
        /// <param name="address">The line of the address that now holds it.</param>
        /// <returns><c>true</c> if the line was attached to an address.</returns>
        internal static bool TryAttachLabel(VcardCard card, int labelIndex, out VcardContentLine address)
        {
            address = null;
            var label = card.Properties[labelIndex];
            string text;
            if (!HasOnlyLabelParameters(label) || !TryUnescape(label.Value, out text)) return false;

            var addressIndex = FindAddressOfLabel(card, label);
            if (addressIndex < 0) return false;

            var line = card.Properties[addressIndex];
            var parameters = new List<VcardContentLineParameter>(line.ParameterArray) { new VcardContentLineParameter(VcardParameterNames.Label, text) };
            address = line.With(line.Name, parameters.ToArray(), line.Value);
            card.Properties[addressIndex] = address;
            card.Properties.RemoveAt(labelIndex);
            return true;
        }

        /// <summary>
        /// Puts a <c>SORT-STRING</c> line on what it sorts, if it can.
        /// </summary>
        internal static bool TryAttachSortStringAt(VcardCard card, int index)
        {
            return TryAttachSortString(card, index);
        }

        /// <summary>
        /// Gets the lowest <c>PREF</c> among the instances of a property.
        /// </summary>
        internal static int GetLowestPreference(IEnumerable<VcardContentLine> lines, string name)
        {
            var lowest = int.MaxValue;
            foreach (var line in lines)
            {
                if (string.Equals(line.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    lowest = Math.Min(lowest, VcardPropertyParameterReader.GetPreferenceOrLeast(line));
                }
            }

            return lowest;
        }

        /// <summary>
        /// Gets the media type a token of vCard 3.0 stands for.
        /// </summary>
        /// <param name="token">The value of a <c>TYPE</c> parameter.</param>
        /// <param name="propertyName">The property, which says what kind of media a token not otherwise known is.</param>
        /// <param name="mediaType">The media type.</param>
        internal static bool TryGetMediaType(string token, string propertyName, out MimeMediaType mediaType)
        {
            if (VcardLegacyMediaTypes.TryGetMediaType(token.AsSpan(), out mediaType)) return true;
            if (token.IndexOf('/') >= 0) return MimeMediaType.TryParse(token.AsSpan(), out mediaType);

            // ref. RFC 2426 §3.1.4, §3.5.3 and §3.6.6: a TYPE names a format of IANA's image or audio registry, or one that isn't standard, so it is a subtype of that type.
            return MimeMediaType.TryParse((GetTopLevelType(propertyName) + "/" + token.ToLowerInvariant()).AsSpan(), out mediaType);
        }

        /// <summary>
        /// Gets the token of vCard 3.0 for a media type.  It is the reverse of <see cref="TryGetMediaType"/>, so a format this package has no entry for comes back as it was read.
        /// </summary>
        internal static string GetToken(MimeMediaType mediaType, string propertyName)
        {
            string token;
            if (VcardLegacyMediaTypes.TryGetToken(mediaType, VcardVersion.V3_0, out token)) return token;

            return string.Equals(mediaType.Type, GetTopLevelType(propertyName), StringComparison.OrdinalIgnoreCase) ? mediaType.Subtype.ToUpperInvariant() : mediaType.Type + "/" + mediaType.Subtype;
        }

        /// <summary>
        /// Gets whether a media type is what a binary value with no <c>TYPE</c> gets.
        /// </summary>
        internal static bool IsUntyped(MimeMediaType mediaType)
        {
            return string.Equals(mediaType.Type, OctetStream.Type, StringComparison.OrdinalIgnoreCase) && string.Equals(mediaType.Subtype, OctetStream.Subtype, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the canonical text of a binary value.  ref. RFC 2397 §2 and RFC 6350 §3.4: a comma in a value "MUST be escaped".
        /// </summary>
        internal static string FormatDataUri(MimeMediaType mediaType, string base64)
        {
            return VcardValue.EscapeUri(DataPrefix + mediaType + Base64Marker + base64);
        }

        /// <summary>
        /// Adds a backslash to each semicolon that has none: vCard 4.0 leaves that to the writer (RFC 6350 §3.4), and RFC 2426 §4 requires it.
        /// </summary>
        internal static string EscapeSemicolons(string value)
        {
            if (value.IndexOf(';') < 0) return value;

            var builder = new StringBuilder(value.Length + 1);
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character == '\\' && index + 1 < value.Length)
                {
                    builder.Append(character).Append(value[++index]);
                }
                else
                {
                    if (character == ';')
                    {
                        builder.Append('\\');
                    }

                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        private static string GetTopLevelType(string propertyName)
        {
            if (string.Equals(propertyName, VcardPropertyNames.Sound, StringComparison.OrdinalIgnoreCase)) return AudioType;

            return string.Equals(propertyName, VcardPropertyNames.Key, StringComparison.OrdinalIgnoreCase) ? ApplicationType : ImageType;
        }

        private static bool IsName(VcardContentLine line, string name)
        {
            return string.Equals(line.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsParameterName(VcardContentLineParameter parameter, string name)
        {
            return string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        internal static string Find(List<VcardContentLineParameter> parameters, string name)
        {
            foreach (var parameter in parameters)
            {
                if (IsParameterName(parameter, name)) return parameter.ValueArray[0];
            }

            return null;
        }

        internal static void Remove(List<VcardContentLineParameter> parameters, string name)
        {
            parameters.RemoveAll(parameter => IsParameterName(parameter, name));
        }

        internal static List<string> GetTypes(List<VcardContentLineParameter> parameters)
        {
            var types = new List<string>();
            foreach (var parameter in parameters)
            {
                if (!IsParameterName(parameter, VcardParameterNames.Type)) continue;

                foreach (var value in parameter.ValueArray)
                {
                    types.AddRange(value.Split(','));
                }
            }

            return types;
        }

        /// <summary>
        /// Replaces the <c>TYPE</c> parameters with one holding the given values, where the first of them was.
        /// </summary>
        internal static void SetTypes(List<VcardContentLineParameter> parameters, List<string> types)
        {
            var position = parameters.FindIndex(parameter => IsParameterName(parameter, VcardParameterNames.Type));
            Remove(parameters, VcardParameterNames.Type);
            if (types.Count != 0)
            {
                parameters.Insert(position < 0 ? parameters.Count : position, new VcardContentLineParameter(VcardParameterNames.Type, types));
            }
        }

        private static bool ContainsIgnoringCase(IEnumerable<string> values, string value)
        {
            return values.Any(candidate => string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool TryUnescape(string value, out string text)
        {
            text = null;
            if (!VcardText.IsValidText(value.AsSpan(), VcardVersion.V4_0)) return false;

            text = VcardText.Unescape(value.AsSpan(), VcardVersion.V4_0);
            return true;
        }

        private static VcardContentLine ToCanonicalLine(VcardContentLine line, VcardOptions options, int depth)
        {
            var parameters = new List<VcardContentLineParameter>(line.ParameterArray);
            var value = line.Value;
            var isBinary = Vcard30Conformance.IsBinary(line);

            // Under AcceptLegacySyntax, a line can have the forms of vCard 2.1.  A binary value is converted below.
            if (!isBinary && VcardConformance.HasEncodingParameter(line))
            {
                if (!VcardConformance.TryGetValueText(line, out value)) return line;

                Remove(parameters, VcardLegacyTokens.EncodingName);
                Remove(parameters, VcardLegacyTokens.CharsetName);
            }

            if (Vcard30Conformance.AllowsPreference(line.Name) || (VcardPropertyInfo.Find(line.Name)?.IsDefinedIn(VcardVersion.V3_0) == true && !IsMedia(line)))
            {
                ConvertPreferredType(parameters);
            }

            switch (line.Name.ToUpperInvariant())
            {
                case VcardPropertyNames.Name:
                    VcardName name;
                    if (VcardName.TryParse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0, out name))
                    {
                        value = name.ToString(VcardValueFormat.Text, VcardVersion.V4_0);
                    }

                    break;
                case VcardPropertyNames.Address:
                    VcardAddress address;
                    if (VcardAddress.TryParse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0, out address))
                    {
                        value = address.ToString(VcardValueFormat.Text, VcardVersion.V4_0);
                    }

                    break;
                case VcardPropertyNames.Birthday:
                    VcardDateAndOrTime date;
                    if (VcardDateAndOrTime.TryParse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0, out date))
                    {
                        value = date.ToString(VcardValueFormat.Text, VcardVersion.V4_0);
                        Remove(parameters, VcardParameterNames.Value);
                    }

                    break;
                case VcardPropertyNames.Revision:
                    value = RevisionToCanonical(value, parameters);
                    break;
                case VcardPropertyNames.TimeZone:
                    value = TimeZoneToCanonical(value, parameters);
                    break;
                case VcardPropertyNames.Geo:
                    VcardGeo geo;
                    if (VcardGeo.TryParse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0, out geo))
                    {
                        value = geo.ToString(VcardValueFormat.Text, VcardVersion.V4_0);
                    }

                    break;
                case VcardPropertyNames.Uid:
                    value = UidToCanonical(value, parameters);
                    break;
                case VcardPropertyNames.Source:
                case VcardPropertyNames.Url:
                case VcardPropertyNames.InstantMessaging:
                case VcardPropertyNames.FreeBusyUrl:
                case VcardPropertyNames.CalendarAddressUri:
                case VcardPropertyNames.CalendarAccessUri:
                case VcardPropertyNames.CalendarUri:
                    // ref. RFC 6350 §3.4 and its errata 3845 and 3846: a comma in a URI value is escaped in vCard 4.0, which the verifier of 3845 calls "a difference from earlier versions".
                    value = VcardValue.EscapeBareCommas(value);
                    Remove(parameters, VcardParameterNames.Value);
                    break;
                case VcardPropertyNames.Photo:
                case VcardPropertyNames.Logo:
                case VcardPropertyNames.Sound:
                case VcardPropertyNames.Key:
                    if (!TryMediaToCanonical(line, isBinary, parameters, ref value)) return line;

                    break;
                case VcardPropertyNames.Agent:
                    return AgentToCanonical(line, parameters, value, options, depth);
            }

            return line.With(line.Name, parameters.ToArray(), value);
        }

        private static bool IsMedia(VcardContentLine line)
        {
            return IsName(line, VcardPropertyNames.Photo) || IsName(line, VcardPropertyNames.Logo) || IsName(line, VcardPropertyNames.Sound) || IsName(line, VcardPropertyNames.Key);
        }

        /// <summary>
        /// ref. RFC 2426 §3.3.1: "pref" says an instance is preferred, which RFC 6350 §5.3 says with the lowest <c>PREF</c>.
        /// </summary>
        private static void ConvertPreferredType(List<VcardContentLineParameter> parameters)
        {
            var types = GetTypes(parameters);
            if (types.RemoveAll(type => string.Equals(type, PreferredType, StringComparison.OrdinalIgnoreCase)) == 0) return;

            SetTypes(parameters, types);
            parameters.Add(new VcardContentLineParameter(VcardParameterNames.Preference, MostPreferred));
        }

        private static string RevisionToCanonical(string value, List<VcardContentLineParameter> parameters)
        {
            VcardTimestamp timestamp;
            if (VcardTimestamp.TryParse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0, out timestamp))
            {
                Remove(parameters, VcardParameterNames.Value);
                return timestamp.ToString(VcardValueFormat.Text, VcardVersion.V4_0);
            }

            // ref. RFC 6350 §6.7.4: REV is a timestamp, which has a time, so a date becomes its first moment in UTC.
            VcardDate date;
            if (!VcardDate.TryParse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0, out date)) return value;

            Remove(parameters, VcardParameterNames.Value);
            return date.ToString(VcardValueFormat.Text, VcardVersion.V4_0) + "T000000Z";
        }

        private static string TimeZoneToCanonical(string value, List<VcardContentLineParameter> parameters)
        {
            // ref. RFC 2426 §3.4.1: "The default is a single utc-offset value"; RFC 6350 §6.5.1: 'The default is a single text value', so an offset says what it is.
            if (Find(parameters, VcardParameterNames.Value) != null)
            {
                Remove(parameters, VcardParameterNames.Value);
                return value;
            }

            VcardUtcOffset offset;
            if (!VcardUtcOffset.TryParse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0, out offset)) return value;

            parameters.Add(new VcardContentLineParameter(VcardParameterNames.Value, VcardValueTypeRegistry.GetValueTypeName(VcardValueType.UtcOffset)));
            return offset.ToString(VcardValueFormat.Text, VcardVersion.V4_0);
        }

        private static string UidToCanonical(string value, List<VcardContentLineParameter> parameters)
        {
            // ref. RFC 2426 §3.6.7: a text value.  RFC 6350 §6.7.6: 'The default is a single URI value.  It can also be reset to free-form text'.
            string text;
            if (!TryUnescape(value, out text)) return value;
            if (VcardValue.IsUriText(text.AsSpan())) return VcardValue.EscapeUri(text);

            parameters.Add(new VcardContentLineParameter(VcardParameterNames.Value, Vcard30Conformance.TextValueName));
            return value;
        }

        private static bool TryMediaToCanonical(VcardContentLine line, bool isBinary, List<VcardContentLineParameter> parameters, ref string value)
        {
            var valueType = Find(parameters, VcardParameterNames.Value);
            var isUri = string.Equals(valueType, Vcard30Conformance.UriValueName, StringComparison.OrdinalIgnoreCase);
            var types = GetTypes(parameters);
            MimeMediaType mediaType = default(MimeMediaType);
            var hasMediaType = types.Count != 0 && TryGetMediaType(types[0], line.Name, out mediaType);

            if (isBinary)
            {
                string base64;
                try
                {
                    // Written again, since a BASE64 of vCard 2.1 can hold white space, and a b of RFC 2426 §4 can't.
                    base64 = Convert.ToBase64String(line.GetDecodedValue());
                }
                catch (VcardFileException)
                {
                    return false;
                }

                value = FormatDataUri(hasMediaType ? mediaType : OctetStream, base64);
                Remove(parameters, VcardLegacyTokens.EncodingName);
                Remove(parameters, VcardParameterNames.Value);
            }
            else if (isUri)
            {
                value = VcardValue.EscapeBareCommas(value);
                Remove(parameters, VcardParameterNames.Value);
                if (hasMediaType)
                {
                    parameters.Add(new VcardContentLineParameter(VcardParameterNames.MediaType, mediaType.ToString()));
                }
            }
            else if (IsName(line, VcardPropertyNames.Key))
            {
                // ref. RFC 6350 §6.8.1: mediatype-param is only with a URI, so the TYPE of a text key stays.
                Remove(parameters, VcardParameterNames.Value);
                parameters.Add(new VcardContentLineParameter(VcardParameterNames.Value, Vcard30Conformance.TextValueName));
                return true;
            }
            else
            {
                return false;
            }

            if (hasMediaType)
            {
                types.RemoveAt(0);
                SetTypes(parameters, types);
            }

            return true;
        }

        private static VcardContentLine AgentToCanonical(VcardContentLine line, List<VcardContentLineParameter> parameters, string value, VcardOptions options, int depth)
        {
            var valueType = Find(parameters, VcardParameterNames.Value);
            var isUri = string.Equals(valueType, Vcard30Conformance.UriValueName, StringComparison.OrdinalIgnoreCase);
            var isText = string.Equals(valueType, Vcard30Conformance.TextValueName, StringComparison.OrdinalIgnoreCase);
            if (!isUri && !isText)
            {
                // ref. RFC 2426 §3.5.4: "The default is a single vcard value", escaped as text.
                string text;
                VcardCard nested;
                if (valueType != null || !VcardText.IsValidText(value.AsSpan(), VcardVersion.V3_0)) return line;

                text = VcardText.Unescape(value.AsSpan(), VcardVersion.V3_0);
                if (!VcardNestedCards.TryRead(text, options, depth, out nested)) return line;

                value = VcardValue.FormatText(VcardNestedCards.Write(nested, VcardVersion.V4_0), VcardValueFormat.Text, VcardVersion.V4_0);
                isText = true;
            }

            Remove(parameters, VcardParameterNames.Value);
            if (isUri)
            {
                value = VcardValue.EscapeBareCommas(value);
            }
            else
            {
                // ref. RFC 6350 §6.6.6: 'The default is a single URI.  It can also be reset to a single text value.'
                parameters.Insert(0, new VcardContentLineParameter(VcardParameterNames.Value, Vcard30Conformance.TextValueName));
            }

            parameters.Add(new VcardContentLineParameter(VcardParameterNames.Type, AgentType));
            return line.With(VcardPropertyNames.Related, parameters.ToArray(), value);
        }

        internal static void AttachLabels(VcardCard card)
        {
            for (var index = 0; index < card.Properties.Count; index++)
            {
                VcardContentLine address;
                if (IsName(card.Properties[index], VcardPropertyNames.PostalLabel) && TryAttachLabel(card, index, out address))
                {
                    index--;
                }
            }
        }

        private static bool HasOnlyLabelParameters(VcardContentLine label)
        {
            foreach (var parameter in label.ParameterArray)
            {
                if (!IsParameterName(parameter, VcardParameterNames.Type) && !IsParameterName(parameter, VcardParameterNames.Preference) && !IsParameterName(parameter, VcardParameterNames.Language))
                {
                    return false;
                }
            }

            return true;
        }

        private static int FindAddressOfLabel(VcardCard card, VcardContentLine label)
        {
            if (label.Group != null)
            {
                for (var index = 0; index < card.Properties.Count; index++)
                {
                    var line = card.Properties[index];
                    if (IsName(line, VcardPropertyNames.Address) && string.Equals(line.Group, label.Group, StringComparison.OrdinalIgnoreCase) && CanHoldLabel(line, label) && HasNoOtherTypes(line, label)) return index;
                }
            }

            for (var index = 0; index < card.Properties.Count; index++)
            {
                var line = card.Properties[index];
                if (IsName(line, VcardPropertyNames.Address) && line.Group is null && CanHoldLabel(line, label) && HaveSameTypes(line, label)) return index;
            }

            return -1;
        }

        private static bool CanHoldLabel(VcardContentLine address, VcardContentLine label)
        {
            // Anything on the label that the address doesn't say would be lost, so that label stays a property of its own.
            var labelLanguage = VcardPropertyParameterReader.GetValue(label, VcardParameterNames.Language);
            return address.GetParameter(VcardParameterNames.Label) is null &&
                   string.Equals(VcardPropertyParameterReader.GetValue(address, VcardParameterNames.Language), labelLanguage, StringComparison.OrdinalIgnoreCase) &&
                   (label.GetParameter(VcardParameterNames.Preference) is null || address.GetParameter(VcardParameterNames.Preference) != null);
        }

        /// <summary>
        /// Gets whether a label in the group of an address says nothing about its types that would be lost.  With no <c>TYPE</c> of its own, the group alone ties it to the address.
        /// </summary>
        private static bool HasNoOtherTypes(VcardContentLine address, VcardContentLine label)
        {
            return VcardPropertyParameterReader.GetListValues(label, VcardParameterNames.Type).Count == 0 || HaveSameTypes(address, label);
        }

        private static bool HaveSameTypes(VcardContentLine address, VcardContentLine label)
        {
            return new HashSet<string>(GetTypesOrDefault(address), StringComparer.OrdinalIgnoreCase).SetEquals(GetTypesOrDefault(label));
        }

        private static IEnumerable<string> GetTypesOrDefault(VcardContentLine line)
        {
            var types = VcardPropertyParameterReader.GetListValues(line, VcardParameterNames.Type);
            return types.Count == 0 ? DefaultAddressTypes : (IEnumerable<string>)types;
        }

        private static void AttachSortStrings(VcardCard card)
        {
            for (var index = 0; index < card.Properties.Count; index++)
            {
                var line = card.Properties[index];
                if (IsName(line, VcardPropertyNames.SortString) && TryAttachSortString(card, index))
                {
                    index--;
                }
            }
        }

        /// <summary>
        /// Puts a <c>SORT-STRING</c> line on the <c>N</c>, or if there is none the <c>ORG</c>, as a <c>SORT-AS</c>.  One with parameters, or a comma that a <c>SORT-AS</c> would take for
        /// a separator (RFC 6350 §5.9), stays a property of its own.
        /// </summary>
        private static bool TryAttachSortString(VcardCard card, int index)
        {
            var line = card.Properties[index];
            string text;
            if (line.ParameterArray.Length != 0 || !TryUnescape(line.Value, out text) || text.IndexOf(',') >= 0) return false;

            var targetIndex = FindSortTarget(card, VcardPropertyNames.Name);
            if (targetIndex < 0)
            {
                targetIndex = FindSortTarget(card, VcardPropertyNames.Organization);
            }

            if (targetIndex < 0) return false;

            var target = card.Properties[targetIndex];
            var parameters = new List<VcardContentLineParameter>(target.ParameterArray) { new VcardContentLineParameter(VcardParameterNames.SortAs, text) };
            card.Properties[targetIndex] = target.With(target.Name, parameters.ToArray(), target.Value);
            card.Properties.RemoveAt(index);
            return true;
        }

        private static int FindSortTarget(VcardCard card, string name)
        {
            for (var index = 0; index < card.Properties.Count; index++)
            {
                var line = card.Properties[index];
                if (IsName(line, name) && line.GetParameter(VcardParameterNames.SortAs) is null) return index;
            }

            return -1;
        }

        /// <summary>
        /// One conversion of a card from canonical form to vCard 3.0.
        /// </summary>
        private sealed class Downgrade
        {
            private readonly VcardCard _card;
            private readonly List<VcardDiagnostic> _found;
            private readonly List<VcardContentLine> _lines;
            private readonly VcardOptions _options;
            private bool _hasSortString;

            internal Downgrade(VcardCard card, VcardOptions options, List<VcardDiagnostic> found)
            {
                _card = card;
                _options = options;
                _found = found;
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
                _found.Add(new VcardDiagnostic(VcardDiagnosticKind.Lossy, string.Format(CultureInfo.InvariantCulture, "vCard 3.0 can't hold {0} of {1}.", what, line.Name), line.Name, line.Group, line.LineNumber, _card.CardIndex));
            }

            private void ReportLostProperty(VcardContentLine line, string reason)
            {
                _found.Add(new VcardDiagnostic(VcardDiagnosticKind.Lossy, string.Format(CultureInfo.InvariantCulture, "vCard 3.0 can't hold this {0} property: {1}.", line.Name, reason), line.Name, line.Group, line.LineNumber, _card.CardIndex));
            }

            private void Convert(VcardContentLine line)
            {
                if (IsName(line, VcardPropertyNames.Related))
                {
                    ConvertRelated(line);
                    return;
                }

                var info = VcardPropertyInfo.Find(line.Name);
                if (info != null && !info.IsDefinedIn(VcardVersion.V3_0) && !IsStructural(line))
                {
                    ReportLostProperty(line, "RFC 2426 doesn't define it");
                    return;
                }

                if (line.GetParameter(VcardParameterNames.Phonetic) != null && (IsName(line, VcardPropertyNames.Name) || IsName(line, VcardPropertyNames.Address)))
                {
                    ReportLostProperty(line, "it is the pronunciation of another (RFC 9554 §4.6)");
                    return;
                }

                // A property no version defines is passed on as it is, with what its value means unknown.
                if (info is null || IsStructural(line))
                {
                    _lines.Add(line);
                    return;
                }

                var parameters = new List<VcardContentLineParameter>(line.ParameterArray);
                var value = line.Value;
                var label = IsName(line, VcardPropertyNames.Address) ? Find(parameters, VcardParameterNames.Label) : null;
                var sortAs = IsName(line, VcardPropertyNames.Name) || IsName(line, VcardPropertyNames.Organization) ? VcardPropertyParameterReader.GetListValues(line, VcardParameterNames.SortAs) : new List<string>();
                var mediaType = Find(parameters, VcardParameterNames.MediaType);
                Remove(parameters, VcardParameterNames.Label);
                Remove(parameters, VcardParameterNames.SortAs);
                if (IsMedia(line))
                {
                    Remove(parameters, VcardParameterNames.MediaType);
                }

                RemoveVersion40Parameters(line, parameters);
                if (!TryConvertValue(line, parameters, mediaType, ref value)) return;

                ConvertPreference(line, parameters);
                RemoveDisallowedParameters(line, parameters);
                CheckParameterCharacters(line, parameters);
                var converted = line.With(line.Name, parameters.ToArray(), value);
                _lines.Add(converted);
                AddLabel(converted, parameters, label);
                AddSortString(line, sortAs);
            }

            private static bool IsStructural(VcardContentLine line)
            {
                return IsName(line, VcardPropertyNames.Begin) || IsName(line, VcardPropertyNames.End) || IsName(line, VcardPropertyNames.Version);
            }

            private void RemoveVersion40Parameters(VcardContentLine line, List<VcardContentLineParameter> parameters)
            {
                foreach (var name in Version40OnlyParameters)
                {
                    if (Find(parameters, name) != null)
                    {
                        ReportLoss(line, "the parameter " + name);
                        Remove(parameters, name);
                    }
                }

                var language = Find(parameters, VcardParameterNames.Language);
                if (language != null && !Vcard30Conformance.IsRfc1766Tag(language))
                {
                    ReportLoss(line, "the language tag " + language + ", which isn't one of RFC 1766");
                    Remove(parameters, VcardParameterNames.Language);
                }
            }

            private bool TryConvertValue(VcardContentLine line, List<VcardContentLineParameter> parameters, string mediaType, ref string value)
            {
                switch (line.Name.ToUpperInvariant())
                {
                    case VcardPropertyNames.Name:
                        return TryConvertName(line, ref value);
                    case VcardPropertyNames.Address:
                        return TryConvertAddress(line, parameters, ref value);
                    case VcardPropertyNames.Organization:
                        return true;
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
                        Remove(parameters, VcardParameterNames.Value);
                        value = EscapeSemicolons(value);
                        return true;
                    case VcardPropertyNames.Source:
                    case VcardPropertyNames.Url:
                    case VcardPropertyNames.InstantMessaging:
                    case VcardPropertyNames.FreeBusyUrl:
                    case VcardPropertyNames.CalendarAddressUri:
                    case VcardPropertyNames.CalendarAccessUri:
                    case VcardPropertyNames.CalendarUri:
                        value = UnescapeUri(value);
                        return true;
                    case VcardPropertyNames.Photo:
                    case VcardPropertyNames.Logo:
                    case VcardPropertyNames.Sound:
                    case VcardPropertyNames.Key:
                        return TryConvertMedia(line, parameters, mediaType, ref value);
                    default:
                        // Every other type of both versions is text, or a list of it.
                        value = EscapeSemicolons(value);
                        return true;
                }
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

                value = name.WithoutRfc9554Components().ToString(VcardValueFormat.Text, VcardVersion.V3_0);
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
                if (!address.IsDefinedIn(VcardVersion.V3_0))
                {
                    // ref. RFC 2426 §4: adr-value has one text-value in a component.
                    address = address.WithSingleItems();
                    ReportLoss(line, "several values in one component");
                }

                value = address.ToString(VcardValueFormat.Text, VcardVersion.V3_0);

                var types = GetTypes(parameters);
                if (types.RemoveAll(type => ContainsIgnoringCase(Rfc9554AddressTypes, type)) != 0)
                {
                    ReportLoss(line, "the TYPE values of RFC 9554");
                    SetTypes(parameters, types);
                }

                return true;
            }

            private bool TryConvertBirthday(VcardContentLine line, List<VcardContentLineParameter> parameters, ref string value)
            {
                if (VcardValueTypeRegistry.GetValueType(line) == VcardValueType.Text)
                {
                    ReportLostProperty(line, "its value is text");
                    return false;
                }

                VcardDateAndOrTime date;
                if (!VcardDateAndOrTime.TryParse(value.AsSpan(), VcardValueFormat.Text, out date)) return true;

                if (!date.IsDefinedIn(VcardVersion.V3_0))
                {
                    ReportLostProperty(line, "its date isn't complete");
                    return false;
                }

                value = date.ToString(VcardValueFormat.Text, VcardVersion.V3_0);
                Remove(parameters, VcardParameterNames.Value);

                // ref. RFC 2426 §3.1.5: "The default is a single date value.  It can also be reset to a single date-time value."
                if (date.Kind == VcardDateAndOrTimeKind.DateTime)
                {
                    parameters.Add(new VcardContentLineParameter(VcardParameterNames.Value, Vcard30Conformance.DateTimeValueName));
                }

                return true;
            }

            private bool TryConvertRevision(VcardContentLine line, ref string value)
            {
                VcardTimestamp timestamp;
                if (!VcardTimestamp.TryParse(value.AsSpan(), VcardValueFormat.Text, out timestamp)) return true;

                if (!timestamp.IsDefinedIn(VcardVersion.V3_0))
                {
                    ReportLostProperty(line, "its time isn't one vCard 3.0 can write");
                    return false;
                }

                value = timestamp.ToString(VcardValueFormat.Text, VcardVersion.V3_0);
                return true;
            }

            private bool TryConvertTimeZone(VcardContentLine line, List<VcardContentLineParameter> parameters, ref string value)
            {
                switch (VcardValueTypeRegistry.GetValueType(line))
                {
                    case VcardValueType.UtcOffset:
                        VcardUtcOffset offset;
                        if (VcardUtcOffset.TryParse(value.AsSpan(), VcardValueFormat.Text, out offset))
                        {
                            if (!offset.IsDefinedIn(VcardVersion.V3_0))
                            {
                                ReportLostProperty(line, "its offset isn't one vCard 3.0 can write");
                                return false;
                            }

                            value = offset.ToString(VcardValueFormat.Text, VcardVersion.V3_0);
                            Remove(parameters, VcardParameterNames.Value);
                        }

                        return true;
                    case VcardValueType.Text:
                        value = EscapeSemicolons(value);
                        Remove(parameters, VcardParameterNames.Value);
                        parameters.Add(new VcardContentLineParameter(VcardParameterNames.Value, Vcard30Conformance.TextValueName));
                        return true;
                    case VcardValueType.Uri:
                        ReportLostProperty(line, "its value is a URI");
                        return false;
                    default:
                        return true;
                }
            }

            private bool TryConvertGeo(VcardContentLine line, ref string value)
            {
                VcardGeo geo;
                if (VcardGeo.TryParse(value.AsSpan(), VcardValueFormat.Text, out geo) && geo.IsDefinedIn(VcardVersion.V3_0))
                {
                    value = geo.ToString(VcardValueFormat.Text, VcardVersion.V3_0);
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
                    value = EscapeSemicolons(value);
                    return true;
                }

                // ref. RFC 2426 §3.3.1: "A single phone-number value", which RFC 3966 §3 writes after its scheme.
                Uri uri;
                if (!VcardValue.TryParseUri(value.AsSpan(), VcardValueFormat.Text, out uri)) return true;

                var text = uri.OriginalString;
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
                Remove(parameters, VcardParameterNames.Value);
                return true;
            }

            private bool TryConvertMedia(VcardContentLine line, List<VcardContentLineParameter> parameters, string mediaType, ref string value)
            {
                var valueType = VcardValueTypeRegistry.GetValueType(line);
                if (valueType == VcardValueType.Text)
                {
                    value = EscapeSemicolons(value);
                    return true;
                }

                string uriText;
                if (!VcardValue.TryGetUriText(value, out uriText)) return true;

                RemoveUseTypes(line, parameters);
                Remove(parameters, VcardParameterNames.Value);

                MimeDataUri data;
                if (MimeDataUri.TryParse(uriText, out data))
                {
                    value = System.Convert.ToBase64String(data.GetData());
                    parameters.Add(new VcardContentLineParameter(VcardLegacyTokens.EncodingName, VcardLegacyTokens.Version30Base64));
                    if (!IsUntyped(data.MediaType))
                    {
                        AddMediaToken(parameters, GetToken(data.MediaType, line.Name));
                    }

                    return true;
                }

                // ref. RFC 2426 §4: a key is binary or text.
                if (IsName(line, VcardPropertyNames.Key))
                {
                    ReportLostProperty(line, "its value is a URI");
                    return false;
                }

                value = uriText;
                parameters.Insert(0, new VcardContentLineParameter(VcardParameterNames.Value, Vcard30Conformance.UriValueName));

                MimeMediaType parsedType;
                if (mediaType != null && MimeMediaType.TryParse(mediaType.AsSpan(), out parsedType))
                {
                    AddMediaToken(parameters, GetToken(parsedType, line.Name));
                }

                return true;
            }

            private void RemoveUseTypes(VcardContentLine line, List<VcardContentLineParameter> parameters)
            {
                var types = GetTypes(parameters);
                if (types.RemoveAll(type => ContainsIgnoringCase(Version40MediaUses, type)) != 0)
                {
                    ReportLoss(line, "the TYPE values home and work, since its TYPE says the format");
                    SetTypes(parameters, types);
                }
            }

            private static void AddMediaToken(List<VcardContentLineParameter> parameters, string token)
            {
                var types = GetTypes(parameters);
                types.Insert(0, token);
                SetTypes(parameters, types);
            }

            private void ConvertRelated(VcardContentLine line)
            {
                var types = VcardPropertyParameterReader.GetListValues(line, VcardParameterNames.Type);
                if (!ContainsIgnoringCase(types, AgentType))
                {
                    ReportLostProperty(line, "RFC 2426 has only AGENT, which is its TYPE=agent");
                    return;
                }

                var parameters = new List<VcardContentLineParameter>(line.ParameterArray);
                var valueType = VcardValueTypeRegistry.GetValueType(line);
                if (types.Count > 1)
                {
                    ReportLoss(line, "the TYPE values other than agent");
                }

                Remove(parameters, VcardParameterNames.Type);
                Remove(parameters, VcardParameterNames.Value);
                RemoveVersion40Parameters(line, parameters);
                ConvertPreference(line, parameters);
                var value = line.Value;
                if (valueType == VcardValueType.Text)
                {
                    value = ConvertAgentText(line, parameters, value);
                }
                else
                {
                    Uri uri;
                    if (VcardValue.TryParseUri(value.AsSpan(), VcardValueFormat.Text, out uri))
                    {
                        value = uri.OriginalString;
                    }

                    parameters.Insert(0, new VcardContentLineParameter(VcardParameterNames.Value, Vcard30Conformance.UriValueName));
                }

                RemoveDisallowedParameters(line.With(VcardPropertyNames.Agent, line.ParameterArray, value), parameters);
                _lines.Add(line.With(VcardPropertyNames.Agent, parameters.ToArray(), value));
            }

            private string ConvertAgentText(VcardContentLine line, List<VcardContentLineParameter> parameters, string value)
            {
                string text;
                VcardCard nested;
                if (TryUnescape(value, out text) && VcardNestedCards.TryReadHeld(text, out nested))
                {
                    // ref. RFC 2426 §4: "Value MUST be a valid vCard object", and §5: "The VERSION, N and FN types MUST be specified in a vCard."
                    var nestedFound = new List<VcardDiagnostic>();
                    var nestedLines = VcardVersionMapping.Version30.FromCanonical(nested, _options, nestedFound);
                    var nestedChecks = new List<VcardDiagnostic>();
                    Vcard30Conformance.Validate(nestedLines, nested, VcardNestedCards.HeldCardOptions, 0, nestedChecks);
                    nestedFound.AddRange(nestedChecks.FindAll(diagnostic => diagnostic.Kind == VcardDiagnosticKind.MissingRequiredProperty));
                    foreach (var diagnostic in nestedFound)
                    {
                        _found.Add(new VcardDiagnostic(diagnostic.Kind, "In the card of an AGENT: " + diagnostic.Message, diagnostic.PropertyName, diagnostic.Group, line.LineNumber, _card.CardIndex));
                    }

                    return VcardValue.FormatText(VcardNestedCards.Write(nested, VcardVersion.V3_0), VcardValueFormat.Text, VcardVersion.V3_0);
                }

                // ref. RFC 2426 §3.5.4: "It can also be reset to either a single text or uri value."
                parameters.Insert(0, new VcardContentLineParameter(VcardParameterNames.Value, Vcard30Conformance.TextValueName));
                return EscapeSemicolons(value);
            }

            private void ConvertPreference(VcardContentLine line, List<VcardContentLineParameter> parameters)
            {
                var text = Find(parameters, VcardParameterNames.Preference);
                if (text is null) return;

                Remove(parameters, VcardParameterNames.Preference);
                int preference;
                var isLowest = VcardPropertyParameterReader.TryParsePreference(text, out preference) && preference == GetLowestPreference(_card.Properties, line.Name);
                if (!isLowest)
                {
                    // ref. RFC 2426 §3.3.1: "pref" says only that an instance is preferred, so it has no order after the first.
                    ReportLoss(line, "the order of a PREF that isn't the lowest");
                    return;
                }

                if (!Vcard30Conformance.AllowsPreference(line.Name) && !_options.AllowInvalidValues)
                {
                    ReportLoss(line, "a preference");
                    return;
                }

                var types = GetTypes(parameters);
                types.Add(PreferredType);
                SetTypes(parameters, types);
            }

            private void RemoveDisallowedParameters(VcardContentLine line, List<VcardContentLineParameter> parameters)
            {
                // AllowInvalidValues keeps such a parameter, as its documentation says, and the check of the line accepts it.
                if (_options.AllowInvalidValues) return;

                for (var index = parameters.Count - 1; index >= 0; index--)
                {
                    var name = parameters[index].Name;
                    var isLegacy = string.Equals(name, VcardLegacyTokens.EncodingName, StringComparison.OrdinalIgnoreCase) || string.Equals(name, VcardLegacyTokens.CharsetName, StringComparison.OrdinalIgnoreCase);
                    if (!isLegacy && !Vcard30Conformance.IsParameterAllowed(line.Name, name))
                    {
                        ReportLoss(line, "the parameter " + name);
                        parameters.RemoveAt(index);
                    }
                }
            }

            private void CheckParameterCharacters(VcardContentLine line, List<VcardContentLineParameter> parameters)
            {
                foreach (var parameter in parameters)
                {
                    foreach (var value in parameter.ValueArray)
                    {
                        // ref. RFC 2426 §4: QSAFE-CHAR has no DQUOTE and no control character, and RFC 6868 updates only RFC 6350.
                        if (value.IndexOfAny(new[] { '"', '\r', '\n' }) >= 0)
                        {
                            ReportLoss(line, "a double quote or a line break in the parameter " + parameter.Name);
                        }
                    }
                }
            }

            private void AddLabel(VcardContentLine address, List<VcardContentLineParameter> parameters, string label)
            {
                if (label is null) return;

                // ref. RFC 6350 §6.3.1: its example writes a line break in a LABEL as "\n", where RFC 6868 would write it "^n".
                var labelParameters = new List<VcardContentLineParameter>();
                foreach (var parameter in parameters)
                {
                    if (IsParameterName(parameter, VcardParameterNames.Type) || IsParameterName(parameter, VcardParameterNames.Language))
                    {
                        labelParameters.Add(parameter);
                    }
                }

                var text = VcardValue.FormatText(label.Replace("\\n", "\n"), VcardValueFormat.Text, VcardVersion.V3_0);
                _lines.Add(address.With(VcardPropertyNames.PostalLabel, labelParameters.ToArray(), text));
            }

            private void AddSortString(VcardContentLine line, List<string> sortAs)
            {
                if (sortAs.Count == 0) return;

                // ref. RFC 2426 §3.6.5: "A single text value."
                if (sortAs.Count > 1 || _hasSortString)
                {
                    ReportLoss(line, "more than one sort string");
                }

                if (_hasSortString) return;

                _hasSortString = true;
                _lines.Add(line.With(VcardPropertyNames.SortString, new VcardContentLineParameter[0], VcardValue.FormatText(sortAs[0], VcardValueFormat.Text, VcardVersion.V3_0)));
            }
        }
    }
}
