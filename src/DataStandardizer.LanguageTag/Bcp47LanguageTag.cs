#if NETSTANDARD
using JetBrains.Annotations;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DataStandardizer.Core;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using DataStandardizer.LanguageTag.InternalState;
using static DataStandardizer.LanguageTag.Bcp47Constants;
using UnM49 = DataStandardizer.Geography.UnM49AreaByAlpha2CountryCode;

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
            internal const string UninitializedInstance = "The " + nameof(Bcp47LanguageTag) + " instance is not initialised.";
        }

        // global (static) declarations
        private static readonly Bcp47LanguageTagDefaultState DefaultState = new Bcp47LanguageTagDefaultState();
        private static readonly Bcp47LanguageTagContext GlobalContext;

        // local (instance) declarations
        private readonly Bcp47LanguageTagContext _context;
#if NETCOREAPP3_0_OR_GREATER
        private readonly string? _languageTag;
#else
        [CanBeNull] private readonly string _languageTag;
#endif

        #endregion

        #region Constructors

        static Bcp47LanguageTag()
        {
            var states = new Dictionary<string, IBcp47LanguageTagState> { { LanguageTagStateName.Default, DefaultState } };
            GlobalContext = new Bcp47LanguageTagContext(states);
        }

#if NETCOREAPP3_0_OR_GREATER
        private Bcp47LanguageTag(string languageTag)
#else
        private Bcp47LanguageTag([NotNull] string languageTag)
#endif
        {
            _languageTag = languageTag;

            var states = new Dictionary<string, IBcp47LanguageTagState> { { LanguageTagStateName.Default, DefaultState } };
            _context = new Bcp47LanguageTagContext(states);
        }

#if NETCOREAPP3_0_OR_GREATER
        private Bcp47LanguageTag(string languageTag, SubtagRegistry.SubtagRegistry subtagRegistry)
#else
        private Bcp47LanguageTag([NotNull] string languageTag, [NotNull] SubtagRegistry.SubtagRegistry subtagRegistry)
#endif
        {
            _languageTag = languageTag;

            var states = new Dictionary<string, IBcp47LanguageTagState>
            {
                { LanguageTagStateName.Default, DefaultState },
                { LanguageTagStateName.Registry, new Bcp47LanguageTagRegistryState(subtagRegistry) }
            };
            _context = new Bcp47LanguageTagContext(states);
        }

        #endregion

        #region Operators

#if NETCOREAPP3_0_OR_GREATER
        public static explicit operator Bcp47LanguageTag(string languageTag)
#else
        public static explicit operator Bcp47LanguageTag([NotNull] string languageTag)
