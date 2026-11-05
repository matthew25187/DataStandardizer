using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DataStandardizer.Communication.Mime;
using DataStandardizer.Geography;
using DataStandardizer.LanguageTag;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A property of a card with its value parsed: a typed view of one <see cref="VcardContentLine"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The members are version-agnostic.  <see cref="IVcard40Property{TValue}"/> is implemented explicitly, and gives the shape of vCard 4.0.  The value is in the canonical form
    /// of the model, which is the form of vCard 4.0 wherever 4.0 has an equivalent.
    /// </para>
    /// <para>
    /// An instance doesn't change: a change to the card is a new content line, so a list or card member gives a new instance for it.
    /// </para>
    /// </remarks>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public sealed class VcardProperty<TValue> : IVcard40Property<TValue>
    {
        internal VcardProperty(VcardContentLine line, TValue value)
        {
            Line = line;
            Value = value;
        }

        /// <summary>
        /// Gets the <c>CC</c> (RFC 8605 §3.1), the country code of the country name of an address; or <c>null</c> if the property doesn't have a valid one, or doesn't allow it.
        /// </summary>
        public Iso3166Part1Alpha2Country? CountryCode
        {
            get
            {
                var text = VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.CountryCode);
                Iso3166Part1Alpha2Country result;
                return text != null && VcardPropertyParameterReader.IsAllowedOnProperty(VcardParameterSet.CountryCode, Line.Name) && VcardPropertyParameterReader.TryParseCountryCode(text, out result)
                    ? result
                    : (Iso3166Part1Alpha2Country?)null;
            }
        }

        /// <summary>
        /// Gets the parameters of the property that RFC 6350 and its extensions don't define, in the order they appear.
        /// </summary>
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

        /// <summary>
        /// Gets the <c>INDEX</c> (RFC 6715 §3.1), the position of the value among the instances of the property; or <c>null</c> if the property doesn't have a valid one, or doesn't allow it.  See
        /// <see cref="VcardPropertyList{TValue}.GetOrderedByIndex"/>.
        /// </summary>
        public long? Index
        {
            get
            {
                var text = VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.Index);
                long result;
                return text != null && VcardPropertyParameterReader.IsAllowedOnProperty(VcardParameterSet.Index, Line.Name) && VcardPropertyParameterReader.TryParseIndex(text, out result)
                    ? result
                    : (long?)null;
            }
        }

        /// <summary>
        /// Gets the <c>LANGUAGE</c> (RFC 6350 §5.1); or <c>null</c> if the property doesn't have a valid one.
        /// </summary>
        public Bcp47LanguageTag? Language
        {
            get
            {
                var text = VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.Language);
                Bcp47LanguageTag result;
                return text != null && VcardValue.TryParseLanguageTag(text.AsSpan(), VcardValueFormat.Text, out result) ? result : (Bcp47LanguageTag?)null;
            }
        }

        /// <summary>
        /// Gets the <c>LEVEL</c> (RFC 6715 §3.2); or <c>null</c> if the property doesn't have one that is valid for it.
        /// </summary>
        public VcardLevel? Level
        {
            get
            {
                var text = VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.Level);
                VcardLevel result;
                return text != null && VcardPropertyParameterReader.IsAllowedOnProperty(VcardParameterSet.Level, Line.Name) && VcardPropertyParameterReader.TryParseLevel(text, Line.Name, out result)
                    ? result
                    : (VcardLevel?)null;
            }
        }

        /// <summary>
        /// Gets the underlying content line.
        /// </summary>
        public VcardContentLine Line { get; }

        /// <summary>
        /// Gets the <c>MEDIATYPE</c> (RFC 6350 §5.7); or <c>null</c> if the property doesn't have a valid one.
        /// </summary>
        public MimeMediaType? MediaType
        {
            get
            {
                var text = VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.MediaType);
                MimeMediaType result;
                return text != null && MimeMediaType.TryParse(text.AsSpan(), out result) ? result : (MimeMediaType?)null;
            }
        }

        /// <summary>
        /// Gets the name of the property, as written.
        /// </summary>
        public string Name => Line.Name;

        /// <summary>
        /// Gets the <c>PID</c> values (RFC 6350 §5.5) that are valid.
        /// </summary>
        public IReadOnlyList<VcardPid> Pid
        {
            get
            {
                var result = new List<VcardPid>();
                foreach (var text in VcardPropertyParameterReader.GetListValues(Line, VcardParameterNames.Pid))
                {
                    VcardPid pid;
                    if (VcardPid.TryParse(text.AsSpan(), VcardValueFormat.Text, out pid))
                    {
                        result.Add(pid);
                    }
                }

                return new ReadOnlyCollection<VcardPid>(result);
            }
        }

        /// <summary>
        /// Gets the <c>PREF</c> (RFC 6350 §5.3), from 1 (most preferred) to 100; or <c>null</c> if the property doesn't have a valid one, which makes it least preferred.
        /// </summary>
        public int? Pref
        {
            get
            {
                var preference = VcardPropertyParameterReader.GetPreferenceOrLeast(Line);
                return preference > VcardPropertyParameterReader.MaxPreference ? (int?)null : preference;
            }
        }

        /// <summary>
        /// Gets the <c>SORT-AS</c> strings (RFC 6350 §5.9).
        /// </summary>
        public IReadOnlyList<string> SortAs => new ReadOnlyCollection<string>(VcardPropertyParameterReader.GetListValues(Line, VcardParameterNames.SortAs));

        /// <summary>
        /// Gets the <c>TYPE</c> values (RFC 6350 §5.6), as written.  Whether they were written as a parameter list or a value list makes no difference.
        /// </summary>
        public IReadOnlyList<string> Types => new ReadOnlyCollection<string>(VcardPropertyParameterReader.GetListValues(Line, VcardParameterNames.Type));

        /// <summary>
        /// Gets the value.
        /// </summary>
        public TValue Value { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the <c>ALTID</c> (RFC 6350 §5.4); or <c>null</c> if the property doesn't have one.
        /// </summary>
        public string? AltId => VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.AlternativeId);

        /// <summary>
        /// Gets the <c>CALSCALE</c> (RFC 6350 §5.8); or <c>null</c> if the property doesn't have one.
        /// </summary>
        public string? CalendarScale => VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.CalendarScale);

        /// <summary>
        /// Gets the <c>GEO</c> (RFC 6350 §5.10); or <c>null</c> if the property doesn't have a valid one.
        /// </summary>
        public Uri? GeoLocation
        {
            get
            {
                var text = VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.Geo);
                Uri? result;
                return text != null && VcardValue.TryParseUriText(text.AsSpan(), out result) ? result : null;
            }
        }

        /// <summary>
        /// Gets the group (RFC 6350 §3.3), as written and without its '.'; or <c>null</c> if the property has none.
        /// </summary>
        public string? Group => Line.Group;

        /// <summary>
        /// Gets the <c>LABEL</c> (RFC 6350 §6.3.1); or <c>null</c> if the property doesn't have one.
        /// </summary>
        public string? Label => VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.Label);

        /// <summary>
        /// Gets the <c>TZ</c> (RFC 6350 §5.11); or <c>null</c> if the property doesn't have one.
        /// </summary>
        public string? TimeZone => VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.TimeZone);

        string? IVcard40Property<TValue>.AltId => AltId;

        string? IVcard40Property<TValue>.CalendarScale => CalendarScale;

        Uri? IVcard40Property<TValue>.GeoLocation => GeoLocation;

        string? IVcard40Property<TValue>.Group => Group;

        string? IVcard40Property<TValue>.Label => Label;

        string? IVcard40Property<TValue>.TimeZone => TimeZone;
