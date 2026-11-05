namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The names of the properties of vCard 4.0: those RFC 6350 §6 defines, and those of the extensions RFC 6474, RFC 6715, RFC 8605 and RFC 9554.
    /// </summary>
    /// <remarks>
    /// Property names are case-insensitive; these are in the upper case the RFC uses.  Use them with <see cref="VcardCard.GetProperties"/>, or to
    /// create a <see cref="VcardContentLine"/>.
    /// </remarks>
    public static class VcardPropertyNames
    {
        /// <summary>
        /// <c>ADR</c> (RFC 6350 §6.3.1): a delivery address.
        /// </summary>
        public const string Address = "ADR";

        /// <summary>
        /// <c>ANNIVERSARY</c> (RFC 6350 §6.2.6): the date of marriage, or equivalent.
        /// </summary>
        public const string Anniversary = "ANNIVERSARY";

        /// <summary>
        /// <c>BIRTHPLACE</c> (RFC 6474 §2.1): the place of birth.
        /// </summary>
        public const string BirthPlace = "BIRTHPLACE";

        /// <summary>
        /// <c>BEGIN</c> (RFC 6350 §6.1.1): the start of a card.  A reader or writer deals with it, so it is not in <see cref="VcardCard.Properties"/>.
        /// </summary>
        public const string Begin = "BEGIN";

        /// <summary>
        /// <c>BDAY</c> (RFC 6350 §6.2.5): the date of birth.
        /// </summary>
        public const string Birthday = "BDAY";

        /// <summary>
        /// <c>CALADRURI</c> (RFC 6350 §6.9.2): where to send a scheduling request.
        /// </summary>
        public const string CalendarAddressUri = "CALADRURI";

        /// <summary>
        /// <c>CALURI</c> (RFC 6350 §6.9.3): a calendar.
        /// </summary>
        public const string CalendarUri = "CALURI";

        /// <summary>
        /// <c>CATEGORIES</c> (RFC 6350 §6.7.1): application categories, also known as tags.
        /// </summary>
        public const string Categories = "CATEGORIES";

        /// <summary>
        /// <c>CLIENTPIDMAP</c> (RFC 6350 §6.7.7): the global meaning of a PID source identifier.
        /// </summary>
        public const string ClientPidMap = "CLIENTPIDMAP";

        /// <summary>
        /// <c>CONTACT-URI</c> (RFC 8605 §2.1): a way to contact the entity without identifying the individual.
        /// </summary>
        public const string ContactUri = "CONTACT-URI";

        /// <summary>
        /// <c>CREATED</c> (RFC 9554 §3.1): when the card was created.
        /// </summary>
        public const string Created = "CREATED";

        /// <summary>
        /// <c>DEATHDATE</c> (RFC 6474 §2.3): the date of death.
        /// </summary>
        public const string DeathDate = "DEATHDATE";

        /// <summary>
        /// <c>DEATHPLACE</c> (RFC 6474 §2.2): the place of death.
        /// </summary>
        public const string DeathPlace = "DEATHPLACE";

        /// <summary>
        /// <c>LANGUAGE</c> (RFC 9554 §3.3): the language that the text values of the card are assumed to be written in.  It is not <see cref="Language"/>, which is <c>LANG</c>.
        /// </summary>
        public const string DefaultLanguage = "LANGUAGE";

        /// <summary>
        /// <c>EMAIL</c> (RFC 6350 §6.4.2): an electronic mail address.
        /// </summary>
        public const string Email = "EMAIL";

        /// <summary>
        /// <c>END</c> (RFC 6350 §6.1.2): the end of a card.  A reader or writer deals with it, so it is not in <see cref="VcardCard.Properties"/>.
        /// </summary>
        public const string End = "END";

        /// <summary>
        /// <c>EXPERTISE</c> (RFC 6715 §2.1): a field of expertise.
        /// </summary>
        public const string Expertise = "EXPERTISE";

        /// <summary>
        /// <c>FN</c> (RFC 6350 §6.2.1): the formatted name.
        /// </summary>
        public const string FormattedName = "FN";

        /// <summary>
        /// <c>FBURL</c> (RFC 6350 §6.9.1): busy time.
        /// </summary>
        public const string FreeBusyUrl = "FBURL";

        /// <summary>
        /// <c>GENDER</c> (RFC 6350 §6.2.7): sex and gender identity.
        /// </summary>
        public const string Gender = "GENDER";

        /// <summary>
        /// <c>GEO</c> (RFC 6350 §6.5.2): a global position.
        /// </summary>
        public const string Geo = "GEO";

        /// <summary>
        /// <c>GRAMGENDER</c> (RFC 9554 §3.2): the grammatical gender to use in salutations and other grammatical constructs.
        /// </summary>
        public const string GrammaticalGender = "GRAMGENDER";

        /// <summary>
        /// <c>HOBBY</c> (RFC 6715 §2.2): an activity one actively engages in.
        /// </summary>
        public const string Hobby = "HOBBY";

        /// <summary>
        /// <c>IMPP</c> (RFC 6350 §6.4.3): instant messaging and presence.
        /// </summary>
        public const string InstantMessaging = "IMPP";

        /// <summary>
        /// <c>INTEREST</c> (RFC 6715 §2.3): an activity or topic one finds interesting, but doesn't necessarily engage in.
        /// </summary>
        public const string Interest = "INTEREST";

        /// <summary>
        /// <c>KEY</c> (RFC 6350 §6.8.1): a public key or authentication certificate.
        /// </summary>
        public const string Key = "KEY";

        /// <summary>
        /// <c>KIND</c> (RFC 6350 §6.1.4): the kind of object the card represents.
        /// </summary>
        public const string Kind = "KIND";

        /// <summary>
        /// <c>LANG</c> (RFC 6350 §6.4.4): a language that may be used for contact.
        /// </summary>
        public const string Language = "LANG";

        /// <summary>
        /// <c>LOGO</c> (RFC 6350 §6.6.3): a logo.
        /// </summary>
        public const string Logo = "LOGO";

        /// <summary>
        /// <c>MEMBER</c> (RFC 6350 §6.6.5): a member of the group the card represents.
        /// </summary>
        public const string Member = "MEMBER";

        /// <summary>
        /// <c>N</c> (RFC 6350 §6.2.2): the components of the name.
        /// </summary>
        public const string Name = "N";

        /// <summary>
        /// <c>NICKNAME</c> (RFC 6350 §6.2.3): nicknames.
        /// </summary>
        public const string Nickname = "NICKNAME";

        /// <summary>
        /// <c>NOTE</c> (RFC 6350 §6.7.2): a supplemental comment.
        /// </summary>
        public const string Note = "NOTE";

        /// <summary>
        /// <c>ORG</c> (RFC 6350 §6.6.4): the organization name and units.
        /// </summary>
        public const string Organization = "ORG";

        /// <summary>
        /// <c>ORG-DIRECTORY</c> (RFC 6715 §2.4): a directory of an organization the entity belongs to.  The RFC calls it <c>ORG-URI</c> in two places, which its errata 3340 and 3341 correct;
        /// see <see cref="VcardOptions.AcceptOrgUriProperty"/>.
        /// </summary>
        public const string OrganizationDirectory = "ORG-DIRECTORY";

        /// <summary>
        /// <c>PHOTO</c> (RFC 6350 §6.2.4): an image or photograph.
        /// </summary>
        public const string Photo = "PHOTO";

        /// <summary>
        /// <c>PRODID</c> (RFC 6350 §6.7.3): the product that created the card.
        /// </summary>
        public const string ProductId = "PRODID";

        /// <summary>
        /// <c>PRONOUNS</c> (RFC 9554 §3.4): the pronouns to use for the entity.
        /// </summary>
        public const string Pronouns = "PRONOUNS";

        /// <summary>
        /// <c>RELATED</c> (RFC 6350 §6.6.6): a relationship with another entity.
        /// </summary>
        public const string Related = "RELATED";

        /// <summary>
        /// <c>REV</c> (RFC 6350 §6.7.4): the revision of the card.
        /// </summary>
        public const string Revision = "REV";

        /// <summary>
        /// <c>ROLE</c> (RFC 6350 §6.6.2): a function or part played.
        /// </summary>
        public const string Role = "ROLE";

        /// <summary>
        /// <c>SOCIALPROFILE</c> (RFC 9554 §3.5): a social media profile, as a URI or a username.
        /// </summary>
        public const string SocialProfile = "SOCIALPROFILE";

        /// <summary>
        /// <c>SOUND</c> (RFC 6350 §6.7.5): a digital sound, often the pronunciation of the name.
        /// </summary>
        public const string Sound = "SOUND";

        /// <summary>
        /// <c>SOURCE</c> (RFC 6350 §6.1.3): the source of the directory information.
        /// </summary>
        public const string Source = "SOURCE";

        /// <summary>
        /// <c>TEL</c> (RFC 6350 §6.4.1): a telephone number.
        /// </summary>
        public const string Telephone = "TEL";

        /// <summary>
        /// <c>TZ</c> (RFC 6350 §6.5.1): a time zone.
        /// </summary>
        public const string TimeZone = "TZ";

        /// <summary>
        /// <c>TITLE</c> (RFC 6350 §6.6.1): a position or job.
        /// </summary>
        public const string Title = "TITLE";

        /// <summary>
        /// <c>UID</c> (RFC 6350 §6.7.6): a globally unique identifier of the entity.
        /// </summary>
        public const string Uid = "UID";

        /// <summary>
        /// <c>URL</c> (RFC 6350 §6.7.8): a uniform resource locator.
        /// </summary>
        public const string Url = "URL";

        /// <summary>
        /// <c>VERSION</c> (RFC 6350 §6.7.9): the version of the specification.  A reader or writer deals with it, so it is not in <see cref="VcardCard.Properties"/>.
        /// </summary>
        public const string Version = "VERSION";

        /// <summary>
        /// <c>XML</c> (RFC 6350 §6.1.5): extended XML-encoded vCard data.
        /// </summary>
        public const string Xml = "XML";
    }
}
