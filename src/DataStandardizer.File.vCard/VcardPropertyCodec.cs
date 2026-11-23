using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using DataStandardizer.LanguageTag;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Parses a value of a value type from its text as it appears in a content line.
    /// </summary>
    internal delegate bool VcardValueParser<TValue>(string text, VcardValueType valueType, out TValue value);

    /// <summary>
    /// Parses a value that a line of a card read from an older version holds in the form of that version, because vCard 4.0 has no form for it.
    /// </summary>
    internal delegate bool VcardSourceFormParser<TValue>(string text, VcardValueType valueType, VcardVersion version, out TValue value);

    /// <summary>
    /// Converts between the content lines of one property and its typed values.
    /// </summary>
    internal sealed class VcardPropertyCodec<TValue>
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly Func<TValue, TValue>? _copy;
        private readonly Func<TValue, TValue>? _withBackwardCompatibleComponents;
#else
        private readonly Func<TValue, TValue> _copy;
        private readonly Func<TValue, TValue> _withBackwardCompatibleComponents;
#endif
        private readonly VcardValueType _defaultValueType;
        private readonly Func<TValue, string> _format;
        private readonly Func<TValue, VcardValueType> _getValueType;
        private readonly VcardValueParser<TValue> _parse;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal VcardPropertyCodec(string name, VcardValueParser<TValue> parse, Func<TValue, string> format, Func<TValue, VcardValueType> getValueType, Func<TValue, TValue>? copy = null,
            Func<TValue, TValue>? withBackwardCompatibleComponents = null)
#else
        internal VcardPropertyCodec(string name, VcardValueParser<TValue> parse, Func<TValue, string> format, Func<TValue, VcardValueType> getValueType, Func<TValue, TValue> copy = null,
            Func<TValue, TValue> withBackwardCompatibleComponents = null)
#endif
        {
            Name = name;
            _copy = copy;
            _withBackwardCompatibleComponents = withBackwardCompatibleComponents;
            _parse = parse;
            _format = format;
            _getValueType = getValueType;
            _defaultValueType = VcardValueTypeRegistry.GetDefaultValueType(name.AsSpan());
        }

        /// <summary>
        /// Gets a flag indicating if the property is one that only older versions define, so that the registry of vCard 4.0 doesn't know its value type, and a line has no <c>VALUE</c> to read.
        /// </summary>
        internal bool IsLegacy { get; set; }

        /// <summary>
        /// Gets the parser of a value that a card read from an older version keeps in the form of that version, because vCard 4.0 has no form for it; or <c>null</c> if the property
        /// has no such value.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal VcardSourceFormParser<TValue>? ParseSourceForm { get; set; }
#else
        internal VcardSourceFormParser<TValue> ParseSourceForm { get; set; }
#endif

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Gets the wrapper of a line.
        /// </summary>
        /// <returns><c>false</c> if the value of the line isn't valid, or a <c>VALUE</c> parameter names a type the property doesn't allow.  An application must also ignore a
        /// property with a <c>CALSCALE</c> it doesn't understand (RFC 6350 §5.8).</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal bool TryCreateProperty(VcardContentLine line, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out VcardProperty<TValue>? property)
#else
        internal bool TryCreateProperty(VcardContentLine line, out VcardProperty<TValue> property)
#endif
        {
            property = null;
            string text;
            var valueType = VcardValueTypeRegistry.GetValueType(line);
            if (IsLegacy)
            {
                if (!VcardConformance.TryGetValueText(line, out text)) return false;
            }
            else if (!VcardValueTypeRegistry.IsAllowedValueType(line.Name.AsSpan(), valueType) || !VcardConformance.IsUnderstoodCalendarScale(line) ||
                     !VcardConformance.TryGetValueText(line, out text))
            {
                return false;
            }

            TValue value;
            if (!_parse(text, valueType, out value)) return false;

            property = new VcardProperty<TValue>(line, value);
            return true;
        }

        /// <summary>
        /// Gets the wrapper of a line, or of a line whose value a card read from an older version keeps in the form of that version, because vCard 4.0 has no form for it.  Only a view
        /// of an older version uses the second kind, which isn't a valid vCard 4.0 property.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="sourceVersion">The version the card was read from; or <c>null</c> if it was built in code.</param>
        /// <param name="property">The wrapper, if the line has a valid value.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal bool TryCreateProperty(VcardContentLine line, VcardVersion? sourceVersion, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out VcardProperty<TValue>? property)