#endif
        {
            return new Bcp47LanguageTag(languageTag);
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

            var result = new Bcp47LanguageTag(languageTag);
            result._context.SelectState(LanguageTagStateName.Default);

            if (!result._context.LanguageTagExpression.IsMatch(languageTag))
            {
                throw new LanguageTagFormatException(ErrorMessage.LanguageTagInvalidFormat, languageTag);
            }

            return result;
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

            var result = new Bcp47LanguageTag(languageTag);
            result._context.SelectState(LanguageTagStateName.Default);

            if (!result._context.LanguageTagExpression.IsMatch(languageTag, matchTimeout))
            {
                throw new LanguageTagFormatException(ErrorMessage.LanguageTagInvalidFormat, languageTag);
            }

            return result;
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

            var result = new Bcp47LanguageTag(languageTag, subtagRegistry);
            result._context.SelectState(LanguageTagStateName.Registry);

            if (!result._context.LanguageTagExpression.IsMatch(languageTag))
            {
                throw new LanguageTagFormatException(ErrorMessage.LanguageTagInvalidFormat, languageTag);
            }

            return result;
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

            var result = new Bcp47LanguageTag(languageTag, subtagRegistry);
            result._context.SelectState(LanguageTagStateName.Registry);

            if (!result._context.LanguageTagExpression.IsMatch(languageTag, matchTimeout))
            {
                throw new LanguageTagFormatException(ErrorMessage.LanguageTagInvalidFormat, languageTag);
            }

            return result;
        }

        public bool Equals(Bcp47LanguageTag other)
        {
            return string.Equals(_languageTag, other._languageTag, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return (_languageTag != null ? _languageTag.GetHashCode() : 0);
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
            if (!Enum.TryParse<Iso3166Part1Alpha2Country>(RegionSubtag, true, out var result)) return null;
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
            var m49Codes = UnM49Extensions.GetM49Codes(typeof(UnM49));
            if (ushort.TryParse(RegionSubtag, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unM49CandidateCodeResult) && m49Codes.Contains(unM49CandidateCodeResult))
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
            return GlobalContext.ExtendedLanguageSubtagExpression.IsMatch(extendedLanguageSubtag);
        }

        /// <summary>
        /// Determine if a string contains a valid Extension subtag.
        /// </summary>
        /// <param name="extensionSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckExtensionSubtag(string extensionSubtag)
#else
        public static bool CheckExtensionSubtag([NotNull] string extensionSubtag)
#endif
        {
            var extensionSubtagMatch = GlobalContext.ExtensionSubtagExpression.Match(extensionSubtag);
            if (extensionSubtagMatch.Success)
            {
                var extensionSubtagGroup = extensionSubtagMatch.Groups[LanguageTagSubtagGroupName.Extension];
                if (extensionSubtagGroup.Success && !string.IsNullOrEmpty(extensionSubtagGroup.Value))
                {
                    var extensionSubtags = extensionSubtagGroup.Captures
                        .Cast<Capture>()
                        .ToLookup(capture => capture.Value.Split('-').ElementAtOrDefault(0), capture => capture.Value.Split('-').Skip(1));
                    if (extensionSubtags.Any(subtag => subtag.Count() > 1))
                    {
                        // Duplicate extensions detected; extension subtags must be unique (ref. RFC 5646 §2.2.6¶3).
                        return false;
                    }
                }
            }

            return extensionSubtagMatch.Success;
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
            return GlobalContext.PrimaryLanguageSubtagExpression.IsMatch(primaryLanguageSubtag);
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
            return GlobalContext.PrivateUseSubtagExpression.IsMatch(privateUseSubtag);
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
            return GlobalContext.RegionSubtagExpression.IsMatch(regionSubtag);
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
            return GlobalContext.ScriptSubtagExpression.IsMatch(scriptSubtag);
        }

        /// <summary>
        /// Determine if a string contains a valid Variant subtag.
        /// </summary>
        /// <param name="variantSubtag">Subtag to check.</param>
        /// <returns><c>true</c> if the subtag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool CheckVariantSubtag(string variantSubtag)
#else
        public static bool CheckVariantSubtag([NotNull] string variantSubtag)
#endif
        {
            var variantSubtagMatch = GlobalContext.VariantSubtagExpression.Match(variantSubtag);
            if (variantSubtagMatch?.Success ?? false)
            {
                var variantSubtagGroup = variantSubtagMatch.Groups[LanguageTagSubtagGroupName.Variant];
                if (variantSubtagGroup.Success && !string.IsNullOrEmpty(variantSubtagGroup.Value))
                {
                    var variantSubtags = variantSubtagGroup.Captures.Cast<Capture>().Select(capture => capture.Value).ToArray();
                    if (variantSubtags.Length > variantSubtags.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                    {
                        // Duplicate subtags detected; variant subtags must be unique (ref. RFC 5646 §2.2.5¶5).
                        return false;
                    }
                }
            }

            return variantSubtagMatch?.Success ?? false;
        }

        /// <summary>
        /// Determine if a string contains a valid BCP 47 language tag.
        /// </summary>
        /// <param name="languageTagString">Language tag to test.</param>
        /// <returns><c>true</c> if the language tag is valid; <c>false</c> if not.</returns>
        /// <remarks>
        /// Validation will be performed using the default BCP 47 rules.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool IsWellFormedLanguageTagString(string languageTagString)
#else
        public static bool IsWellFormedLanguageTagString([NotNull] string languageTagString)
#endif
        {
            var languageTagMatch = GlobalContext.LanguageTagExpression.Match(languageTagString);
            var isValidLanguageTag = languageTagMatch?.Success ?? false;

            if (languageTagMatch?.Groups[LanguageTagSubtagGroupName.Variant] is Group variantSubtagGroup)
            {
                if (variantSubtagGroup.Success && !string.IsNullOrEmpty(variantSubtagGroup.Value) && !CheckVariantSubtag(variantSubtagGroup.Value))
                {
                    isValidLanguageTag = false;
                }
            }

            if (languageTagMatch?.Groups[LanguageTagSubtagGroupName.Extension] is Group extensionSubtagGroup)
            {
                if (extensionSubtagGroup.Success && !string.IsNullOrEmpty(extensionSubtagGroup.Value) && !CheckExtensionSubtag(extensionSubtagGroup.Value))
                {
                    isValidLanguageTag = false;
                }
            }

            return isValidLanguageTag;
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

            result = new Bcp47LanguageTag(languageTag);
            result._context.SelectState(LanguageTagStateName.Default);

            if (!result._context.LanguageTagExpression.IsMatch(languageTag))
            {
                result = default;
                return false;
            }

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

            result = new Bcp47LanguageTag(languageTag);
            result._context.SelectState(LanguageTagStateName.Default);

            if (!result._context.LanguageTagExpression.IsMatch(languageTag, matchTimeout))
            {
                result = default;
                return false;
            }

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

            result = new Bcp47LanguageTag(languageTag, subtagRegistry);
            result._context.SelectState(LanguageTagStateName.Registry);

            if (!result._context.LanguageTagExpression.IsMatch(languageTag))
            {
                result = default;
                return false;
            }

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
            if (languageTag is null || subtagRegistry is null || !IsWellFormedLanguageTagString(languageTag))
            {
                result = default;
                return false;
            }

            result = new Bcp47LanguageTag(languageTag, subtagRegistry);
            result._context.SelectState(LanguageTagStateName.Registry);

            if (!result._context.LanguageTagExpression.IsMatch(languageTag, matchTimeout))
            {
                result = default;
                return false;
            }

            return true;
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

                return DoGetExtendedLanguageSubtags();
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

                return DoGetExtensionSubtags();
            }
        }

        /// <summary>
        /// Gets the Primary Language Subtag of the BCP 47 language tag.
        /// </summary>
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

                return DoGetPrimaryLanguageSubtag();
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

                return DoGetPrivateUseSubtag();
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

                return DoGetRegionSubtag();
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

                return DoGetScriptSubtag();
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

                return DoGetVariantSubtags();
            }
        }

        #endregion

        #region Private Methods

