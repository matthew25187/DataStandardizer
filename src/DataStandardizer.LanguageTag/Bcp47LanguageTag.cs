#if NETSTANDARD
using JetBrains.Annotations;
#endif
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
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
    {
        #region Declarations

        // constant declarations
        internal static class ErrorMessage
        {
            internal const string LanguageTagInvalidFormat = "The language tag is not in a valid format.";
            internal const string LanguageTagNull = "The language tag must not be null.";
            internal const string UninitializedInstance = "The " + nameof(Bcp47LanguageTag) + " instance is not initialised.";
        }

        /// <summary>
        /// Parameter name of the <see cref="ArgumentNullException"/> that the static validation methods throw for <c>null</c>.
        /// </summary>
        /// <remarks>
        /// D12 (#245): it is the name the regular expressions reported before the parser replaced them; #245 corrects it.
        /// </remarks>
        private const string NullInputParameterName = "input";

        /// <summary>
        /// The longest match timeout that <see cref="Regex"/> accepts.
        /// </summary>
        private static readonly TimeSpan MaximumMatchTimeout = TimeSpan.FromMilliseconds(int.MaxValue - 1);

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
        /// Validation will be performed using the default BCP 47 rules.
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
                throw new InvalidCastException(ErrorMessage.LanguageTagInvalidFormat);
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
        /// <exception cref="LanguageTagFormatException">The original language tag was not recognised.</exception>
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

            var parts = ParseByRules(languageTag);
            if (parts is null)
            {
                throw new LanguageTagFormatException(ErrorMessage.LanguageTagInvalidFormat, languageTag);
            }

            return new Bcp47LanguageTag(languageTag, parts);
        }

        /// <summary>
        /// Create a language tag object with timeout for the regular expressions.
        /// </summary>
        /// <param name="languageTag">Language tag on which the object will be based.</param>
        /// <param name="matchTimeout">Time limit for evaluating the regular expressions.</param>
        /// <returns>Language tag object.</returns>
        /// <exception cref="LanguageTagFormatException">The original language tag was not recognised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static Bcp47LanguageTag Create(string languageTag, TimeSpan matchTimeout)
#else
        public static Bcp47LanguageTag Create([NotNull] string languageTag, TimeSpan matchTimeout)
#endif
        {
            if (languageTag is null)
            {
                throw new ArgumentNullException(nameof(languageTag));
            }

            ValidateMatchTimeout(matchTimeout);

            var parts = ParseByRules(languageTag);
            if (parts is null)
            {
                throw new LanguageTagFormatException(ErrorMessage.LanguageTagInvalidFormat, languageTag);
            }

            return new Bcp47LanguageTag(languageTag, parts);
        }

        /// <summary>
        /// Create a language tag object based on the Subtag Registry.
        /// </summary>
        /// <param name="languageTag">Language tag on which the object will be based.</param>
        /// <param name="subtagRegistry">A copy of the IANA Subtag Registry.</param>
        /// <returns>Language tag object.</returns>
        /// <exception cref="LanguageTagFormatException">The original language tag was not recognised.</exception>
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

            var parts = Bcp47LanguageTagParts.FromMatch(GetRegistryExpression(subtagRegistry).Match(languageTag));
            if (parts is null)
            {
                throw new LanguageTagFormatException(ErrorMessage.LanguageTagInvalidFormat, languageTag);
            }

            return new Bcp47LanguageTag(languageTag, parts);
        }

        /// <summary>
        /// Create a language tag object based on the Subtag Registry and with a timeout for the regular expressions.
        /// </summary>
        /// <param name="languageTag">Language tag on which the object will be based.</param>
        /// <param name="subtagRegistry">A copy of the IANA Subtag Registry.</param>
        /// <param name="matchTimeout">Time limit for evaluating the regular expressions.</param>
        /// <returns>Language tag object.</returns>
        /// <exception cref="LanguageTagFormatException">The original language tag was not recognised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static Bcp47LanguageTag Create(string languageTag, SubtagRegistry.SubtagRegistry subtagRegistry, TimeSpan matchTimeout)