#else
        internal bool TryCreateProperty(VcardContentLine line, VcardVersion? sourceVersion, out VcardProperty<TValue> property)
#endif
        {
            if (TryCreateProperty(line, out property)) return true;
            if (ParseSourceForm is null || !sourceVersion.HasValue || sourceVersion.Value == VcardVersion.V4_0) return false;

            string text;
            TValue value;
            if (!VcardConformance.TryGetValueText(line, out text) || !ParseSourceForm(text, GetSourceValueType(line, sourceVersion.Value), sourceVersion.Value, out value)) return false;

            property = new VcardProperty<TValue>(line, value);
            return true;
        }

        /// <summary>
        /// Gets the value type of a line kept in the form of an older version.  ref. vCard 2.1 §2.1.8: a <c>VALUE</c> is where the value is, and <c>INLINE</c>, its default, says that
        /// it is on the line, so the property has its default type.
        /// </summary>
        private static VcardValueType GetSourceValueType(VcardContentLine line, VcardVersion sourceVersion)
        {
            var valueParameter = line.GetParameter(VcardParameterNames.Value);
            return sourceVersion == VcardVersion.V2_1 && valueParameter != null && string.Equals(valueParameter.ValueArray[0], VcardLegacyTokens.InlineValue, StringComparison.OrdinalIgnoreCase)
                ? VcardValueTypeRegistry.GetDefaultValueType(line.Name.AsSpan())
                : VcardValueTypeRegistry.GetValueType(line);
        }

        /// <summary>
        /// Gets the content line of a value.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised, can't be written, or is of a type the property doesn't allow; or
        /// <paramref name="parameters"/> has a <c>VALUE</c>, which comes from the value.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal VcardContentLine CreateLine(TValue value, VcardPropertyParameters? parameters)
#else
        internal VcardContentLine CreateLine(TValue value, VcardPropertyParameters parameters)
#endif
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (EqualityComparer<TValue>.Default.Equals(value, default(TValue)!))
#else
            if (EqualityComparer<TValue>.Default.Equals(value, default(TValue)))
