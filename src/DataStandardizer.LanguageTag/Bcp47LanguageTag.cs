#if NETSTANDARD
using JetBrains.Annotations;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using DataStandardizer.Core;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using DataStandardizer.LanguageTag.InternalState;

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// IETF BCP 47 language tag.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public readonly struct Bcp47LanguageTag : IEquatable<Bcp47LanguageTag>
#if NET7_0_OR_GREATER
        , ISpanFormattable, IParsable<Bcp47LanguageTag>, ISpanParsable<Bcp47LanguageTag>
#elif NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        #region Declarations

        // constant declarations
        internal static class ErrorMessage
        {
            internal const string ConformanceValidNeedsRegistry = "Validity needs a subtag registry. Use the overload that takes a SubtagRegistry.";
            internal const string LanguageTagNotInIsoCodeLists = "The language tag is not well-formed, or is not in the ISO code lists.";
            internal const string LanguageTagNotValid = "The language tag is not valid against the subtag registry.";
            internal const string LanguageTagNotWellFormed = "The language tag is not well-formed.";
            internal const string LanguageTagNull = "The language tag must not be null.";
            internal const string UninitializedInstance = "The " + nameof(Bcp47LanguageTag) + " instance is not initialised.";
        }

        // local (instance) declarations
#if NETCOREAPP3_0_OR_GREATER
        private readonly string? _languageTag;
        private readonly Bcp47LanguageTagParts? _parts; // null only when _languageTag is
#else
        [CanBeNull] private readonly string _languageTag;
        [CanBeNull] private readonly Bcp47LanguageTagParts _parts; // null only when _languageTag is
#endif

        #endregion

        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        private Bcp47LanguageTag(string languageTag, Bcp47LanguageTagParts parts)
#else
        private Bcp47LanguageTag([NotNull] string languageTag, [NotNull] Bcp47LanguageTagParts parts)
#endif
        {
            _languageTag = languageTag;
            _parts = parts;
        }

        #endregion

        #region Operators

        /// <summary>
        /// Convert a string to a language tag object.
        /// </summary>
        /// <param name="languageTag">Language tag on which the object will be based.</param>
        /// <returns>Language tag object.</returns>
        /// <exception cref="InvalidCastException">The language tag is <c>null</c> or was not recognised.</exception>
        /// <remarks>
        /// The language tag is checked for well-formedness only (RFC 5646 §2.2.9).
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static explicit operator Bcp47LanguageTag(string languageTag)
#else
        public static explicit operator Bcp47LanguageTag([NotNull] string languageTag)
#endif
        {
            if (languageTag is null)
            {
                throw new InvalidCastException(ErrorMessage.LanguageTagNull);
            }

            if (!TryCreate(languageTag, out var result))
            {
                throw new InvalidCastException(ErrorMessage.LanguageTagNotWellFormed);
            }

            return result;
        }

#if NETCOREAPP3_0_OR_GREATER
        public static implicit operator string?(Bcp47LanguageTag languageTag)
#else
        [CanBeNull]
        public static implicit operator string(Bcp47LanguageTag languageTag)
