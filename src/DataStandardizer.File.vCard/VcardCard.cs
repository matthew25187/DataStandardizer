using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DataStandardizer.LanguageTag;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A vCard: an open list of properties, with typed accessors that don't depend on the version the card was read from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The model is a hybrid, because the versions of vCard are not additive.  The public members of this class are the common half: they cover every version, and each concept is
    /// held once, in a canonical form, which is the form of vCard 4.0 wherever 4.0 has an equivalent.  The versioned half is an interface per version, such as
    /// <see cref="IVcard40Card"/>, which this class implements explicitly.  Each gives the shape of its own version only.  Use <see cref="AsVersion40"/> to get one.
    /// </para>
    /// <para>
    /// <see cref="Properties"/> holds every property in order, including unknown and <c>X-</c> properties, with their parameters, whether or not their values are valid.  It doesn't
    /// hold <c>BEGIN</c>, <c>VERSION</c> or <c>END</c>, which a reader takes in and a writer writes.  The typed accessors read and change this list, so nothing is held twice.  They
    /// leave out a property whose value isn't valid, and it stays in <see cref="Properties"/>.  A property with a <c>CALSCALE</c> other than <c>gregorian</c> is left out too, since
    /// RFC 6350 §5.8 says to ignore it.
    /// </para>
    /// <para>
    /// A setter of a property of which a card may have only one replaces every instance, including alternatives with the same <c>ALTID</c>, in the place of the first.  Instances of
    /// other properties go through a <see cref="VcardPropertyList{TValue}"/>.  The class is not safe for use by several threads while it is being changed.
    /// </para>
    /// <para>
    /// Such a setter throws <see cref="InvalidOperationException"/> if an instance has <c>DERIVED=true</c>, since RFC 9554 §4.4 says that a client "MUST NOT update the property".  A client
    /// that works out the value again removes the instance first, with <see cref="RemoveProperties"/>.
    /// </para>
    /// <para>
    /// <see cref="SetName(VcardName)"/> and <see cref="Addresses"/> also put the components of RFC 9554 in the older components, as its §2.1 and §2.2 say to; see
    /// <see cref="VcardName.WithBackwardCompatibleComponents"/> and <see cref="VcardAddress.WithBackwardCompatibleComponents"/>.  A line added to <see cref="Properties"/> is left as it is.
    /// </para>
    /// </remarks>
    public sealed class VcardCard : IVcard40Card
    {
        private static readonly VcardPropertyCodec<string> KindCodec = VcardPropertyCodecs.Text(VcardPropertyNames.Kind);
        private static readonly VcardPropertyCodec<VcardName> NameCodec = VcardPropertyCodecs.Name();
        private static readonly VcardPropertyCodec<VcardTypedValue> BirthdayCodec = VcardPropertyCodecs.Typed(VcardPropertyNames.Birthday);
        private static readonly VcardPropertyCodec<VcardTypedValue> AnniversaryCodec = VcardPropertyCodecs.Typed(VcardPropertyNames.Anniversary);
        private static readonly VcardPropertyCodec<VcardGender> GenderCodec = VcardPropertyCodecs.Gender();
        private static readonly VcardPropertyCodec<string> ProductIdCodec = VcardPropertyCodecs.Text(VcardPropertyNames.ProductId);
        private static readonly VcardPropertyCodec<VcardTimestamp> RevisionCodec = VcardPropertyCodecs.Timestamp(VcardPropertyNames.Revision);
        private static readonly VcardPropertyCodec<VcardTimestamp> CreatedCodec = VcardPropertyCodecs.Timestamp(VcardPropertyNames.Created);
        private static readonly VcardPropertyCodec<Bcp47LanguageTag> DefaultLanguageCodec = VcardPropertyCodecs.LanguageTag(VcardPropertyNames.DefaultLanguage);
        private static readonly VcardPropertyCodec<VcardTypedValue> UidCodec = VcardPropertyCodecs.Typed(VcardPropertyNames.Uid);

        private static readonly VcardPropertyCodec<VcardTypedValue> BirthPlaceCodec = VcardPropertyCodecs.Typed(VcardPropertyNames.BirthPlace);
        private static readonly VcardPropertyCodec<VcardTypedValue> DeathPlaceCodec = VcardPropertyCodecs.Typed(VcardPropertyNames.DeathPlace);
        private static readonly VcardPropertyCodec<VcardTypedValue> DeathDateCodec = VcardPropertyCodecs.Typed(VcardPropertyNames.DeathDate);

        private static readonly VcardPropertyCodec<Uri> SourcesCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.Source);
        private static readonly VcardPropertyCodec<string> XmlCodec = VcardPropertyCodecs.Xml();
        private static readonly VcardPropertyCodec<string> FormattedNamesCodec = VcardPropertyCodecs.Text(VcardPropertyNames.FormattedName);
        private static readonly VcardPropertyCodec<IReadOnlyList<string>> NicknamesCodec = VcardPropertyCodecs.TextList(VcardPropertyNames.Nickname);
        private static readonly VcardPropertyCodec<Uri> PhotosCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.Photo);
        private static readonly VcardPropertyCodec<VcardAddress> AddressesCodec = VcardPropertyCodecs.Address();
        private static readonly VcardPropertyCodec<VcardTypedValue> TelephonesCodec = VcardPropertyCodecs.Typed(VcardPropertyNames.Telephone);
        private static readonly VcardPropertyCodec<string> EmailsCodec = VcardPropertyCodecs.Text(VcardPropertyNames.Email);
        private static readonly VcardPropertyCodec<Uri> InstantMessagingCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.InstantMessaging);
        private static readonly VcardPropertyCodec<Bcp47LanguageTag> LanguagesCodec = VcardPropertyCodecs.LanguageTag(VcardPropertyNames.Language);
        private static readonly VcardPropertyCodec<VcardTypedValue> TimeZonesCodec = VcardPropertyCodecs.Typed(VcardPropertyNames.TimeZone);
        private static readonly VcardPropertyCodec<Uri> GeoLocationsCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.Geo);
        private static readonly VcardPropertyCodec<string> TitlesCodec = VcardPropertyCodecs.Text(VcardPropertyNames.Title);
        private static readonly VcardPropertyCodec<string> RolesCodec = VcardPropertyCodecs.Text(VcardPropertyNames.Role);
        private static readonly VcardPropertyCodec<Uri> LogosCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.Logo);
        private static readonly VcardPropertyCodec<VcardOrganization> OrganizationsCodec = VcardPropertyCodecs.Organization();
        private static readonly VcardPropertyCodec<Uri> MembersCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.Member);
        private static readonly VcardPropertyCodec<VcardTypedValue> RelatedCodec = VcardPropertyCodecs.Typed(VcardPropertyNames.Related);
        private static readonly VcardPropertyCodec<IReadOnlyList<string>> CategoriesCodec = VcardPropertyCodecs.TextList(VcardPropertyNames.Categories);
        private static readonly VcardPropertyCodec<string> NotesCodec = VcardPropertyCodecs.Text(VcardPropertyNames.Note);
        private static readonly VcardPropertyCodec<Uri> SoundsCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.Sound);
        private static readonly VcardPropertyCodec<VcardClientPidMap> ClientPidMapsCodec = VcardPropertyCodecs.ClientPidMap();
        private static readonly VcardPropertyCodec<Uri> UrlsCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.Url);
        private static readonly VcardPropertyCodec<VcardTypedValue> KeysCodec = VcardPropertyCodecs.Typed(VcardPropertyNames.Key);
        private static readonly VcardPropertyCodec<Uri> FreeBusyUrlsCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.FreeBusyUrl);
        private static readonly VcardPropertyCodec<Uri> CalendarAddressUrisCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.CalendarAddressUri);
        private static readonly VcardPropertyCodec<Uri> CalendarUrisCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.CalendarUri);
        private static readonly VcardPropertyCodec<string> ExpertiseCodec = VcardPropertyCodecs.Text(VcardPropertyNames.Expertise);
        private static readonly VcardPropertyCodec<string> HobbiesCodec = VcardPropertyCodecs.Text(VcardPropertyNames.Hobby);
        private static readonly VcardPropertyCodec<string> InterestsCodec = VcardPropertyCodecs.Text(VcardPropertyNames.Interest);
        private static readonly VcardPropertyCodec<Uri> OrganizationDirectoriesCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.OrganizationDirectory);
        private static readonly VcardPropertyCodec<Uri> ContactUrisCodec = VcardPropertyCodecs.Uri(VcardPropertyNames.ContactUri);
        private static readonly VcardPropertyCodec<string> GrammaticalGendersCodec = VcardPropertyCodecs.Text(VcardPropertyNames.GrammaticalGender);
        private static readonly VcardPropertyCodec<string> PronounsCodec = VcardPropertyCodecs.Text(VcardPropertyNames.Pronouns);
        private static readonly VcardPropertyCodec<VcardTypedValue> SocialProfilesCodec = VcardPropertyCodecs.Typed(VcardPropertyNames.SocialProfile);

        private const string IndividualText = "individual";
        private const string GroupText = "group";
        private const string OrganizationText = "org";
        private const string LocationText = "location";

        // ref. RFC 6473 §2.
        private const string ApplicationText = "application";

        // ref. RFC 6869 §2.
        private const string DeviceText = "device";

        // ref. RFC 4122 §3: UUIDs are case-insensitive on input.
        private const string UuidUrnPrefix = "urn:uuid:";

        /// <summary>
        /// Creates a card with no properties.  It needs at least one <c>FN</c> to conform to RFC 6350.
        /// </summary>
        public VcardCard()
        {
            Sources = new VcardPropertyList<Uri>(this, SourcesCodec);
            Xml = new VcardPropertyList<string>(this, XmlCodec);
            FormattedNames = new VcardPropertyList<string>(this, FormattedNamesCodec);
            Nicknames = new VcardPropertyList<IReadOnlyList<string>>(this, NicknamesCodec);
            Photos = new VcardPropertyList<Uri>(this, PhotosCodec);
            Addresses = new VcardPropertyList<VcardAddress>(this, AddressesCodec);
            Telephones = new VcardPropertyList<VcardTypedValue>(this, TelephonesCodec);
            Emails = new VcardPropertyList<string>(this, EmailsCodec);
            InstantMessaging = new VcardPropertyList<Uri>(this, InstantMessagingCodec);
            Languages = new VcardPropertyList<Bcp47LanguageTag>(this, LanguagesCodec);
            TimeZones = new VcardPropertyList<VcardTypedValue>(this, TimeZonesCodec);
            GeoLocations = new VcardPropertyList<Uri>(this, GeoLocationsCodec);
            Titles = new VcardPropertyList<string>(this, TitlesCodec);
            Roles = new VcardPropertyList<string>(this, RolesCodec);
            Logos = new VcardPropertyList<Uri>(this, LogosCodec);
            Organizations = new VcardPropertyList<VcardOrganization>(this, OrganizationsCodec);
            Members = new VcardPropertyList<Uri>(this, MembersCodec);
            Related = new VcardPropertyList<VcardTypedValue>(this, RelatedCodec);
            Categories = new VcardPropertyList<IReadOnlyList<string>>(this, CategoriesCodec);
            Notes = new VcardPropertyList<string>(this, NotesCodec);
            Sounds = new VcardPropertyList<Uri>(this, SoundsCodec);
            ClientPidMaps = new VcardPropertyList<VcardClientPidMap>(this, ClientPidMapsCodec);
            Urls = new VcardPropertyList<Uri>(this, UrlsCodec);
            Keys = new VcardPropertyList<VcardTypedValue>(this, KeysCodec);
            FreeBusyUrls = new VcardPropertyList<Uri>(this, FreeBusyUrlsCodec);
            CalendarAddressUris = new VcardPropertyList<Uri>(this, CalendarAddressUrisCodec);
            CalendarUris = new VcardPropertyList<Uri>(this, CalendarUrisCodec);
            Expertise = new VcardPropertyList<string>(this, ExpertiseCodec);
            Hobbies = new VcardPropertyList<string>(this, HobbiesCodec);
            Interests = new VcardPropertyList<string>(this, InterestsCodec);
            OrganizationDirectories = new VcardPropertyList<Uri>(this, OrganizationDirectoriesCodec);
            ContactUris = new VcardPropertyList<Uri>(this, ContactUrisCodec);
            GrammaticalGenders = new VcardPropertyList<string>(this, GrammaticalGendersCodec);
            Pronouns = new VcardPropertyList<string>(this, PronounsCodec);
            SocialProfiles = new VcardPropertyList<VcardTypedValue>(this, SocialProfilesCodec);
        }

        /// <summary>
        /// Gets the properties, in order.  It can be changed.
        /// </summary>
        /// <remarks>
        /// It doesn't hold <c>BEGIN</c>, <c>VERSION</c> or <c>END</c>.  <see cref="Validate"/> reports any that are added.
        /// </remarks>
        public IList<VcardContentLine> Properties { get; } = new NonNullCollection();

        /// <summary>
        /// Gets the version the card was read from; or <c>null</c> if it was built in code, or read with no <c>VERSION</c> under
        /// <see cref="VcardOptions.AllowMissingRequiredProperties"/>.
        /// </summary>
        public VcardVersion? SourceVersion { get; internal set; }

        /// <summary>
        /// Gets the 0-based index of the card in its source; or -1 if it was built in code.
        /// </summary>
        internal int CardIndex { get; set; } = -1;

        /// <summary>
        /// Gets the 1-based number of the logical line that starts the card in its source; or 0 if it was built in code.
        /// </summary>
        internal long LineNumber { get; set; }

        /// <summary>
        /// Gets the kind of object the card represents (RFC 6350 §6.1.4, RFC 6473 and RFC 6869).  With no <c>KIND</c> it is <see cref="VcardKind.Individual"/>, as RFC 6350 says.  Any value
        /// other than those the RFCs define is <see cref="VcardKind.Other"/>, and <see cref="KindText"/> holds it.
        /// </summary>
        public VcardKind Kind
        {
            get
            {
                var text = KindText;
                if (text is null || string.Equals(text, IndividualText, StringComparison.OrdinalIgnoreCase)) return VcardKind.Individual;
                if (string.Equals(text, GroupText, StringComparison.OrdinalIgnoreCase)) return VcardKind.Group;
                if (string.Equals(text, OrganizationText, StringComparison.OrdinalIgnoreCase)) return VcardKind.Organization;
                if (string.Equals(text, LocationText, StringComparison.OrdinalIgnoreCase)) return VcardKind.Location;
                if (string.Equals(text, ApplicationText, StringComparison.OrdinalIgnoreCase)) return VcardKind.Application;
                if (string.Equals(text, DeviceText, StringComparison.OrdinalIgnoreCase)) return VcardKind.Device;

                return VcardKind.Other;
            }
        }

        /// <summary>
        /// Gets the <c>SOURCE</c> properties (RFC 6350 §6.1.3).
        /// </summary>
        public VcardPropertyList<Uri> Sources { get; }

        /// <summary>
        /// Gets the <c>XML</c> properties (RFC 6350 §6.1.5), each a single XML element.
        /// </summary>
        public VcardPropertyList<string> Xml { get; }

        /// <summary>
        /// Gets the <c>FN</c> properties (RFC 6350 §6.2.1).
        /// </summary>
        public VcardPropertyList<string> FormattedNames { get; }

        /// <summary>
        /// Gets the <c>NICKNAME</c> properties (RFC 6350 §6.2.3), each a list of nicknames.
        /// </summary>
        public VcardPropertyList<IReadOnlyList<string>> Nicknames { get; }

        /// <summary>
        /// Gets the <c>PHOTO</c> properties (RFC 6350 §6.2.4).
        /// </summary>
        public VcardPropertyList<Uri> Photos { get; }

        /// <summary>
        /// Gets the <c>ADR</c> properties (RFC 6350 §6.3.1).
        /// </summary>
        public VcardPropertyList<VcardAddress> Addresses { get; }

        /// <summary>
        /// Gets the <c>TEL</c> properties (RFC 6350 §6.4.1), each text or a URI.
        /// </summary>
        public VcardPropertyList<VcardTypedValue> Telephones { get; }

        /// <summary>
        /// Gets the <c>EMAIL</c> properties (RFC 6350 §6.4.2).
        /// </summary>
        public VcardPropertyList<string> Emails { get; }

        /// <summary>
        /// Gets the <c>IMPP</c> properties (RFC 6350 §6.4.3).
        /// </summary>
        public VcardPropertyList<Uri> InstantMessaging { get; }

        /// <summary>
        /// Gets the <c>LANG</c> properties (RFC 6350 §6.4.4).
        /// </summary>
        public VcardPropertyList<Bcp47LanguageTag> Languages { get; }

        /// <summary>
        /// Gets the <c>TZ</c> properties (RFC 6350 §6.5.1), each text, a URI or a UTC offset.
        /// </summary>
        public VcardPropertyList<VcardTypedValue> TimeZones { get; }

        /// <summary>
        /// Gets the <c>GEO</c> properties (RFC 6350 §6.5.2).  <see cref="VcardGeo"/> reads a URI of the <c>geo</c> scheme.
        /// </summary>
        public VcardPropertyList<Uri> GeoLocations { get; }

        /// <summary>
        /// Gets the <c>TITLE</c> properties (RFC 6350 §6.6.1).
        /// </summary>
        public VcardPropertyList<string> Titles { get; }

        /// <summary>
        /// Gets the <c>ROLE</c> properties (RFC 6350 §6.6.2).
        /// </summary>
        public VcardPropertyList<string> Roles { get; }

        /// <summary>
        /// Gets the <c>LOGO</c> properties (RFC 6350 §6.6.3).
        /// </summary>
        public VcardPropertyList<Uri> Logos { get; }

        /// <summary>
        /// Gets the <c>ORG</c> properties (RFC 6350 §6.6.4).
        /// </summary>
        public VcardPropertyList<VcardOrganization> Organizations { get; }

        /// <summary>
        /// Gets the <c>MEMBER</c> properties (RFC 6350 §6.6.5).  See <see cref="ResolveMembers"/>.
        /// </summary>
        public VcardPropertyList<Uri> Members { get; }

        /// <summary>
        /// Gets the <c>RELATED</c> properties (RFC 6350 §6.6.6), each a URI or text.
        /// </summary>
        public VcardPropertyList<VcardTypedValue> Related { get; }

        /// <summary>
        /// Gets the <c>CATEGORIES</c> properties (RFC 6350 §6.7.1), each a list of categories.
        /// </summary>
        public VcardPropertyList<IReadOnlyList<string>> Categories { get; }

        /// <summary>
        /// Gets the <c>NOTE</c> properties (RFC 6350 §6.7.2).
        /// </summary>
        public VcardPropertyList<string> Notes { get; }

        /// <summary>
        /// Gets the <c>SOUND</c> properties (RFC 6350 §6.7.5).
        /// </summary>
        public VcardPropertyList<Uri> Sounds { get; }

        /// <summary>
        /// Gets the <c>CLIENTPIDMAP</c> properties (RFC 6350 §6.7.7).
        /// </summary>
        public VcardPropertyList<VcardClientPidMap> ClientPidMaps { get; }

        /// <summary>
        /// Gets the <c>URL</c> properties (RFC 6350 §6.7.8).
        /// </summary>
        public VcardPropertyList<Uri> Urls { get; }

        /// <summary>
        /// Gets the <c>KEY</c> properties (RFC 6350 §6.8.1), each a URI or text.
        /// </summary>
        public VcardPropertyList<VcardTypedValue> Keys { get; }

        /// <summary>
        /// Gets the <c>FBURL</c> properties (RFC 6350 §6.9.1).
        /// </summary>
        public VcardPropertyList<Uri> FreeBusyUrls { get; }

        /// <summary>
        /// Gets the <c>CALADRURI</c> properties (RFC 6350 §6.9.2).
        /// </summary>
        public VcardPropertyList<Uri> CalendarAddressUris { get; }

        /// <summary>
        /// Gets the <c>CALURI</c> properties (RFC 6350 §6.9.3).
        /// </summary>
        public VcardPropertyList<Uri> CalendarUris { get; }

        /// <summary>
        /// Gets the <c>EXPERTISE</c> properties (RFC 6715 §2.1).  <see cref="VcardProperty{TValue}.Level"/> and <see cref="VcardProperty{TValue}.Index"/> give their parameters.
        /// </summary>
        public VcardPropertyList<string> Expertise { get; }

        /// <summary>
        /// Gets the <c>HOBBY</c> properties (RFC 6715 §2.2).  <see cref="VcardProperty{TValue}.Level"/> and <see cref="VcardProperty{TValue}.Index"/> give their parameters.
        /// </summary>
        public VcardPropertyList<string> Hobbies { get; }

        /// <summary>
        /// Gets the <c>INTEREST</c> properties (RFC 6715 §2.3).  <see cref="VcardProperty{TValue}.Level"/> and <see cref="VcardProperty{TValue}.Index"/> give their parameters.
        /// </summary>
        public VcardPropertyList<string> Interests { get; }

        /// <summary>
        /// Gets the <c>ORG-DIRECTORY</c> properties (RFC 6715 §2.4).  See <see cref="VcardOptions.AcceptOrgUriProperty"/>.
        /// </summary>
        public VcardPropertyList<Uri> OrganizationDirectories { get; }

        /// <summary>
        /// Gets the <c>CONTACT-URI</c> properties (RFC 8605 §2.1).
        /// </summary>
        public VcardPropertyList<Uri> ContactUris { get; }

        /// <summary>
        /// Gets the <c>GRAMGENDER</c> properties (RFC 9554 §3.2), each the grammatical gender to use in a language: <c>animate</c>, <c>common</c>, <c>feminine</c>, <c>inanimate</c>,
        /// <c>masculine</c>, <c>neuter</c>, or an <c>iana-token</c> or <c>x-name</c>.  Several have to differ in their <c>LANGUAGE</c> parameter.
        /// </summary>
        public VcardPropertyList<string> GrammaticalGenders { get; }

        /// <summary>
        /// Gets the <c>PRONOUNS</c> properties (RFC 9554 §3.4).
        /// </summary>
        public VcardPropertyList<string> Pronouns { get; }

        /// <summary>
        /// Gets the <c>SOCIALPROFILE</c> properties (RFC 9554 §3.5), each a URI or a username.  A username has to have a <see cref="VcardPropertyParameters.ServiceType"/>.
        /// </summary>
        public VcardPropertyList<VcardTypedValue> SocialProfiles { get; }

        /// <summary>
        /// Gets the properties that RFC 6350 and its extensions don't define, including <c>X-</c> properties, in the order they appear.  Each value is as it appears in the content line, since what its
        /// escaping means is not known.
        /// </summary>
        public IReadOnlyList<VcardProperty<string>> ExtensionProperties
        {
            get
            {
                var result = new List<VcardProperty<string>>();
                foreach (var line in Properties)
                {
                    if (!IsDefinedInVersion40(line.Name))
                    {
                        result.Add(new VcardProperty<string>(line, line.Value));
                    }
                }

                return new ReadOnlyCollection<VcardProperty<string>>(result);
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the value of the <c>KIND</c> property (RFC 6350 §6.1.4) as written; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public string? KindText => GetSingle(KindCodec)?.Value;

        /// <summary>
        /// Gets the <c>N</c> property (RFC 6350 §6.2.2), leaving one with a <c>PHONETIC</c> parameter for last; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardName>? Name => GetSingle(NameCodec);

        /// <summary>
        /// Gets the <c>BDAY</c> property (RFC 6350 §6.2.5), a date-and-or-time or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTypedValue>? Birthday => GetSingle(BirthdayCodec);

        /// <summary>
        /// Gets the <c>ANNIVERSARY</c> property (RFC 6350 §6.2.6), a date-and-or-time or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTypedValue>? Anniversary => GetSingle(AnniversaryCodec);

        /// <summary>
        /// Gets the <c>GENDER</c> property (RFC 6350 §6.2.7); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardGender>? Gender => GetSingle(GenderCodec);

        /// <summary>
        /// Gets the <c>PRODID</c> property (RFC 6350 §6.7.3); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<string>? ProductId => GetSingle(ProductIdCodec);

        /// <summary>
        /// Gets the <c>REV</c> property (RFC 6350 §6.7.4); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTimestamp>? Revision => GetSingle(RevisionCodec);

        /// <summary>
        /// Gets the <c>UID</c> property (RFC 6350 §6.7.6), a URI or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTypedValue>? Uid => GetSingle(UidCodec);

        /// <summary>
        /// Gets the <c>BIRTHPLACE</c> property (RFC 6474 §2.1), text or a URI; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTypedValue>? BirthPlace => GetSingle(BirthPlaceCodec);

        /// <summary>
        /// Gets the <c>DEATHPLACE</c> property (RFC 6474 §2.2), text or a URI; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTypedValue>? DeathPlace => GetSingle(DeathPlaceCodec);

        /// <summary>
        /// Gets the <c>DEATHDATE</c> property (RFC 6474 §2.3), a date-and-or-time or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTypedValue>? DeathDate => GetSingle(DeathDateCodec);

        /// <summary>
        /// Gets the <c>CREATED</c> property (RFC 9554 §3.1), when the card was created; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTimestamp>? Created => GetSingle(CreatedCodec);

        /// <summary>
        /// Gets the <c>LANGUAGE</c> property (RFC 9554 §3.3), the language that the text values of the card are assumed to be written in, unless a <c>LANGUAGE</c> parameter says otherwise; or
        /// <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<Bcp47LanguageTag>? DefaultLanguage => GetSingle(DefaultLanguageCodec);

        string? IVcard40Card.KindText => KindText;

        IVcard40Property<VcardName>? IVcard40Card.Name => Name;

        IVcard40Property<VcardTypedValue>? IVcard40Card.Birthday => Birthday;

        IVcard40Property<VcardTypedValue>? IVcard40Card.Anniversary => Anniversary;

        IVcard40Property<VcardGender>? IVcard40Card.Gender => Gender;

        IVcard40Property<string>? IVcard40Card.ProductId => ProductId;

        IVcard40Property<VcardTimestamp>? IVcard40Card.Revision => Revision;

        IVcard40Property<VcardTypedValue>? IVcard40Card.Uid => Uid;

        IVcard40Property<VcardTypedValue>? IVcard40Card.BirthPlace => BirthPlace;

        IVcard40Property<VcardTypedValue>? IVcard40Card.DeathPlace => DeathPlace;

        IVcard40Property<VcardTypedValue>? IVcard40Card.DeathDate => DeathDate;

        IVcard40Property<VcardTimestamp>? IVcard40Card.Created => Created;

        IVcard40Property<Bcp47LanguageTag>? IVcard40Card.DefaultLanguage => DefaultLanguage;
#else
        /// <summary>
        /// Gets the value of the <c>KIND</c> property (RFC 6350 §6.1.4) as written; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public string KindText => GetSingle(KindCodec)?.Value;

        /// <summary>
        /// Gets the <c>N</c> property (RFC 6350 §6.2.2), leaving one with a <c>PHONETIC</c> parameter for last; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardName> Name => GetSingle(NameCodec);

        /// <summary>
        /// Gets the <c>BDAY</c> property (RFC 6350 §6.2.5), a date-and-or-time or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTypedValue> Birthday => GetSingle(BirthdayCodec);

        /// <summary>
        /// Gets the <c>ANNIVERSARY</c> property (RFC 6350 §6.2.6), a date-and-or-time or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTypedValue> Anniversary => GetSingle(AnniversaryCodec);

        /// <summary>
        /// Gets the <c>GENDER</c> property (RFC 6350 §6.2.7); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardGender> Gender => GetSingle(GenderCodec);

        /// <summary>
        /// Gets the <c>PRODID</c> property (RFC 6350 §6.7.3); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<string> ProductId => GetSingle(ProductIdCodec);

        /// <summary>
        /// Gets the <c>REV</c> property (RFC 6350 §6.7.4); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTimestamp> Revision => GetSingle(RevisionCodec);

        /// <summary>
        /// Gets the <c>UID</c> property (RFC 6350 §6.7.6), a URI or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTypedValue> Uid => GetSingle(UidCodec);

        /// <summary>
        /// Gets the <c>BIRTHPLACE</c> property (RFC 6474 §2.1), text or a URI; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTypedValue> BirthPlace => GetSingle(BirthPlaceCodec);

        /// <summary>
        /// Gets the <c>DEATHPLACE</c> property (RFC 6474 §2.2), text or a URI; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTypedValue> DeathPlace => GetSingle(DeathPlaceCodec);

        /// <summary>
        /// Gets the <c>DEATHDATE</c> property (RFC 6474 §2.3), a date-and-or-time or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTypedValue> DeathDate => GetSingle(DeathDateCodec);

        /// <summary>
        /// Gets the <c>CREATED</c> property (RFC 9554 §3.1), when the card was created; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<VcardTimestamp> Created => GetSingle(CreatedCodec);

        /// <summary>
        /// Gets the <c>LANGUAGE</c> property (RFC 9554 §3.3), the language that the text values of the card are assumed to be written in, unless a <c>LANGUAGE</c> parameter says otherwise; or
        /// <c>null</c> if the card has none with a valid value.
        /// </summary>
        public VcardProperty<Bcp47LanguageTag> DefaultLanguage => GetSingle(DefaultLanguageCodec);

        string IVcard40Card.KindText => KindText;

        IVcard40Property<VcardName> IVcard40Card.Name => Name;

        IVcard40Property<VcardTypedValue> IVcard40Card.Birthday => Birthday;

        IVcard40Property<VcardTypedValue> IVcard40Card.Anniversary => Anniversary;

        IVcard40Property<VcardGender> IVcard40Card.Gender => Gender;

        IVcard40Property<string> IVcard40Card.ProductId => ProductId;

        IVcard40Property<VcardTimestamp> IVcard40Card.Revision => Revision;

        IVcard40Property<VcardTypedValue> IVcard40Card.Uid => Uid;

        IVcard40Property<VcardTypedValue> IVcard40Card.BirthPlace => BirthPlace;

        IVcard40Property<VcardTypedValue> IVcard40Card.DeathPlace => DeathPlace;

        IVcard40Property<VcardTypedValue> IVcard40Card.DeathDate => DeathDate;

        IVcard40Property<VcardTimestamp> IVcard40Card.Created => Created;

        IVcard40Property<Bcp47LanguageTag> IVcard40Card.DefaultLanguage => DefaultLanguage;
#endif

        VcardKind IVcard40Card.Kind => Kind;

        IVcard40PropertyList<Uri> IVcard40Card.Sources => Sources.Version40;

        IVcard40PropertyList<string> IVcard40Card.Xml => Xml.Version40;

        IVcard40PropertyList<string> IVcard40Card.FormattedNames => FormattedNames.Version40;

        IVcard40PropertyList<IReadOnlyList<string>> IVcard40Card.Nicknames => Nicknames.Version40;

        IVcard40PropertyList<Uri> IVcard40Card.Photos => Photos.Version40;

        IVcard40PropertyList<VcardAddress> IVcard40Card.Addresses => Addresses.Version40;

        IVcard40PropertyList<VcardTypedValue> IVcard40Card.Telephones => Telephones.Version40;

        IVcard40PropertyList<string> IVcard40Card.Emails => Emails.Version40;

        IVcard40PropertyList<Uri> IVcard40Card.InstantMessaging => InstantMessaging.Version40;

        IVcard40PropertyList<Bcp47LanguageTag> IVcard40Card.Languages => Languages.Version40;

        IVcard40PropertyList<VcardTypedValue> IVcard40Card.TimeZones => TimeZones.Version40;

        IVcard40PropertyList<Uri> IVcard40Card.GeoLocations => GeoLocations.Version40;

        IVcard40PropertyList<string> IVcard40Card.Titles => Titles.Version40;

        IVcard40PropertyList<string> IVcard40Card.Roles => Roles.Version40;

        IVcard40PropertyList<Uri> IVcard40Card.Logos => Logos.Version40;

        IVcard40PropertyList<VcardOrganization> IVcard40Card.Organizations => Organizations.Version40;

        IVcard40PropertyList<Uri> IVcard40Card.Members => Members.Version40;

        IVcard40PropertyList<VcardTypedValue> IVcard40Card.Related => Related.Version40;

        IVcard40PropertyList<IReadOnlyList<string>> IVcard40Card.Categories => Categories.Version40;

        IVcard40PropertyList<string> IVcard40Card.Notes => Notes.Version40;

        IVcard40PropertyList<Uri> IVcard40Card.Sounds => Sounds.Version40;

        IVcard40PropertyList<VcardClientPidMap> IVcard40Card.ClientPidMaps => ClientPidMaps.Version40;

        IVcard40PropertyList<Uri> IVcard40Card.Urls => Urls.Version40;

        IVcard40PropertyList<VcardTypedValue> IVcard40Card.Keys => Keys.Version40;

        IVcard40PropertyList<Uri> IVcard40Card.FreeBusyUrls => FreeBusyUrls.Version40;

        IVcard40PropertyList<Uri> IVcard40Card.CalendarAddressUris => CalendarAddressUris.Version40;

        IVcard40PropertyList<Uri> IVcard40Card.CalendarUris => CalendarUris.Version40;

        IVcard40PropertyList<string> IVcard40Card.Expertise => Expertise.Version40;

        IVcard40PropertyList<string> IVcard40Card.Hobbies => Hobbies.Version40;

        IVcard40PropertyList<string> IVcard40Card.Interests => Interests.Version40;

        IVcard40PropertyList<Uri> IVcard40Card.OrganizationDirectories => OrganizationDirectories.Version40;

        IVcard40PropertyList<Uri> IVcard40Card.ContactUris => ContactUris.Version40;

        IVcard40PropertyList<string> IVcard40Card.GrammaticalGenders => GrammaticalGenders.Version40;

        IVcard40PropertyList<string> IVcard40Card.Pronouns => Pronouns.Version40;

        IVcard40PropertyList<VcardTypedValue> IVcard40Card.SocialProfiles => SocialProfiles.Version40;

        IReadOnlyList<IVcard40Property<string>> IVcard40Card.ExtensionProperties => ExtensionProperties;

        /// <summary>
        /// Gets the card in the shape of vCard 4.0.  It is this object, so a change through either is a change to both.
        /// </summary>
        /// <remarks>
        /// Any card can be viewed this way, whatever version it was read from, since the canonical form of the model is the form of vCard 4.0.
        /// </remarks>
        public IVcard40Card AsVersion40()
        {
            return this;
        }

        /// <summary>
        /// Gets a card from a set of cards by its <c>UID</c>.
        /// </summary>
        /// <remarks>
        /// Two UIDs match if they are equivalent as RFC 6350 §7.1.1 says: by RFC 3986 §6 for URIs, and with the case of a <c>urn:uuid:</c> ignored, as RFC 4122 §3 says.  Free-form
        /// text has to be identical.
        /// </remarks>
        /// <param name="cards">The cards.</param>
        /// <param name="uid">The UID, as it appears in the card with any escaping removed.</param>
        /// <returns>The first card with the UID; or <c>null</c> if there is none.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/> or <paramref name="uid"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="cards"/> contains <c>null</c>.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static VcardCard? FindByUid(IEnumerable<VcardCard> cards, string uid)
#else
        public static VcardCard FindByUid(IEnumerable<VcardCard> cards, string uid)
#endif
        {
            if (cards is null)
                throw new ArgumentNullException(nameof(cards));
            if (uid is null)
                throw new ArgumentNullException(nameof(uid));

            foreach (var card in cards)
            {
                if (card is null)
                    throw new ArgumentException("The cards cannot contain null.", nameof(cards));

                var cardUid = card.Uid;
                if (cardUid != null && AreEquivalentUids(cardUid.Value, uid)) return card;
            }

            return null;
        }

        /// <summary>
        /// Gets the alternatives of a property: its instances grouped by <c>ALTID</c>, in the order each group first appears.
        /// </summary>
        /// <remarks>
        /// Instances are alternatives if and only if they have the same name and the same <c>ALTID</c>, which is an opaque string (RFC 6350 §5.4).  An instance without it is a group
        /// of its own.
        /// </remarks>
        /// <param name="name">The name of the property, in any case.</param>
        /// <returns>The groups.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        public IReadOnlyList<IReadOnlyList<VcardContentLine>> GetAlternatives(string name)
        {
            var groups = VcardPropertyParameterReader.GroupByAlternatives(GetProperties(name), line => line);
            var result = new List<IReadOnlyList<VcardContentLine>>(groups.Count);
            foreach (var group in groups)
            {
                result.Add(new ReadOnlyCollection<VcardContentLine>(group));
            }

            return new ReadOnlyCollection<IReadOnlyList<VcardContentLine>>(result);
        }

        /// <summary>
        /// Gets the instances that have the same <c>PROP-ID</c> as another of the same name, as groups in the order each first appears.
        /// </summary>
        /// <remarks>
        /// ref. RFC 9554 §4.7: an application "SHOULD assign a unique identifier to each sibling property of the same name", and "Resolving duplicate identifier conflicts is specific to the
        /// application".  So a duplicate isn't an error that <see cref="Validate"/> reports, and this is how to find one.  The same value on a property of another name isn't a duplicate.
        /// Values are compared without regard to case, since a <c>PROP-ID</c> isn't quoted (RFC 6350 §5).
        /// </remarks>
        /// <returns>The groups, each of two or more instances.</returns>
        public IReadOnlyList<IReadOnlyList<VcardContentLine>> GetDuplicatePropertyIds()
        {
            var groups = new List<List<VcardContentLine>>();
            var groupsByKey = new Dictionary<string, List<VcardContentLine>>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in Properties)
            {
                var propertyId = VcardPropertyParameterReader.GetValue(line, VcardParameterNames.PropertyId);
                if (propertyId is null) continue;

                var key = line.Name + "\0" + propertyId;
                if (!groupsByKey.ContainsKey(key))
                {
                    var group = new List<VcardContentLine>();
                    groupsByKey.Add(key, group);
                    groups.Add(group);
                }

                groupsByKey[key].Add(line);
            }

            var result = new List<IReadOnlyList<VcardContentLine>>();
            foreach (var group in groups)
            {
                if (group.Count > 1)
                {
                    result.Add(new ReadOnlyCollection<VcardContentLine>(group));
                }
            }

            return new ReadOnlyCollection<IReadOnlyList<VcardContentLine>>(result);
        }

        /// <summary>
        /// Gets the preferred instance of a property: the first of those with the lowest <c>PREF</c>.
        /// </summary>
        /// <remarks>
        /// An instance without a valid <c>PREF</c> is least preferred (RFC 6350 §5.3), so one of them is returned only if none has it.
        /// </remarks>
        /// <param name="name">The name of the property, in any case.</param>
        /// <returns>The instance; or <c>null</c> if there are none.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public VcardContentLine? GetPreferred(string name)
#else
        public VcardContentLine GetPreferred(string name)
#endif
        {
            var instances = GetProperties(name);
            var index = VcardPropertyParameterReader.IndexOfPreferred(instances, line => line);
            return index < 0 ? null : instances[index];
        }

        /// <summary>
        /// Gets the instances of a property, in the order they appear.
        /// </summary>
        /// <param name="name">The name of the property, in any case.</param>
        /// <returns>The instances, whether or not their values are valid.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        public IReadOnlyList<VcardContentLine> GetProperties(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            var result = new List<VcardContentLine>();
            foreach (var line in Properties)
            {
                if (string.Equals(line.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(line);
                }
            }

            return new ReadOnlyCollection<VcardContentLine>(result);
        }

        /// <summary>
        /// Removes every instance of a property.
        /// </summary>
        /// <param name="name">The name of the property, in any case.</param>
        /// <returns>The number of instances removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        public int RemoveProperties(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            var count = 0;
            for (var index = Properties.Count - 1; index >= 0; index--)
            {
                if (string.Equals(Properties[index].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    Properties.RemoveAt(index);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Gets the cards of a set that the <c>MEMBER</c> properties of this card link to, by their <c>UID</c>, in the order of the properties.
        /// </summary>
        /// <remarks>
        /// A <c>MEMBER</c> may refer to something other than a card, such as a <c>mailto</c> URI (RFC 6350 §6.6.5), and it may be a card that isn't in the set, so one that doesn't
        /// resolve is left out.  Nothing outside the set is looked up.
        /// </remarks>
        /// <param name="cards">The cards to look in.</param>
        /// <returns>The cards found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="cards"/> contains <c>null</c>.</exception>
        public IReadOnlyList<VcardCard> ResolveMembers(IEnumerable<VcardCard> cards)
        {
            if (cards is null)
                throw new ArgumentNullException(nameof(cards));

            // Each UID is parsed once here, rather than once for each member.
            var candidates = new List<KeyValuePair<VcardCard, VcardTypedValue>>();
            foreach (var card in cards)
            {
                if (card is null)
                    throw new ArgumentException("The cards cannot contain null.", nameof(cards));

                var cardUid = card.Uid;
                if (cardUid != null)
                {
                    candidates.Add(new KeyValuePair<VcardCard, VcardTypedValue>(card, cardUid.Value));
                }
            }

            var result = new List<VcardCard>();
            foreach (var member in Members)
            {
                foreach (var candidate in candidates)
                {
                    if (AreEquivalentUids(candidate.Value, member.Value.OriginalString))
                    {
                        result.Add(candidate.Key);
                        break;
                    }
                }
            }

            return new ReadOnlyCollection<VcardCard>(result);
        }

        /// <summary>
        /// Sets the <c>KIND</c> property, replacing any there is.
        /// </summary>
        /// <param name="kind">The kind.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is <see cref="VcardKind.Other"/>, which has no text, or not a <see cref="VcardKind"/> member.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetKind(VcardKind kind)
        {
            switch (kind)
            {
                case VcardKind.Individual:
                    SetKind(IndividualText);
                    break;
                case VcardKind.Group:
                    SetKind(GroupText);
                    break;
                case VcardKind.Organization:
                    SetKind(OrganizationText);
                    break;
                case VcardKind.Location:
                    SetKind(LocationText);
                    break;
                case VcardKind.Application:
                    SetKind(ApplicationText);
                    break;
                case VcardKind.Device:
                    SetKind(DeviceText);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "The kind has no text.");
            }
        }

        /// <summary>
        /// Sets the <c>KIND</c> property, replacing any there is.
        /// </summary>
        /// <param name="kind">The value, which RFC 6350 §6.1.4 allows to be an <c>iana-token</c> or <c>x-name</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="kind"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetKind(string kind)
        {
            SetSingle(KindCodec, kind, null);
        }

        /// <summary>
        /// Sets the <c>N</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetName(VcardName value)
        {
            SetSingle(NameCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>N</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetName(VcardName value, VcardPropertyParameters parameters)
        {
            SetSingle(NameCodec, value, CheckParameters(parameters));
        }

        /// <summary>
        /// Sets the <c>BDAY</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, a date-and-or-time or text.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetBirthday(VcardTypedValue value)
        {
            SetSingle(BirthdayCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>BDAY</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, a date-and-or-time or text.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetBirthday(VcardTypedValue value, VcardPropertyParameters parameters)
        {
            SetSingle(BirthdayCodec, value, CheckParameters(parameters));
        }

        /// <summary>
        /// Sets the <c>ANNIVERSARY</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, a date-and-or-time or text.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetAnniversary(VcardTypedValue value)
        {
            SetSingle(AnniversaryCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>ANNIVERSARY</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, a date-and-or-time or text.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetAnniversary(VcardTypedValue value, VcardPropertyParameters parameters)
        {
            SetSingle(AnniversaryCodec, value, CheckParameters(parameters));
        }

        /// <summary>
        /// Sets the <c>GENDER</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetGender(VcardGender value)
        {
            SetSingle(GenderCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>GENDER</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetGender(VcardGender value, VcardPropertyParameters parameters)
        {
            SetSingle(GenderCodec, value, CheckParameters(parameters));
        }

        /// <summary>
        /// Sets the <c>PRODID</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetProductId(string value)
        {
            SetSingle(ProductIdCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>REV</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetRevision(VcardTimestamp value)
        {
            SetSingle(RevisionCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>UID</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, a URI or text.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetUid(VcardTypedValue value)
        {
            SetSingle(UidCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>BIRTHPLACE</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, text or a URI.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetBirthPlace(VcardTypedValue value)
        {
            SetSingle(BirthPlaceCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>BIRTHPLACE</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, text or a URI.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetBirthPlace(VcardTypedValue value, VcardPropertyParameters parameters)
        {
            SetSingle(BirthPlaceCodec, value, CheckParameters(parameters));
        }

        /// <summary>
        /// Sets the <c>DEATHPLACE</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, text or a URI.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetDeathPlace(VcardTypedValue value)
        {
            SetSingle(DeathPlaceCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>DEATHPLACE</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, text or a URI.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetDeathPlace(VcardTypedValue value, VcardPropertyParameters parameters)
        {
            SetSingle(DeathPlaceCodec, value, CheckParameters(parameters));
        }

        /// <summary>
        /// Sets the <c>DEATHDATE</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, a date-and-or-time or text.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetDeathDate(VcardTypedValue value)
        {
            SetSingle(DeathDateCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>DEATHDATE</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, a date-and-or-time or text.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetDeathDate(VcardTypedValue value, VcardPropertyParameters parameters)
        {
            SetSingle(DeathDateCodec, value, CheckParameters(parameters));
        }

        /// <summary>
        /// Sets the <c>CREATED</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetCreated(VcardTimestamp value)
        {
            SetSingle(CreatedCodec, value, null);
        }

        /// <summary>
        /// Sets the <c>LANGUAGE</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        public void SetDefaultLanguage(Bcp47LanguageTag value)
        {
            SetSingle(DefaultLanguageCodec, value, null);
        }

        /// <summary>
        /// Adds a property that RFC 6350 and its extensions don't define.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value, as it appears in the content line.</param>
        /// <returns>The new property.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">vCard 4.0 defines the property, so it has a member of its own.</exception>
        public VcardProperty<string> AddExtensionProperty(string name, string value)
        {
            return AddExtensionPropertyCore(name, value, new VcardPropertyParameters());
        }

        /// <summary>
        /// Adds a property that RFC 6350 and its extensions don't define.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value, as it appears in the content line.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <returns>The new property.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="value"/> or <paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">vCard 4.0 defines the property, so it has a member of its own.</exception>
        public VcardProperty<string> AddExtensionProperty(string name, string value, VcardPropertyParameters parameters)
        {
            return AddExtensionPropertyCore(name, value, CheckParameters(parameters));
        }

        /// <summary>
        /// Checks the card against RFC 6350 and its extensions, without throwing.
        /// </summary>
        /// <remarks>
        /// It checks the rules a writer checks, whatever the options, so a reader or writer may accept a card that has problems here.  The structure of the lines a card was read from
        /// isn't kept, so it doesn't report that.  A problem in the card never throws, but an exception from a <see cref="VcardOptions.CharsetResolver"/> of the caller's isn't caught.
        /// </remarks>
        /// <returns>Every problem found, in the order of the lines they were found on.</returns>
        public IReadOnlyList<VcardDiagnostic> Validate()
        {
            var diagnostics = new List<VcardDiagnostic>();
            VcardConformance.Validate(this, diagnostics);
            return new ReadOnlyCollection<VcardDiagnostic>(diagnostics);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "VCARD (" + Properties.Count + " properties)";
        }

        void IVcard40Card.SetKind(VcardKind kind)
        {
            SetKind(kind);
        }

        void IVcard40Card.SetKind(string kind)
        {
            SetKind(kind);
        }

        void IVcard40Card.SetName(VcardName value)
        {
            SetName(value);
        }

        void IVcard40Card.SetName(VcardName value, VcardPropertyParameters parameters)
        {
            SetName(value, parameters);
        }

        void IVcard40Card.SetBirthday(VcardTypedValue value)
        {
            SetBirthday(value);
        }

        void IVcard40Card.SetBirthday(VcardTypedValue value, VcardPropertyParameters parameters)
        {
            SetBirthday(value, parameters);
        }

        void IVcard40Card.SetAnniversary(VcardTypedValue value)
        {
            SetAnniversary(value);
        }

        void IVcard40Card.SetAnniversary(VcardTypedValue value, VcardPropertyParameters parameters)
        {
            SetAnniversary(value, parameters);
        }

        void IVcard40Card.SetGender(VcardGender value)
        {
            SetGender(value);
        }

        void IVcard40Card.SetGender(VcardGender value, VcardPropertyParameters parameters)
        {
            SetGender(value, parameters);
        }

        void IVcard40Card.SetProductId(string value)
        {
            SetProductId(value);
        }

        void IVcard40Card.SetRevision(VcardTimestamp value)
        {
            SetRevision(value);
        }

        void IVcard40Card.SetUid(VcardTypedValue value)
        {
            SetUid(value);
        }

        void IVcard40Card.SetBirthPlace(VcardTypedValue value)
        {
            SetBirthPlace(value);
        }

        void IVcard40Card.SetBirthPlace(VcardTypedValue value, VcardPropertyParameters parameters)
        {
            SetBirthPlace(value, parameters);
        }

        void IVcard40Card.SetDeathPlace(VcardTypedValue value)
        {
            SetDeathPlace(value);
        }

        void IVcard40Card.SetDeathPlace(VcardTypedValue value, VcardPropertyParameters parameters)
        {
            SetDeathPlace(value, parameters);
        }

        void IVcard40Card.SetDeathDate(VcardTypedValue value)
        {
            SetDeathDate(value);
        }

        void IVcard40Card.SetDeathDate(VcardTypedValue value, VcardPropertyParameters parameters)
        {
            SetDeathDate(value, parameters);
        }

        void IVcard40Card.SetCreated(VcardTimestamp value)
        {
            SetCreated(value);
        }

        void IVcard40Card.SetDefaultLanguage(Bcp47LanguageTag value)
        {
            SetDefaultLanguage(value);
        }

        IVcard40Property<string> IVcard40Card.AddExtensionProperty(string name, string value)
        {
            return AddExtensionProperty(name, value);
        }

        IVcard40Property<string> IVcard40Card.AddExtensionProperty(string name, string value, VcardPropertyParameters parameters)
        {
            return AddExtensionProperty(name, value, parameters);
        }

        int IVcard40Card.RemoveProperties(string name)
        {
            return RemoveProperties(name);
        }

        IReadOnlyList<VcardDiagnostic> IVcard40Card.Validate()
        {
            return Validate();
        }

        private static bool IsDefinedInVersion40(string name)
        {
            var info = VcardPropertyInfo.Find(name);
            return info != null && info.IsDefinedIn(VcardVersion.V4_0);
        }

        private static VcardPropertyParameters CheckParameters(VcardPropertyParameters parameters)
        {
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));

            return parameters;
        }

        private static bool AreEquivalentUids(VcardTypedValue cardUid, string right)
        {
            // Free-form text has no equivalence of its own, whatever it looks like.
            if (cardUid.ValueType != VcardValueType.Uri) return string.Equals(cardUid.Text, right, StringComparison.Ordinal);

            var left = cardUid.Uri.OriginalString;
            if (string.Equals(left, right, StringComparison.Ordinal)) return true;

            if (left.StartsWith(UuidUrnPrefix, StringComparison.OrdinalIgnoreCase) && right.StartsWith(UuidUrnPrefix, StringComparison.OrdinalIgnoreCase))
                return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Uri? leftUri, rightUri;
#else
            Uri leftUri, rightUri;
#endif
            return VcardValue.TryParseUriText(left.AsSpan(), out leftUri) && VcardValue.TryParseUriText(right.AsSpan(), out rightUri) &&
                   Uri.Compare(leftUri, rightUri, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, StringComparison.Ordinal) == 0;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardProperty<TValue>? GetSingle<TValue>(VcardPropertyCodec<TValue> codec)
#else
        private VcardProperty<TValue> GetSingle<TValue>(VcardPropertyCodec<TValue> codec)
#endif
        {
            // ref. RFC 9554 §4.6: a property with PHONETIC is how to pronounce another of the same name, so it is not the value unless there's nothing else.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            VcardProperty<TValue>? phonetic = null;
#else
            VcardProperty<TValue> phonetic = null;
#endif
            foreach (var line in Properties)
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                VcardProperty<TValue>? property;
#else
                VcardProperty<TValue> property;
#endif
                if (string.Equals(line.Name, codec.Name, StringComparison.OrdinalIgnoreCase) && codec.TryCreateProperty(line, out property))
                {
                    if (VcardPropertyParameterReader.GetValue(line, VcardParameterNames.Phonetic) is null) return property;

                    if (phonetic is null)
                    {
                        phonetic = property;
                    }
                }
            }

            return phonetic;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private void SetSingle<TValue>(VcardPropertyCodec<TValue> codec, TValue value, VcardPropertyParameters? parameters)
#else
        private void SetSingle<TValue>(VcardPropertyCodec<TValue> codec, TValue value, VcardPropertyParameters parameters)
#endif
        {
            // Create the line first, so that a value that can't be written leaves the card as it was.
            var newLine = codec.CreateLine(value, parameters);
            var position = -1;
            for (var index = 0; index < Properties.Count; index++)
            {
                if (string.Equals(Properties[index].Name, codec.Name, StringComparison.OrdinalIgnoreCase))
                {
                    // ref. RFC 9554 §4.4: 'When present with a value of "true", clients MUST NOT update the property.'
                    if (VcardPropertyParameterReader.IsDerived(Properties[index]))
                        throw new InvalidOperationException("The property " + codec.Name + " has DERIVED=true, which RFC 9554 §4.4 says a client must not update.  Remove it first to work it out again.");

                    if (position < 0)
                    {
                        position = index;
                    }
                }
            }

            RemoveProperties(codec.Name);
            Properties.Insert(position < 0 ? Properties.Count : position, newLine);
        }

        private VcardProperty<string> AddExtensionPropertyCore(string name, string value, VcardPropertyParameters parameters)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (IsDefinedInVersion40(name))
                throw new ArgumentException("vCard 4.0 defines the property " + name + ", so it has a member of its own.", nameof(name));

            var line = new VcardContentLine(parameters.Group, name, parameters.ToContentLineParameters(string.Empty), value);
            Properties.Add(line);
            return new VcardProperty<string>(line, value);
        }

        private sealed class NonNullCollection : Collection<VcardContentLine>
        {
            protected override void InsertItem(int index, VcardContentLine item)
            {
                if (item is null)
                    throw new ArgumentNullException(nameof(item));

                base.InsertItem(index, item);
            }

            protected override void SetItem(int index, VcardContentLine item)
            {
                if (item is null)
                    throw new ArgumentNullException(nameof(item));

                base.SetItem(index, item);
            }
        }
    }
}