#else
        public static Bcp47LanguageTag Create([NotNull] string languageTag, [NotNull] SubtagRegistry.SubtagRegistry subtagRegistry, TimeSpan matchTimeout)
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

            var parts = Bcp47LanguageTagParts.FromMatch(GetRegistryExpression(subtagRegistry).Match(languageTag, matchTimeout));
            if (parts is null)
            {
                throw new LanguageTagFormatException(ErrorMessage.LanguageTagInvalidFormat, languageTag);
            }

            return new Bcp47LanguageTag(languageTag, parts);
        }

        public bool Equals(Bcp47LanguageTag other)
        {
            return string.Equals(_languageTag, other._languageTag, StringComparison.OrdinalIgnoreCase);
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
        /// <returns>A <see cref="Iso639Part1Language"/> enum representing the Primary Language subtag, if compatible; <c>null</c> if the Primary Language subtag is not an ISO 639-1 code.</returns>
        public Iso639Part1Language? ToIso639Part1()
        {
            if (!StringEnum.TryParse<Iso639Part1Language>(PrimaryLanguageSubtag, true, out var result)) return null;
            return result;

        }

        /// <summary>
        /// Convert the Primary Language subtag of the language tag to a <see cref="Iso639Part2TLanguage"/> enum.
        /// </summary>
        /// <returns>A <see cref="Iso639Part2TLanguage"/> enum representing the Primary Language subtag, if compatible; <c>null</c> if the Primary Language subtag is not an ISO 639-2/T code.</returns>
        public Iso639Part2TLanguage? ToIso639Part2T()
        {
            if (!StringEnum.TryParse<Iso639Part2TLanguage>(PrimaryLanguageSubtag, true, out var result)) return null;
            return result;

        }

        /// <summary>
        /// Convert the Primary Language subtag of the language tag to a <see cref="Iso639Part3Language"/> enum.
        /// </summary>
        /// <returns>A <see cref="Iso639Part3Language"/> enum representing the Primary Language subtag, if compatible; <c>null</c> if the Primary Language subtag is not an ISO 639-3 code.</returns>
        public Iso639Part3Language? ToIso639Part3()
        {
            if (!StringEnum.TryParse<Iso639Part3Language>(PrimaryLanguageSubtag, true, out var result)) return null;
            return result;

        }

        /// <summary>
        /// Convert the Primary Language subtag of the language tag to a <see cref="Iso639Part5LanguageFamily"/> enum.
        /// </summary>
        /// <returns>A <see cref="Iso639Part5LanguageFamily"/> enum representing the Primary Language subtag, if compatible; <c>null</c> if the Primary Language subtag is not an ISO 639-5 code.</returns>
        public Iso639Part5LanguageFamily? ToIso639Part5()
        {
            if (!StringEnum.TryParse<Iso639Part5LanguageFamily>(PrimaryLanguageSubtag, true, out var result)) return null;
            return result;

        }

        public override string ToString()
        {
            return _languageTag ?? base.ToString() ?? GetType().FullName ?? GetType().Name;
        }

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
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckExtendedLanguageSubtag(string extendedLanguageSubtag)
#else
        public static bool CheckExtendedLanguageSubtag([NotNull] string extendedLanguageSubtag)
#endif
        {
            if (extendedLanguageSubtag is null) throw new ArgumentNullException(NullInputParameterName); // D12 (#245)

            return Bcp47RulesBasedSubtagClassifier.Instance.IsExtendedLanguage(extendedLanguageSubtag.AsSpan());
        }

        /// <summary>
        /// Determine if a string contains a valid Extension subtag.
        /// </summary>
        /// <param name="extensionSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
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
            if (extensionSubtag is null) throw new ArgumentNullException(NullInputParameterName); // D12 (#245)

            return Bcp47LanguageTagParser.IsExtension(extensionSubtag.AsSpan());
        }

        /// <summary>
        /// Determine if a string contains a valid Primary Language subtag.
        /// </summary>
        /// <param name="primaryLanguageSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckPrimaryLanguageSubtag(string primaryLanguageSubtag)
#else
        public static bool CheckPrimaryLanguageSubtag([NotNull] string primaryLanguageSubtag)
#endif
        {
            if (primaryLanguageSubtag is null) throw new ArgumentNullException(NullInputParameterName); // D12 (#245)

            return Bcp47RulesBasedSubtagClassifier.Instance.IsPrimaryLanguage(primaryLanguageSubtag.AsSpan(), out _);
        }

        /// <summary>
        /// Determine if a string contains a valid Private Use subtag.
        /// </summary>
        /// <param name="privateUseSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckPrivateUseSubtag(string privateUseSubtag)
#else
        public static bool CheckPrivateUseSubtag([NotNull] string privateUseSubtag)
#endif
        {
            if (privateUseSubtag is null) throw new ArgumentNullException(NullInputParameterName); // D12 (#245)

            return Bcp47LanguageTagParser.IsPrivateUse(privateUseSubtag.AsSpan());
        }

        /// <summary>
        /// Determine if a string contains a valid Region subtag.
        /// </summary>
        /// <param name="regionSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckRegionSubtag(string regionSubtag)
#else
        public static bool CheckRegionSubtag([NotNull] string regionSubtag)
