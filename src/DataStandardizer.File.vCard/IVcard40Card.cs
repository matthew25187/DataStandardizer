using System;
using System.Collections.Generic;
using DataStandardizer.LanguageTag;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A card in the shape of vCard 4.0 (RFC 6350).  Get one from <see cref="VcardCard.AsVersion40"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the versioned half of the model.  <see cref="VcardCard"/> has common members, which cover every version and hold each concept once, in its canonical form.  It also
    /// implements this interface explicitly, so a versioned view is the same object seen a different way, not a copy: a change through one is a change through the other.
    /// </para>
    /// <para>
    /// The interface exposes only what vCard 4.0 defines, so code written against it is checked by the compiler against that version.  It doesn't inherit from the interface of any
    /// other version, and it doesn't expose the raw <see cref="VcardCard.Properties"/> list.  Properties and parameters that RFC 6350 doesn't define are reached through
    /// <see cref="ExtensionProperties"/> and <see cref="IVcard40Property{TValue}.ExtensionParameters"/>.
    /// </para>
    /// <para>
    /// The canonical form is the form of vCard 4.0, so here each member passes the common value through as it is.  An interface for an older version adapts it instead, through the
    /// mapping that the reader and writer of that version use, and hides data that its version can't show.  Reading is never lossy, so any card can be viewed as any version.
    /// </para>
    /// </remarks>
    public interface IVcard40Card
    {
        /// <summary>
        /// Gets the kind of object the card represents (RFC 6350 §6.1.4).  With no <c>KIND</c> it is <see cref="VcardKind.Individual"/>.
        /// </summary>
        VcardKind Kind { get; }

        /// <summary>
        /// Gets the <c>SOURCE</c> properties (RFC 6350 §6.1.3).
        /// </summary>
        IVcard40PropertyList<Uri> Sources { get; }

        /// <summary>
        /// Gets the <c>XML</c> properties (RFC 6350 §6.1.5), each a single XML element.
        /// </summary>
        IVcard40PropertyList<string> Xml { get; }

        /// <summary>
        /// Gets the <c>FN</c> properties (RFC 6350 §6.2.1).
        /// </summary>
        IVcard40PropertyList<string> FormattedNames { get; }

        /// <summary>
        /// Gets the <c>NICKNAME</c> properties (RFC 6350 §6.2.3), each a list of nicknames.
        /// </summary>
        IVcard40PropertyList<IReadOnlyList<string>> Nicknames { get; }

        /// <summary>
        /// Gets the <c>PHOTO</c> properties (RFC 6350 §6.2.4).
        /// </summary>
        IVcard40PropertyList<Uri> Photos { get; }

        /// <summary>
        /// Gets the <c>ADR</c> properties (RFC 6350 §6.3.1).
        /// </summary>
        IVcard40PropertyList<VcardAddress> Addresses { get; }

        /// <summary>
        /// Gets the <c>TEL</c> properties (RFC 6350 §6.4.1), each text or a URI.
        /// </summary>
        IVcard40PropertyList<VcardTypedValue> Telephones { get; }

        /// <summary>
        /// Gets the <c>EMAIL</c> properties (RFC 6350 §6.4.2).
        /// </summary>
        IVcard40PropertyList<string> Emails { get; }

        /// <summary>
        /// Gets the <c>IMPP</c> properties (RFC 6350 §6.4.3).
        /// </summary>
        IVcard40PropertyList<Uri> InstantMessaging { get; }

        /// <summary>
        /// Gets the <c>LANG</c> properties (RFC 6350 §6.4.4).
        /// </summary>
        IVcard40PropertyList<Bcp47LanguageTag> Languages { get; }

        /// <summary>
        /// Gets the <c>TZ</c> properties (RFC 6350 §6.5.1), each text, a URI or a UTC offset.
        /// </summary>
        IVcard40PropertyList<VcardTypedValue> TimeZones { get; }

        /// <summary>
        /// Gets the <c>GEO</c> properties (RFC 6350 §6.5.2).  <see cref="VcardGeo"/> reads a URI of the <c>geo</c> scheme.
        /// </summary>
        IVcard40PropertyList<Uri> GeoLocations { get; }

        /// <summary>
        /// Gets the <c>TITLE</c> properties (RFC 6350 §6.6.1).
        /// </summary>
        IVcard40PropertyList<string> Titles { get; }

        /// <summary>
        /// Gets the <c>ROLE</c> properties (RFC 6350 §6.6.2).
        /// </summary>
        IVcard40PropertyList<string> Roles { get; }

        /// <summary>
        /// Gets the <c>LOGO</c> properties (RFC 6350 §6.6.3).
        /// </summary>
        IVcard40PropertyList<Uri> Logos { get; }

        /// <summary>
        /// Gets the <c>ORG</c> properties (RFC 6350 §6.6.4).
        /// </summary>
        IVcard40PropertyList<VcardOrganization> Organizations { get; }

        /// <summary>
        /// Gets the <c>MEMBER</c> properties (RFC 6350 §6.6.5).
        /// </summary>
        IVcard40PropertyList<Uri> Members { get; }

        /// <summary>
        /// Gets the <c>RELATED</c> properties (RFC 6350 §6.6.6), each a URI or text.
        /// </summary>
        IVcard40PropertyList<VcardTypedValue> Related { get; }

        /// <summary>
        /// Gets the <c>CATEGORIES</c> properties (RFC 6350 §6.7.1), each a list of categories.
        /// </summary>
        IVcard40PropertyList<IReadOnlyList<string>> Categories { get; }

        /// <summary>
        /// Gets the <c>NOTE</c> properties (RFC 6350 §6.7.2).
        /// </summary>
        IVcard40PropertyList<string> Notes { get; }

        /// <summary>
        /// Gets the <c>SOUND</c> properties (RFC 6350 §6.7.5).
        /// </summary>
        IVcard40PropertyList<Uri> Sounds { get; }

        /// <summary>
        /// Gets the <c>CLIENTPIDMAP</c> properties (RFC 6350 §6.7.7).
        /// </summary>
        IVcard40PropertyList<VcardClientPidMap> ClientPidMaps { get; }

        /// <summary>
        /// Gets the <c>URL</c> properties (RFC 6350 §6.7.8).
        /// </summary>
        IVcard40PropertyList<Uri> Urls { get; }

        /// <summary>
        /// Gets the <c>KEY</c> properties (RFC 6350 §6.8.1), each a URI or text.
        /// </summary>
        IVcard40PropertyList<VcardTypedValue> Keys { get; }

        /// <summary>
        /// Gets the <c>FBURL</c> properties (RFC 6350 §6.9.1).
        /// </summary>
        IVcard40PropertyList<Uri> FreeBusyUrls { get; }

        /// <summary>
        /// Gets the <c>CALADRURI</c> properties (RFC 6350 §6.9.2).
        /// </summary>
        IVcard40PropertyList<Uri> CalendarAddressUris { get; }

        /// <summary>
        /// Gets the <c>CALURI</c> properties (RFC 6350 §6.9.3).
        /// </summary>
        IVcard40PropertyList<Uri> CalendarUris { get; }

        /// <summary>
        /// Gets the properties RFC 6350 doesn't define, including <c>X-</c> properties, in the order they appear.  Each value is as it appears in the content line, since what its
        /// escaping means is not known.
        /// </summary>
        IReadOnlyList<IVcard40Property<string>> ExtensionProperties { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the value of the <c>KIND</c> property (RFC 6350 §6.1.4) as written; or <c>null</c> if the card has none.
        /// </summary>
        string? KindText { get; }

        /// <summary>
        /// Gets the <c>N</c> property (RFC 6350 §6.2.2); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<VcardName>? Name { get; }

        /// <summary>
        /// Gets the <c>BDAY</c> property (RFC 6350 §6.2.5), a date-and-or-time or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<VcardTypedValue>? Birthday { get; }

        /// <summary>
        /// Gets the <c>ANNIVERSARY</c> property (RFC 6350 §6.2.6), a date-and-or-time or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<VcardTypedValue>? Anniversary { get; }

        /// <summary>
        /// Gets the <c>GENDER</c> property (RFC 6350 §6.2.7); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<VcardGender>? Gender { get; }

        /// <summary>
        /// Gets the <c>PRODID</c> property (RFC 6350 §6.7.3); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<string>? ProductId { get; }

        /// <summary>
        /// Gets the <c>REV</c> property (RFC 6350 §6.7.4); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<VcardTimestamp>? Revision { get; }

        /// <summary>
        /// Gets the <c>UID</c> property (RFC 6350 §6.7.6), a URI or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<VcardTypedValue>? Uid { get; }
#else
        /// <summary>
        /// Gets the value of the <c>KIND</c> property (RFC 6350 §6.1.4) as written; or <c>null</c> if the card has none.
        /// </summary>
        string KindText { get; }

        /// <summary>
        /// Gets the <c>N</c> property (RFC 6350 §6.2.2); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<VcardName> Name { get; }

        /// <summary>
        /// Gets the <c>BDAY</c> property (RFC 6350 §6.2.5), a date-and-or-time or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<VcardTypedValue> Birthday { get; }

        /// <summary>
        /// Gets the <c>ANNIVERSARY</c> property (RFC 6350 §6.2.6), a date-and-or-time or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<VcardTypedValue> Anniversary { get; }

        /// <summary>
        /// Gets the <c>GENDER</c> property (RFC 6350 §6.2.7); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<VcardGender> Gender { get; }

        /// <summary>
        /// Gets the <c>PRODID</c> property (RFC 6350 §6.7.3); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<string> ProductId { get; }

        /// <summary>
        /// Gets the <c>REV</c> property (RFC 6350 §6.7.4); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<VcardTimestamp> Revision { get; }

        /// <summary>
        /// Gets the <c>UID</c> property (RFC 6350 §6.7.6), a URI or text; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard40Property<VcardTypedValue> Uid { get; }
#endif

        /// <summary>
        /// Sets the <c>KIND</c> property, replacing any there is.
        /// </summary>
        /// <param name="kind">The kind.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is <see cref="VcardKind.Other"/>, which has no text, or not a <see cref="VcardKind"/> member.</exception>
        void SetKind(VcardKind kind);

        /// <summary>
        /// Sets the <c>KIND</c> property, replacing any there is.
        /// </summary>
        /// <param name="kind">The value, which RFC 6350 §6.1.4 allows to be an <c>iana-token</c> or <c>x-name</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="kind"/> is <c>null</c>.</exception>
        void SetKind(string kind);

        /// <summary>
        /// Sets the <c>N</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        void SetName(VcardName value);

        /// <summary>
        /// Sets the <c>N</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        void SetName(VcardName value, VcardPropertyParameters parameters);

        /// <summary>
        /// Sets the <c>BDAY</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, a date-and-or-time or text.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        void SetBirthday(VcardTypedValue value);

        /// <summary>
        /// Sets the <c>BDAY</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, a date-and-or-time or text.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        void SetBirthday(VcardTypedValue value, VcardPropertyParameters parameters);

        /// <summary>
        /// Sets the <c>ANNIVERSARY</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, a date-and-or-time or text.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        void SetAnniversary(VcardTypedValue value);

        /// <summary>
        /// Sets the <c>ANNIVERSARY</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, a date-and-or-time or text.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        void SetAnniversary(VcardTypedValue value, VcardPropertyParameters parameters);

        /// <summary>
        /// Sets the <c>GENDER</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        void SetGender(VcardGender value);

        /// <summary>
        /// Sets the <c>GENDER</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        void SetGender(VcardGender value, VcardPropertyParameters parameters);

        /// <summary>
        /// Sets the <c>PRODID</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        void SetProductId(string value);

        /// <summary>
        /// Sets the <c>REV</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        void SetRevision(VcardTimestamp value);

        /// <summary>
        /// Sets the <c>UID</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, a URI or text.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is of a type the property doesn't allow, or can't be written.</exception>
        void SetUid(VcardTypedValue value);

        /// <summary>
        /// Adds a property that RFC 6350 doesn't define.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value, as it appears in the content line.</param>
        /// <returns>The new property.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">RFC 6350 defines the property, so it has a member of its own.</exception>
        IVcard40Property<string> AddExtensionProperty(string name, string value);

        /// <summary>
        /// Adds a property that RFC 6350 doesn't define.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value, as it appears in the content line.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <returns>The new property.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="value"/> or <paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">RFC 6350 defines the property, so it has a member of its own.</exception>
        IVcard40Property<string> AddExtensionProperty(string name, string value, VcardPropertyParameters parameters);

        /// <summary>
        /// Removes every instance of a property.
        /// </summary>
        /// <param name="name">The name of the property, in any case.</param>
        /// <returns>The number of instances removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        int RemoveProperties(string name);

        /// <summary>
        /// Checks the card against RFC 6350, without throwing.
        /// </summary>
        /// <returns>Every problem found.</returns>
        IReadOnlyList<VcardDiagnostic> Validate();
    }
}
