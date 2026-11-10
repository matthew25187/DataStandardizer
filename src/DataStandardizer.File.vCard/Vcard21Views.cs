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
    /// Gets the value of a property in the shape of vCard 2.1, if that version can show it.
    /// </summary>
    internal delegate bool Vcard21ValueGetter<TCanonical, TValue>(VcardProperty<TCanonical> property, out TValue value);

    /// <summary>
    /// A property in the shape of vCard 2.1.
    /// </summary>
    internal sealed class Vcard21Property<TValue> : IVcard21Property<TValue>
    {
        internal Vcard21Property(VcardContentLine line, string name, TValue value, bool isPreferred)
        {
            Line = line;
            Name = name;
            Value = value;
            IsPreferred = isPreferred;
        }

        /// <summary>
        /// Gets the line that holds it in canonical form.
        /// </summary>
        internal VcardContentLine Line { get; }

        public string Name { get; }

        public TValue Value { get; }

        public bool IsPreferred { get; }

        public string Group => Line.Group;

        public string Language => VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.Language);

        public IReadOnlyList<string> Types
        {
            get
            {
                var result = new List<string>();
                foreach (var type in VcardPropertyParameterReader.GetListValues(Line, VcardParameterNames.Type))
                {
                    if (Vcard21Views.IsPreferredType(type)) continue;

                    if (VcardLegacyTokens.IsKnownType(type.AsSpan()))
                    {
                        result.Add(type.ToUpperInvariant());
                    }
                    else if (VcardLegacyTokens.IsExtensionWord(type))
                    {
                        result.Add(type);
                    }
                }

                return new ReadOnlyCollection<string>(result);
            }
        }

        public IReadOnlyList<VcardContentLineParameter> ExtensionParameters
        {
            get
            {
                var result = new List<VcardContentLineParameter>();
                foreach (var parameter in Line.ParameterArray)
                {
                    if (VcardLegacyTokens.IsExtensionWord(parameter.Name))
                    {
                        result.Add(parameter);
                    }
                }

                return new ReadOnlyCollection<VcardContentLineParameter>(result);
            }
        }
    }

    /// <summary>
    /// A list of a property in the shape of vCard 2.1, which adapts a list in canonical form.
    /// </summary>
    internal sealed class Vcard21PropertyList<TCanonical, TValue> : IVcard21PropertyList<TValue>
    {
        private readonly VcardCard _card;
        private readonly Func<TValue, TCanonical> _fromVersion;
        private readonly VcardPropertyList<TCanonical> _list;
        private readonly Action<TValue, VcardPropertyParameters> _prepareParameters;
        private readonly Vcard21ValueGetter<TCanonical, TValue> _toVersion;

        internal Vcard21PropertyList(VcardCard card, VcardPropertyList<TCanonical> list, Vcard21ValueGetter<TCanonical, TValue> toVersion, Func<TValue, TCanonical> fromVersion,
            Action<TValue, VcardPropertyParameters> prepareParameters = null)
        {
            _card = card;
            _list = list;
            _toVersion = toVersion;
            _fromVersion = fromVersion;
            _prepareParameters = prepareParameters;
        }

        public int Count => GetInstances().Count;

        public IVcard21Property<TValue> this[int index] => GetInstances()[index];

        public IVcard21Property<TValue> Add(TValue value)
        {
            return Add(value, new Vcard21PropertyParameters());
        }

        public IVcard21Property<TValue> Add(TValue value, Vcard21PropertyParameters parameters)
        {
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var canonicalParameters = parameters.ToCanonical(nameof(parameters));
            _prepareParameters?.Invoke(value, canonicalParameters);
            var added = _list.Add(_fromVersion(value), canonicalParameters);
            TValue shown;
            if (!_toVersion(added, out shown))
            {
                _list.Remove(added);
                throw new ArgumentException("vCard 2.1 can't hold this value.", nameof(value));
            }

            return new Vcard21Property<TValue>(added.Line, added.Name, shown, Vcard21Views.IsPreferred(_card, added.Line));
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Remove(IVcard21Property<TValue> property)
        {
            if (property is null)
                throw new ArgumentNullException(nameof(property));

            var instance = property as Vcard21Property<TValue>;
            return instance != null && string.Equals(instance.Line.Name, _list.PropertyName, StringComparison.OrdinalIgnoreCase) && _card.Properties.Remove(instance.Line);
        }

        public IEnumerator<IVcard21Property<TValue>> GetEnumerator()
        {
            return GetInstances().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<IVcard21Property<TValue>> GetInstances()
        {
            var result = new List<IVcard21Property<TValue>>();
            foreach (var property in _list)
            {
                var view = Vcard21Views.Create(_card, property, _toVersion);
                if (view != null)
                {
                    result.Add(view);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// The <c>LABEL</c> properties of a card in the shape of vCard 2.1.  In canonical form each is a parameter of its <c>ADR</c>, or a property of its own if it goes with none.
    /// </summary>
    internal sealed class Vcard21LabelList : IVcard21PropertyList<string>
    {
        private readonly VcardCard _card;

        internal Vcard21LabelList(VcardCard card)
        {
            _card = card;
        }

        public int Count => GetInstances().Count;

        public IVcard21Property<string> this[int index] => GetInstances()[index];

        public IVcard21Property<string> Add(string value)
        {
            return Add(value, new Vcard21PropertyParameters());
        }

        public IVcard21Property<string> Add(string value, Vcard21PropertyParameters parameters)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));

            var canonical = parameters.ToCanonical(nameof(parameters));
            var line = new VcardContentLine(canonical.Group, VcardPropertyNames.PostalLabel, canonical.ToContentLineParameters(string.Empty), VcardValue.FormatText(value, VcardValueFormat.Text, VcardVersion.V4_0));
            _card.Properties.Add(line);

            VcardContentLine address;
            var holder = VcardVersion30Mapping.TryAttachLabel(_card, _card.Properties.Count - 1, out address) ? address : line;
            return new Vcard21Property<string>(holder, VcardPropertyNames.PostalLabel, value, Vcard21Views.IsPreferred(_card, holder));
        }

        public void Clear()
        {
            Vcard30Views.RemoveLabels(_card);
        }

        public bool Remove(IVcard21Property<string> property)
        {
            if (property is null)
                throw new ArgumentNullException(nameof(property));

            var instance = property as Vcard21Property<string>;
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

        public IEnumerator<IVcard21Property<string>> GetEnumerator()
        {
            return GetInstances().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<IVcard21Property<string>> GetInstances()
        {
            var result = new List<IVcard21Property<string>>();
            var separate = new List<VcardContentLine>();
            foreach (var line in _card.Properties)
            {
                if (string.Equals(line.Name, VcardPropertyNames.Address, StringComparison.OrdinalIgnoreCase) && line.GetParameter(VcardParameterNames.Label) != null)
                {
                    var text = VcardPropertyParameterReader.GetValue(line, VcardParameterNames.Label).Replace("\\n", "\n");
                    result.Add(new Vcard21Property<string>(line, VcardPropertyNames.PostalLabel, text, Vcard21Views.IsPreferred(_card, line)));
                }
                else if (string.Equals(line.Name, VcardPropertyNames.PostalLabel, StringComparison.OrdinalIgnoreCase))
                {
                    separate.Add(line);
                }
            }

            foreach (var line in separate)
            {
                string text;
                if (VcardPropertyCodecs.TryParseText(line.Value, out text))
                {
                    result.Add(new Vcard21Property<string>(line, VcardPropertyNames.PostalLabel, text, Vcard21Views.IsPreferred(_card, line)));
                }
            }

            return result;
        }
    }

    /// <summary>
    /// The <c>SOUND</c> properties of a card in the shape of vCard 2.1, which can be text (§2.6.3).  RFC 6350 §6.7.5 has only a URI, so the model holds text as <c>VALUE=text</c>, which
    /// <see cref="VcardCard.Sounds"/> doesn't give.
    /// </summary>
    internal sealed class Vcard21SoundList : IVcard21PropertyList<VcardMediaValue>
    {
        private readonly VcardCard _card;
        private readonly IVcard21PropertyList<VcardMediaValue> _media;

        internal Vcard21SoundList(VcardCard card, IVcard21PropertyList<VcardMediaValue> media)
        {
            _card = card;
            _media = media;
        }

        public int Count => GetInstances().Count;

        public IVcard21Property<VcardMediaValue> this[int index] => GetInstances()[index];

        public IVcard21Property<VcardMediaValue> Add(VcardMediaValue value)
        {
            return Add(value, new Vcard21PropertyParameters());
        }

        public IVcard21Property<VcardMediaValue> Add(VcardMediaValue value, Vcard21PropertyParameters parameters)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));
            if (value.Text is null) return _media.Add(value, parameters);

            var canonical = parameters.ToCanonical(nameof(parameters));
            if (value.TypeToken != null)
            {
                if (!VcardLegacyTokens.IsFormatType(value.TypeToken.AsSpan()))
                    throw new ArgumentException("vCard 2.1 has no format " + value.TypeToken + ".", nameof(value));

                canonical.Types.Insert(0, value.TypeToken.ToLowerInvariant());
            }

            var line = new VcardContentLine(canonical.Group, VcardPropertyNames.Sound, canonical.ToContentLineParameters(Vcard30Conformance.TextValueName),
                VcardValue.FormatText(value.Text, VcardValueFormat.Text, VcardVersion.V4_0));
            _card.Properties.Add(line);
            return new Vcard21Property<VcardMediaValue>(line, VcardPropertyNames.Sound, value, Vcard21Views.IsPreferred(_card, line));
        }

        public void Clear()
        {
            _card.RemoveProperties(VcardPropertyNames.Sound);
        }

        public bool Remove(IVcard21Property<VcardMediaValue> property)
        {
            if (property is null)
                throw new ArgumentNullException(nameof(property));

            var instance = property as Vcard21Property<VcardMediaValue>;
            return instance != null && string.Equals(instance.Line.Name, VcardPropertyNames.Sound, StringComparison.OrdinalIgnoreCase) && _card.Properties.Remove(instance.Line);
        }

        public IEnumerator<IVcard21Property<VcardMediaValue>> GetEnumerator()
        {
            return GetInstances().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<IVcard21Property<VcardMediaValue>> GetInstances()
        {
            var media = new Dictionary<VcardContentLine, IVcard21Property<VcardMediaValue>>();
            foreach (var instance in _media)
            {
                media[((Vcard21Property<VcardMediaValue>)instance).Line] = instance;
            }

            var result = new List<IVcard21Property<VcardMediaValue>>();
            foreach (var line in _card.Properties)
            {
                IVcard21Property<VcardMediaValue> instance;
                if (media.TryGetValue(line, out instance))
                {
                    result.Add(instance);
                }
                else if (string.Equals(line.Name, VcardPropertyNames.Sound, StringComparison.OrdinalIgnoreCase) && VcardValueTypeRegistry.GetValueType(line) == VcardValueType.Text)
                {
                    string text;
                    if (VcardPropertyCodecs.TryParseText(line.Value, out text))
                    {
                        result.Add(new Vcard21Property<VcardMediaValue>(line, VcardPropertyNames.Sound, VcardMediaValue.FromText(text, GetToken(line)), Vcard21Views.IsPreferred(_card, line)));
                    }
                }
            }

            return result;
        }

        private static string GetToken(VcardContentLine line)
        {
            foreach (var type in VcardPropertyParameterReader.GetListValues(line, VcardParameterNames.Type))
            {
                if (VcardLegacyTokens.IsFormatType(type.AsSpan())) return type.ToUpperInvariant();
            }

            return null;
        }
    }

    /// <summary>
    /// What the views of vCard 2.1 share.
    /// </summary>
    internal static class Vcard21Views
    {
        /// <summary>
        /// Gets the view of a property, or <c>null</c> if vCard 2.1 can't show it.
        /// </summary>
        internal static IVcard21Property<TValue> Create<TCanonical, TValue>(VcardCard card, VcardProperty<TCanonical> property, Vcard21ValueGetter<TCanonical, TValue> toVersion)
        {
            TValue value;
            return property != null && toVersion(property, out value) ? new Vcard21Property<TValue>(property.Line, property.Name, value, IsPreferred(card, property.Line)) : null;
        }

        /// <summary>
        /// Gets whether a line is what vCard 2.1 writes with <c>PREF</c>: it has the lowest <c>PREF</c> of its property.  The grammar of §2.9 allows <c>PREF</c> on any property.
        /// </summary>
        internal static bool IsPreferred(VcardCard card, VcardContentLine line)
        {
            // A property on which vCard 4.0 has no PREF parameter keeps PREF as a TYPE.
            if (VcardPropertyParameterReader.GetListValues(line, VcardParameterNames.Type).Exists(IsPreferredType)) return true;

            var preference = VcardPropertyParameterReader.GetPreferenceOrLeast(line);
            return preference <= VcardPropertyParameterReader.MaxPreference && preference == VcardVersion30Mapping.GetLowestPreference(card.Properties, line.Name);
        }

        /// <summary>
        /// Gets whether a <c>TYPE</c> value is <c>PREF</c> (vCard 2.1 §2.4.1).
        /// </summary>
        internal static bool IsPreferredType(string type)
        {
            return string.Equals(type, "PREF", StringComparison.OrdinalIgnoreCase);
        }
    }
}
