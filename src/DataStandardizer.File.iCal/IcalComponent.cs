using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A component of iCalendar data (RFC 5545 §3.6): a name, an ordered list of properties, and the components nested in it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The types derived from this one give typed access to the properties of the components RFC 5545 and RFC 7986 define.
    /// <see cref="IcalReader"/> creates an instance of this type itself for a component it does not recognise, such as an <c>X-</c>
    /// component.
    /// </para>
    /// <para>
    /// Each property is held as an <see cref="IcalContentLine"/>, so its parameters stay reachable and properties that have no typed accessor,
    /// including unknown and <c>X-</c> properties, are written back as they were read.  The typed accessors read from and write to
    /// <see cref="Properties"/>.  A getter reads the first property of its name.  A setter replaces every property of its name with one property
    /// in the place of the first, keeping the parameters of the first that the value does not set; <c>null</c> removes them all.
    /// </para>
    /// </remarks>
    public class IcalComponent
    {
        /// <summary>
        /// The options of an occurrence expansion that is given none.
        /// </summary>
        internal static readonly IcalOptions DefaultOptions = new IcalOptions();

        /// <summary>
        /// Creates a component.
        /// </summary>
        /// <param name="name">The component name, such as <c>X-ABC-PROJECT</c>.  It must be an iana-token or x-name (RFC 5545 §3.1): letters, digits
        /// and hyphens.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty or holds a character other than a letter, digit or hyphen.</exception>
        public IcalComponent(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (!IsValidName(name))
                throw new ArgumentException("A component name must be one or more letters, digits and hyphens.", nameof(name));

            Name = name;
            Properties = new NonNullCollection<IcalContentLine>();
            Components = new NonNullCollection<IcalComponent>();
        }

        /// <summary>
        /// Gets the components nested in this one, in order.
        /// </summary>
        /// <remarks>
        /// The list does not accept <c>null</c>.
        /// </remarks>
        public IList<IcalComponent> Components { get; }

        /// <summary>
        /// Gets the 1-based number of the logical line holding the component's <c>BEGIN</c>; or 0 if it was not read from a source.
        /// </summary>
        public long LineNumber { get; internal set; }

        /// <summary>
        /// Gets the component name, such as <c>VEVENT</c>.  Component names are case-insensitive.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the properties, in order.
        /// </summary>
        /// <remarks>
        /// The list does not accept <c>null</c>.  Property names are case-insensitive.
        /// </remarks>
        public IList<IcalContentLine> Properties { get; }

        /// <summary>
        /// Gets the first property with a name, compared case-insensitively.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <returns>The property; or <c>null</c> if there is none with that name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public IcalContentLine? GetProperty(string name)
#else
        public IcalContentLine GetProperty(string name)
#endif
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            foreach (var property in Properties)
            {
                if (IsNamed(property, name)) return property;
            }

            return null;
        }

        /// <summary>
        /// Gets the properties with a name, compared case-insensitively.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <returns>The properties, in order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        public IReadOnlyList<IcalContentLine> GetProperties(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            var properties = new List<IcalContentLine>();
            foreach (var property in Properties)
            {
                if (IsNamed(property, name))
                {
                    properties.Add(property);
                }
            }

            return properties;
        }

        /// <summary>
        /// Removes the properties with a name, compared case-insensitively.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <returns>The number of properties removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        public int RemoveProperties(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            var removed = 0;
            for (var index = Properties.Count - 1; index >= 0; index--)
            {
                if (IsNamed(Properties[index], name))
                {
                    Properties.RemoveAt(index);
                    removed++;
                }
            }

            return removed;
        }

        /// <summary>
        /// Sets a property, replacing every property of the same name.  The new property takes the place of the first it replaces, or is added
        /// at the end if there was none.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <exception cref="ArgumentNullException"><paramref name="property"/> is <c>null</c>.</exception>
        public void SetProperty(IcalContentLine property)
        {
            if (property is null)
                throw new ArgumentNullException(nameof(property));

            ReplaceProperties(property.Name, new[] { property });
        }

        /// <summary>
        /// Checks the component, and the components nested in it, against the rules of RFC 5545 and RFC 7986, and returns every problem found.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This checks the rules that <see cref="IcalReader"/> and <see cref="IcalWriter"/> enforce: required properties and components, how
        /// often each property may appear, the syntax of property values, where each component may be nested, and the <c>VERSION</c> of a
        /// calendar.  Whether a <c>TZID</c> parameter names a VTIMEZONE, and whether a VEVENT needs a <c>DTSTART</c>, depend on the calendar,
        /// so they are checked only when this is an <see cref="IcalCalendar"/>.
        /// </para>
        /// <para>
        /// Nothing is thrown for a problem, whatever the options of a reader or writer; use this for a model built in code, or one read with
        /// accommodations turned on.  A component path in a diagnostic starts with this component's name.
        /// </para>
        /// </remarks>
        /// <returns>The problems, in the order of the lines they were found on; or an empty list if there are none.</returns>
        public IReadOnlyList<IcalDiagnostic> Validate()
        {
            var diagnostics = new List<IcalDiagnostic>();
            IcalConformance.Validate(this, diagnostics);
            return diagnostics;
        }

        /// <summary>
        /// Gets the component name and the number of properties and components in it.
        /// </summary>
        public override string ToString()
        {
            return Name + " (" + Properties.Count + " properties, " + Components.Count + " components)";
        }

        #region Typed accessor support

        /// <summary>
        /// Gets the components nested in this one that are of a type.
        /// </summary>
        internal IEnumerable<T> GetComponents<T>() where T : IcalComponent
        {
            foreach (var component in Components)
            {
                var typed = component as T;
                if (typed != null)
                {
                    yield return typed;
                }
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal string? GetText(string name)
#else
        internal string GetText(string name)
#endif
        {
            return GetProperty(name)?.GetTextValue();
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal void SetText(string name, string? value)
#else
        internal void SetText(string name, string value)
#endif
        {
            SetValue(name, value is null ? null : IcalValue.FormatText(value, IcalValueFormat.Text));
        }

        /// <summary>
        /// Gets the items of every property with a name whose value is a list of TEXT values, such as <c>CATEGORIES</c>.
        /// </summary>
        internal IReadOnlyList<string> GetTextList(string name)
        {
            var items = new List<string>();
            foreach (var property in GetProperties(name))
            {
                foreach (var item in IcalValue.EnumerateTextList(property.Value.AsSpan(), IcalValueFormat.Text))
                {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    string? text;
#else
                    string text;
#endif
                    if (!IcalValue.TryParseText(item, IcalValueFormat.Text, out text))
                        throw IcalPropertyValue.BuildInvalidValueException(property, IcalValueType.Text);

                    items.Add(text);
                }
            }

            return items;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal void SetTextList(string name, IEnumerable<string>? values)
#else
        internal void SetTextList(string name, IEnumerable<string> values)
#endif
        {
            if (values is null)
            {
                SetValue(name, null);
                return;
            }

            var formatted = new List<string>();
            foreach (var value in values)
            {
                if (value is null)
                    throw new ArgumentException("The values cannot contain null.", nameof(values));

                formatted.Add(IcalValue.FormatText(value, IcalValueFormat.Text));
            }

            SetValue(name, formatted.Count == 0 ? null : string.Join(",", formatted));
        }

        internal IcalDateOrDateTime? GetDateOrDateTime(string name)
        {
            var property = GetProperty(name);
            if (property is null) return null;

            IcalDateOrDateTime value;
            if (!IcalPropertyValue.TryParseDateOrDateTime(property, property.Value.AsSpan(), out value))
                throw IcalPropertyValue.BuildInvalidValueException(property, IcalPropertyValue.GetValueType(property));

            return value;
        }

        internal void SetDateOrDateTime(string name, IcalDateOrDateTime? value)
        {
            if (!value.HasValue)
            {
                SetValue(name, null);
                return;
            }

            var parameters = KeepParameters(name, IcalParameterNames.Value, IcalParameterNames.TimeZoneId);
            string text;
            if (value.Value.IsDate)
            {
                parameters.Add(new IcalContentLineParameter(IcalParameterNames.Value, IcalValueTypeRegistry.GetValueTypeName(IcalValueType.Date)));
                text = value.Value.Date.ToString(IcalValueFormat.Text);
            }
            else
            {
                var dateTime = value.Value.DateTime;
                if (dateTime.TimeZoneId != null)
                {
                    parameters.Add(new IcalContentLineParameter(IcalParameterNames.TimeZoneId, dateTime.TimeZoneId));
                }

                text = dateTime.ToString(IcalValueFormat.Text);
            }

            ReplaceProperties(name, new[] { new IcalContentLine(name, parameters, text) });
        }

        internal IcalDateTime? GetDateTime(string name)
        {
            var property = GetProperty(name);
            if (property is null) return null;

            IcalDateOrDateTime value;
            if (IcalPropertyValue.GetValueType(property) != IcalValueType.DateTime ||
                !IcalPropertyValue.TryParseDateOrDateTime(property, property.Value.AsSpan(), out value))
                throw IcalPropertyValue.BuildInvalidValueException(property, IcalValueType.DateTime);

            return value.DateTime;
        }

        internal void SetDateTime(string name, IcalDateTime? value)
        {
            SetDateOrDateTime(name, value.HasValue ? new IcalDateOrDateTime(value.Value) : (IcalDateOrDateTime?)null);
        }

        internal IcalDuration? GetDuration(string name)
        {
            var property = GetProperty(name);
            if (property is null) return null;

            IcalDuration value;
            if (IcalPropertyValue.GetValueType(property) != IcalValueType.Duration ||
                !IcalDuration.TryParse(property.Value.AsSpan(), IcalValueFormat.Text, out value))
                throw IcalPropertyValue.BuildInvalidValueException(property, IcalValueType.Duration);

            return value;
        }

        internal void SetDuration(string name, IcalDuration? value, bool isValueParameterRequired)
        {
            if (!value.HasValue)
            {
                SetValue(name, null);
                return;
            }

            var parameters = KeepParameters(name, IcalParameterNames.Value);
            if (isValueParameterRequired)
            {
                parameters.Add(new IcalContentLineParameter(IcalParameterNames.Value,
                    IcalValueTypeRegistry.GetValueTypeName(IcalValueType.Duration)));
            }

            ReplaceProperties(name, new[] { new IcalContentLine(name, parameters, value.Value.ToString(IcalValueFormat.Text)) });
        }

        internal int? GetInteger(string name)
        {
            var property = GetProperty(name);
            if (property is null) return null;

            int value;
            if (!IcalValue.TryParseInteger(property.Value.AsSpan(), IcalValueFormat.Text, out value))
                throw IcalPropertyValue.BuildInvalidValueException(property, IcalValueType.Integer);

            return value;
        }

        internal void SetInteger(string name, int? value)
        {
            SetValue(name, value.HasValue ? IcalPropertyValue.FormatInteger(value.Value) : null);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal Uri? GetUri(string name)
#else
        internal Uri GetUri(string name)
#endif
        {
            var property = GetProperty(name);
            if (property is null) return null;

            var valueType = IcalPropertyValue.GetValueType(property);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Uri? value;
#else
            Uri value;
#endif
            var isValid = valueType == IcalValueType.CalAddress
                ? IcalValue.TryParseCalAddress(property.Value.AsSpan(), IcalValueFormat.Text, out value)
                : IcalValue.TryParseUri(property.Value.AsSpan(), IcalValueFormat.Text, out value);
            if (!isValid || value is null || (valueType != IcalValueType.Uri && valueType != IcalValueType.CalAddress))
                throw IcalPropertyValue.BuildInvalidValueException(property, valueType);

            return value;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal void SetUri(string name, Uri? value, bool isValueParameterRequired)
#else
        internal void SetUri(string name, Uri value, bool isValueParameterRequired)
#endif
        {
            if (value is null)
            {
                SetValue(name, null);
                return;
            }

            if (!value.IsAbsoluteUri)
                throw new ArgumentException("Expected an absolute URI.", nameof(value));

            var parameters = KeepParameters(name, IcalParameterNames.Value, IcalParameterNames.Encoding);
            if (isValueParameterRequired)
            {
                parameters.Add(new IcalContentLineParameter(IcalParameterNames.Value, IcalValueTypeRegistry.GetValueTypeName(IcalValueType.Uri)));
            }

            ReplaceProperties(name, new[] { new IcalContentLine(name, parameters, value.AbsoluteUri) });
        }

        internal IcalUtcOffset? GetUtcOffset(string name)
        {
            var property = GetProperty(name);
            if (property is null) return null;

            IcalUtcOffset value;
            if (!IcalUtcOffset.TryParse(property.Value.AsSpan(), IcalValueFormat.Text, out value))
                throw IcalPropertyValue.BuildInvalidValueException(property, IcalValueType.UtcOffset);

            return value;
        }

        internal void SetUtcOffset(string name, IcalUtcOffset? value)
        {
            SetValue(name, value.HasValue ? value.Value.ToString(IcalValueFormat.Text) : null);
        }

        internal IReadOnlyList<IcalRecurrenceRule> GetRecurrenceRules(string name)
        {
            var rules = new List<IcalRecurrenceRule>();
            foreach (var property in GetProperties(name))
            {
                IcalRecurrenceRule rule;
                if (!IcalRecurrenceRule.TryParse(property.Value.AsSpan(), IcalValueFormat.Text, out rule))
                    throw IcalPropertyValue.BuildInvalidValueException(property, IcalValueType.Recur);

                rules.Add(rule);
            }

            return rules;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal void SetRecurrenceRules(string name, IEnumerable<IcalRecurrenceRule>? rules)
#else
        internal void SetRecurrenceRules(string name, IEnumerable<IcalRecurrenceRule> rules)
#endif
        {
            var properties = new List<IcalContentLine>();
            if (rules != null)
            {
                foreach (var rule in rules)
                {
                    properties.Add(new IcalContentLine(name, rule.ToString(IcalValueFormat.Text)));
                }
            }

            ReplaceProperties(name, properties);
        }

        /// <summary>
        /// Sets the value of a property, keeping the parameters of the first property of the name.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The value, as it appears in the content line; or <c>null</c> to remove the properties.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private void SetValue(string name, string? value)
#else
        private void SetValue(string name, string value)
#endif
        {
            if (value is null)
            {
                RemoveProperties(name);
                return;
            }

            ReplaceProperties(name, new[] { new IcalContentLine(name, KeepParameters(name), value) });
        }

        /// <summary>
        /// Gets the parameters of the first property of a name, without those that a value being set will decide.
        /// </summary>
        private List<IcalContentLineParameter> KeepParameters(string name, params string[] excludedNames)
        {
            var parameters = new List<IcalContentLineParameter>();
            var property = GetProperty(name);
            if (property is null) return parameters;

            foreach (var parameter in property.Parameters)
            {
                var isExcluded = false;
                foreach (var excludedName in excludedNames)
                {
                    isExcluded |= string.Equals(parameter.Name, excludedName, StringComparison.OrdinalIgnoreCase);
                }

                if (!isExcluded)
                {
                    parameters.Add(parameter);
                }
            }

            return parameters;
        }

        /// <summary>
        /// Replaces the properties of a name with others, in the place of the first, or at the end if there was none.
        /// </summary>
        private void ReplaceProperties(string name, IList<IcalContentLine> replacements)
        {
            var insertAt = -1;
            for (var index = Properties.Count - 1; index >= 0; index--)
            {
                if (IsNamed(Properties[index], name))
                {
                    Properties.RemoveAt(index);
                    insertAt = index;
                }
            }

            if (insertAt < 0)
            {
                insertAt = Properties.Count;
            }

            foreach (var replacement in replacements)
            {
                Properties.Insert(insertAt++, replacement);
            }
        }

        #endregion

        internal static bool IsNamed(IcalContentLine property, string name)
        {
            return string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Indicates whether text is an iana-token or x-name (RFC 5545 §3.1): one or more letters, digits and hyphens.
        /// </summary>
        internal static bool IsValidName(string name)
        {
            if (name.Length == 0) return false;

            foreach (var character in name)
            {
                if (!(character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9' ||
                      character == '-'))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// A collection that rejects <c>null</c>.
        /// </summary>
        private sealed class NonNullCollection<T> : Collection<T> where T : class
        {
            protected override void InsertItem(int index, T item)
            {
                if (item is null)
                    throw new ArgumentNullException(nameof(item));

                base.InsertItem(index, item);
            }

            protected override void SetItem(int index, T item)
            {
                if (item is null)
                    throw new ArgumentNullException(nameof(item));

                base.SetItem(index, item);
            }
        }
    }
}