#endif
                throw new ArgumentException("The value is not initialised.", nameof(value));
            if (parameters != null)
            {
                foreach (var parameter in parameters.AdditionalParameters)
                {
                    if (string.Equals(parameter.Name, VcardParameterNames.Value, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("A VALUE parameter comes from the value, so it can't be an additional parameter.", nameof(parameters));
                }
            }

            var valueType = _getValueType(value);
            if (!IsLegacy && !VcardValueTypeRegistry.IsAllowedValueType(Name.AsSpan(), valueType))
                throw new ArgumentException("The property " + Name + " doesn't allow a value of type " + valueType + ".", nameof(value));

            var valueParameter = IsLegacy || valueType == _defaultValueType ? string.Empty : VcardValueTypeRegistry.GetValueTypeName(valueType);
            var contentLineParameters = (parameters ?? new VcardPropertyParameters()).ToContentLineParameters(valueParameter);
            var isPhonetic = contentLineParameters.Exists(parameter => string.Equals(parameter.Name, VcardParameterNames.Phonetic, StringComparison.OrdinalIgnoreCase));
            return new VcardContentLine(parameters?.Group, Name, contentLineParameters, _format(GetFormToWrite(isPhonetic, value)));
        }

        /// <summary>
        /// Gets the wrapper of a line just created from a value, which keeps the form of it that the line holds, or a copy of a value the caller could go on to change.
        /// </summary>
        internal VcardProperty<TValue> CreateProperty(VcardContentLine line, TValue value)
        {
            var form = GetFormToWrite(line.GetParameter(VcardParameterNames.Phonetic) != null, value);
            return new VcardProperty<TValue>(line, _copy is null ? form : _copy(form));
        }

        /// <summary>
        /// Gets the form of a value to write on a line.  ref. RFC 9554 §4.6: a component set on a line with PHONETIC "MUST be set at that same position in the related property", which is
        /// not this line, so that outranks the SHOULD of §2.1 and §2.2 to also fill in the older components.
        /// </summary>
        private TValue GetFormToWrite(bool isPhonetic, TValue value)
        {
            return _withBackwardCompatibleComponents is null || isPhonetic ? value : _withBackwardCompatibleComponents(value);
        }
    }

    /// <summary>
    /// Creates the codecs of the properties.
    /// </summary>
    internal static class VcardPropertyCodecs
    {
        internal static VcardPropertyCodec<string> Text(string name)
        {
            return new VcardPropertyCodec<string>(name, ParseText, FormatText, GetTextType);
        }

        /// <summary>
        /// Gets the codec of a text property that only older versions define.
        /// </summary>
        internal static VcardPropertyCodec<string> LegacyText(string name)
        {
            return new VcardPropertyCodec<string>(name, ParseText, FormatText, GetTextType) { IsLegacy = true };
        }

        /// <summary>
        /// Gets the codec of a URI property that only older versions define.
        /// </summary>
        internal static VcardPropertyCodec<Uri> LegacyUri(string name)
        {
            return new VcardPropertyCodec<Uri>(name, ParseUri, FormatUri, value => VcardValueType.Uri) { IsLegacy = true };
        }

        /// <summary>
        /// Parses the text of a value in canonical form.
        /// </summary>
        internal static bool TryParseText(string text, out string value)
        {
            return ParseText(text, VcardValueType.Text, out value);
        }

        internal static VcardPropertyCodec<string> Token(string name)
        {
            return new VcardPropertyCodec<string>(name, ParseToken, FormatText, GetTextType);
        }

        internal static VcardPropertyCodec<IReadOnlyList<string>> TextList(string name)
        {
            return new VcardPropertyCodec<IReadOnlyList<string>>(name, ParseTextList, FormatTextList, value => VcardValueType.Text, CopyTextList);
        }

        internal static VcardPropertyCodec<string> Xml()
        {
            return new VcardPropertyCodec<string>(VcardPropertyNames.Xml, ParseXml, FormatXml, GetTextType);
        }

        internal static VcardPropertyCodec<Uri> Uri(string name)
        {
            return new VcardPropertyCodec<Uri>(name, ParseUri, FormatUri, value => VcardValueType.Uri);
        }

        /// <summary>
        /// Gets the codec of a property whose value is a URI of media, which can be longer than <see cref="System.Uri"/> holds on .NET Framework.
        /// </summary>
        internal static VcardPropertyCodec<VcardMediaUri> MediaUri(string name)
        {
            return new VcardPropertyCodec<VcardMediaUri>(name, ParseMediaUri, FormatMediaUri, value => VcardValueType.Uri);
        }

        internal static VcardPropertyCodec<VcardTypedValue> Typed(string name)
        {
            return new VcardPropertyCodec<VcardTypedValue>(name, ParseTyped, FormatTyped, value => value.ValueType);
        }

        /// <summary>
        /// Gets the codec of <c>BDAY</c>, whose date-time can have a time-secfrac in vCard 3.0 (RFC 2425 §5.8.4), which vCard 4.0 has no form for.
        /// </summary>
        internal static VcardPropertyCodec<VcardTypedValue> Birthday()
        {
            var codec = Typed(VcardPropertyNames.Birthday);
            codec.ParseSourceForm = ParseDateAndOrTimeSourceForm;
            return codec;
        }

        /// <summary>
        /// Gets the codec of <c>TZ</c>, whose offset can have an hour of 24 in vCard 2.1 (§2.4.5, which cites ISO 8601), which vCard 4.0 has no form for.
        /// </summary>
        internal static VcardPropertyCodec<VcardTypedValue> TimeZone()
        {
            var codec = Typed(VcardPropertyNames.TimeZone);
            codec.ParseSourceForm = ParseUtcOffsetSourceForm;
            return codec;
        }

        internal static VcardPropertyCodec<VcardName> Name()
        {
            return new VcardPropertyCodec<VcardName>(VcardPropertyNames.Name, (string text, VcardValueType valueType, out VcardName value) =>
                VcardName.TryParse(text.AsSpan(), VcardValueFormat.Text, out value), value => value.ToString(VcardValueFormat.Text),
                value => VcardValueType.Text, null, value => value.WithBackwardCompatibleComponents());
        }

        internal static VcardPropertyCodec<VcardAddress> Address()
        {
            return new VcardPropertyCodec<VcardAddress>(VcardPropertyNames.Address, (string text, VcardValueType valueType, out VcardAddress value) =>
                VcardAddress.TryParse(text.AsSpan(), VcardValueFormat.Text, out value), value => value.ToString(VcardValueFormat.Text),
                value => VcardValueType.Text, null, value => value.WithBackwardCompatibleComponents());
        }

        internal static VcardPropertyCodec<VcardOrganization> Organization()
        {
            return new VcardPropertyCodec<VcardOrganization>(VcardPropertyNames.Organization, (string text, VcardValueType valueType, out VcardOrganization value) =>
                VcardOrganization.TryParse(text.AsSpan(), VcardValueFormat.Text, out value), value => value.ToString(VcardValueFormat.Text), value => VcardValueType.Text);
        }

        internal static VcardPropertyCodec<VcardGender> Gender()
        {
            return new VcardPropertyCodec<VcardGender>(VcardPropertyNames.Gender, (string text, VcardValueType valueType, out VcardGender value) =>
                VcardGender.TryParse(text.AsSpan(), VcardValueFormat.Text, out value), value => value.ToString(VcardValueFormat.Text), value => VcardValueType.Text);
        }

        internal static VcardPropertyCodec<VcardClientPidMap> ClientPidMap()
        {
            return new VcardPropertyCodec<VcardClientPidMap>(VcardPropertyNames.ClientPidMap, (string text, VcardValueType valueType, out VcardClientPidMap value) =>
                VcardClientPidMap.TryParse(text.AsSpan(), VcardValueFormat.Text, out value), value => value.ToString(), value => VcardValueType.Text);
        }

        internal static VcardPropertyCodec<VcardTimestamp> Timestamp(string name)
        {
            return new VcardPropertyCodec<VcardTimestamp>(name, (string text, VcardValueType valueType, out VcardTimestamp value) =>
                VcardTimestamp.TryParse(text.AsSpan(), VcardValueFormat.Text, out value), FormatTimestamp, value => VcardValueType.Timestamp);
        }

        /// <summary>
        /// Gets the codec of <c>REV</c>, whose timestamp can have a time-secfrac in vCard 3.0 (RFC 2425 §5.8.4) or an hour of 24 in vCard 2.1 (§2.6.2, which cites ISO 8601), which
        /// vCard 4.0 has no form for.
        /// </summary>
        internal static VcardPropertyCodec<VcardTimestamp> Revision()
        {
            var codec = Timestamp(VcardPropertyNames.Revision);
            codec.ParseSourceForm = ParseTimestampSourceForm;
            return codec;
        }

        internal static VcardPropertyCodec<Bcp47LanguageTag> LanguageTag(string name)
        {
            return new VcardPropertyCodec<Bcp47LanguageTag>(name, (string text, VcardValueType valueType, out Bcp47LanguageTag value) =>
                VcardValue.TryParseLanguageTag(text.AsSpan(), VcardValueFormat.Text, out value), value => value.ToString(), value => VcardValueType.LanguageTag);
        }

        private static VcardValueType GetTextType(string value)
        {
            return VcardValueType.Text;
        }

        private static bool ParseText(string text, VcardValueType valueType, out string value)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? parsed;
            var isValid = VcardValue.TryParseText(text.AsSpan(), VcardValueFormat.Text, out parsed);
            value = parsed ?? string.Empty;
            return isValid;
#else
            return VcardValue.TryParseText(text.AsSpan(), VcardValueFormat.Text, out value);
#endif
        }

        private static bool ParseToken(string text, VcardValueType valueType, out string value)
        {
            value = text;
            return VcardConformance.IsToken(text);
        }

        private static string FormatText(string value)
        {
            return VcardValue.FormatText(value, VcardValueFormat.Text, VcardVersion.V4_0);
        }

        private static bool ParseXml(string text, VcardValueType valueType, out string value)
        {
            return ParseText(text, valueType, out value) && VcardConformance.IsValidXmlElement(value);
        }

        private static string FormatXml(string value)
        {
            if (!VcardConformance.IsValidXmlElement(value))
                throw new ArgumentException("RFC 6350 §6.1.5 requires one XML element, in an explicit namespace that isn't that of vCard 4.0.", nameof(value));

            return FormatText(value);
        }

        private static IReadOnlyList<string> CopyTextList(IReadOnlyList<string> value)
        {
            return new ReadOnlyCollection<string>(new List<string>(value));
        }

        private static bool ParseTextList(string text, VcardValueType valueType, out IReadOnlyList<string> value)
        {
            var items = new List<string>();
            value = new ReadOnlyCollection<string>(items);
            foreach (var item in VcardValue.EnumerateList(text.AsSpan(), VcardValueFormat.Text))
            {
                string itemText;
                if (!ParseText(item.ToString(), VcardValueType.Text, out itemText)) return false;

                items.Add(itemText);
            }

            return true;
        }

        private static string FormatTextList(IReadOnlyList<string> value)
        {
            var builder = new StringBuilder();
            for (var index = 0; index < value.Count; index++)
            {
                var item = value[index];
                if (item is null)
                    throw new ArgumentException("The items cannot be null.", nameof(value));

                if (index != 0)
                {
                    builder.Append(',');
                }

                builder.Append(FormatText(item));
            }

            return builder.ToString();
        }

        private static bool ParseUri(string text, VcardValueType valueType, out Uri value)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Uri? parsed;
            var isValid = VcardValue.TryParseUri(text.AsSpan(), VcardValueFormat.Text, out parsed);
            value = parsed!;
            return isValid;