#endif
        {
            if (regionSubtag is null) throw new ArgumentNullException(NullInputParameterName); // D12 (#245)

            return Bcp47RulesBasedSubtagClassifier.Instance.IsRegion(regionSubtag.AsSpan());
        }

        /// <summary>
        /// Determine if a string contains a valid Script subtag.
        /// </summary>
        /// <param name="scriptSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckScriptSubtag(string scriptSubtag)
#else
        public static bool CheckScriptSubtag([NotNull] string scriptSubtag)
#endif
        {
            if (scriptSubtag is null) throw new ArgumentNullException(NullInputParameterName); // D12 (#245)

            return Bcp47RulesBasedSubtagClassifier.Instance.IsScript(scriptSubtag.AsSpan());
        }

        /// <summary>
        /// Determine if a string contains a valid Variant subtag.
        /// </summary>
        /// <param name="variantSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
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
            if (variantSubtag is null) throw new ArgumentNullException(NullInputParameterName); // D12 (#245)

            return Bcp47RulesBasedSubtagClassifier.Instance.IsVariant(variantSubtag.AsSpan());
        }

        /// <summary>
        /// Determine if a string contains a valid BCP 47 language tag.
        /// </summary>
        /// <param name="languageTagString">Language tag to test.</param>
        /// <returns><c>true</c> if the language tag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules. A language tag that repeats a variant subtag or an extension singleton,
        /// compared case-insensitively, is not well-formed (RFC 5646 §2.2.5, §2.2.6). The 26 grandfathered language tags are well-formed (§2.1, §2.2.9).
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool IsWellFormedLanguageTagString(string languageTagString)
#else
        public static bool IsWellFormedLanguageTagString([NotNull] string languageTagString)
#endif
        {
            if (languageTagString is null) throw new ArgumentNullException(NullInputParameterName); // D12 (#245)

            return IsWellFormedByRules(languageTagString);
        }

        /// <summary>
        /// Create a <see cref="Bcp47LanguageTag"/> with the specified language tag.
        /// </summary>
        /// <param name="languageTag">An IETF BCP 47 language tag.</param>
        /// <param name="result">When this method returns, contains the constructed <see cref="Bcp47LanguageTag"/>.</param>
        /// <returns><c>true</c> if the <see cref="Bcp47LanguageTag"/> was successfully created; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
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

            var parts = ParseByRules(languageTag);
            if (parts is null)
            {
                result = default;
                return false;
            }

            result = new Bcp47LanguageTag(languageTag, parts);
            return true;
        }

        /// <summary>
        /// Create a <see cref="Bcp47LanguageTag"/> with the specified language tag and timeout.
        /// </summary>
        /// <param name="languageTag">An IETF BCP 47 language tag.</param>
        /// <param name="matchTimeout">Time limit for evaluating the regular expressions.</param>
        /// <param name="result">When this method returns, contains the constructed <see cref="Bcp47LanguageTag"/>.</param>
        /// <returns><c>true</c> if the <see cref="Bcp47LanguageTag"/> was successfully created; otherwise, <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryCreate(string? languageTag, TimeSpan matchTimeout, out Bcp47LanguageTag result)
#else
        public static bool TryCreate([CanBeNull] string languageTag, TimeSpan matchTimeout, out Bcp47LanguageTag result)
#endif
        {
            if (languageTag is null)
            {
                result = default;
                return false;
            }

            ValidateMatchTimeout(matchTimeout);

            var parts = ParseByRules(languageTag);
            if (parts is null)
            {
                result = default;
                return false;
            }

            result = new Bcp47LanguageTag(languageTag, parts);
            return true;
        }

        /// <summary>
        /// Create a <see cref="Bcp47LanguageTag"/> based on the Subtag Registry with the specified language tag.
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

            var parts = Bcp47LanguageTagParts.FromMatch(GetRegistryExpression(subtagRegistry).Match(languageTag));
            if (parts is null)
            {
                result = default;
                return false;
            }

            result = new Bcp47LanguageTag(languageTag, parts);
            return true;
        }

        /// <summary>
        /// Create a <see cref="Bcp47LanguageTag"/> based on the Subtag Registry with the specified language tag and match timeout.
        /// </summary>
        /// <param name="languageTag">An IETF BCP 47 language tag.</param>
        /// <param name="subtagRegistry">A copy of the Subtag Registry.</param>
        /// <param name="matchTimeout">Time limit for evaluating the regular expressions.</param>
        /// <param name="result">When this method returns, contains the constructed <see cref="Bcp47LanguageTag"/>.</param>
        /// <returns><c>true</c> if the <see cref="Bcp47LanguageTag"/> was successfully created; otherwise, <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryCreate(string? languageTag, SubtagRegistry.SubtagRegistry subtagRegistry, TimeSpan matchTimeout, out Bcp47LanguageTag result)
