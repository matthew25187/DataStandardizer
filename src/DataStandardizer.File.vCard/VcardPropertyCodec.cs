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
    /// Converts between the content lines of one property and its typed values.
    /// </summary>
    internal sealed class VcardPropertyCodec<TValue>
    {
        private readonly VcardValueType _defaultValueType;
        private readonly Func<TValue, string> _format;
        private readonly Func<TValue, VcardValueType> _getValueType;
        private readonly VcardValueParser<TValue> _parse;

        internal VcardPropertyCodec(string name, VcardValueParser<TValue> parse, Func<TValue, string> format, Func<TValue, VcardValueType> getValueType)
        {
            Name = name;
            _parse = parse;
            _format = format;
            _getValueType = getValueType;
            _defaultValueType = VcardValueTypeRegistry.GetDefaultValueType(name.AsSpan());
        }

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
            var valueType = VcardValueTypeRegistry.GetValueType(line);
            string text;
            if (!VcardValueTypeRegistry.IsAllowedValueType(line.Name.AsSpan(), valueType) || !VcardConformance.IsUnderstoodCalendarScale(line) ||
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
        /// Gets the content line of a value.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> can't be written, or is of a type the property doesn't allow.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal VcardContentLine CreateLine(TValue value, VcardPropertyParameters? parameters)
#else
        internal VcardContentLine CreateLine(TValue value, VcardPropertyParameters parameters)
#endif
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var valueType = _getValueType(value);
            if (!VcardValueTypeRegistry.IsAllowedValueType(Name.AsSpan(), valueType))
                throw new ArgumentException("The property " + Name + " doesn't allow a value of type " + valueType + ".", nameof(value));

            var valueParameter = valueType == _defaultValueType ? string.Empty : VcardValueTypeRegistry.GetValueTypeName(valueType);
            var text = _format(value);
            return new VcardContentLine(parameters?.Group, Name, (parameters ?? new VcardPropertyParameters()).ToContentLineParameters(valueParameter), text);
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

        internal static VcardPropertyCodec<IReadOnlyList<string>> TextList(string name)
        {
            return new VcardPropertyCodec<IReadOnlyList<string>>(name, ParseTextList, FormatTextList, value => VcardValueType.Text);
        }

        internal static VcardPropertyCodec<Uri> Uri(string name)
        {
            return new VcardPropertyCodec<Uri>(name, ParseUri, FormatUri, value => VcardValueType.Uri);
        }

        internal static VcardPropertyCodec<VcardTypedValue> Typed(string name)
        {
            return new VcardPropertyCodec<VcardTypedValue>(name, ParseTyped, FormatTyped, GetTypedValueType);
        }

        internal static VcardPropertyCodec<VcardName> Name()
        {
            return new VcardPropertyCodec<VcardName>(VcardPropertyNames.Name, (string text, VcardValueType valueType, out VcardName value) =>
                VcardName.TryParse(text.AsSpan(), VcardValueFormat.Text, out value), value => value.ToString(VcardValueFormat.Text), value => VcardValueType.Text);
        }

        internal static VcardPropertyCodec<VcardAddress> Address()
        {
            return new VcardPropertyCodec<VcardAddress>(VcardPropertyNames.Address, (string text, VcardValueType valueType, out VcardAddress value) =>
                VcardAddress.TryParse(text.AsSpan(), VcardValueFormat.Text, out value), value => value.ToString(VcardValueFormat.Text), value => VcardValueType.Text);
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

        internal static VcardPropertyCodec<VcardTimestamp> Timestamp()
        {
            return new VcardPropertyCodec<VcardTimestamp>(VcardPropertyNames.Revision, (string text, VcardValueType valueType, out VcardTimestamp value) =>
                VcardTimestamp.TryParse(text.AsSpan(), VcardValueFormat.Text, out value), value => value.ToString(VcardValueFormat.Text), value => VcardValueType.Timestamp);
        }

        internal static VcardPropertyCodec<Bcp47LanguageTag> LanguageTag()
        {
            return new VcardPropertyCodec<Bcp47LanguageTag>(VcardPropertyNames.Language, (string text, VcardValueType valueType, out Bcp47LanguageTag value) =>
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

        private static string FormatText(string value)
        {
            return VcardValue.FormatText(value, VcardValueFormat.Text, VcardVersion.V4_0);
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
            return value.OriginalString;
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

        private static VcardValueType GetTypedValueType(VcardTypedValue value)
        {
            if (value == default(VcardTypedValue))
                throw new ArgumentException("The value is not initialised.", nameof(value));

            return value.ValueType;
        }

        private static string FormatTyped(VcardTypedValue value)
        {
            return value.ToString();
        }
    }
}