#if NETCOREAPP3_0_OR_GREATER
        private string[] DoGetExtendedLanguageSubtags()
#else
        [NotNull]
        private string[] DoGetExtendedLanguageSubtags()
#endif
        {
            Debug.Assert(_languageTag != null);

            var languageTagMatch = _context.LanguageTagExpression.Match(_languageTag);
            Debug.Assert(languageTagMatch.Success);

            var extendedLanguageSubtagGroup = languageTagMatch.Groups[LanguageTagSubtagGroupName.ExtendedLanguage];
            if (extendedLanguageSubtagGroup.Success)
            {
                return extendedLanguageSubtagGroup.Captures.Cast<Capture>().Select(capture => capture.Value).ToArray();
            }

#if NETSTANDARD1_3_OR_GREATER || NET
            return Array.Empty<string>();
#else
            return new string[] { };
#endif
        }

        private Bcp47KeyedSubtag[] DoGetExtensionSubtags()
        {
            Debug.Assert(_languageTag != null);

            var languageTagMatch = _context.LanguageTagExpression.Match(_languageTag);
            Debug.Assert(languageTagMatch.Success);

            var extensionSubtagGroup = languageTagMatch.Groups[LanguageTagSubtagGroupName.Extension];
            if (extensionSubtagGroup.Success)
            {
                return extensionSubtagGroup.Captures.Cast<Capture>().Select(capture => new Bcp47KeyedSubtag(capture.Value)).ToArray();
            }

#if NETSTANDARD1_3_OR_GREATER
            return Array.Empty<Bcp47KeyedSubtag>();
#else
            return new Bcp47KeyedSubtag[] { };
#endif
        }