#else
        public static bool TryCreate([CanBeNull] string languageTag, [NotNull] SubtagRegistry.SubtagRegistry subtagRegistry, TimeSpan matchTimeout, out Bcp47LanguageTag result)
#endif
        {
            if (languageTag is null || subtagRegistry is null)
            {
                result = default;
                return false;
            }

            Match languageTagMatch;
            try
            {
                languageTagMatch = GetRegistryExpression(subtagRegistry).Match(languageTag, matchTimeout);
            }
            catch (RegexMatchTimeoutException)
            {
                result = default;
                return false;
            }

            var parts = Bcp47LanguageTagParts.FromMatch(languageTagMatch);
            if (parts is null)
            {
                result = default;
                return false;
            }

            result = new Bcp47LanguageTag(languageTag, parts);
            return true;
        }

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
        /// Gets the Extended Language Subtags of the BCP 47 language tag.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public string[] ExtendedLanguageSubtags
#else
        [NotNull]
        public string[] ExtendedLanguageSubtags
#endif
        {
            get
            {
                if (_languageTag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                return Bcp47LanguageTagParts.Copy(Parts.ExtendedLanguages);
            }
        }

        /// <summary>
        /// Gets the Extension Subtags of the BCP 47 language tag.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public Bcp47KeyedSubtag[] ExtensionSubtags
#else
        [NotNull]
        public Bcp47KeyedSubtag[] ExtensionSubtags
#endif
        {
            get
            {
                if (_languageTag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                return Bcp47LanguageTagParts.Copy(Parts.Extensions);
            }
        }

        /// <summary>
        /// Gets the Primary Language Subtag of the BCP 47 language tag.
        /// </summary>
        /// <remarks>
        /// The empty string for a grandfathered language tag, which is interpreted as a whole and has no subtags (RFC 5646 §2.2.8), and for a private use language tag.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public string PrimaryLanguageSubtag
#else
        [NotNull]
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
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47LanguageTag"/> is not initialised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public string[] VariantSubtags
#else
        [NotNull]
        public string[] VariantSubtags
#endif
        {
            get
            {
                if (_languageTag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                return Bcp47LanguageTagParts.Copy(Parts.Variants);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Parse a language tag by the default BCP 47 rules.
        /// </summary>
        /// <returns>The subtags; <c>null</c> if the language tag is not well-formed.</returns>
#if NETCOREAPP3_0_OR_GREATER
        private static Bcp47LanguageTagParts? ParseByRules(string languageTag)
#else
        [CanBeNull]
        private static Bcp47LanguageTagParts ParseByRules([NotNull] string languageTag)
#endif
        {
            if (Bcp47LanguageTagParser.Parse(languageTag.AsSpan(), Bcp47RulesBasedSubtagClassifier.Instance, out var ranges) != Bcp47ParseStatus.WellFormed) return null;

            return Bcp47LanguageTagParts.FromRanges(languageTag, ranges);
        }

        /// <summary>
        /// Determine whether a string is a well-formed language tag by the default BCP 47 rules.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        private static bool IsWellFormedByRules(string languageTag)
#else
        private static bool IsWellFormedByRules([NotNull] string languageTag)
#endif
        {
            return Bcp47LanguageTagParser.Parse(languageTag.AsSpan(), Bcp47RulesBasedSubtagClassifier.Instance, out _) == Bcp47ParseStatus.WellFormed;
        }

        /// <summary>
        /// Gets the expression that matches a language tag against a Subtag Registry.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        private static IBcp47TagExpression GetRegistryExpression(SubtagRegistry.SubtagRegistry subtagRegistry)
#else
        [NotNull]
        private static IBcp47TagExpression GetRegistryExpression([NotNull] SubtagRegistry.SubtagRegistry subtagRegistry)
#endif
        {
            return new Bcp47TagExpression<Bcp47RegistryBasedLanguageTagExpressionFactory>(subtagRegistry);
        }

        /// <summary>
        /// Validate a match timeout as the regular expressions did.
        /// </summary>
        /// <remarks>
        /// The default BCP 47 rules are checked by a parser, which cannot time out, so the timeout is otherwise unused (#236).
        /// </remarks>
        private static void ValidateMatchTimeout(TimeSpan matchTimeout)
        {
            if (matchTimeout == Regex.InfiniteMatchTimeout) return;

            if (matchTimeout <= TimeSpan.Zero || matchTimeout > MaximumMatchTimeout) throw new ArgumentOutOfRangeException(nameof(matchTimeout));
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