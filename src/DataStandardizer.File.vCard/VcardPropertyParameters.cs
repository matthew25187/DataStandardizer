using System;
using System.Collections.Generic;
using DataStandardizer.Communication.Mime;
using DataStandardizer.LanguageTag;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The group and parameters of a property being added to a card.  Each one left at its default is left out.
    /// </summary>
    /// <remarks>
    /// <c>VALUE</c> isn't here, since it comes from the value that is added.  Nothing here is checked until the card is validated or written, except <see cref="Pref"/>, whose
    /// range is checked when it is set.
    /// </remarks>
    public sealed class VcardPropertyParameters
    {
        private int? _pref;

        /// <summary>
        /// Gets parameters that RFC 6350 doesn't define, to go after the others.  It can be changed.
        /// </summary>
        public IList<VcardContentLineParameter> AdditionalParameters { get; } = new List<VcardContentLineParameter>();

        /// <summary>
        /// Gets the <c>PID</c> values (RFC 6350 §5.5).  It can be changed.
        /// </summary>
        public IList<VcardPid> Pid { get; } = new List<VcardPid>();

        /// <summary>
        /// Gets the <c>SORT-AS</c> strings (RFC 6350 §5.9).  It can be changed.
        /// </summary>
        public IList<string> SortAs { get; } = new List<string>();

        /// <summary>
        /// Gets the <c>TYPE</c> values (RFC 6350 §5.6).  It can be changed.
        /// </summary>
        public IList<string> Types { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the <c>LANGUAGE</c> (RFC 6350 §5.1).
        /// </summary>
        public Bcp47LanguageTag? Language { get; set; }

        /// <summary>
        /// Gets or sets the <c>MEDIATYPE</c> (RFC 6350 §5.7).
        /// </summary>
        public MimeMediaType? MediaType { get; set; }

        /// <summary>
        /// Gets or sets the <c>PREF</c> (RFC 6350 §5.3), from 1 (most preferred) to 100.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is less than 1 or more than 100.</exception>
        public int? Pref
        {
            get => _pref;
            set
            {
                if (value < VcardPropertyParameterReader.MinPreference || value > VcardPropertyParameterReader.MaxPreference)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The preference must be between 1 and 100.");

                _pref = value;
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets or sets the <c>ALTID</c> (RFC 6350 §5.4).
        /// </summary>
        public string? AltId { get; set; }

        /// <summary>
        /// Gets or sets the <c>CALSCALE</c> (RFC 6350 §5.8).
        /// </summary>
        public string? CalendarScale { get; set; }

        /// <summary>
        /// Gets or sets the <c>GEO</c> (RFC 6350 §5.10).
        /// </summary>
        public Uri? GeoLocation { get; set; }

        /// <summary>
        /// Gets or sets the group (RFC 6350 §3.3), without its '.'.
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// Gets or sets the <c>LABEL</c> (RFC 6350 §6.3.1).
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Gets or sets the <c>TZ</c> (RFC 6350 §5.11).
        /// </summary>
        public string? TimeZone { get; set; }
#else
        /// <summary>
        /// Gets or sets the <c>ALTID</c> (RFC 6350 §5.4).
        /// </summary>
        public string AltId { get; set; }

        /// <summary>
        /// Gets or sets the <c>CALSCALE</c> (RFC 6350 §5.8).
        /// </summary>
        public string CalendarScale { get; set; }

        /// <summary>
        /// Gets or sets the <c>GEO</c> (RFC 6350 §5.10).
        /// </summary>
        public Uri GeoLocation { get; set; }

        /// <summary>
        /// Gets or sets the group (RFC 6350 §3.3), without its '.'.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the <c>LABEL</c> (RFC 6350 §6.3.1).
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the <c>TZ</c> (RFC 6350 §5.11).
        /// </summary>
        public string TimeZone { get; set; }
#endif

        /// <summary>
        /// Gets the parameters of a content line, in the order of RFC 6350 §5.
        /// </summary>
        /// <param name="valueType">The name of the value type, for a value that isn't the property's default type; or an empty string.</param>
        internal List<VcardContentLineParameter> ToContentLineParameters(string valueType)
        {
            var parameters = new List<VcardContentLineParameter>();
            AddParameter(parameters, VcardParameterNames.Language, Language?.ToString());
            if (valueType.Length != 0)
            {
                parameters.Add(new VcardContentLineParameter(VcardParameterNames.Value, valueType));
            }

            if (_pref.HasValue)
            {
                parameters.Add(new VcardContentLineParameter(VcardParameterNames.Preference, VcardPropertyParameterReader.FormatInteger(_pref.Value)));
            }

            AddParameter(parameters, VcardParameterNames.AlternativeId, AltId);
            if (Pid.Count != 0)
            {
                var values = new string[Pid.Count];
                for (var index = 0; index < values.Length; index++)
                {
                    values[index] = Pid[index].ToString();
                }

                parameters.Add(new VcardContentLineParameter(VcardParameterNames.Pid, values));
            }

            AddParameterList(parameters, VcardParameterNames.Type, Types);
            AddParameter(parameters, VcardParameterNames.MediaType, MediaType?.ToString());
            AddParameter(parameters, VcardParameterNames.CalendarScale, CalendarScale);
            AddParameterList(parameters, VcardParameterNames.SortAs, SortAs);
            AddParameter(parameters, VcardParameterNames.Geo, GeoLocation?.OriginalString);
            AddParameter(parameters, VcardParameterNames.TimeZone, TimeZone);
            AddParameter(parameters, VcardParameterNames.Label, Label);
            parameters.AddRange(AdditionalParameters);
            return parameters;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static void AddParameter(List<VcardContentLineParameter> parameters, string name, string? value)
#else
        private static void AddParameter(List<VcardContentLineParameter> parameters, string name, string value)
#endif
        {
            if (value != null)
            {
                parameters.Add(new VcardContentLineParameter(name, value));
            }
        }

        private static void AddParameterList(List<VcardContentLineParameter> parameters, string name, IList<string> values)
        {
            if (values.Count != 0)
            {
                parameters.Add(new VcardContentLineParameter(name, values));
            }
        }
    }
}