#if NETCOREAPP3_0_OR_GREATER
        private string DoGetPrimaryLanguageSubtag()
#else
        [NotNull]
        private string DoGetPrimaryLanguageSubtag()
#endif
        {
            Debug.Assert(_languageTag != null);

            var languageTagMatch = _context.LanguageTagExpression.Match(_languageTag);
            Debug.Assert(languageTagMatch.Success);

            var primaryLanguageSubtagGroup = languageTagMatch.Groups[LanguageTagSubtagGroupName.PrimaryLanguage];
            Debug.Assert(primaryLanguageSubtagGroup.Success);

            return primaryLanguageSubtagGroup.Value;
        }

        private Bcp47KeyedSubtag? DoGetPrivateUseSubtag()
        {
            Debug.Assert(_languageTag != null);

            var languageTagMatch = _context.LanguageTagExpression.Match(_languageTag);
            Debug.Assert(languageTagMatch.Success);

            var privateUseSubtagGroup = languageTagMatch.Groups[LanguageTagSubtagGroupName.PrivateUse];
            if (privateUseSubtagGroup.Success)
            {
                return new Bcp47KeyedSubtag(privateUseSubtagGroup.Value);
            }

            return null;
        }

#if NETCOREAPP3_0_OR_GREATER
        private string? DoGetRegionSubtag()
#else
        [CanBeNull]
        private string DoGetRegionSubtag()
#endif
        {
            Debug.Assert(_languageTag != null);

            var languageTagMatch = _context.LanguageTagExpression.Match(_languageTag);
            Debug.Assert(languageTagMatch.Success);

            var regionSubtagGroup = languageTagMatch.Groups[LanguageTagSubtagGroupName.Region];
            if (regionSubtagGroup.Success)
            {
                return regionSubtagGroup.Value;
            }

            return null;
        }

#if NETCOREAPP3_0_OR_GREATER
        private string? DoGetScriptSubtag()
#else
        [CanBeNull]
        private string DoGetScriptSubtag()
#endif
        {
            Debug.Assert(_languageTag != null);

            var languageTagMatch = _context.LanguageTagExpression.Match(_languageTag);
            Debug.Assert(languageTagMatch.Success);

            var scriptSubtagGroup = languageTagMatch.Groups[LanguageTagSubtagGroupName.Script];
            if (scriptSubtagGroup.Success)
            {
                return scriptSubtagGroup.Value;
            }

            return null;
        }

#if NETCOREAPP3_0_OR_GREATER
        private string[] DoGetVariantSubtags()
#else
        [NotNull]
        private string[] DoGetVariantSubtags()
#endif
        {
            Debug.Assert(_languageTag != null);

            var languageTagMatch = _context.LanguageTagExpression.Match(_languageTag);
            Debug.Assert(languageTagMatch.Success);

            var variantSubtagGroup = languageTagMatch.Groups[LanguageTagSubtagGroupName.Variant];
            if (variantSubtagGroup.Success)
            {
                return variantSubtagGroup.Captures.Cast<Capture>().Select(capture => capture.Value).ToArray();
            }

#if NETSTANDARD1_3_OR_GREATER || NET
            return Array.Empty<string>();
#else
            return new string[] { };
#endif
        }

        #endregion
    }
}