#endif
        {
            return languageTag._languageTag;
        }

        public static bool operator ==(Bcp47LanguageTag left, Bcp47LanguageTag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Bcp47LanguageTag left, Bcp47LanguageTag right)
        {
            return !left.Equals(right);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Create a language tag object.
        /// </summary>
        /// <param name="languageTag">Language tag on which the object will be based.</param>
        /// <returns>Language tag object.</returns>
        /// <exception cref="ArgumentNullException">The language tag is <c>null</c>.</exception>
        /// <exception cref="LanguageTagFormatException">The language tag is not well-formed.</exception>
        /// <remarks>
        /// The language tag is checked for well-formedness only (RFC 5646 §2.2.9): it matches the ABNF of RFC 5646 §2.1, and nothing else is checked.
        /// To also check its subtags, use <see cref="Create(string, SubtagRegistry.SubtagRegistry)"/>.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static Bcp47LanguageTag Create(string languageTag)
#else
        public static Bcp47LanguageTag Create([NotNull] string languageTag)
#endif
        {
            if (languageTag is null)
            {
                throw new ArgumentNullException(nameof(languageTag));
            }

            var parts = ParseToParts(languageTag, Bcp47SyntaxSubtagClassifier.Instance, LanguageTagConformance.WellFormed);
            if (parts is null)
            {
                throw new LanguageTagFormatException(ErrorMessage.LanguageTagNotWellFormed, languageTag);
            }

            return new Bcp47LanguageTag(languageTag, parts);
        }

        /// <summary>
        /// Create a language tag object that is valid against the Subtag Registry.
        /// </summary>
        /// <param name="languageTag">Language tag on which the object will be based.</param>
        /// <param name="subtagRegistry">A copy of the IANA Subtag Registry.</param>
        /// <returns>Language tag object.</returns>
        /// <exception cref="ArgumentNullException">The language tag or the Subtag Registry is <c>null</c>.</exception>
        /// <exception cref="LanguageTagFormatException">The language tag is not valid against the Subtag Registry.</exception>
        /// <remarks>
        /// The language tag must satisfy RFC 5646 §2.2.9: it is well-formed, either it is a grandfathered tag in the registry or all of its primary language,
        /// extended language, script, region and variant subtags appear in the registry, and it repeats no variant subtag or extension singleton.
        /// The <c>Prefix</c> and <c>Deprecated</c> fields of the registry's records are not checked, because §2.2.9 does not list them.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static Bcp47LanguageTag Create(string languageTag, SubtagRegistry.SubtagRegistry subtagRegistry)
#else
        public static Bcp47LanguageTag Create([NotNull] string languageTag, [NotNull] SubtagRegistry.SubtagRegistry subtagRegistry)
#endif
        {
            if (languageTag is null)
            {
                throw new ArgumentNullException(nameof(languageTag));
            }

            if (subtagRegistry is null)
            {
                throw new ArgumentNullException(nameof(subtagRegistry));
            }

            var parts = ParseToParts(languageTag, subtagRegistry.Classifier, LanguageTagConformance.Valid);
            if (parts is null)
            {
                throw new LanguageTagFormatException(ErrorMessage.LanguageTagNotValid, languageTag);
            }

            return new Bcp47LanguageTag(languageTag, parts);
        }

        /// <summary>
        /// Create a language tag object, with a choice of what is checked.
        /// </summary>
        /// <param name="languageTag">Language tag on which the object will be based.</param>
        /// <param name="conformance">What to check: <see cref="LanguageTagConformance.WellFormed"/> or <see cref="LanguageTagConformance.IsoCodeLists"/>.</param>
        /// <returns>Language tag object.</returns>
        /// <exception cref="ArgumentNullException">The language tag is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="conformance"/> is <see cref="LanguageTagConformance.Valid"/>, which needs a Subtag Registry; use <see cref="Create(string, SubtagRegistry.SubtagRegistry)"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="conformance"/> is not a defined value.</exception>
        /// <exception cref="LanguageTagFormatException">The language tag does not satisfy <paramref name="conformance"/>.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static Bcp47LanguageTag Create(string languageTag, LanguageTagConformance conformance)
#else
        public static Bcp47LanguageTag Create([NotNull] string languageTag, LanguageTagConformance conformance)
#endif
        {
            if (languageTag is null)
            {
                throw new ArgumentNullException(nameof(languageTag));
            }

            var classifier = GetClassifier(conformance);
            var parts = ParseToParts(languageTag, classifier, conformance);
            if (parts is null)
            {
                throw new LanguageTagFormatException(conformance == LanguageTagConformance.WellFormed ? ErrorMessage.LanguageTagNotWellFormed : ErrorMessage.LanguageTagNotInIsoCodeLists, languageTag);
            }

            return new Bcp47LanguageTag(languageTag, parts);
        }

        /// <summary>
        /// Gets the language tag as a span of characters, without copying it.
        /// </summary>
        /// <returns>The language tag; an empty span if the <see cref="Bcp47LanguageTag"/> is not initialised.</returns>
        public ReadOnlySpan<char> AsSpan()
        {
            return _languageTag.AsSpan();
        }

        public bool Equals(Bcp47LanguageTag other)
        {
            return string.Equals(_languageTag, other._languageTag, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determine whether the language tag is valid against a Subtag Registry, whatever was checked when it was created.
        /// </summary>
        /// <param name="subtagRegistry">A copy of the IANA Subtag Registry.</param>
        /// <returns><c>true</c> if the language tag is valid (RFC 5646 §2.2.9); <c>false</c> if not.</returns>
        /// <exception cref="ArgumentNullException">The Subtag Registry is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
        /// <remarks>
        /// The result does not change <see cref="Conformance"/>. To carry <see cref="LanguageTagConformance.Valid"/>, create the language tag with <see cref="Create(string, SubtagRegistry.SubtagRegistry)"/>.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public bool IsValid(SubtagRegistry.SubtagRegistry subtagRegistry)
#else
        public bool IsValid([NotNull] SubtagRegistry.SubtagRegistry subtagRegistry)
#endif
        {
            if (_languageTag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

            return IsValid(_languageTag.AsSpan(), subtagRegistry);
        }

        public override int GetHashCode()
        {
            // Must agree with Equals, which compares case-insensitively.
            return (_languageTag != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(_languageTag) : 0);
        }

        /// <summary>
        /// Convert the Script subtag of the language tag to a <see cref="Iso15924Script"/> enum.
        /// </summary>
        /// <returns>A <see cref="Iso15924Script"/> enum representing the Script subtag, if compatible; <c>null</c> if the Script subtag does not exist or is not an ISO 15924 code.</returns>
        public Iso15924Script? ToIso15924()
        {
            if (!Enum.TryParse<Iso15924Script>(ScriptSubtag, true, out var result)) return null;
            return result;

        }

        /// <summary>
        /// Convert the Region subtag of the language tag to a <see cref="Iso3166Part1Alpha2Country"/> enum.
        /// </summary>
        /// <returns>A <see cref="Iso3166Part1Alpha2Country"/> enum representing the Region subtag, if compatible; <c>null</c> if the Region subtag does not exist or is not an ISO 3166-1 alpha-2 code.</returns>
        public Iso3166Part1Alpha2Country? ToIso3166Part1Alpha2()
        {
            // A region subtag is either 2ALPHA or 3DIGIT. Enum.TryParse would accept a 3DIGIT (UN M.49) subtag
            // as a raw numeric value, so only the two-letter form is an ISO 3166-1 alpha-2 candidate.
            var regionSubtag = RegionSubtag;
            if (regionSubtag == null || regionSubtag.Length != 2) return null;
            if (!Enum.TryParse<Iso3166Part1Alpha2Country>(regionSubtag, true, out var result)) return null;
            return result;

        }

        /// <summary>
        /// Convert the Primary Language subtag of the language tag to a <see cref="Iso639Part1Language"/> enum.
        /// </summary>
        /// <returns>A <see cref="Iso639Part1Language"/> enum representing the Primary Language subtag, if compatible; <c>null</c> if the language tag has no Primary Language subtag, or it is not an ISO 639-1 code.</returns>
        public Iso639Part1Language? ToIso639Part1()
        {
            if (!StringEnum.TryParse<Iso639Part1Language>(PrimaryLanguageSubtag, true, out var result)) return null;
            return result;

        }

        /// <summary>
        /// Convert the Primary Language subtag of the language tag to a <see cref="Iso639Part2TLanguage"/> enum.
        /// </summary>
        /// <returns>A <see cref="Iso639Part2TLanguage"/> enum representing the Primary Language subtag, if compatible; <c>null</c> if the language tag has no Primary Language subtag, or it is not an ISO 639-2/T code.</returns>
        public Iso639Part2TLanguage? ToIso639Part2T()
        {
            if (!StringEnum.TryParse<Iso639Part2TLanguage>(PrimaryLanguageSubtag, true, out var result)) return null;
            return result;

        }

        /// <summary>
        /// Convert the Primary Language subtag of the language tag to a <see cref="Iso639Part3Language"/> enum.
        /// </summary>
        /// <returns>A <see cref="Iso639Part3Language"/> enum representing the Primary Language subtag, if compatible; <c>null</c> if the language tag has no Primary Language subtag, or it is not an ISO 639-3 code.</returns>
        public Iso639Part3Language? ToIso639Part3()
        {
            if (!StringEnum.TryParse<Iso639Part3Language>(PrimaryLanguageSubtag, true, out var result)) return null;
            return result;

        }

        /// <summary>
        /// Convert the Primary Language subtag of the language tag to a <see cref="Iso639Part5LanguageFamily"/> enum.
        /// </summary>
        /// <returns>A <see cref="Iso639Part5LanguageFamily"/> enum representing the Primary Language subtag, if compatible; <c>null</c> if the language tag has no Primary Language subtag, or it is not an ISO 639-5 code.</returns>
        public Iso639Part5LanguageFamily? ToIso639Part5()
        {
            if (!StringEnum.TryParse<Iso639Part5LanguageFamily>(PrimaryLanguageSubtag, true, out var result)) return null;
            return result;

        }

        public override string ToString()
        {
            return _languageTag ?? base.ToString() ?? GetType().FullName ?? GetType().Name;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Copy the language tag into a span of characters.
        /// </summary>
        /// <param name="destination">Span to copy the language tag into.</param>
        /// <param name="charsWritten">When this method returns, the number of characters written to <paramref name="destination"/>; 0 if it is too short.</param>
        /// <param name="format">Ignored, as a language tag has only one form.</param>
        /// <param name="provider">Ignored, as language tags do not depend on culture.</param>
        /// <returns><c>true</c> if the language tag was copied; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <remarks>
        /// The characters written are those that <see cref="ToString()"/> returns.
        /// </remarks>
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            var text = ToString();
            if (!text.AsSpan().TryCopyTo(destination))
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = text.Length;
            return true;
        }

        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString();
        }
#endif

        /// <summary>
        /// Convert the Region subtag of the language tag to an M.49 code.
        /// </summary>
        /// <returns>An M.49 code representing the Region subtag, if compatible; <c>null</c> if the Region subtag doesn't exist or is not an M.49 code.</returns>
        public ushort? ToUnM49()
        {
            if (ushort.TryParse(RegionSubtag, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unM49CandidateCodeResult) && Bcp47UnM49Codes.Contains(unM49CandidateCodeResult))
            {
                return unM49CandidateCodeResult;
            }

            return null;
        }

#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
        {
            return obj is Bcp47LanguageTag other && Equals(other);
        }
#else
        public override bool Equals(object obj)
        {
            return obj is Bcp47LanguageTag other && Equals(other);
        }
#endif

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Determine if a string contains a valid Extended Language subtag.
        /// </summary>
        /// <param name="extendedLanguageSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <exception cref="ArgumentNullException">The subtag is <c>null</c>.</exception>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckExtendedLanguageSubtag(string extendedLanguageSubtag)
#else
        public static bool CheckExtendedLanguageSubtag([NotNull] string extendedLanguageSubtag)
#endif
        {
            if (extendedLanguageSubtag is null) throw new ArgumentNullException(nameof(extendedLanguageSubtag));

            return Bcp47RulesBasedSubtagClassifier.Instance.IsExtendedLanguage(extendedLanguageSubtag.AsSpan());
        }

        /// <summary>
        /// Determine if a span of characters contains a valid Extended Language subtag.
        /// </summary>
        /// <param name="extendedLanguageSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
        public static bool CheckExtendedLanguageSubtag(ReadOnlySpan<char> extendedLanguageSubtag)
        {
            return Bcp47RulesBasedSubtagClassifier.Instance.IsExtendedLanguage(extendedLanguageSubtag);
        }

        /// <summary>
        /// Determine if a string contains a valid Extension subtag.
        /// </summary>
        /// <param name="extensionSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <exception cref="ArgumentNullException">The subtag is <c>null</c>.</exception>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules. The string must contain exactly one extension: a singleton followed by its subtags.
        /// Whether a singleton is repeated within a language tag is checked when the whole tag is validated.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckExtensionSubtag(string extensionSubtag)
#else
        public static bool CheckExtensionSubtag([NotNull] string extensionSubtag)
#endif
        {
            if (extensionSubtag is null) throw new ArgumentNullException(nameof(extensionSubtag));

            return Bcp47LanguageTagParser.IsExtension(extensionSubtag.AsSpan());
        }

        /// <summary>
        /// Determine if a span of characters contains a valid Extension subtag.
        /// </summary>
        /// <param name="extensionSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules. The span must contain exactly one extension: a singleton followed by its subtags.
        /// Whether a singleton is repeated within a language tag is checked when the whole tag is validated.
        /// </remarks>
        public static bool CheckExtensionSubtag(ReadOnlySpan<char> extensionSubtag)
        {
            return Bcp47LanguageTagParser.IsExtension(extensionSubtag);
        }

        /// <summary>
        /// Determine if a string contains a valid Primary Language subtag.
        /// </summary>
        /// <param name="primaryLanguageSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <exception cref="ArgumentNullException">The subtag is <c>null</c>.</exception>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckPrimaryLanguageSubtag(string primaryLanguageSubtag)
#else
        public static bool CheckPrimaryLanguageSubtag([NotNull] string primaryLanguageSubtag)
#endif
        {
            if (primaryLanguageSubtag is null) throw new ArgumentNullException(nameof(primaryLanguageSubtag));

            return Bcp47RulesBasedSubtagClassifier.Instance.IsPrimaryLanguage(primaryLanguageSubtag.AsSpan(), out _);
        }

        /// <summary>
        /// Determine if a span of characters contains a valid Primary Language subtag.
        /// </summary>
        /// <param name="primaryLanguageSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
        public static bool CheckPrimaryLanguageSubtag(ReadOnlySpan<char> primaryLanguageSubtag)
        {
            return Bcp47RulesBasedSubtagClassifier.Instance.IsPrimaryLanguage(primaryLanguageSubtag, out _);
        }

        /// <summary>
        /// Determine if a string contains a valid Private Use subtag.
        /// </summary>
        /// <param name="privateUseSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <exception cref="ArgumentNullException">The subtag is <c>null</c>.</exception>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckPrivateUseSubtag(string privateUseSubtag)
#else
        public static bool CheckPrivateUseSubtag([NotNull] string privateUseSubtag)
#endif
        {
            if (privateUseSubtag is null) throw new ArgumentNullException(nameof(privateUseSubtag));

            return Bcp47LanguageTagParser.IsPrivateUse(privateUseSubtag.AsSpan());
        }

        /// <summary>
        /// Determine if a span of characters contains a valid Private Use subtag.
        /// </summary>
        /// <param name="privateUseSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
        public static bool CheckPrivateUseSubtag(ReadOnlySpan<char> privateUseSubtag)
        {
            return Bcp47LanguageTagParser.IsPrivateUse(privateUseSubtag);
        }

        /// <summary>
        /// Determine if a string contains a valid Region subtag.
        /// </summary>
        /// <param name="regionSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <exception cref="ArgumentNullException">The subtag is <c>null</c>.</exception>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckRegionSubtag(string regionSubtag)
#else
        public static bool CheckRegionSubtag([NotNull] string regionSubtag)
#endif
        {
            if (regionSubtag is null) throw new ArgumentNullException(nameof(regionSubtag));

            return Bcp47RulesBasedSubtagClassifier.Instance.IsRegion(regionSubtag.AsSpan());
        }

        /// <summary>
        /// Determine if a span of characters contains a valid Region subtag.
        /// </summary>
        /// <param name="regionSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
        public static bool CheckRegionSubtag(ReadOnlySpan<char> regionSubtag)
        {
            return Bcp47RulesBasedSubtagClassifier.Instance.IsRegion(regionSubtag);
        }

        /// <summary>
        /// Determine if a string contains a valid Script subtag.
        /// </summary>
        /// <param name="scriptSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <exception cref="ArgumentNullException">The subtag is <c>null</c>.</exception>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckScriptSubtag(string scriptSubtag)
#else
        public static bool CheckScriptSubtag([NotNull] string scriptSubtag)
#endif
        {
            if (scriptSubtag is null) throw new ArgumentNullException(nameof(scriptSubtag));

            return Bcp47RulesBasedSubtagClassifier.Instance.IsScript(scriptSubtag.AsSpan());
        }

        /// <summary>
        /// Determine if a span of characters contains a valid Script subtag.
        /// </summary>
        /// <param name="scriptSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
        public static bool CheckScriptSubtag(ReadOnlySpan<char> scriptSubtag)
        {
            return Bcp47RulesBasedSubtagClassifier.Instance.IsScript(scriptSubtag);
        }

        /// <summary>
        /// Determine if a string contains a valid Variant subtag.
        /// </summary>
        /// <param name="variantSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <exception cref="ArgumentNullException">The subtag is <c>null</c>.</exception>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules. The string must contain exactly one subtag.
        /// Whether a variant is repeated within a language tag is checked when the whole tag is validated.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckVariantSubtag(string variantSubtag)
#else
        public static bool CheckVariantSubtag([NotNull] string variantSubtag)
#endif
        {
            if (variantSubtag is null) throw new ArgumentNullException(nameof(variantSubtag));

            return Bcp47RulesBasedSubtagClassifier.Instance.IsVariant(variantSubtag.AsSpan());
        }

        /// <summary>
        /// Determine if a span of characters contains a valid Variant subtag.
        /// </summary>
        /// <param name="variantSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules. The span must contain exactly one subtag.
        /// Whether a variant is repeated within a language tag is checked when the whole tag is validated.
        /// </remarks>
        public static bool CheckVariantSubtag(ReadOnlySpan<char> variantSubtag)
        {
            return Bcp47RulesBasedSubtagClassifier.Instance.IsVariant(variantSubtag);
        }

        /// <summary>
        /// Determine whether a string is a well-formed language tag: it matches the ABNF of RFC 5646 §2.1, and nothing else is checked (§2.2.9).
        /// </summary>
        /// <param name="languageTag">Language tag to check.</param>
        /// <returns><c>true</c> if the language tag is well-formed; <c>false</c> if not.</returns>
        /// <exception cref="ArgumentNullException">The language tag is <c>null</c>.</exception>
        /// <remarks>
        /// A well-formed language tag may use subtags that nothing has assigned, and may repeat a variant subtag or an extension singleton. The 26 grandfathered language tags are well-formed (§2.1).
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool IsWellFormed(string languageTag)
#else
        public static bool IsWellFormed([NotNull] string languageTag)
#endif
        {
            if (languageTag is null) throw new ArgumentNullException(nameof(languageTag));

            return IsWellFormed(languageTag.AsSpan());
        }

        /// <summary>
        /// Determine whether a span of characters is a well-formed language tag: it matches the ABNF of RFC 5646 §2.1, and nothing else is checked (§2.2.9).
        /// </summary>
        /// <param name="languageTag">Language tag to check.</param>
        /// <returns><c>true</c> if the language tag is well-formed; <c>false</c> if not.</returns>
        /// <remarks>
        /// A well-formed language tag may use subtags that nothing has assigned, and may repeat a variant subtag or an extension singleton. The 26 grandfathered language tags are well-formed (§2.1).
        /// This method does not allocate.
        /// </remarks>
        public static bool IsWellFormed(ReadOnlySpan<char> languageTag)
        {
            return Bcp47LanguageTagParser.Parse(languageTag, Bcp47SyntaxSubtagClassifier.Instance, out _) != Bcp47ParseStatus.Invalid;
        }

        /// <summary>
        /// Determine whether a string is a language tag that is valid against a Subtag Registry (RFC 5646 §2.2.9).
        /// </summary>
        /// <param name="languageTag">Language tag to check.</param>
        /// <param name="subtagRegistry">A copy of the IANA Subtag Registry.</param>
        /// <returns><c>true</c> if the language tag is valid; <c>false</c> if not.</returns>
        /// <exception cref="ArgumentNullException">The language tag or the Subtag Registry is <c>null</c>.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool IsValid(string languageTag, SubtagRegistry.SubtagRegistry subtagRegistry)
#else
        public static bool IsValid([NotNull] string languageTag, [NotNull] SubtagRegistry.SubtagRegistry subtagRegistry)
#endif
        {
            if (languageTag is null) throw new ArgumentNullException(nameof(languageTag));

            return IsValid(languageTag.AsSpan(), subtagRegistry);
        }

        /// <summary>
        /// Determine whether a span of characters is a language tag that is valid against a Subtag Registry (RFC 5646 §2.2.9).
        /// </summary>
        /// <param name="languageTag">Language tag to check.</param>
        /// <param name="subtagRegistry">A copy of the IANA Subtag Registry.</param>
        /// <returns><c>true</c> if the language tag is valid; <c>false</c> if not.</returns>
        /// <exception cref="ArgumentNullException">The Subtag Registry is <c>null</c>.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool IsValid(ReadOnlySpan<char> languageTag, SubtagRegistry.SubtagRegistry subtagRegistry)
#else
        public static bool IsValid(ReadOnlySpan<char> languageTag, [NotNull] SubtagRegistry.SubtagRegistry subtagRegistry)
#endif
        {
            if (subtagRegistry is null) throw new ArgumentNullException(nameof(subtagRegistry));

            return Bcp47LanguageTagParser.Parse(languageTag, subtagRegistry.Classifier, out _) == Bcp47ParseStatus.Accepted;
        }

        /// <summary>
        /// Create a <see cref="Bcp47LanguageTag"/> with the specified language tag.
        /// </summary>
        /// <param name="languageTag">An IETF BCP 47 language tag.</param>
        /// <param name="result">When this method returns, contains the constructed <see cref="Bcp47LanguageTag"/>.</param>
        /// <returns><c>true</c> if the <see cref="Bcp47LanguageTag"/> was successfully created; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The language tag is checked for well-formedness only (RFC 5646 §2.2.9).
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryCreate(string? languageTag, out Bcp47LanguageTag result)
#else
        public static bool TryCreate([CanBeNull] string languageTag, out Bcp47LanguageTag result)
#endif
        {
            if (languageTag is null)
            {
                result = default;
                return false;
            }

            var parts = ParseToParts(languageTag, Bcp47SyntaxSubtagClassifier.Instance, LanguageTagConformance.WellFormed);
            if (parts is null)
            {
                result = default;
                return false;
            }

            result = new Bcp47LanguageTag(languageTag, parts);
            return true;
        }

        /// <summary>
        /// Create a <see cref="Bcp47LanguageTag"/> that is valid against the Subtag Registry (RFC 5646 §2.2.9) with the specified language tag.
        /// </summary>
        /// <param name="languageTag">An IETF BCP 47 language tag.</param>
        /// <param name="subtagRegistry">A copy of the Subtag Registry.</param>
        /// <param name="result">When this method returns, contains the constructed <see cref="Bcp47LanguageTag"/>.</param>
        /// <returns><c>true</c> if the <see cref="Bcp47LanguageTag"/> was successfully created; otherwise, <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryCreate(string? languageTag, SubtagRegistry.SubtagRegistry subtagRegistry, out Bcp47LanguageTag result)
#else
        public static bool TryCreate([CanBeNull] string languageTag, [NotNull] SubtagRegistry.SubtagRegistry subtagRegistry, out Bcp47LanguageTag result)
#endif
        {
            if (languageTag is null || subtagRegistry is null)
            {
                result = default;
                return false;
            }

            var parts = ParseToParts(languageTag, subtagRegistry.Classifier, LanguageTagConformance.Valid);
            if (parts is null)
            {
                result = default;
                return false;
            }

            result = new Bcp47LanguageTag(languageTag, parts);
            return true;
        }

        /// <summary>
        /// Create a <see cref="Bcp47LanguageTag"/> with the specified language tag, with a choice of what is checked.
        /// </summary>
        /// <param name="languageTag">An IETF BCP 47 language tag.</param>
        /// <param name="conformance">What to check: <see cref="LanguageTagConformance.WellFormed"/> or <see cref="LanguageTagConformance.IsoCodeLists"/>.</param>
        /// <param name="result">When this method returns, contains the constructed <see cref="Bcp47LanguageTag"/>.</param>
        /// <returns><c>true</c> if the <see cref="Bcp47LanguageTag"/> was successfully created; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="conformance"/> is <see cref="LanguageTagConformance.Valid"/>, which needs a Subtag Registry.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="conformance"/> is not a defined value.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryCreate(string? languageTag, LanguageTagConformance conformance, out Bcp47LanguageTag result)
#else
        public static bool TryCreate([CanBeNull] string languageTag, LanguageTagConformance conformance, out Bcp47LanguageTag result)
#endif
        {
            var classifier = GetClassifier(conformance);
            if (languageTag is null)
            {
                result = default;
                return false;
            }

            var parts = ParseToParts(languageTag, classifier, conformance);
            if (parts is null)
            {
                result = default;
                return false;
            }

            result = new Bcp47LanguageTag(languageTag, parts);
            return true;
        }

        /// <summary>
        /// Create a <see cref="Bcp47LanguageTag"/> from a span of characters that contains a language tag.
        /// </summary>
        /// <param name="languageTag">An IETF BCP 47 language tag.</param>
        /// <param name="result">When this method returns, contains the constructed <see cref="Bcp47LanguageTag"/>.</param>
        /// <returns><c>true</c> if the <see cref="Bcp47LanguageTag"/> was successfully created; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The language tag is checked for well-formedness only (RFC 5646 §2.2.9), and is copied to a string only if it is well-formed.
        /// </remarks>
        public static bool TryCreate(ReadOnlySpan<char> languageTag, out Bcp47LanguageTag result)
        {
            return TryCreateFromSpan(languageTag, Bcp47SyntaxSubtagClassifier.Instance, LanguageTagConformance.WellFormed, out result);
        }

        /// <summary>
        /// Create a <see cref="Bcp47LanguageTag"/> that is valid against the Subtag Registry (RFC 5646 §2.2.9) from a span of characters that contains a language tag.
        /// </summary>
        /// <param name="languageTag">An IETF BCP 47 language tag.</param>
        /// <param name="subtagRegistry">A copy of the Subtag Registry.</param>
        /// <param name="result">When this method returns, contains the constructed <see cref="Bcp47LanguageTag"/>.</param>
        /// <returns><c>true</c> if the <see cref="Bcp47LanguageTag"/> was successfully created; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The language tag is copied to a string only if it is valid.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryCreate(ReadOnlySpan<char> languageTag, SubtagRegistry.SubtagRegistry subtagRegistry, out Bcp47LanguageTag result)
#else
        public static bool TryCreate(ReadOnlySpan<char> languageTag, [NotNull] SubtagRegistry.SubtagRegistry subtagRegistry, out Bcp47LanguageTag result)
#endif
        {
            if (subtagRegistry is null)
            {
                result = default;
                return false;
            }

            return TryCreateFromSpan(languageTag, subtagRegistry.Classifier, LanguageTagConformance.Valid, out result);
        }

        /// <summary>
        /// Create a <see cref="Bcp47LanguageTag"/> from a span of characters that contains a language tag, with a choice of what is checked.
        /// </summary>
        /// <param name="languageTag">An IETF BCP 47 language tag.</param>
        /// <param name="conformance">What to check: <see cref="LanguageTagConformance.WellFormed"/> or <see cref="LanguageTagConformance.IsoCodeLists"/>.</param>
        /// <param name="result">When this method returns, contains the constructed <see cref="Bcp47LanguageTag"/>.</param>
        /// <returns><c>true</c> if the <see cref="Bcp47LanguageTag"/> was successfully created; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="conformance"/> is <see cref="LanguageTagConformance.Valid"/>, which needs a Subtag Registry.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="conformance"/> is not a defined value.</exception>
        /// <remarks>
        /// The language tag is copied to a string only if it satisfies <paramref name="conformance"/>.
        /// </remarks>
        public static bool TryCreate(ReadOnlySpan<char> languageTag, LanguageTagConformance conformance, out Bcp47LanguageTag result)
        {
            return TryCreateFromSpan(languageTag, GetClassifier(conformance), conformance, out result);
        }

#if NET7_0_OR_GREATER
        /// <summary>
        /// Create a language tag object.
        /// </summary>
        /// <param name="s">Language tag on which the object will be based.</param>
        /// <param name="provider">Ignored, as language tags do not depend on culture.</param>
        /// <returns>Language tag object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <c>null</c>.</exception>
        /// <exception cref="LanguageTagFormatException">The language tag is not well-formed.</exception>
        /// <remarks>
        /// The language tag is checked for well-formedness only (RFC 5646 §2.2.9), as by <see cref="Create(string)"/>.
        /// </remarks>
        public static Bcp47LanguageTag Parse(string s, IFormatProvider? provider)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));

            return Create(s);
        }

        /// <summary>
        /// Create a language tag object from a span of characters.
        /// </summary>
        /// <param name="s">Language tag on which the object will be based.</param>
        /// <param name="provider">Ignored, as language tags do not depend on culture.</param>
        /// <returns>Language tag object.</returns>
        /// <exception cref="LanguageTagFormatException">The language tag is not well-formed.</exception>
        /// <remarks>
        /// The language tag is checked for well-formedness only (RFC 5646 §2.2.9).
        /// </remarks>
        public static Bcp47LanguageTag Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            if (!TryCreate(s, out var result)) throw new LanguageTagFormatException(ErrorMessage.LanguageTagNotWellFormed, s.ToString());

            return result;
        }

        /// <summary>
        /// Create a <see cref="Bcp47LanguageTag"/> with the specified language tag.
        /// </summary>
        /// <param name="s">An IETF BCP 47 language tag.</param>
        /// <param name="provider">Ignored, as language tags do not depend on culture.</param>
        /// <param name="result">When this method returns, contains the constructed <see cref="Bcp47LanguageTag"/>.</param>
        /// <returns><c>true</c> if the <see cref="Bcp47LanguageTag"/> was successfully created; otherwise, <c>false</c>, including for <c>null</c>.</returns>
        /// <remarks>
        /// The language tag is checked for well-formedness only (RFC 5646 §2.2.9), as by <see cref="TryCreate(string, out Bcp47LanguageTag)"/>.
        /// </remarks>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Bcp47LanguageTag result)
        {
            return TryCreate(s, out result);
        }

        /// <summary>
        /// Create a <see cref="Bcp47LanguageTag"/> from a span of characters that contains a language tag.
        /// </summary>
        /// <param name="s">An IETF BCP 47 language tag.</param>
        /// <param name="provider">Ignored, as language tags do not depend on culture.</param>
        /// <param name="result">When this method returns, contains the constructed <see cref="Bcp47LanguageTag"/>.</param>
        /// <returns><c>true</c> if the <see cref="Bcp47LanguageTag"/> was successfully created; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The language tag is checked for well-formedness only (RFC 5646 §2.2.9), and is copied to a string only if it is well-formed.
        /// </remarks>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Bcp47LanguageTag result)
        {
            return TryCreate(s, out result);
        }
