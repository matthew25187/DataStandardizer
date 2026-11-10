#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#nullable disable
#endif
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.vCard
{
    /// <content>
    /// The members that only older versions define, and the shape of vCard 3.0.
    /// </content>
    public sealed partial class VcardCard
    {
        private const string TelPrefix = "tel:";

        // ref. RFC 6350 §6.6.6: type-param-related.
        private const string AgentTypeText = "agent";

        private static readonly VcardPropertyCodec<string> MailerCodec = VcardPropertyCodecs.LegacyText(VcardPropertyNames.Mailer);
        private static readonly VcardPropertyCodec<string> ClassificationCodec = VcardPropertyCodecs.LegacyText(VcardPropertyNames.Classification);
        private static readonly VcardPropertyCodec<string> SourceNameCodec = VcardPropertyCodecs.LegacyText(VcardPropertyNames.SourceName);
        private static readonly VcardPropertyCodec<string> ProfileCodec = VcardPropertyCodecs.LegacyText(VcardPropertyNames.Profile);
        private static readonly VcardPropertyCodec<Uri> CalendarAccessUrisCodec = VcardPropertyCodecs.LegacyUri(VcardPropertyNames.CalendarAccessUri);

        private readonly VcardPropertyList<Uri> _calendarAccessUris;

        /// <summary>
        /// Gets the <c>CAPURI</c> properties (RFC 2739 §2.3.3), which vCard 4.0 doesn't define.
        /// </summary>
        public VcardPropertyList<Uri> CalendarAccessUris => _calendarAccessUris;

        /// <summary>
        /// Gets the agents: the <c>RELATED</c> properties with <c>TYPE=agent</c> (RFC 6350 §6.6.6), which are the <c>AGENT</c> of RFC 2426 §3.5.4.
        /// </summary>
        public IReadOnlyList<VcardAgent> Agents
        {
            get
            {
                var result = new List<VcardAgent>();
                foreach (var related in Related)
                {
                    if (!ContainsAgentType(related.Line)) continue;

                    if (related.Value.ValueType == VcardValueType.Uri)
                    {
                        result.Add(new VcardAgent(related.Value.Uri, null, null));
                        continue;
                    }

                    VcardCard card;
                    result.Add(VcardNestedCards.TryReadHeld(related.Value.Text, out card)
                        ? new VcardAgent(null, card, null)
                        : new VcardAgent(null, null, related.Value.Text));
                }

                return new ReadOnlyCollection<VcardAgent>(result);
            }
        }

        /// <summary>
        /// Gets the card in the shape of vCard 3.0.  It is this object, so a change through either is a change to both.
        /// </summary>
        /// <remarks>
        /// Any card can be viewed this way, whatever version it was read from.  A view hides what vCard 3.0 can't show; see <see cref="VcardVersionConverter.Analyze"/>.
        /// </remarks>
        public IVcard30Card AsVersion30()
        {
            return this;
        }

        /// <summary>
        /// Adds an agent that is a card, which the property holds a copy of.
        /// </summary>
        /// <param name="agent">The card.</param>
        /// <exception cref="ArgumentNullException"><paramref name="agent"/> is <c>null</c>.</exception>
        public void AddAgent(VcardCard agent)
        {
            if (agent is null)
                throw new ArgumentNullException(nameof(agent));

            Related.Add(new VcardTypedValue(VcardNestedCards.Write(agent, VcardVersion.V4_0)), new VcardPropertyParameters { Types = { AgentTypeText } });
        }

        /// <summary>
        /// Adds an agent that is a URI.
        /// </summary>
        /// <param name="agent">The URI, which must be absolute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="agent"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="agent"/> can't be written.</exception>
        public void AddAgent(Uri agent)
        {
            if (agent is null)
                throw new ArgumentNullException(nameof(agent));

            Related.Add(new VcardTypedValue(agent), new VcardPropertyParameters { Types = { AgentTypeText } });
        }

        /// <summary>
        /// Removes every agent, including any whose value isn't valid.
        /// </summary>
        /// <returns>The number removed.</returns>
        public int RemoveAgents()
        {
            var count = 0;
            for (var index = Properties.Count - 1; index >= 0; index--)
            {
                if (string.Equals(Properties[index].Name, VcardPropertyNames.Related, StringComparison.OrdinalIgnoreCase) && ContainsAgentType(Properties[index]))
                {
                    Properties.RemoveAt(index);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Sets the <c>MAILER</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetMailer(string value)
        {
            SetSingle(MailerCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>CLASS</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value: <c>PUBLIC</c>, <c>PRIVATE</c>, <c>CONFIDENTIAL</c>, or an <c>iana-token</c> or <c>x-name</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetClassification(string value)
        {
            SetSingle(ClassificationCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>NAME</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetSourceName(string value)
        {
            SetSingle(SourceNameCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>PROFILE</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, which RFC 2426 §4 says must be <c>VCARD</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetProfile(string value)
        {
            SetSingle(ProfileCodec, value, null);
        }

        /// <summary>
        /// Checks the card against the rules of a version, without throwing.
        /// </summary>
        /// <remarks>
        /// It checks what <see cref="VcardWriter"/> checks for that <see cref="VcardOptions.WriteVersion"/>, whatever the options, so the result also holds what the version can't hold, as
        /// <see cref="VcardDiagnosticKind.Lossy"/> items.
        /// </remarks>
        /// <param name="version">The version.</param>
        /// <returns>Every problem found.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        public IReadOnlyList<VcardDiagnostic> Validate(VcardVersion version)
        {
            var mapping = VcardVersionMapping.For(version, nameof(version));
            var diagnostics = new List<VcardDiagnostic>();
            var lines = mapping.FromCanonical(this, new VcardOptions { MaxLineLength = null }, diagnostics);
            mapping.Validate(lines, this, new VcardOptions { MaxLineLength = null }, 0, diagnostics);
            return new ReadOnlyCollection<VcardDiagnostic>(diagnostics);
        }

        private static bool ContainsAgentType(VcardContentLine line)
        {
            foreach (var type in VcardPropertyParameterReader.GetListValues(line, VcardParameterNames.Type))
            {
                if (string.Equals(type, AgentTypeText, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static bool GetSame<TValue>(VcardProperty<TValue> property, out TValue value)
        {
            value = property.Value;
            return true;
        }

        private static TValue Same<TValue>(TValue value)
        {
            return value;
        }

        private IVcard30PropertyList<TValue> View<TValue>(VcardPropertyList<TValue> list)
        {
            return new Vcard30PropertyList<TValue, TValue>(this, list, GetSame, Same);
        }

        private static bool GetTelephone(VcardProperty<VcardTypedValue> property, out string value)
        {
            value = null;
            if (property.Value.ValueType == VcardValueType.Text)
            {
                value = property.Value.Text;
                return true;
            }

            // ref. RFC 2426 §3.3.1: a phone number.  A tel URI holds one, after its scheme and before any parameters (RFC 3966 §3).
            var text = property.Value.ValueType == VcardValueType.Uri ? property.Value.Uri.OriginalString : string.Empty;
            if (!text.StartsWith(TelPrefix, StringComparison.OrdinalIgnoreCase)) return false;

            var end = text.IndexOf(';');
            value = end < 0 ? text.Substring(TelPrefix.Length) : text.Substring(TelPrefix.Length, end - TelPrefix.Length);
            return true;
        }

        private static bool GetTimeZone(VcardProperty<VcardTypedValue> property, out VcardTypedValue value)
        {
            value = property.Value;
            if (value.ValueType == VcardValueType.UtcOffset) return value.UtcOffset.IsDefinedIn(VcardVersion.V3_0);

            return value.ValueType == VcardValueType.Text;
        }

        private static bool GetGeo(VcardProperty<Uri> property, out VcardGeo value)
        {
            return VcardGeo.TryParse(property.Value, out value) && value.IsDefinedIn(VcardVersion.V3_0);
        }

        private static bool GetAddress(VcardProperty<VcardAddress> property, out VcardAddress value)
        {
            value = property.Value.WithoutRfc9554Components();
            return property.Phonetic is null && value.IsDefinedIn(VcardVersion.V3_0);
        }

        private static bool GetMedia(VcardProperty<VcardMediaUri> property, out VcardMediaValue value)
        {
            value = null;
            var name = property.Name;
            MimeDataUri data;
            if (property.Value.TryGetData(out data))
            {
                value = VcardMediaValue.FromData(data.GetData(), GetDataToken(data, name));
                return true;
            }

            // ref. VcardValue.MaxUriLengthOnEveryRuntime: a longer URI has no Uri on any runtime.
            if (property.Value.Uri is null) return false;

            value = VcardMediaValue.FromUri(property.Value.Uri, property.MediaType.HasValue ? VcardVersion30Mapping.GetToken(property.MediaType.Value, name) : null);
            return true;
        }

        private static bool GetKey(VcardProperty<VcardTypedValue> property, out VcardMediaValue value)
        {
            value = null;
            if (property.Value.ValueType == VcardValueType.Text)
            {
                value = VcardMediaValue.FromText(property.Value.Text, property.Types.Count == 0 ? null : property.Types[0]);
                return true;
            }

            // ref. RFC 2426 §4: a key is binary or text.
            MimeDataUri data;
            if (!MimeDataUri.TryParse(property.Value.Uri.OriginalString, out data)) return false;

            value = VcardMediaValue.FromData(data.GetData(), GetDataToken(data, property.Name));
            return true;
        }

        private static string GetDataToken(MimeDataUri data, string name)
        {
            return VcardVersion30Mapping.IsUntyped(data.MediaType) ? null : VcardVersion30Mapping.GetToken(data.MediaType, name);
        }

        private static VcardMediaUri MediaToCanonical(VcardMediaValue value, string name)
        {
            if (value.Uri != null) return VcardMediaUri.FromUri(value.Uri);

            return VcardMediaUri.FromData(MediaToData(value, name));
        }

        private static MimeDataUri MediaToData(VcardMediaValue value, string name)
        {
            var data = value.GetData();
            if (data is null)
                throw new ArgumentException("Only a key can have text.", nameof(value));

            var mediaType = value.TypeToken is null ? MimeMediaType.Parse("application/octet-stream".AsSpan()) : GetMediaTypeOfToken(value.TypeToken, name);
            return MimeDataUri.Create(mediaType, data);
        }

        /// <summary>
        /// Gets the <see cref="Uri"/> of inline data, for a property whose canonical value is a <see cref="VcardTypedValue"/>.
        /// </summary>
        /// <exception cref="ArgumentException">The <c>data:</c> URI has more than <see cref="VcardValue.MaxUriLengthOnEveryRuntime"/> characters, on every runtime.</exception>
        private static Uri DataToUri(MimeDataUri data, string parameterName)
        {
            var text = data.ToString();
            if (text.Length > VcardValue.MaxUriLengthOnEveryRuntime)
                throw new ArgumentException("The data is too long for a Uri, which holds no more than " + VcardValue.MaxUriLengthOnEveryRuntime +
                                            " characters on .NET Framework.  Add a line to VcardCard.Properties instead.", parameterName);

            return new Uri(text, UriKind.Absolute);
        }

        private static MimeMediaType GetMediaTypeOfToken(string token, string name)
        {
            MimeMediaType mediaType;
            if (!VcardVersion30Mapping.TryGetMediaType(token, name, out mediaType))
                throw new ArgumentException("The token \"" + token + "\" can't name a media type.", nameof(token));

            return mediaType;
        }

        private static void PrepareMediaParameters(VcardMediaValue value, VcardPropertyParameters parameters, string name)
        {
            if (value.Uri != null && value.TypeToken != null)
            {
                parameters.MediaType = GetMediaTypeOfToken(value.TypeToken, name);
            }
        }

        private IVcard30PropertyList<VcardMediaValue> MediaView(VcardPropertyList<VcardMediaUri> list, string name)
        {
            return new Vcard30PropertyList<VcardMediaUri, VcardMediaValue>(this, list, GetMedia, value => MediaToCanonical(value, name),
                (value, parameters) => PrepareMediaParameters(value, parameters, name));
        }

        private static VcardTypedValue KeyToCanonical(VcardMediaValue value)
        {
            if (value.Text != null) return new VcardTypedValue(value.Text);

            if (value.Uri != null) return new VcardTypedValue(value.Uri);

            return new VcardTypedValue(DataToUri(MediaToData(value, VcardPropertyNames.Key), nameof(value)));
        }

        private static void PrepareKeyParameters(VcardMediaValue value, VcardPropertyParameters parameters)
        {
            // ref. RFC 6350 §6.8.1: mediatype-param is only with a URI, so the TYPE of a text key stays.
            if (value.Text != null && value.TypeToken != null)
            {
                parameters.Types.Add(value.TypeToken);
            }
        }

        private static bool GetBirthdayValue(VcardProperty<VcardTypedValue> property, out VcardDateAndOrTime value)
        {
            value = default(VcardDateAndOrTime);
            if (property.Value.ValueType != VcardValueType.DateAndOrTime) return false;

            value = property.Value.DateAndOrTime;
            return value.IsDefinedIn(VcardVersion.V3_0);
        }

        private static bool GetNameValue(VcardProperty<VcardName> property, out VcardName value)
        {
            value = property.Value.WithoutRfc9554Components();
            return true;
        }

        private static bool GetUidValue(VcardProperty<VcardTypedValue> property, out string value)
        {
            value = property.Value.ValueType == VcardValueType.Uri ? property.Value.Uri.OriginalString : property.Value.Text;
            return true;
        }

        IVcard30PropertyList<string> IVcard30Card.FormattedNames => View(FormattedNames);

        IVcard30PropertyList<IReadOnlyList<string>> IVcard30Card.Nicknames => View(Nicknames);

        IVcard30PropertyList<VcardMediaValue> IVcard30Card.Photos => MediaView(Photos, VcardPropertyNames.Photo);

        IVcard30PropertyList<VcardAddress> IVcard30Card.Addresses => new Vcard30PropertyList<VcardAddress, VcardAddress>(this, Addresses, GetAddress, Same);

        IVcard30PropertyList<string> IVcard30Card.Labels => new Vcard30LabelList(this);

        IVcard30PropertyList<string> IVcard30Card.Telephones => new Vcard30PropertyList<VcardTypedValue, string>(this, Telephones, GetTelephone, value => new VcardTypedValue(value));

        IVcard30PropertyList<string> IVcard30Card.Emails => View(Emails);

        IVcard30PropertyList<VcardTypedValue> IVcard30Card.TimeZones => new Vcard30PropertyList<VcardTypedValue, VcardTypedValue>(this, TimeZones, GetTimeZone, Same);

        IVcard30PropertyList<VcardGeo> IVcard30Card.GeoLocations => new Vcard30PropertyList<Uri, VcardGeo>(this, GeoLocations, GetGeo, value => new Uri(value.UriText));

        IVcard30PropertyList<string> IVcard30Card.Titles => View(Titles);

        IVcard30PropertyList<string> IVcard30Card.Roles => View(Roles);

        IVcard30PropertyList<VcardMediaValue> IVcard30Card.Logos => MediaView(Logos, VcardPropertyNames.Logo);

        IVcard30PropertyList<VcardOrganization> IVcard30Card.Organizations => View(Organizations);

        IVcard30PropertyList<IReadOnlyList<string>> IVcard30Card.Categories => View(Categories);

        IVcard30PropertyList<string> IVcard30Card.Notes => View(Notes);

        IVcard30PropertyList<VcardMediaValue> IVcard30Card.Sounds => MediaView(Sounds, VcardPropertyNames.Sound);

        IVcard30PropertyList<Uri> IVcard30Card.Urls => View(Urls);

        IVcard30PropertyList<VcardMediaValue> IVcard30Card.Keys => new Vcard30PropertyList<VcardTypedValue, VcardMediaValue>(this, Keys, GetKey, KeyToCanonical, PrepareKeyParameters);

        IVcard30PropertyList<Uri> IVcard30Card.Sources => View(Sources);

        IVcard30PropertyList<Uri> IVcard30Card.InstantMessaging => View(InstantMessaging);

        IVcard30PropertyList<Uri> IVcard30Card.FreeBusyUrls => View(FreeBusyUrls);

        IVcard30PropertyList<Uri> IVcard30Card.CalendarAddressUris => View(CalendarAddressUris);

        IVcard30PropertyList<Uri> IVcard30Card.CalendarAccessUris => View(CalendarAccessUris);

        IVcard30PropertyList<Uri> IVcard30Card.CalendarUris => View(CalendarUris);

        IReadOnlyList<IVcard30Property<string>> IVcard30Card.ExtensionProperties
        {
            get
            {
                var result = new List<IVcard30Property<string>>();
                foreach (var line in Properties)
                {
                    if (VcardPropertyInfo.Find(line.Name) is null)
                    {
                        result.Add(new Vcard30Property<string>(line, line.Name, line.Value, Vcard30Views.IsPreferred(this, line)));
                    }
                }

                return new ReadOnlyCollection<IVcard30Property<string>>(result);
            }
        }

        IVcard30Property<VcardName> IVcard30Card.Name => Vcard30Views.Create<VcardName, VcardName>(this, Name, GetNameValue);

        IVcard30Property<VcardDateAndOrTime> IVcard30Card.Birthday => Vcard30Views.Create<VcardTypedValue, VcardDateAndOrTime>(this, Birthday, GetBirthdayValue);

        IVcard30Property<string> IVcard30Card.Mailer => Vcard30Views.Create<string, string>(this, Mailer, GetSame);

        IVcard30Card IVcard30Card.Agent
        {
            get
            {
                foreach (var agent in Agents)
                {
                    if (agent.Card != null) return agent.Card;
                }

                return null;
            }
        }

        Uri IVcard30Card.AgentUri
        {
            get
            {
                foreach (var agent in Agents)
                {
                    if (agent.Uri != null) return agent.Uri;
                }

                return null;
            }
        }

        IVcard30Property<string> IVcard30Card.ProductId => Vcard30Views.Create<string, string>(this, ProductId, GetSame);

        IVcard30Property<VcardTimestamp> IVcard30Card.Revision => Vcard30Views.Create<VcardTimestamp, VcardTimestamp>(this, Revision, GetSame);

        string IVcard30Card.SortString
        {
            get
            {
                foreach (var name in new[] { VcardPropertyNames.Name, VcardPropertyNames.Organization })
                {
                    foreach (var line in GetProperties(name))
                    {
                        var sortAs = VcardPropertyParameterReader.GetListValues(line, VcardParameterNames.SortAs);
                        if (sortAs.Count != 0) return sortAs[0];
                    }
                }

                foreach (var line in GetProperties(VcardPropertyNames.SortString))
                {
                    string text;
                    if (VcardPropertyCodecs.TryParseText(line.Value, out text)) return text;
                }

                return null;
            }
        }

        IVcard30Property<string> IVcard30Card.Uid => Vcard30Views.Create<VcardTypedValue, string>(this, Uid, GetUidValue);

        IVcard30Property<string> IVcard30Card.Classification => Vcard30Views.Create<string, string>(this, Classification, GetSame);

        IVcard30Property<string> IVcard30Card.SourceName => Vcard30Views.Create<string, string>(this, SourceName, GetSame);

        IVcard30Property<string> IVcard30Card.Profile => Vcard30Views.Create<string, string>(this, Profile, GetSame);

        void IVcard30Card.SetName(VcardName value)
        {
            SetName(value);
        }

        void IVcard30Card.SetBirthday(VcardDateAndOrTime value)
        {
            if (value.Equals(default(VcardDateAndOrTime)) || !value.IsDefinedIn(VcardVersion.V3_0))
                throw new ArgumentException("vCard 3.0 can't hold this value.", nameof(value));

            SetBirthday(new VcardTypedValue(value));
        }

        void IVcard30Card.SetMailer(string value)
        {
            SetMailer(value);
        }

        void IVcard30Card.SetAgent(IVcard30Card agent)
        {
            if (agent is null)
                throw new ArgumentNullException(nameof(agent));

            var card = agent as VcardCard;
            if (card is null)
                throw new ArgumentException("The agent must be a VcardCard.", nameof(agent));

            RemoveAgents();
            AddAgent(card);
        }

        void IVcard30Card.SetAgent(Uri agent)
        {
            if (agent is null)
                throw new ArgumentNullException(nameof(agent));

            VcardValue.CheckUri(agent, nameof(agent));
            RemoveAgents();
            AddAgent(agent);
        }

        void IVcard30Card.SetProductId(string value)
        {
            SetProductId(value);
        }

        void IVcard30Card.SetRevision(VcardTimestamp value)
        {
            SetRevision(value);
        }

        void IVcard30Card.SetSortString(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var text = VcardValue.FormatText(value, VcardValueFormat.Text, VcardVersion.V4_0);
            ((IVcard30Card)this).RemoveProperties(VcardPropertyNames.SortString);
            Properties.Add(new VcardContentLine(VcardPropertyNames.SortString, text));
            VcardVersion30Mapping.TryAttachSortStringAt(this, Properties.Count - 1);
        }

        void IVcard30Card.SetUid(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            Uri uri;
            SetUid(VcardValue.TryParseUriText(value.AsSpan(), out uri) ? new VcardTypedValue(uri) : new VcardTypedValue(value));
        }

        void IVcard30Card.SetClassification(string value)
        {
            SetClassification(value);
        }

        void IVcard30Card.SetSourceName(string value)
        {
            SetSourceName(value);
        }

        void IVcard30Card.SetProfile(string value)
        {
            SetProfile(value);
        }

        IVcard30Property<string> IVcard30Card.AddExtensionProperty(string name, string value)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (VcardPropertyInfo.Find(name) != null)
                throw new ArgumentException("A version of vCard defines the property " + name + ".", nameof(name));

            var added = AddExtensionProperty(name, value);
            return new Vcard30Property<string>(added.Line, added.Name, added.Value, false);
        }

        int IVcard30Card.RemoveProperties(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            if (string.Equals(name, VcardPropertyNames.PostalLabel, StringComparison.OrdinalIgnoreCase)) return Vcard30Views.RemoveLabels(this);
            if (string.Equals(name, VcardPropertyNames.Agent, StringComparison.OrdinalIgnoreCase)) return RemoveAgents();

            if (string.Equals(name, VcardPropertyNames.SortString, StringComparison.OrdinalIgnoreCase))
            {
                return RemoveProperties(name) + Vcard30Views.RemoveParameter(this, VcardPropertyNames.Name, VcardParameterNames.SortAs) +
                       Vcard30Views.RemoveParameter(this, VcardPropertyNames.Organization, VcardParameterNames.SortAs);
            }

            return RemoveProperties(name);
        }

        IReadOnlyList<VcardDiagnostic> IVcard30Card.Validate()
        {
            return Validate(VcardVersion.V3_0);
        }
    }
}