#else
        /// <summary>
        /// Gets the <c>ALTID</c> (RFC 6350 §5.4); or <c>null</c> if the property doesn't have one.
        /// </summary>
        public string AltId => VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.AlternativeId);

        /// <summary>
        /// Gets the <c>CALSCALE</c> (RFC 6350 §5.8); or <c>null</c> if the property doesn't have one.
        /// </summary>
        public string CalendarScale => VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.CalendarScale);

        /// <summary>
        /// Gets the <c>GEO</c> (RFC 6350 §5.10); or <c>null</c> if the property doesn't have a valid one.
        /// </summary>
        public Uri GeoLocation
        {
            get
            {
                var text = VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.Geo);
                Uri result;
                return text != null && VcardValue.TryParseUriText(text.AsSpan(), out result) ? result : null;
            }
        }

        /// <summary>
        /// Gets the group (RFC 6350 §3.3), as written and without its '.'; or <c>null</c> if the property has none.
        /// </summary>
        public string Group => Line.Group;

        /// <summary>
        /// Gets the <c>LABEL</c> (RFC 6350 §6.3.1); or <c>null</c> if the property doesn't have one.
        /// </summary>
        public string Label => VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.Label);

        /// <summary>
        /// Gets the <c>TZ</c> (RFC 6350 §5.11); or <c>null</c> if the property doesn't have one.
        /// </summary>
        public string TimeZone => VcardPropertyParameterReader.GetValue(Line, VcardParameterNames.TimeZone);

        string IVcard40Property<TValue>.AltId => AltId;

        string IVcard40Property<TValue>.CalendarScale => CalendarScale;

        Uri IVcard40Property<TValue>.GeoLocation => GeoLocation;

        string IVcard40Property<TValue>.Group => Group;

        string IVcard40Property<TValue>.Label => Label;

        string IVcard40Property<TValue>.TimeZone => TimeZone;
#endif

        Iso3166Part1Alpha2Country? IVcard40Property<TValue>.CountryCode => CountryCode;

        IReadOnlyList<VcardContentLineParameter> IVcard40Property<TValue>.ExtensionParameters => ExtensionParameters;

        long? IVcard40Property<TValue>.Index => Index;

        Bcp47LanguageTag? IVcard40Property<TValue>.Language => Language;

        VcardLevel? IVcard40Property<TValue>.Level => Level;

        MimeMediaType? IVcard40Property<TValue>.MediaType => MediaType;

        string IVcard40Property<TValue>.Name => Name;

        IReadOnlyList<VcardPid> IVcard40Property<TValue>.Pid => Pid;

        int? IVcard40Property<TValue>.Pref => Pref;

        IReadOnlyList<string> IVcard40Property<TValue>.SortAs => SortAs;

        IReadOnlyList<string> IVcard40Property<TValue>.Types => Types;

        TValue IVcard40Property<TValue>.Value => Value;
    }
}