#endif

        #endregion

        #region Internal Static Methods

        /// <summary>
        /// Find the first variant subtag that repeats an earlier one, compared case-insensitively (ref. RFC 5646 §2.1.1, §2.2.5¶5).
        /// </summary>
        /// <returns>Index of the repeated subtag; -1 if every subtag is unique.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal static int IndexOfRepeatedVariantSubtag(string[] variantSubtags)
#else
        internal static int IndexOfRepeatedVariantSubtag([NotNull] string[] variantSubtags)
#endif
        {
            for (var index = 1; index < variantSubtags.Length; index++)
            {
                for (var earlierIndex = 0; earlierIndex < index; earlierIndex++)
                {
                    if (string.Equals(variantSubtags[index], variantSubtags[earlierIndex], StringComparison.OrdinalIgnoreCase)) return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Find the first extension whose singleton repeats an earlier one, compared case-insensitively (ref. RFC 5646 §2.1.1, §2.2.6¶3).
        /// </summary>
        /// <param name="extensionSubtags">Extensions, each a singleton followed by its subtags.</param>
        /// <returns>Index of the extension with the repeated singleton; -1 if every singleton is unique.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal static int IndexOfRepeatedExtensionSingleton(string[] extensionSubtags)
#else
        internal static int IndexOfRepeatedExtensionSingleton([NotNull] string[] extensionSubtags)
#endif
        {
            for (var index = 1; index < extensionSubtags.Length; index++)
            {
                for (var earlierIndex = 0; earlierIndex < index; earlierIndex++)
                {
                    if (char.ToLowerInvariant(extensionSubtags[index][0]) == char.ToLowerInvariant(extensionSubtags[earlierIndex][0])) return index;
                }
            }

            return -1;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets what was checked when the language tag was created.
        /// </summary>
        /// <remarks>
        /// This is <see cref="LanguageTagConformance.WellFormed"/> unless the language tag was created with a Subtag Registry or a choice of conformance.
        /// It never changes: <see cref="IsValid(SubtagRegistry.SubtagRegistry)"/> reports a result without changing it.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
        public LanguageTagConformance Conformance
        {
            get
            {
                if (_languageTag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                return Parts.Conformance;
            }
        }

        /// <summary>
        /// Gets the Extended Language Subtags of the BCP 47 language tag.
        /// </summary>
        /// <remarks>
        /// The list is read-only, and every read returns the same list without copying it.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public IReadOnlyList<string> ExtendedLanguageSubtags
#else
        [NotNull]
        public IReadOnlyList<string> ExtendedLanguageSubtags
#endif
        {
            get
            {
                if (_languageTag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                return Parts.ExtendedLanguages;
            }
        }

        /// <summary>
        /// Gets the Extension Subtags of the BCP 47 language tag.
        /// </summary>
        /// <remarks>
        /// The list is read-only, and every read returns the same list without copying it.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public IReadOnlyList<Bcp47KeyedSubtag> ExtensionSubtags
#else
        [NotNull]
        public IReadOnlyList<Bcp47KeyedSubtag> ExtensionSubtags
#endif
        {
            get
            {
                if (_languageTag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                return Parts.Extensions;
            }
        }

        /// <summary>
        /// Gets the Primary Language Subtag of the BCP 47 language tag.
        /// </summary>
        /// <remarks>
        /// <c>null</c> for a grandfathered language tag, which is interpreted as a whole and has no subtags (RFC 5646 §2.2.8), and for a private use language tag (§2.2.7).
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public string? PrimaryLanguageSubtag
#else
        [CanBeNull]
        public string PrimaryLanguageSubtag
#endif
        {
            get
            {
                if (_languageTag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                return Parts.PrimaryLanguage;
            }
        }

        /// <summary>
        /// Gets the Private Use Subtag of the BCP 47 language tag.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
        public Bcp47KeyedSubtag? PrivateUseSubtag
        {
            get
            {
                if (_languageTag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                return Parts.PrivateUse;
            }
        }

        /// <summary>
        /// Gets the Region Subtag of the BCP 47 language tag.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public string? RegionSubtag
#else
        [CanBeNull]
        public string RegionSubtag
#endif
        {
            get
            {
                if (_languageTag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                return Parts.Region;
            }
        }

        /// <summary>
        /// Gets the Script Subtag of the BCP 47 language tag.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public string? ScriptSubtag
#else
        [CanBeNull]
        public string ScriptSubtag
#endif
        {
            get
            {
                if (_languageTag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                return Parts.Script;
            }
        }

        /// <summary>
        /// Gets the Variant Subtags of the BCP 47 language tag.
        /// </summary>
        /// <remarks>
        /// The list is read-only, and every read returns the same list without copying it.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public IReadOnlyList<string> VariantSubtags
#else
        [NotNull]
        public IReadOnlyList<string> VariantSubtags
#endif
        {
            get
            {
                if (_languageTag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                return Parts.Variants;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get the classifier for a conformance that needs no Subtag Registry.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        private static Bcp47SubtagClassifier GetClassifier(LanguageTagConformance conformance)
#else
        [NotNull]
        private static Bcp47SubtagClassifier GetClassifier(LanguageTagConformance conformance)
#endif
        {
            switch (conformance)
            {
                case LanguageTagConformance.WellFormed:
                    return Bcp47SyntaxSubtagClassifier.Instance;
                case LanguageTagConformance.IsoCodeLists:
                    return Bcp47RulesBasedSubtagClassifier.Instance;
                case LanguageTagConformance.Valid:
                    throw new ArgumentException(ErrorMessage.ConformanceValidNeedsRegistry, nameof(conformance));
                default:
                    throw new ArgumentOutOfRangeException(nameof(conformance), conformance, null);
            }
        }

        /// <summary>
        /// Determine whether a parse outcome satisfies a conformance: only a well-formed language tag may repeat a variant subtag or an extension singleton (ref. RFC 5646 §2.2.9).
        /// </summary>
        private static bool Satisfies(Bcp47ParseStatus status, LanguageTagConformance conformance)
        {
            return status == Bcp47ParseStatus.Accepted || (status == Bcp47ParseStatus.RepeatedSubtag && conformance == LanguageTagConformance.WellFormed);
        }

        /// <summary>
        /// Parse a language tag with a classifier.
        /// </summary>
        /// <returns>The subtags; <c>null</c> if the language tag does not satisfy the conformance.</returns>
#if NETCOREAPP3_0_OR_GREATER
        private static Bcp47LanguageTagParts? ParseToParts(string languageTag, Bcp47SubtagClassifier classifier, LanguageTagConformance conformance)
#else
        [CanBeNull]
        private static Bcp47LanguageTagParts ParseToParts([NotNull] string languageTag, [NotNull] Bcp47SubtagClassifier classifier, LanguageTagConformance conformance)
#endif
        {
            if (!Satisfies(Bcp47LanguageTagParser.Parse(languageTag.AsSpan(), classifier, out var ranges), conformance)) return null;

            return Bcp47LanguageTagParts.FromRanges(languageTag, ranges, conformance);
        }

        /// <summary>
        /// Parse a span of characters with a classifier, and copy it to a string only if it satisfies the conformance.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        private static bool TryCreateFromSpan(ReadOnlySpan<char> languageTag, Bcp47SubtagClassifier classifier, LanguageTagConformance conformance, out Bcp47LanguageTag result)
#else
        private static bool TryCreateFromSpan(ReadOnlySpan<char> languageTag, [NotNull] Bcp47SubtagClassifier classifier, LanguageTagConformance conformance, out Bcp47LanguageTag result)
#endif
        {
            if (!Satisfies(Bcp47LanguageTagParser.Parse(languageTag, classifier, out var ranges), conformance))
            {
                result = default;
                return false;
            }

            // The ranges are positions in the span, so they apply equally to its copy.
            var languageTagString = languageTag.ToString();
            result = new Bcp47LanguageTag(languageTagString, Bcp47LanguageTagParts.FromRanges(languageTagString, ranges, conformance));
            return true;
        }

        /// <summary>
        /// Gets the subtags of an initialised language tag, decomposed when it was created.
        /// </summary>
        private Bcp47LanguageTagParts Parts
        {
            get
            {
                Debug.Assert(_parts != null);

                return _parts;
            }
        }

        #endregion
    }
}