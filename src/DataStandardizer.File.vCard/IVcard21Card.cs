using System;
using System.Collections.Generic;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A card in the shape of vCard 2.1, the versit specification of 1996.  Get one with <see cref="VcardCard.AsVersion21"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It has only the properties and parameters vCard 2.1 defines, in its forms: <c>TYPE</c> values as the bare tokens of §2.9, such as <c>WORK</c>, a preference as <c>PREF</c>, an inline
    /// picture, sound or key as its octets with the token of its format, such as <c>GIF</c>, a time zone as an offset from UTC, and an <c>AGENT</c> as the card it holds.  It doesn't give the
    /// raw list of properties, so code written against it uses only what vCard 2.1 has.  Unknown properties are those whose names start with <c>X-</c> (§2.8.1), through
    /// <see cref="ExtensionProperties"/>.  It doesn't inherit from <see cref="IVcard30Card"/> or <see cref="IVcard40Card"/>, since the versions aren't additive.
    /// </para>
    /// <para>
    /// It adapts the canonical form of the model, so a change made through it is a change to the <see cref="VcardCard"/>, and the other way round.  What vCard 2.1 can't show, such as a
    /// <c>KIND</c> or a text <c>TZ</c>, is hidden, not removed; <see cref="VcardVersionConverter.Analyze"/> lists it.  vCard 2.1 encodes values with <c>ENCODING</c> and <c>CHARSET</c>,
    /// which the reader decodes and the writer chooses, so they aren't parameters here.
    /// </para>
    /// </remarks>
    public interface IVcard21Card
    {
        /// <summary>
        /// Gets the <c>FN</c> properties (vCard 2.1 §2.2.1).
        /// </summary>
        IVcard21PropertyList<string> FormattedNames { get; }

        /// <summary>
        /// Gets the <c>PHOTO</c> properties (vCard 2.1 §2.2.3): the octets of an inline picture with the token of its format, or a URL.
        /// </summary>
        IVcard21PropertyList<VcardMediaValue> Photos { get; }

        /// <summary>
        /// Gets the <c>ADR</c> properties (vCard 2.1 §2.3.1).  An address with a component vCard 2.1 can't write, such as one of RFC 9554, isn't shown.
        /// </summary>
        IVcard21PropertyList<VcardAddress> Addresses { get; }

        /// <summary>
        /// Gets the <c>LABEL</c> properties (vCard 2.1 §2.3.2), as text with line breaks.  The model holds each that goes with an <c>ADR</c> as that address's <c>LABEL</c> parameter.
        /// </summary>
        IVcard21PropertyList<string> Labels { get; }

        /// <summary>
        /// Gets the <c>TEL</c> properties (vCard 2.1 §2.4.1), each a telephone number.  A <c>tel</c> URI is shown as its number.
        /// </summary>
        IVcard21PropertyList<string> Telephones { get; }

        /// <summary>
        /// Gets the <c>EMAIL</c> properties (vCard 2.1 §2.4.2).
        /// </summary>
        IVcard21PropertyList<string> Emails { get; }

        /// <summary>
        /// Gets the <c>TZ</c> properties (vCard 2.1 §2.4.5), each an offset from UTC.
        /// </summary>
        IVcard21PropertyList<VcardUtcOffset> TimeZones { get; }

        /// <summary>
        /// Gets the <c>GEO</c> properties (vCard 2.1 §2.4.6).
        /// </summary>
        IVcard21PropertyList<VcardGeo> GeoLocations { get; }

        /// <summary>
        /// Gets the <c>TITLE</c> properties (vCard 2.1 §2.5.1).
        /// </summary>
        IVcard21PropertyList<string> Titles { get; }

        /// <summary>
        /// Gets the <c>ROLE</c> properties (vCard 2.1 §2.5.2).
        /// </summary>
        IVcard21PropertyList<string> Roles { get; }

        /// <summary>
        /// Gets the <c>LOGO</c> properties (vCard 2.1 §2.5.3).
        /// </summary>
        IVcard21PropertyList<VcardMediaValue> Logos { get; }

        /// <summary>
        /// Gets the <c>ORG</c> properties (vCard 2.1 §2.5.5).
        /// </summary>
        IVcard21PropertyList<VcardOrganization> Organizations { get; }

        /// <summary>
        /// Gets the <c>NOTE</c> properties (vCard 2.1 §2.6.1).
        /// </summary>
        IVcard21PropertyList<string> Notes { get; }

        /// <summary>
        /// Gets the <c>SOUND</c> properties (vCard 2.1 §2.6.3): inline audio, a URL, or "a string representation" of how the name is said, as text.
        /// </summary>
        IVcard21PropertyList<VcardMediaValue> Sounds { get; }

        /// <summary>
        /// Gets the <c>URL</c> properties (vCard 2.1 §2.6.4).
        /// </summary>
        IVcard21PropertyList<Uri> Urls { get; }

        /// <summary>
        /// Gets the <c>KEY</c> properties (vCard 2.1 §2.7.1): a key in clear text, whose "default format is clear-text", or its octets, each with the token of its type, such as <c>PGP</c>.
        /// </summary>
        IVcard21PropertyList<VcardMediaValue> Keys { get; }

        /// <summary>
        /// Gets the properties whose names start with <c>X-</c> (vCard 2.1 §2.8.1), each with its value as it is held.
        /// </summary>
        IReadOnlyList<IVcard21Property<string>> ExtensionProperties { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the <c>N</c> property (vCard 2.1 §2.2.2); or <c>null</c>.  It has the five components of vCard 2.1, each with one value.
        /// </summary>
        IVcard21Property<VcardName>? Name { get; }

        /// <summary>
        /// Gets the <c>BDAY</c> property (vCard 2.1 §2.2.4), a complete date; or <c>null</c>.
        /// </summary>
        IVcard21Property<VcardDate>? Birthday { get; }

        /// <summary>
        /// Gets the <c>MAILER</c> property (vCard 2.1 §2.4.3); or <c>null</c>.
        /// </summary>
        IVcard21Property<string>? Mailer { get; }

        /// <summary>
        /// Gets the card of the first <c>AGENT</c> (vCard 2.1 §2.5.4) that holds one; or <c>null</c>.
        /// </summary>
        IVcard21Card? Agent { get; }

        /// <summary>
        /// Gets the URL of the first <c>AGENT</c> whose value is elsewhere (vCard 2.1 §2.1.8); or <c>null</c>.  A Content-ID is a <c>cid</c> URI (RFC 2392).
        /// </summary>
        Uri? AgentUri { get; }

        /// <summary>
        /// Gets the <c>REV</c> property (vCard 2.1 §2.6.2); or <c>null</c>.
        /// </summary>
        IVcard21Property<VcardTimestamp>? Revision { get; }

        /// <summary>
        /// Gets the <c>UID</c> property (vCard 2.1 §2.6.5); or <c>null</c>.
        /// </summary>
        IVcard21Property<string>? Uid { get; }
#else
        /// <summary>
        /// Gets the <c>N</c> property (vCard 2.1 §2.2.2); or <c>null</c>.  It has the five components of vCard 2.1, each with one value.
        /// </summary>
        IVcard21Property<VcardName> Name { get; }

        /// <summary>
        /// Gets the <c>BDAY</c> property (vCard 2.1 §2.2.4), a complete date; or <c>null</c>.
        /// </summary>
        IVcard21Property<VcardDate> Birthday { get; }

        /// <summary>
        /// Gets the <c>MAILER</c> property (vCard 2.1 §2.4.3); or <c>null</c>.
        /// </summary>
        IVcard21Property<string> Mailer { get; }

        /// <summary>
        /// Gets the card of the first <c>AGENT</c> (vCard 2.1 §2.5.4) that holds one; or <c>null</c>.
        /// </summary>
        IVcard21Card Agent { get; }

        /// <summary>
        /// Gets the URL of the first <c>AGENT</c> whose value is elsewhere (vCard 2.1 §2.1.8); or <c>null</c>.  A Content-ID is a <c>cid</c> URI (RFC 2392).
        /// </summary>
        Uri AgentUri { get; }

        /// <summary>
        /// Gets the <c>REV</c> property (vCard 2.1 §2.6.2); or <c>null</c>.
        /// </summary>
        IVcard21Property<VcardTimestamp> Revision { get; }

        /// <summary>
        /// Gets the <c>UID</c> property (vCard 2.1 §2.6.5); or <c>null</c>.
        /// </summary>
        IVcard21Property<string> Uid { get; }
#endif

        /// <summary>
        /// Sets the <c>N</c> property, replacing any there is.
        /// </summary>
        /// <param name="value">The name.</param>
        /// <exception cref="ArgumentException">vCard 2.1 can't hold the name: it has a component of RFC 9554, several values in a component, or a component that ends with a backslash.</exception>
        void SetName(VcardName value);

        /// <summary>
        /// Sets the <c>BDAY</c> property, replacing any there is.
        /// </summary>
        /// <param name="value">The date, which must be complete.</param>
        /// <exception cref="ArgumentException">vCard 2.1 can't hold the date.</exception>
        void SetBirthday(VcardDate value);

        /// <summary>
        /// Sets the <c>MAILER</c> property, replacing any there is.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a value can't.</exception>
        void SetMailer(string value);

        /// <summary>
        /// Sets the agent to a card, replacing any agent there is.  The property holds a copy of the card.
        /// </summary>
        /// <param name="agent">The card, which must be a <see cref="VcardCard"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="agent"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="agent"/> isn't a <see cref="VcardCard"/>.</exception>
        void SetAgent(IVcard21Card agent);

        /// <summary>
        /// Sets the agent to a URL, replacing any agent there is.
        /// </summary>
        /// <param name="agent">The URL, which must be absolute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="agent"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="agent"/> can't be written.</exception>
        void SetAgent(Uri agent);

        /// <summary>
        /// Sets the <c>REV</c> property, replacing any there is.
        /// </summary>
        /// <param name="value">The time.</param>
        /// <exception cref="ArgumentException">vCard 2.1 can't hold the time.</exception>
        void SetRevision(VcardTimestamp value);

        /// <summary>
        /// Sets the <c>UID</c> property, replacing any there is.
        /// </summary>
        /// <param name="value">The identifier.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character a value can't.</exception>
        void SetUid(string value);

        /// <summary>
        /// Adds a property whose name starts with <c>X-</c> (vCard 2.1 §2.8.1).
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value, as it is to be held.</param>
        /// <returns>The property.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> isn't <c>X-</c> and a <c>word</c> of §2.9, or <paramref name="value"/> holds a character a value can't.</exception>
        IVcard21Property<string> AddExtensionProperty(string name, string value);

        /// <summary>
        /// Removes every instance of a property, including those vCard 2.1 can't show.  <c>LABEL</c> removes the labels of addresses as well, and <c>AGENT</c> every agent.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <returns>The number removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        int RemoveProperties(string name);

        /// <summary>
        /// Checks the card against the rules of vCard 2.1, without throwing.  It is <see cref="VcardCard.Validate(VcardVersion)"/> for <see cref="VcardVersion.V2_1"/>.
        /// </summary>
        /// <returns>Every problem found, including what vCard 2.1 can't hold, as <see cref="VcardDiagnosticKind.Lossy"/> items.</returns>
        IReadOnlyList<VcardDiagnostic> Validate();
    }

    /// <summary>
    /// A property in the shape of vCard 2.1.
    /// </summary>
    /// <typeparam name="TValue">The type of its value.</typeparam>
    public interface IVcard21Property<out TValue>
    {
        /// <summary>
        /// Gets the name, in upper case.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        TValue Value { get; }

        /// <summary>
        /// Gets a flag indicating if it has <c>PREF</c> in vCard 2.1: it has the lowest <c>PREF</c> of the instances of its property.
        /// </summary>
        bool IsPreferred { get; }

        /// <summary>
        /// Gets the <c>TYPE</c> values vCard 2.1 can write: each <c>knowntype</c> of §2.9, in upper case, and each that starts with <c>X-</c>.  It has no <c>PREF</c>, which
        /// <see cref="IsPreferred"/> gives, and no token of the format of a value, which the value gives.
        /// </summary>
        IReadOnlyList<string> Types { get; }

        /// <summary>
        /// Gets the parameters whose names start with <c>X-</c>.
        /// </summary>
        IReadOnlyList<VcardContentLineParameter> ExtensionParameters { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the group (vCard 2.1 §2.1.4.2); or <c>null</c>.
        /// </summary>
        string? Group { get; }

        /// <summary>
        /// Gets the <c>LANGUAGE</c> parameter (vCard 2.1 §2.1.7); or <c>null</c>.
        /// </summary>
        string? Language { get; }
#else
        /// <summary>
        /// Gets the group (vCard 2.1 §2.1.4.2); or <c>null</c>.
        /// </summary>
        string Group { get; }

        /// <summary>
        /// Gets the <c>LANGUAGE</c> parameter (vCard 2.1 §2.1.7); or <c>null</c>.
        /// </summary>
        string Language { get; }
#endif
    }

    /// <summary>
    /// The instances of a property in the shape of vCard 2.1, which can be changed.
    /// </summary>
    /// <typeparam name="TValue">The type of its value.</typeparam>
    public interface IVcard21PropertyList<TValue> : IReadOnlyList<IVcard21Property<TValue>>
    {
        /// <summary>
        /// Adds an instance.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">vCard 2.1 can't hold the value.</exception>
        IVcard21Property<TValue> Add(TValue value);

        /// <summary>
        /// Adds an instance with parameters.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">vCard 2.1 can't hold the value or a parameter.</exception>
        IVcard21Property<TValue> Add(TValue value, Vcard21PropertyParameters parameters);

        /// <summary>
        /// Removes every instance, including those vCard 2.1 can't show.
        /// </summary>
        void Clear();

        /// <summary>
        /// Removes an instance that this list gave.
        /// </summary>
        /// <param name="property">The instance.</param>
        /// <returns><c>true</c> if it was removed; <c>false</c> if it isn't an instance of this list or has already been removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="property"/> is <c>null</c>.</exception>
        bool Remove(IVcard21Property<TValue> property);
    }
}
