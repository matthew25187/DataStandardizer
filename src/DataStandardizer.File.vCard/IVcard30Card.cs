using System;
using System.Collections.Generic;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A card in the shape of vCard 3.0 (RFC 2426, with RFC 2739 and RFC 4770).  Get one from <see cref="VcardCard.AsVersion30"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="VcardCard"/> implements this explicitly, so a view is the same object seen a different way, not a copy: a change through one is a change through the other.  The interface exposes only
    /// what vCard 3.0 defines, so code written against it is checked by the compiler against that version.  It doesn't inherit from the interface of any other version, and it doesn't expose the
    /// raw <see cref="VcardCard.Properties"/> list.
    /// </para>
    /// <para>
    /// Each member adapts the common value through the mapping that the reader and writer of vCard 3.0 use, and a setter adapts back into the canonical form.  A view hides what vCard 3.0 can't show, such as a
    /// <c>GEO</c> that isn't a latitude and a longitude.  Reading is never lossy, so any card can be viewed this way, whatever version it was read from.
    /// </para>
    /// </remarks>
    public interface IVcard30Card
    {
        /// <summary>
        /// Gets the <c>FN</c> properties (RFC 2426 §3.1.1).
        /// </summary>
        IVcard30PropertyList<string> FormattedNames { get; }

        /// <summary>
        /// Gets the <c>NICKNAME</c> properties (RFC 2426 §3.1.3), each a list of nicknames.
        /// </summary>
        IVcard30PropertyList<IReadOnlyList<string>> Nicknames { get; }

        /// <summary>
        /// Gets the <c>PHOTO</c> properties (RFC 2426 §3.1.4), each inline data or a URI.
        /// </summary>
        IVcard30PropertyList<VcardMediaValue> Photos { get; }

        /// <summary>
        /// Gets the <c>ADR</c> properties (RFC 2426 §3.2.1), without the components of RFC 9554.
        /// </summary>
        IVcard30PropertyList<VcardAddress> Addresses { get; }

        /// <summary>
        /// Gets the <c>LABEL</c> properties (RFC 2426 §3.2.2): the text of each delivery address.  The model holds a label on the <c>ADR</c> it goes with where it can tell which that is, and adding one puts it there.
        /// </summary>
        IVcard30PropertyList<string> Labels { get; }

        /// <summary>
        /// Gets the <c>TEL</c> properties (RFC 2426 §3.3.1), each a phone number.  A <c>tel</c> URI gives its number, and another URI is hidden.
        /// </summary>
        IVcard30PropertyList<string> Telephones { get; }

        /// <summary>
        /// Gets the <c>EMAIL</c> properties (RFC 2426 §3.3.2).
        /// </summary>
        IVcard30PropertyList<string> Emails { get; }

        /// <summary>
        /// Gets the <c>TZ</c> properties (RFC 2426 §3.4.1), each a UTC offset or text.
        /// </summary>
        IVcard30PropertyList<VcardTypedValue> TimeZones { get; }

        /// <summary>
        /// Gets the <c>GEO</c> properties (RFC 2426 §3.4.2), each a latitude and a longitude.
        /// </summary>
        IVcard30PropertyList<VcardGeo> GeoLocations { get; }

        /// <summary>
        /// Gets the <c>TITLE</c> properties (RFC 2426 §3.5.1).
        /// </summary>
        IVcard30PropertyList<string> Titles { get; }

        /// <summary>
        /// Gets the <c>ROLE</c> properties (RFC 2426 §3.5.2).
        /// </summary>
        IVcard30PropertyList<string> Roles { get; }

        /// <summary>
        /// Gets the <c>LOGO</c> properties (RFC 2426 §3.5.3), each inline data or a URI.
        /// </summary>
        IVcard30PropertyList<VcardMediaValue> Logos { get; }

        /// <summary>
        /// Gets the <c>ORG</c> properties (RFC 2426 §3.5.5).
        /// </summary>
        IVcard30PropertyList<VcardOrganization> Organizations { get; }

        /// <summary>
        /// Gets the <c>CATEGORIES</c> properties (RFC 2426 §3.6.1), each a list of categories.
        /// </summary>
        IVcard30PropertyList<IReadOnlyList<string>> Categories { get; }

        /// <summary>
        /// Gets the <c>NOTE</c> properties (RFC 2426 §3.6.2).
        /// </summary>
        IVcard30PropertyList<string> Notes { get; }

        /// <summary>
        /// Gets the <c>SOUND</c> properties (RFC 2426 §3.6.6), each inline data or a URI.
        /// </summary>
        IVcard30PropertyList<VcardMediaValue> Sounds { get; }

        /// <summary>
        /// Gets the <c>URL</c> properties (RFC 2426 §3.6.8).
        /// </summary>
        IVcard30PropertyList<Uri> Urls { get; }

        /// <summary>
        /// Gets the <c>KEY</c> properties (RFC 2426 §3.7.2), each inline data or text.
        /// </summary>
        IVcard30PropertyList<VcardMediaValue> Keys { get; }

        /// <summary>
        /// Gets the <c>SOURCE</c> properties (RFC 2425 §6.1, RFC 2426 §5).
        /// </summary>
        IVcard30PropertyList<Uri> Sources { get; }

        /// <summary>
        /// Gets the <c>IMPP</c> properties (RFC 4770).
        /// </summary>
        IVcard30PropertyList<Uri> InstantMessaging { get; }

        /// <summary>
        /// Gets the <c>FBURL</c> properties (RFC 2739 §2.3.1).
        /// </summary>
        IVcard30PropertyList<Uri> FreeBusyUrls { get; }

        /// <summary>
        /// Gets the <c>CALADRURI</c> properties (RFC 2739 §2.3.2).
        /// </summary>
        IVcard30PropertyList<Uri> CalendarAddressUris { get; }

        /// <summary>
        /// Gets the <c>CAPURI</c> properties (RFC 2739 §2.3.3).
        /// </summary>
        IVcard30PropertyList<Uri> CalendarAccessUris { get; }

        /// <summary>
        /// Gets the <c>CALURI</c> properties (RFC 2739 §2.3.4).
        /// </summary>
        IVcard30PropertyList<Uri> CalendarUris { get; }

        /// <summary>
        /// Gets the properties that no version of vCard defines, including <c>X-</c> properties, in the order they appear.  Each value is as it appears in the content line.
        /// </summary>
        IReadOnlyList<IVcard30Property<string>> ExtensionProperties { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the <c>N</c> property (RFC 2426 §3.1.2), without the components of RFC 9554; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<VcardName>? Name { get; }

        /// <summary>
        /// Gets the <c>BDAY</c> property (RFC 2426 §3.1.5), a date or a date-time; or <c>null</c> if the card has none that vCard 3.0 can show.
        /// </summary>
        IVcard30Property<VcardDateAndOrTime>? Birthday { get; }

        /// <summary>
        /// Gets the <c>MAILER</c> property (RFC 2426 §3.3.3); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<string>? Mailer { get; }

        /// <summary>
        /// Gets the card of the <c>AGENT</c> property (RFC 2426 §3.5.4), read from the text of the property, so a change to it isn't a change to this card; or <c>null</c> if the card has none.
        /// </summary>
        IVcard30Card? Agent { get; }

        /// <summary>
        /// Gets the URI of the <c>AGENT</c> property (RFC 2426 §3.5.4); or <c>null</c> if the card has none with a URI.
        /// </summary>
        Uri? AgentUri { get; }

        /// <summary>
        /// Gets the <c>PRODID</c> property (RFC 2426 §3.6.3); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<string>? ProductId { get; }

        /// <summary>
        /// Gets the <c>REV</c> property (RFC 2426 §3.6.4); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<VcardTimestamp>? Revision { get; }

        /// <summary>
        /// Gets the text of the <c>SORT-STRING</c> property (RFC 2426 §3.6.5), which the model holds on the <c>N</c> or <c>ORG</c> it sorts; or <c>null</c> if the card has none.
        /// </summary>
        string? SortString { get; }

        /// <summary>
        /// Gets the <c>UID</c> property (RFC 2426 §3.6.7), which is text in this version; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<string>? Uid { get; }

        /// <summary>
        /// Gets the <c>CLASS</c> property (RFC 2426 §3.7.1); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<string>? Classification { get; }

        /// <summary>
        /// Gets the <c>NAME</c> property (RFC 2425 §6.1, RFC 2426 §5); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<string>? SourceName { get; }

        /// <summary>
        /// Gets the <c>PROFILE</c> property (RFC 2425 §6.2, RFC 2426 §5); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<string>? Profile { get; }
#else
        /// <summary>
        /// Gets the <c>N</c> property (RFC 2426 §3.1.2), without the components of RFC 9554; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<VcardName> Name { get; }

        /// <summary>
        /// Gets the <c>BDAY</c> property (RFC 2426 §3.1.5), a date or a date-time; or <c>null</c> if the card has none that vCard 3.0 can show.
        /// </summary>
        IVcard30Property<VcardDateAndOrTime> Birthday { get; }

        /// <summary>
        /// Gets the <c>MAILER</c> property (RFC 2426 §3.3.3); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<string> Mailer { get; }

        /// <summary>
        /// Gets the card of the <c>AGENT</c> property (RFC 2426 §3.5.4), read from the text of the property, so a change to it isn't a change to this card; or <c>null</c> if the card has none.
        /// </summary>
        IVcard30Card Agent { get; }

        /// <summary>
        /// Gets the URI of the <c>AGENT</c> property (RFC 2426 §3.5.4); or <c>null</c> if the card has none with a URI.
        /// </summary>
        Uri AgentUri { get; }

        /// <summary>
        /// Gets the <c>PRODID</c> property (RFC 2426 §3.6.3); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<string> ProductId { get; }

        /// <summary>
        /// Gets the <c>REV</c> property (RFC 2426 §3.6.4); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<VcardTimestamp> Revision { get; }

        /// <summary>
        /// Gets the text of the <c>SORT-STRING</c> property (RFC 2426 §3.6.5), which the model holds on the <c>N</c> or <c>ORG</c> it sorts; or <c>null</c> if the card has none.
        /// </summary>
        string SortString { get; }

        /// <summary>
        /// Gets the <c>UID</c> property (RFC 2426 §3.6.7), which is text in this version; or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<string> Uid { get; }

        /// <summary>
        /// Gets the <c>CLASS</c> property (RFC 2426 §3.7.1); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<string> Classification { get; }

        /// <summary>
        /// Gets the <c>NAME</c> property (RFC 2425 §6.1, RFC 2426 §5); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<string> SourceName { get; }

        /// <summary>
        /// Gets the <c>PROFILE</c> property (RFC 2425 §6.2, RFC 2426 §5); or <c>null</c> if the card has none with a valid value.
        /// </summary>
        IVcard30Property<string> Profile { get; }
#endif

        /// <summary>
        /// Sets the <c>N</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        void SetName(VcardName value);

        /// <summary>
        /// Sets the <c>BDAY</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException">vCard 3.0 can't write <paramref name="value"/>, or it is not initialised.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        void SetBirthday(VcardDateAndOrTime value);

        /// <summary>
        /// Sets the <c>MAILER</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        void SetMailer(string value);

        /// <summary>
        /// Sets the <c>AGENT</c> property to a card, replacing any there are.
        /// </summary>
        /// <param name="agent">The card, which the property holds a copy of.  It must be a <see cref="VcardCard"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="agent"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="agent"/> is not a <see cref="VcardCard"/>.</exception>
        void SetAgent(IVcard30Card agent);

        /// <summary>
        /// Sets the <c>AGENT</c> property to a URI, replacing any there are.
        /// </summary>
        /// <param name="agent">The URI, which must be absolute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="agent"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="agent"/> can't be written.</exception>
        void SetAgent(Uri agent);

        /// <summary>
        /// Sets the <c>PRODID</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        void SetProductId(string value);

        /// <summary>
        /// Sets the <c>REV</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        void SetRevision(VcardTimestamp value);

        /// <summary>
        /// Sets the <c>SORT-STRING</c> property, replacing any there is.  The model holds it on the <c>N</c>, or else the <c>ORG</c>, and as a property of its own if the card has neither.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        void SetSortString(string value);

        /// <summary>
        /// Sets the <c>UID</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        /// <exception cref="InvalidOperationException">An instance of the property has <c>DERIVED=true</c> (RFC 9554 §4.4).</exception>
        void SetUid(string value);

        /// <summary>
        /// Sets the <c>CLASS</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value: <c>PUBLIC</c>, <c>PRIVATE</c>, <c>CONFIDENTIAL</c>, or an <c>iana-token</c> or <c>x-name</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        void SetClassification(string value);

        /// <summary>
        /// Sets the <c>NAME</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        void SetSourceName(string value);

        /// <summary>
        /// Sets the <c>PROFILE</c> property, replacing any there are.
        /// </summary>
        /// <param name="value">The value, which RFC 2426 §4 says must be <c>VCARD</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a TEXT value can't.</exception>
        void SetProfile(string value);

        /// <summary>
        /// Adds a property that no version of vCard defines.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value, as it appears in the content line.</param>
        /// <returns>The new property.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">A version of vCard defines the property.</exception>
        IVcard30Property<string> AddExtensionProperty(string name, string value);

        /// <summary>
        /// Removes every instance of a property, by its name in vCard 3.0.  So <c>LABEL</c> and <c>SORT-STRING</c> also come off the properties that hold them, and <c>AGENT</c> removes the agents.
        /// </summary>
        /// <param name="name">The name of the property, in any case.</param>
        /// <returns>The number of instances removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        int RemoveProperties(string name);

        /// <summary>
        /// Checks the card against the rules of vCard 3.0, without throwing, as <see cref="VcardCard.Validate(VcardVersion)"/> does.
        /// </summary>
        /// <returns>What it can't hold, and every problem found in the rest.</returns>
        IReadOnlyList<VcardDiagnostic> Validate();
    }

    /// <summary>
    /// A property of a vCard 3.0 card, in the shape RFC 2426 gives it.  See <see cref="IVcard30Card"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public interface IVcard30Property<out TValue>
    {
        /// <summary>
        /// Gets the name of the property in vCard 3.0.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        TValue Value { get; }

        /// <summary>
        /// Gets a flag indicating if the property is preferred: its <c>TYPE</c> has <c>pref</c> in vCard 3.0, which the model holds as the lowest <c>PREF</c> among the instances of the property.
        /// </summary>
        bool IsPreferred { get; }

        /// <summary>
        /// Gets the <c>TYPE</c> values, as written, other than <c>pref</c>.
        /// </summary>
        IReadOnlyList<string> Types { get; }

        /// <summary>
        /// Gets the parameters of the property that no version of vCard defines, in the order they appear.
        /// </summary>
        IReadOnlyList<VcardContentLineParameter> ExtensionParameters { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the group, as written and without its '.'; or <c>null</c> if the property has none.
        /// </summary>
        string? Group { get; }

        /// <summary>
        /// Gets the <c>LANGUAGE</c>, as written; or <c>null</c> if the property doesn't have one.  RFC 2426 §4 cites RFC 1766, so it isn't always a tag that <see cref="DataStandardizer.LanguageTag.Bcp47LanguageTag"/> holds.
        /// </summary>
        string? Language { get; }
#else
        /// <summary>
        /// Gets the group, as written and without its '.'; or <c>null</c> if the property has none.
        /// </summary>
        string Group { get; }

        /// <summary>
        /// Gets the <c>LANGUAGE</c>, as written; or <c>null</c> if the property doesn't have one.  RFC 2426 §4 cites RFC 1766, so it isn't always a tag that <see cref="DataStandardizer.LanguageTag.Bcp47LanguageTag"/> holds.
        /// </summary>
        string Language { get; }
#endif
    }

    /// <summary>
    /// The instances of one property of a vCard 3.0 card, as a live view of the card.  See <see cref="IVcard30Card"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public interface IVcard30PropertyList<TValue> : IReadOnlyList<IVcard30Property<TValue>>
    {
        /// <summary>
        /// Adds an instance after the last property of the card.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The new instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> can't be written.</exception>
        IVcard30Property<TValue> Add(TValue value);

        /// <summary>
        /// Adds an instance after the last property of the card.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The group and parameters.</param>
        /// <returns>The new instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> can't be written.</exception>
        IVcard30Property<TValue> Add(TValue value, Vcard30PropertyParameters parameters);

        /// <summary>
        /// Removes every instance, including any whose value isn't valid or that vCard 3.0 can't show.
        /// </summary>
        void Clear();

        /// <summary>
        /// Removes an instance.
        /// </summary>
        /// <param name="property">The instance.</param>
        /// <returns><c>true</c> if it was an instance of this property in the card; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="property"/> is <c>null</c>.</exception>
        bool Remove(IVcard30Property<TValue> property);
    }
}
