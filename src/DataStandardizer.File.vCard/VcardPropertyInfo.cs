using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The parameters RFC 6350 defines, for the table of which property allows which.  <c>VALUE</c> is left out, as <see cref="VcardValueTypeRegistry"/> deals with it.
    /// </summary>
    [Flags]
    internal enum VcardParameterSet
    {
        None = 0,
        Language = 1 << 0,
        Preference = 1 << 1,
        AlternativeId = 1 << 2,
        Pid = 1 << 3,
        Type = 1 << 4,
        MediaType = 1 << 5,
        CalendarScale = 1 << 6,
        SortAs = 1 << 7,
        Geo = 1 << 8,
        TimeZone = 1 << 9,
        Label = 1 << 10
    }

    /// <summary>
    /// What the vCard specifications say about a property: which versions define it, and in vCard 4.0 how many instances a card may have.
    /// </summary>
    /// <remarks>
    /// The versions are not additive.  For example, <c>KIND</c> is only in vCard 4.0, and <c>MAILER</c> is in vCard 3.0 and 2.1 and not in 4.0.  Property names are compared
    /// case-insensitively.
    /// </remarks>
    public sealed class VcardPropertyInfo
    {
        [Flags]
        private enum Versions
        {
            V4 = 1 << 0,
            V3 = 1 << 1,
            V21 = 1 << 2,
            From30 = V4 | V3,
            All = V4 | V3 | V21,
            Before40 = V3 | V21
        }

        // ref. RFC 6350 §6: the parameters in each property's ABNF.
        private const VcardParameterSet Instance = VcardParameterSet.Preference | VcardParameterSet.AlternativeId | VcardParameterSet.Pid;
        private const VcardParameterSet Uri = Instance | VcardParameterSet.MediaType;
        private const VcardParameterSet TypedUri = Uri | VcardParameterSet.Type;
        private const VcardParameterSet TypedText = Instance | VcardParameterSet.Type;
        private const VcardParameterSet LanguageText = TypedText | VcardParameterSet.Language;

        private static readonly VcardPropertyInfo[] Infos =
        {
            // ref. RFC 6350 §6.1: general properties.
            new VcardPropertyInfo(VcardPropertyNames.Begin, Versions.All, VcardCardinality.ExactlyOne, VcardParameterSet.None, false, false),
            new VcardPropertyInfo(VcardPropertyNames.End, Versions.All, VcardCardinality.ExactlyOne, VcardParameterSet.None, false, false),
            new VcardPropertyInfo(VcardPropertyNames.Source, Versions.From30, VcardCardinality.Any, Uri),
            new VcardPropertyInfo(VcardPropertyNames.Kind, Versions.V4, VcardCardinality.AtMostOne, VcardParameterSet.None),
            new VcardPropertyInfo(VcardPropertyNames.Xml, Versions.V4, VcardCardinality.Any, VcardParameterSet.AlternativeId, false),

            // ref. RFC 6350 §6.2: identification properties.
            new VcardPropertyInfo(VcardPropertyNames.FormattedName, Versions.All, VcardCardinality.AtLeastOne, LanguageText),
            new VcardPropertyInfo(VcardPropertyNames.Name, Versions.All, VcardCardinality.AtMostOne,
                VcardParameterSet.SortAs | VcardParameterSet.Language | VcardParameterSet.AlternativeId),
            new VcardPropertyInfo(VcardPropertyNames.Nickname, Versions.From30, VcardCardinality.Any, LanguageText),
            new VcardPropertyInfo(VcardPropertyNames.Photo, Versions.All, VcardCardinality.Any, TypedUri),
            new VcardPropertyInfo(VcardPropertyNames.Birthday, Versions.All, VcardCardinality.AtMostOne,
                VcardParameterSet.Language | VcardParameterSet.AlternativeId | VcardParameterSet.CalendarScale),
            new VcardPropertyInfo(VcardPropertyNames.Anniversary, Versions.V4, VcardCardinality.AtMostOne, VcardParameterSet.AlternativeId | VcardParameterSet.CalendarScale),
            new VcardPropertyInfo(VcardPropertyNames.Gender, Versions.V4, VcardCardinality.AtMostOne, VcardParameterSet.None),

            // ref. RFC 6350 §6.3: delivery addressing properties.
            new VcardPropertyInfo(VcardPropertyNames.Address, Versions.All, VcardCardinality.Any,
                LanguageText | VcardParameterSet.Label | VcardParameterSet.Geo | VcardParameterSet.TimeZone),

            // ref. RFC 6350 §6.4: communications properties.
            new VcardPropertyInfo(VcardPropertyNames.Telephone, Versions.All, VcardCardinality.Any, TypedUri),
            new VcardPropertyInfo(VcardPropertyNames.Email, Versions.All, VcardCardinality.Any, TypedText),
            new VcardPropertyInfo(VcardPropertyNames.InstantMessaging, Versions.From30, VcardCardinality.Any, TypedUri),
            new VcardPropertyInfo(VcardPropertyNames.Language, Versions.V4, VcardCardinality.Any, TypedText),

            // ref. RFC 6350 §6.5: geographical properties.
            new VcardPropertyInfo(VcardPropertyNames.TimeZone, Versions.All, VcardCardinality.Any, TypedUri),
            new VcardPropertyInfo(VcardPropertyNames.Geo, Versions.All, VcardCardinality.Any, TypedUri),

            // ref. RFC 6350 §6.6: organizational properties.
            new VcardPropertyInfo(VcardPropertyNames.Title, Versions.All, VcardCardinality.Any, LanguageText),
            new VcardPropertyInfo(VcardPropertyNames.Role, Versions.All, VcardCardinality.Any, LanguageText),
            new VcardPropertyInfo(VcardPropertyNames.Logo, Versions.All, VcardCardinality.Any, TypedUri | VcardParameterSet.Language),
            new VcardPropertyInfo(VcardPropertyNames.Organization, Versions.All, VcardCardinality.Any, LanguageText | VcardParameterSet.SortAs),
            new VcardPropertyInfo(VcardPropertyNames.Member, Versions.V4, VcardCardinality.Any, Uri),
            new VcardPropertyInfo(VcardPropertyNames.Related, Versions.V4, VcardCardinality.Any, TypedUri | VcardParameterSet.Language),

            // ref. RFC 6350 §6.7: explanatory properties.
            new VcardPropertyInfo(VcardPropertyNames.Categories, Versions.From30, VcardCardinality.Any, TypedText),
            new VcardPropertyInfo(VcardPropertyNames.Note, Versions.All, VcardCardinality.Any, LanguageText),
            new VcardPropertyInfo(VcardPropertyNames.ProductId, Versions.From30, VcardCardinality.AtMostOne, VcardParameterSet.None),
            new VcardPropertyInfo(VcardPropertyNames.Revision, Versions.All, VcardCardinality.AtMostOne, VcardParameterSet.None),
            new VcardPropertyInfo(VcardPropertyNames.Sound, Versions.All, VcardCardinality.Any, TypedUri | VcardParameterSet.Language),
            new VcardPropertyInfo(VcardPropertyNames.Uid, Versions.All, VcardCardinality.AtMostOne, VcardParameterSet.None),
            new VcardPropertyInfo(VcardPropertyNames.ClientPidMap, Versions.V4, VcardCardinality.Any, VcardParameterSet.None, true, false),
            new VcardPropertyInfo(VcardPropertyNames.Url, Versions.All, VcardCardinality.Any, TypedUri),
            new VcardPropertyInfo(VcardPropertyNames.Version, Versions.All, VcardCardinality.ExactlyOne, VcardParameterSet.None),

            // ref. RFC 6350 §6.8: security properties.
            new VcardPropertyInfo(VcardPropertyNames.Key, Versions.All, VcardCardinality.Any, TypedUri),

            // ref. RFC 6350 §6.9: calendar properties, which RFC 2739 added to vCard 3.0.
            new VcardPropertyInfo(VcardPropertyNames.FreeBusyUrl, Versions.From30, VcardCardinality.Any, TypedUri),
            new VcardPropertyInfo(VcardPropertyNames.CalendarAddressUri, Versions.From30, VcardCardinality.Any, TypedUri),
            new VcardPropertyInfo(VcardPropertyNames.CalendarUri, Versions.From30, VcardCardinality.Any, TypedUri),

            // ref. RFC 6350 Appendix A: the properties vCard 4.0 removed, from RFC 2426, RFC 2739 and vCard 2.1.  To vCard 4.0 they are unknown properties.
            new VcardPropertyInfo("AGENT", Versions.Before40, VcardCardinality.Any, VcardParameterSet.None),
            new VcardPropertyInfo("CAPURI", Versions.V3, VcardCardinality.Any, VcardParameterSet.None),
            new VcardPropertyInfo("CLASS", Versions.V3, VcardCardinality.Any, VcardParameterSet.None),
            new VcardPropertyInfo("LABEL", Versions.Before40, VcardCardinality.Any, VcardParameterSet.None),
            new VcardPropertyInfo("MAILER", Versions.Before40, VcardCardinality.Any, VcardParameterSet.None),
            new VcardPropertyInfo("NAME", Versions.V3, VcardCardinality.Any, VcardParameterSet.None),
            new VcardPropertyInfo("PROFILE", Versions.V3, VcardCardinality.Any, VcardParameterSet.None),
            new VcardPropertyInfo("SORT-STRING", Versions.V3, VcardCardinality.Any, VcardParameterSet.None)
        };

        private static readonly ReadOnlyCollection<VcardPropertyInfo> AllInfos = new ReadOnlyCollection<VcardPropertyInfo>(Infos);

        private static readonly Dictionary<string, VcardPropertyInfo> InfosByName = CreateInfosByName();

        private readonly Versions _versions;

        private VcardPropertyInfo(string name, Versions versions, VcardCardinality cardinality, VcardParameterSet parameters, bool allowsOtherParameters = true,
            bool allowsValueParameter = true)
        {
            Name = name;
            _versions = versions;
            Cardinality = cardinality;
            Parameters = parameters;
            AllowsOtherParameters = allowsOtherParameters;
            AllowsValueParameter = allowsValueParameter;
        }

        /// <summary>
        /// Gets every property any version defines.
        /// </summary>
        public static IReadOnlyList<VcardPropertyInfo> All => AllInfos;

        /// <summary>
        /// Gets how many instances a vCard 4.0 card may have, as RFC 6350 §6 says.  A property vCard 4.0 doesn't define is <see cref="VcardCardinality.Any"/>.
        /// </summary>
        public VcardCardinality Cardinality { get; }

        /// <summary>
        /// Gets the name of the property, in upper case.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the parameters RFC 6350 defines that the property allows in vCard 4.0, other than <c>VALUE</c>.
        /// </summary>
        internal VcardParameterSet Parameters { get; }

        /// <summary>
        /// Gets a flag indicating if the property allows the <c>any-param</c> of RFC 6350 §3.3 in vCard 4.0: parameters that RFC 6350 doesn't define.
        /// </summary>
        internal bool AllowsOtherParameters { get; }

        /// <summary>
        /// Gets a flag indicating if the property has a <c>VALUE</c> parameter in its ABNF in vCard 4.0.
        /// </summary>
        internal bool AllowsValueParameter { get; }

        /// <summary>
        /// Gets what is known about a property.
        /// </summary>
        /// <param name="name">The name of the property, in any case.</param>
        /// <returns>What is known; or <c>null</c> for a property that no version defines.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static VcardPropertyInfo? Find(string name)
#else
        public static VcardPropertyInfo Find(string name)
#endif
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            VcardPropertyInfo? info;
#else
            VcardPropertyInfo info;
#endif
            return InfosByName.TryGetValue(name, out info) ? info : null;
        }

        /// <summary>
        /// Gets a flag indicating if a version defines the property.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns><c>true</c> if the version defines the property; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a <see cref="VcardVersion"/> member.</exception>
        public bool IsDefinedIn(VcardVersion version)
        {
            switch (version)
            {
                case VcardVersion.V4_0:
                    return (_versions & Versions.V4) != 0;
                case VcardVersion.V3_0:
                    return (_versions & Versions.V3) != 0;
                case VcardVersion.V2_1:
                    return (_versions & Versions.V21) != 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(version), version, "The version is not recognised.");
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }

        private static Dictionary<string, VcardPropertyInfo> CreateInfosByName()
        {
            var infosByName = new Dictionary<string, VcardPropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var info in Infos)
            {
                infosByName.Add(info.Name, info);
            }

            return infosByName;
        }
    }
}
