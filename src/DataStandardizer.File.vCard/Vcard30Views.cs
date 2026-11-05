#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#nullable disable
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Gets the value of a property in the shape of vCard 3.0, if that version can show it.
    /// </summary>
    internal delegate bool Vcard30ValueGetter<TCanonical, TValue>(VcardProperty<TCanonical> property, out TValue value);

    /// <summary>
    /// A property in the shape of vCard 3.0.
    /// </summary>
    internal sealed class Vcard30Property<TValue> : IVcard30Property<TValue>
    {
        private readonly bool _isPreferred;

        internal Vcard30Property(VcardContentLine line, string name, TValue value, bool isPreferred)
        {
            Line = line;
            Name = name;
            Value = value;
            _isPreferred = isPreferred;
        }

        /// <summary>
        /// Gets the line that holds it in canonical form.
        /// </summary>
        internal VcardContentLine Line { get; }

        public string Name { get; }

        public TValue Value { get; }

        public bool IsPreferred => _isPreferred;

        public string Group => Line.Group;

        public string Language => VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.Language);

        public IReadOnlyList<string> Types => new ReadOnlyCollection<string>(VcardPropertyParameterReader.GetListValues(Line, VcardParameterNames.Type));

        public IReadOnlyList<VcardContentLineParameter> ExtensionParameters
        {
            get
            {
                var result = new List<VcardContentLineParameter>();
                foreach (var parameter in Line.ParameterArray)
                {
                    if (!VcardConformance.IsDefinedParameter(parameter.Name))
                    {
                        result.Add(parameter);
                    }
                }

                return new ReadOnlyCollection<VcardContentLineParameter>(result);
            }
        }
    }

    /// <summary>
    /// A list of a property in the shape of vCard 3.0, which adapts a list in canonical form.
    /// </summary>
    internal sealed class Vcard30PropertyList<TCanonical, TValue> : IVcard30PropertyList<TValue>
    {
        private readonly Func<TValue, TCanonical> _fromVersion;
        private readonly VcardCard _card;
        private readonly VcardPropertyList<TCanonical> _list;
        private readonly Action<TValue, VcardPropertyParameters> _prepareParameters;
        private readonly Vcard30ValueGetter<TCanonical, TValue> _toVersion;

        internal Vcard30PropertyList(VcardCard card, VcardPropertyList<TCanonical> list, Vcard30ValueGetter<TCanonical, TValue> toVersion, Func<TValue, TCanonical> fromVersion,
            Action<TValue, VcardPropertyParameters> prepareParameters = null)
        {
            _card = card;
            _list = list;
            _toVersion = toVersion;
            _fromVersion = fromVersion;
            _prepareParameters = prepareParameters;
        }

        public int Count => GetInstances().Count;

        public IVcard30Property<TValue> this[int index] => GetInstances()[index];

        public IVcard30Property<TValue> Add(TValue value)
        {
            return Add(value, new Vcard30PropertyParameters());
        }

        public IVcard30Property<TValue> Add(TValue value, Vcard30PropertyParameters parameters)
        {
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var canonicalParameters = parameters.ToCanonical();
            _prepareParameters?.Invoke(value, canonicalParameters);
            var added = _list.Add(_fromVersion(value), canonicalParameters);
            TValue shown;
            if (!_toVersion(added, out shown))
            {
                _list.Remove(added);
                throw new ArgumentException("vCard 3.0 can't hold this value.", nameof(value));
            }

            return new Vcard30Property<TValue>(added.Line, added.Name, shown, Vcard30Views.IsPreferred(_card, added.Line));
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Remove(IVcard30Property<TValue> property)
        {
            if (property is null)
                throw new ArgumentNullException(nameof(property));

            var instance = property as Vcard30Property<TValue>;
            return instance != null && string.Equals(instance.Line.Name, _list.PropertyName, StringComparison.OrdinalIgnoreCase) && _card.Properties.Remove(instance.Line);
        }

        public IEnumerator<IVcard30Property<TValue>> GetEnumerator()
        {
            return GetInstances().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<IVcard30Property<TValue>> GetInstances()
        {
            var result = new List<IVcard30Property<TValue>>();
            foreach (var property in _list)
            {
                var view = Vcard30Views.Create(_card, property, _toVersion);
                if (view != null)
                {
                    result.Add(view);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// The <c>LABEL</c> properties of a card in the shape of vCard 3.0.  In canonical form each is a parameter of its <c>ADR</c>, or a property of its own if it goes with none.
    /// </summary>
    internal sealed class Vcard30LabelList : IVcard30PropertyList<string>
    {
        private readonly VcardCard _card;

        internal Vcard30LabelList(VcardCard card)
        {
            _card = card;
        }

        public int Count => GetInstances().Count;

        public IVcard30Property<string> this[int index] => GetInstances()[index];

        public IVcard30Property<string> Add(string value)
        {
            return Add(value, new Vcard30PropertyParameters());
        }

        public IVcard30Property<string> Add(string value, Vcard30PropertyParameters parameters)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));

            var canonical = parameters.ToCanonical();
            var line = new VcardContentLine(canonical.Group, VcardPropertyNames.PostalLabel, canonical.ToContentLineParameters(string.Empty), VcardValue.FormatText(value, VcardValueFormat.Text, VcardVersion.V4_0));
            _card.Properties.Add(line);

            VcardContentLine address;
            var holder = VcardVersion30Mapping.TryAttachLabel(_card, _card.Properties.Count - 1, out address) ? address : line;
            return new Vcard30Property<string>(holder, VcardPropertyNames.PostalLabel, value, Vcard30Views.IsPreferred(_card, holder));
        }

        public void Clear()
        {
            Vcard30Views.RemoveLabels(_card);
        }

        public bool Remove(IVcard30Property<string> property)
        {
            if (property is null)
                throw new ArgumentNullException(nameof(property));

            var instance = property as Vcard30Property<string>;
            if (instance is null || !string.Equals(instance.Name, VcardPropertyNames.PostalLabel, StringComparison.OrdinalIgnoreCase)) return false;

            var index = _card.Properties.IndexOf(instance.Line);
            if (index < 0) return false;

            if (string.Equals(instance.Line.Name, VcardPropertyNames.PostalLabel, StringComparison.OrdinalIgnoreCase))
            {
                _card.Properties.RemoveAt(index);
            }
            else
            {
                _card.Properties[index] = Vcard30Views.WithoutParameter(instance.Line, VcardParameterNames.Label);
            }

            return true;
        }

        public IEnumerator<IVcard30Property<string>> GetEnumerator()
        {
            return GetInstances().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<IVcard30Property<string>> GetInstances()
        {
            var result = new List<IVcard30Property<string>>();
            var labelled = new List<VcardContentLine>();
            var separate = new List<VcardContentLine>();
            foreach (var line in _card.Properties)
            {
                if (string.Equals(line.Name, VcardPropertyNames.Address, StringComparison.OrdinalIgnoreCase) && line.GetParameter(VcardParameterNames.Label) != null)
                {
                    labelled.Add(line);
                }
                else if (string.Equals(line.Name, VcardPropertyNames.PostalLabel, StringComparison.OrdinalIgnoreCase))
                {
                    separate.Add(line);
                }
            }

            foreach (var line in labelled)
            {
                var text = VcardPropertyParameterReader.GetValue(line, VcardParameterNames.Label).Replace("\\n", "\n");
                result.Add(new Vcard30Property<string>(line, VcardPropertyNames.PostalLabel, text, Vcard30Views.IsPreferred(_card, line)));
            }

            foreach (var line in separate)
            {
                string text;
                if (VcardPropertyCodecs.TryParseText(line.Value, out text))
                {
                    result.Add(new Vcard30Property<string>(line, VcardPropertyNames.PostalLabel, text, Vcard30Views.IsPreferred(_card, line)));
                }
            }

            return result;
        }
    }

    /// <summary>
    /// What the views of vCard 3.0 share.
    /// </summary>
    internal static class Vcard30Views
    {
        /// <summary>
        /// Gets the view of a property, or <c>null</c> if vCard 3.0 can't show it.
        /// </summary>
        internal static IVcard30Property<TValue> Create<TCanonical, TValue>(VcardCard card, VcardProperty<TCanonical> property, Vcard30ValueGetter<TCanonical, TValue> toVersion)
        {
            TValue value;
            return property != null && toVersion(property, out value) ? new Vcard30Property<TValue>(property.Line, property.Name, value, IsPreferred(card, property.Line)) : null;
        }

        /// <summary>
        /// Gets whether a line is what vCard 3.0 writes as preferred.  A type with no <c>TYPE</c> that can say so, or one whose <c>TYPE</c> says its format, has no preference there.
        /// </summary>
        internal static bool IsPreferred(VcardCard card, VcardContentLine line)
        {
            var preference = VcardPropertyParameterReader.GetPreferenceOrLeast(line);
            return Vcard30Conformance.AllowsPreference(line.Name) && preference <= VcardPropertyParameterReader.MaxPreference &&
                   preference == VcardVersion30Mapping.GetLowestPreference(card.Properties, line.Name);
        }

        /// <summary>
        /// Gets a line without a parameter.
        /// </summary>
        internal static VcardContentLine WithoutParameter(VcardContentLine line, string name)
        {
            var parameters = new List<VcardContentLineParameter>(line.ParameterArray);
            parameters.RemoveAll(parameter => string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase));
            return line.With(line.Name, parameters.ToArray(), line.Value);
        }

        /// <summary>
        /// Removes every label, and gets how many there were.
        /// </summary>
        internal static int RemoveLabels(VcardCard card)
        {
            var count = card.RemoveProperties(VcardPropertyNames.PostalLabel);
            return count + RemoveParameter(card, VcardPropertyNames.Address, VcardParameterNames.Label);
        }

        /// <summary>
        /// Removes a parameter from every instance of a property, and gets how many had it.
        /// </summary>
        internal static int RemoveParameter(VcardCard card, string propertyName, string parameterName)
        {
            var count = 0;
            for (var index = 0; index < card.Properties.Count; index++)
            {
                var line = card.Properties[index];
                if (string.Equals(line.Name, propertyName, StringComparison.OrdinalIgnoreCase) && line.GetParameter(parameterName) != null)
                {
                    card.Properties[index] = WithoutParameter(line, parameterName);
                    count++;
                }
            }

            return count;
        }
    }
}
