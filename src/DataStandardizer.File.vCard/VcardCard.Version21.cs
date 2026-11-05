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
    /// The shape of vCard 2.1.
    /// </content>
    public sealed partial class VcardCard
    {

        /// <summary>
        /// Gets the card in the shape of vCard 2.1.  It is this object, so a change through either is a change to both.
        /// </summary>
        /// <remarks>
        /// Any card can be viewed this way, whatever version it was read from.  A view hides what vCard 2.1 can't show; see <see cref="VcardVersionConverter.Analyze"/>.
        /// </remarks>
        public IVcard21Card AsVersion21()
        {
            return this;
        }

        private IVcard21PropertyList<TValue> View21<TValue>(VcardPropertyList<TValue> list)
        {
            return new Vcard21PropertyList<TValue, TValue>(this, list, GetSame, Same);
        }

        private static bool GetVersion21Text(VcardProperty<string> property, out string value)
        {
            value = property.Value;
            return true;
        }

        private static bool GetVersion21TimeZone(VcardProperty<VcardTypedValue> property, out VcardUtcOffset value)
        {
            // ref. vCard 2.1 §2.4.5: "It is an offset from Coordinated Universal Time (UTC)."
            value = default(VcardUtcOffset);
            if (property.Value.ValueType != VcardValueType.UtcOffset) return false;

            value = property.Value.UtcOffset;
            return value.IsDefinedIn(VcardVersion.V2_1);
        }

        private static bool GetVersion21Geo(VcardProperty<Uri> property, out VcardGeo value)
        {
            return VcardGeo.TryParse(property.Value, out value) && value.IsDefinedIn(VcardVersion.V2_1);
        }

        private static bool GetVersion21Address(VcardProperty<VcardAddress> property, out VcardAddress value)
        {
            value = property.Value.WithoutRfc9554Components();
            return property.Phonetic is null && value.IsDefinedIn(VcardVersion.V2_1);
        }

        private static bool GetVersion21Organization(VcardProperty<VcardOrganization> property, out VcardOrganization value)
        {
            value = property.Value;
            return value.IsDefinedIn(VcardVersion.V2_1);
        }

        private static bool GetVersion21Media(VcardProperty<Uri> property, out VcardMediaValue value)
        {
            MimeDataUri data;
            if (MimeDataUri.TryParse(property.Value.OriginalString, out data))
            {
                value = VcardMediaValue.FromData(data.GetData(), GetVersion21DataToken(data.MediaType, property.Types));
                return true;
            }

            value = VcardMediaValue.FromUri(property.Value, GetVersion21UriToken(property.MediaType, property.Types));
            return true;
        }

        private static bool GetVersion21TypedMedia(VcardProperty<VcardTypedValue> property, out VcardMediaValue value)
        {
            value = null;
            switch (property.Value.ValueType)
            {
                case VcardValueType.Text:
                    // ref. vCard 2.1 §2.6.3 and §2.7.1: a sound can be "a string representation", and a key's "default format is clear-text".  Its TYPE says the format.
                    value = VcardMediaValue.FromText(property.Value.Text, GetFormatToken(property.Types));
                    return true;
                case VcardValueType.Uri:
                    MimeDataUri data;
                    value = MimeDataUri.TryParse(property.Value.Uri.OriginalString, out data)
                        ? VcardMediaValue.FromData(data.GetData(), GetVersion21DataToken(data.MediaType, property.Types))
                        : VcardMediaValue.FromUri(property.Value.Uri, GetVersion21UriToken(property.MediaType, property.Types));
                    return true;
                default:
                    return false;
            }
        }

        private static string GetVersion21Token(MimeMediaType mediaType)
        {
            string token;
            return VcardLegacyMediaTypes.TryGetToken(mediaType, VcardVersion.V2_1, out token) ? token : null;
        }

        /// <summary>
        /// Gets the token of the format of data: that of its media type, or for data with none, a format that has no media type, such as <c>PICT</c>, which stays a <c>TYPE</c>.
        /// </summary>
        private static string GetVersion21DataToken(MimeMediaType mediaType, IReadOnlyList<string> types)
        {
            return VcardVersion30Mapping.IsUntyped(mediaType) ? GetFormatToken(types) : GetVersion21Token(mediaType);
        }

        private static string GetVersion21UriToken(MimeMediaType? mediaType, IReadOnlyList<string> types)
        {
            return mediaType.HasValue ? GetVersion21Token(mediaType.Value) : GetFormatToken(types);
        }

        /// <summary>
        /// Gets the first <c>TYPE</c> that names a format (vCard 2.1 §2.9), in upper case, and not one such as <c>HOME</c> that says what the value is for.
        /// </summary>
        private static string GetFormatToken(IReadOnlyList<string> types)
        {
            foreach (var type in types)
            {
                if (VcardLegacyTokens.IsFormatType(type.AsSpan())) return type.ToUpperInvariant();
            }

            return null;
        }

        /// <summary>
        /// Gets whether a token names a format of vCard 2.1 that has no media type, such as <c>PICT</c>.
        /// </summary>
        private static bool IsFormatWithoutMediaType(string token)
        {
            MimeMediaType mediaType;
            return VcardLegacyTokens.IsFormatType(token.AsSpan()) && !VcardLegacyMediaTypes.TryGetMediaType(token.AsSpan(), out mediaType);
        }

        private static Uri Version21MediaToUri(VcardMediaValue value)
        {
            if (value.Uri != null) return value.Uri;

            var data = value.GetData();
            if (data is null)
                throw new ArgumentException("Only a sound or a key can be text.", nameof(value));

            var isUntyped = value.TypeToken is null || IsFormatWithoutMediaType(value.TypeToken);
            var mediaType = isUntyped ? MimeMediaType.Parse("application/octet-stream".AsSpan()) : GetVersion21MediaType(value.TypeToken);
            try
            {
                return new Uri(DataUriText(mediaType, data));
            }
            catch (FormatException exception)
            {
                // ref. #619: Uri on .NET Framework holds no more than 65519 characters.
                throw new ArgumentException("The data is too long for a Uri on this runtime.  Add a line to VcardCard.Properties instead.", nameof(value), exception);
            }
        }

        private static VcardTypedValue Version21MediaToTyped(VcardMediaValue value)
        {
            return value.Text != null ? new VcardTypedValue(value.Text) : new VcardTypedValue(Version21MediaToUri(value));
        }

        private static MimeMediaType GetVersion21MediaType(string token)
        {
            // ref. vCard 2.1 §2.9: the formats of knowntype.  Those that have no media type can't name the format of data.
            MimeMediaType mediaType;
            if (!VcardLegacyTokens.IsFormatType(token.AsSpan()) || !VcardLegacyMediaTypes.TryGetMediaType(token.AsSpan(), out mediaType))
                throw new ArgumentException("The token \"" + token + "\" names no media type of vCard 2.1.", nameof(token));

            return mediaType;
        }

        private static void PrepareVersion21MediaParameters(VcardMediaValue value, VcardPropertyParameters parameters)
        {
            if (value.TypeToken is null) return;

            // ref. vCard 2.1 §2.9: the format of a value is a knowntype that names one.
            if (!VcardLegacyTokens.IsFormatType(value.TypeToken.AsSpan()))
                throw new ArgumentException("vCard 2.1 has no format " + value.TypeToken + ".", nameof(value));

            // ref. RFC 6350 §6.8.1: mediatype-param is only with a URI, so the TYPE of text stays, and so does a format that has no media type.
            if (value.Text != null || IsFormatWithoutMediaType(value.TypeToken))
            {
                parameters.Types.Add(value.TypeToken.ToLowerInvariant());
            }
            else if (value.Uri != null)
            {
                parameters.MediaType = GetVersion21MediaType(value.TypeToken);
            }
        }

        private IVcard21PropertyList<VcardMediaValue> Version21MediaView(VcardPropertyList<Uri> list)
        {
            return new Vcard21PropertyList<Uri, VcardMediaValue>(this, list, GetVersion21Media, Version21MediaToUri, PrepareVersion21MediaParameters);
        }

        private IVcard21PropertyList<VcardMediaValue> Version21TypedMediaView(VcardPropertyList<VcardTypedValue> list)
        {
            return new Vcard21PropertyList<VcardTypedValue, VcardMediaValue>(this, list, GetVersion21TypedMedia, Version21MediaToTyped, PrepareVersion21MediaParameters);
        }

        private static bool GetVersion21Name(VcardProperty<VcardName> property, out VcardName value)
        {
            value = property.Value.WithoutRfc9554Components();
            return property.Phonetic is null && value.IsDefinedIn(VcardVersion.V2_1);
        }

        private static bool GetVersion21Birthday(VcardProperty<VcardTypedValue> property, out VcardDate value)
        {
            // ref. vCard 2.1 §2.2.4: "a calendar date in a complete representation consistent with ISO 8601".
            value = default(VcardDate);
            if (property.Value.ValueType != VcardValueType.DateAndOrTime) return false;

            var date = property.Value.DateAndOrTime.Date;
            if (property.Value.DateAndOrTime.Kind != VcardDateAndOrTimeKind.Date || !date.HasValue || !date.Value.IsDefinedIn(VcardVersion.V2_1)) return false;

            value = date.Value;
            return true;
        }

        private static bool GetVersion21Revision(VcardProperty<VcardTimestamp> property, out VcardTimestamp value)
        {
            value = property.Value;
            return value.IsDefinedIn(VcardVersion.V2_1);
        }

        IVcard21PropertyList<string> IVcard21Card.FormattedNames => View21(FormattedNames);

        IVcard21PropertyList<VcardMediaValue> IVcard21Card.Photos => Version21MediaView(Photos);

        IVcard21PropertyList<VcardAddress> IVcard21Card.Addresses => new Vcard21PropertyList<VcardAddress, VcardAddress>(this, Addresses, GetVersion21Address, Same);

        IVcard21PropertyList<string> IVcard21Card.Labels => new Vcard21LabelList(this);

        IVcard21PropertyList<string> IVcard21Card.Telephones =>
            new Vcard21PropertyList<VcardTypedValue, string>(this, Telephones, (VcardProperty<VcardTypedValue> property, out string value) => GetTelephone(property, out value), value => new VcardTypedValue(value));

        IVcard21PropertyList<string> IVcard21Card.Emails => View21(Emails);

        IVcard21PropertyList<VcardUtcOffset> IVcard21Card.TimeZones => new Vcard21PropertyList<VcardTypedValue, VcardUtcOffset>(this, TimeZones, GetVersion21TimeZone, value => new VcardTypedValue(value));

        IVcard21PropertyList<VcardGeo> IVcard21Card.GeoLocations => new Vcard21PropertyList<Uri, VcardGeo>(this, GeoLocations, GetVersion21Geo, value => new Uri(value.UriText));

        IVcard21PropertyList<string> IVcard21Card.Titles => View21(Titles);

        IVcard21PropertyList<string> IVcard21Card.Roles => View21(Roles);

        IVcard21PropertyList<VcardMediaValue> IVcard21Card.Logos => Version21MediaView(Logos);

        IVcard21PropertyList<VcardOrganization> IVcard21Card.Organizations => new Vcard21PropertyList<VcardOrganization, VcardOrganization>(this, Organizations, GetVersion21Organization, Same);

        IVcard21PropertyList<string> IVcard21Card.Notes => new Vcard21PropertyList<string, string>(this, Notes, GetVersion21Text, Same);

        IVcard21PropertyList<VcardMediaValue> IVcard21Card.Sounds => new Vcard21SoundList(this, Version21MediaView(Sounds));

        IVcard21PropertyList<Uri> IVcard21Card.Urls => View21(Urls);

        IVcard21PropertyList<VcardMediaValue> IVcard21Card.Keys => Version21TypedMediaView(Keys);

        IReadOnlyList<IVcard21Property<string>> IVcard21Card.ExtensionProperties
        {
            get
            {
                var result = new List<IVcard21Property<string>>();
                foreach (var line in Properties)
                {
                    if (VcardPropertyInfo.Find(line.Name) is null && VcardLegacyTokens.IsExtensionWord(line.Name))
                    {
                        result.Add(new Vcard21Property<string>(line, line.Name, line.Value, Vcard21Views.IsPreferred(this, line)));
                    }
                }

                return new ReadOnlyCollection<IVcard21Property<string>>(result);
            }
        }

        IVcard21Property<VcardName> IVcard21Card.Name => Vcard21Views.Create<VcardName, VcardName>(this, Name, GetVersion21Name);

        IVcard21Property<VcardDate> IVcard21Card.Birthday => Vcard21Views.Create<VcardTypedValue, VcardDate>(this, Birthday, GetVersion21Birthday);

        IVcard21Property<string> IVcard21Card.Mailer => Vcard21Views.Create<string, string>(this, Mailer, GetSame);

        IVcard21Card IVcard21Card.Agent
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

        Uri IVcard21Card.AgentUri
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

        IVcard21Property<VcardTimestamp> IVcard21Card.Revision => Vcard21Views.Create<VcardTimestamp, VcardTimestamp>(this, Revision, GetVersion21Revision);

        IVcard21Property<string> IVcard21Card.Uid => Vcard21Views.Create<VcardTypedValue, string>(this, Uid, GetUidValue);

        void IVcard21Card.SetName(VcardName value)
        {
            if (value.Equals(default(VcardName)) || !value.IsDefinedIn(VcardVersion.V2_1))
                throw new ArgumentException("vCard 2.1 can't hold this name.", nameof(value));

            SetName(value);
        }

        void IVcard21Card.SetBirthday(VcardDate value)
        {
            if (value.Equals(default(VcardDate)) || !value.IsDefinedIn(VcardVersion.V2_1))
                throw new ArgumentException("vCard 2.1 can't hold this date.", nameof(value));

            SetBirthday(new VcardTypedValue(new VcardDateAndOrTime(value)));
        }

        void IVcard21Card.SetMailer(string value)
        {
            SetMailer(value);
        }

        void IVcard21Card.SetAgent(IVcard21Card agent)
        {
            if (agent is null)
                throw new ArgumentNullException(nameof(agent));

            var card = agent as VcardCard;
            if (card is null)
                throw new ArgumentException("The agent must be a VcardCard.", nameof(agent));

            RemoveAgents();
            AddAgent(card);
        }

        void IVcard21Card.SetAgent(Uri agent)
        {
            if (agent is null)
                throw new ArgumentNullException(nameof(agent));

            VcardValue.CheckUri(agent, nameof(agent));
            RemoveAgents();
            AddAgent(agent);
        }

        void IVcard21Card.SetRevision(VcardTimestamp value)
        {
            if (value.Equals(default(VcardTimestamp)) || !value.IsDefinedIn(VcardVersion.V2_1))
                throw new ArgumentException("vCard 2.1 can't hold this time.", nameof(value));

            SetRevision(value);
        }

        void IVcard21Card.SetUid(string value)
        {
            ((IVcard30Card)this).SetUid(value);
        }

        IVcard21Property<string> IVcard21Card.AddExtensionProperty(string name, string value)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (VcardPropertyInfo.Find(name) != null || !VcardLegacyTokens.IsExtensionWord(name))
                throw new ArgumentException("The name of an extension of vCard 2.1 starts with X- and is a word (§2.8.1, §2.9).", nameof(name));

            var added = AddExtensionProperty(name, value);
            return new Vcard21Property<string>(added.Line, added.Name, added.Value, false);
        }

        int IVcard21Card.RemoveProperties(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            if (string.Equals(name, VcardPropertyNames.PostalLabel, StringComparison.OrdinalIgnoreCase)) return Vcard30Views.RemoveLabels(this);
            if (string.Equals(name, VcardPropertyNames.Agent, StringComparison.OrdinalIgnoreCase)) return RemoveAgents();

            return RemoveProperties(name);
        }

        IReadOnlyList<VcardDiagnostic> IVcard21Card.Validate()
        {
            return Validate(VcardVersion.V2_1);
        }
    }
}