#else
            return VcardValue.TryParseUri(text.AsSpan(), VcardValueFormat.Text, out value);
#endif
        }

        private static string FormatUri(Uri value)
        {
            VcardValue.CheckUri(value, nameof(value));
            return VcardValue.EscapeUri(value.OriginalString);
        }

        private static bool ParseMediaUri(string text, VcardValueType valueType, out VcardMediaUri value)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            value = null!;
#else
            value = null;
#endif
            // ref. VcardValue.HasOneLetterScheme: such a URI, like a long one, has no Uri on any runtime, so it is held as text.
            if (text.Length <= VcardValue.MaxUriLengthOnEveryRuntime && !VcardValue.HasOneLetterScheme(text.AsSpan()))
            {
                Uri uri;
                if (!ParseUri(text, valueType, out uri)) return false;

                value = VcardMediaUri.FromParsedUri(uri);
                return true;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? uriText;
            VcardMediaUri? parsed;
#else
            string uriText;
            VcardMediaUri parsed;
#endif
            if (!VcardValue.TryGetUriText(text, out uriText) || !VcardMediaUri.TryFromText(uriText, out parsed)) return false;

            value = parsed;
            return true;
        }

        private static string FormatMediaUri(VcardMediaUri value)
        {
            return VcardValue.EscapeUri(value.Text);
        }

        private static bool ParseTyped(string text, VcardValueType valueType, out VcardTypedValue value)
        {
            value = default(VcardTypedValue);
            switch (valueType)
            {
                case VcardValueType.Text:
                    string parsedText;
                    if (!ParseText(text, valueType, out parsedText)) return false;

                    value = new VcardTypedValue(parsedText);
                    return true;
                case VcardValueType.Uri:
                    Uri parsedUri;
                    if (!ParseUri(text, valueType, out parsedUri)) return false;

                    value = new VcardTypedValue(parsedUri);
                    return true;
                case VcardValueType.DateAndOrTime:
                    VcardDateAndOrTime parsedDateAndOrTime;
                    if (!VcardDateAndOrTime.TryParse(text.AsSpan(), VcardValueFormat.Text, out parsedDateAndOrTime)) return false;

                    value = new VcardTypedValue(parsedDateAndOrTime);
                    return true;
                case VcardValueType.UtcOffset:
                    VcardUtcOffset parsedUtcOffset;
                    if (!VcardUtcOffset.TryParse(text.AsSpan(), VcardValueFormat.Text, out parsedUtcOffset)) return false;

                    value = new VcardTypedValue(parsedUtcOffset);
                    return true;
                default:
                    return false;
            }
        }

        private static string FormatTyped(VcardTypedValue value)
        {
            switch (value.ValueType)
            {
                case VcardValueType.DateAndOrTime:
                    CheckDefinedInVersion40(value.DateAndOrTime.IsDefinedIn(VcardVersion.V4_0));
                    break;
                case VcardValueType.UtcOffset:
                    CheckDefinedInVersion40(value.UtcOffset.IsDefinedIn(VcardVersion.V4_0));
                    break;
            }

            return value.ToString();
        }

        private static string FormatTimestamp(VcardTimestamp value)
        {
            CheckDefinedInVersion40(value.IsDefinedIn(VcardVersion.V4_0));
            return value.ToString(VcardValueFormat.Text);
        }

        /// <summary>
        /// A card holds a value in canonical form, which is that of vCard 4.0, so one that vCard 4.0 has no form for, such as a time with a fraction of a second, can't be written to it.
        /// </summary>
        private static void CheckDefinedInVersion40(bool isDefined)
        {
            if (!isDefined)
                throw new ArgumentException("vCard 4.0, the canonical form of a card, has no form for this value.", "value");
        }

        /// <summary>
        /// ref. RFC 2426 §3.1.5: "The default is a single date value.  It can also be reset to a single date-time value."  Its §4 says "Value MUST match value type".
        /// </summary>
        private static bool ParseDateAndOrTimeSourceForm(string text, VcardValueType valueType, VcardVersion version, out VcardTypedValue value)
        {
            value = default(VcardTypedValue);
            VcardDateAndOrTime date;
            if (!VcardDateAndOrTime.TryParse(text.AsSpan(), VcardValueFormat.Text, version, out date) || date.IsDefinedIn(VcardVersion.V4_0)) return false;

            switch (valueType)
            {
                case VcardValueType.DateAndOrTime:
                    break;
                case VcardValueType.Date:
                    if (date.Kind != VcardDateAndOrTimeKind.Date) return false;

                    break;
                case VcardValueType.DateTime:
                    if (date.Kind != VcardDateAndOrTimeKind.DateTime) return false;

                    break;
                default:
                    return false;
            }

            value = new VcardTypedValue(date);
            return true;
        }

        /// <summary>
        /// ref. RFC 2426 §3.6.4: "The default is a single date-time value", which a card in canonical form keeps as <c>VALUE=date-time</c> when vCard 4.0 has no form for it.
        /// </summary>
        private static bool ParseTimestampSourceForm(string text, VcardValueType valueType, VcardVersion version, out VcardTimestamp value)
        {
            value = default(VcardTimestamp);
            return (valueType == VcardValueType.Timestamp || valueType == VcardValueType.DateTime) && VcardTimestamp.TryParse(text.AsSpan(), VcardValueFormat.Text, version, out value) &&
                   !value.IsDefinedIn(VcardVersion.V4_0);
        }

        /// <summary>
        /// A card in canonical form says that a value of <c>TZ</c> is an offset with <c>VALUE=utc-offset</c>, because its default type in vCard 4.0 is text (RFC 6350 §6.5.1).
        /// </summary>
        private static bool ParseUtcOffsetSourceForm(string text, VcardValueType valueType, VcardVersion version, out VcardTypedValue value)
        {
            value = default(VcardTypedValue);
            VcardUtcOffset offset;
            if (valueType != VcardValueType.UtcOffset || !VcardUtcOffset.TryParse(text.AsSpan(), VcardValueFormat.Text, version, out offset) || offset.IsDefinedIn(VcardVersion.V4_0))
                return false;

            value = new VcardTypedValue(offset);
            return true;
        }
    }
}
