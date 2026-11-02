using System;
using System.Text;
using DataStandardizer.Core;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using DataStandardizer.LanguageTag.InternalState;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// Builder for constructing an IETF BCP 47 language tag.
    /// </summary>
    /// <remarks>
    /// The steps that take a string check only the shape of the subtag. <c>Build</c> applies what was chosen: validity against the Subtag Registry from
    /// <see cref="WithLanguageSubtagRegistry"/>, the conformance from <see cref="WithConformance"/>, or otherwise well-formedness. The last of the two steps called applies.
    /// The steps that take a code are ISO by construction. The Variant and Extension steps refuse a repeated variant subtag or extension singleton whichever is chosen:
    /// RFC 5646 §2.2.9 allows one in a well-formed language tag, but a builder has no reason to compose a tag that could never be valid.
    /// </remarks>
    public class Bcp47LanguageTagBuilder : IBcp47LanguageTagBuilderStepWithConformance, IBcp47LanguageTagBuilderStepWithConformanceNext, IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistryNext, IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext,
        IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext, IBcp47LanguageTagBuilderStepUsingScriptSubtagNext, IBcp47LanguageTagBuilderStepUsingRegionSubtagNext, IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext,
        IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext, IBcp47LanguageTagBuilderStepUsingPrivateUseSubtagNext
    {
        #region Declarations

        #region Static Declarations

        internal static class ErrorMessage
        {
            internal const string InternalStateErrorTryAgain = "Internal state error.  Please instantiate a new instance of the builder and try again.";
            internal const string SpecifiedCodeUndefinedTemplate = "The {0} code '{1}' is undefined.";
            internal const string SubtagInvalidTemplate = "'{0}' is not a valid {1}.";
            internal const string SubtagRepeatedTemplate = "The {0} '{1}' appears more than once.";
            internal const string UnspecifiedCodeUndefinedTemplate = "The {0} code is undefined.";
        }

        #endregion

        #region Instance Declarations

#if NETSTANDARD1_3_OR_GREATER||NET
        private string[] _extendedLanguageSubtags = Array.Empty<string>();
        private string[] _extensionSubtags = Array.Empty<string>();
        private string[] _variantSubtags = Array.Empty<string>();
#else
        private string[] _extendedLanguageSubtags = { };
        private string[] _extensionSubtags = { };
        private string[] _variantSubtags = { };
#endif
        private LanguageTagConformance _conformance = LanguageTagConformance.WellFormed;
#if NETCOREAPP3_0_OR_GREATER
        private string? _languageTag;
        private object? _primaryLanguageSubtag;
        private string? _privateUseSubtag;
        private object? _regionSubtag;
        private object? _scriptSubtag;
        private SubtagRegistry.SubtagRegistry? _subtagRegistry;
#else
        [CanBeNull] private string _languageTag;
        [CanBeNull] private object _primaryLanguageSubtag;
        [CanBeNull] private string _privateUseSubtag;
        [CanBeNull] private object _regionSubtag;
        [CanBeNull] private object _scriptSubtag;
        [CanBeNull] private SubtagRegistry.SubtagRegistry _subtagRegistry;
#endif

        #endregion

        #endregion

        #region Public Methods: Builder

#if NETSTANDARD1_3_OR_GREATER||NET
#endif

        /// <inheritdoc />
        public IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistryNext WithLanguageSubtagRegistry(SubtagRegistry.SubtagRegistry subtagRegistry)
        {
            _subtagRegistry = subtagRegistry ?? throw new ArgumentNullException(nameof(subtagRegistry));

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException"><paramref name="conformance"/> is <see cref="LanguageTagConformance.Valid"/>, which needs a Subtag Registry.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="conformance"/> is not a defined value.</exception>
        public IBcp47LanguageTagBuilderStepWithConformanceNext WithConformance(LanguageTagConformance conformance)
        {
            // Reject a conformance that Build could not apply now, rather than when the tag is built.
            Bcp47LanguageTag.GetClassifier(conformance);

            _conformance = conformance;
            _subtagRegistry = null;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The <paramref name="languageTag"/> is <c>null</c>.</exception>
        public IBcp47LanguageTagBuilderStepBuild UsingLanguageTag(string languageTag)
        {
            _languageTag = languageTag ?? throw new ArgumentNullException(nameof(languageTag));

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">The subtag code is undefined.</exception>
        public IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(Iso639Part1Language primaryLanguageSubtag)
        {
#if NETCOREAPP3_0_OR_GREATER
            string? primaryLanguageSubtagString = primaryLanguageSubtag;
#else
            string primaryLanguageSubtagString = primaryLanguageSubtag;
#endif
            if (primaryLanguageSubtagString is null) throw new ArgumentException(string.Format(ErrorMessage.UnspecifiedCodeUndefinedTemplate, "ISO 639-1"), nameof(primaryLanguageSubtag));
            if (!StringEnum.IsDefined<Iso639Part1Language>(primaryLanguageSubtagString)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "ISO 639-1", primaryLanguageSubtagString), nameof(primaryLanguageSubtag));

            _primaryLanguageSubtag = primaryLanguageSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">The subtag code is undefined.</exception>
        public IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(Iso639Part2TLanguage primaryLanguageSubtag)
        {
#if NETCOREAPP3_0_OR_GREATER
            string? primaryLanguageSubtagString = primaryLanguageSubtag;
#else
            string primaryLanguageSubtagString = primaryLanguageSubtag;
#endif
            if (primaryLanguageSubtagString is null) throw new ArgumentException(string.Format(ErrorMessage.UnspecifiedCodeUndefinedTemplate, "ISO 639-2T"), nameof(primaryLanguageSubtag));
            if (!StringEnum.IsDefined<Iso639Part2TLanguage>(primaryLanguageSubtagString)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "ISO 639-2T", primaryLanguageSubtagString), nameof(primaryLanguageSubtag));

            _primaryLanguageSubtag = primaryLanguageSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">The subtag code is undefined.</exception>
        public IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(Iso639Part3Language primaryLanguageSubtag)
        {
#if NETCOREAPP3_0_OR_GREATER
            string? primaryLanguageSubtagString = primaryLanguageSubtag;
#else
            string primaryLanguageSubtagString = primaryLanguageSubtag;
#endif
            if (primaryLanguageSubtagString is null) throw new ArgumentException(string.Format(ErrorMessage.UnspecifiedCodeUndefinedTemplate, "ISO 639-3"), nameof(primaryLanguageSubtag));
            if (!StringEnum.IsDefined<Iso639Part3Language>(primaryLanguageSubtagString)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "ISO 639-3", primaryLanguageSubtagString), nameof(primaryLanguageSubtag));

            _primaryLanguageSubtag = primaryLanguageSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">The subtag code is undefined.</exception>
        public IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(Iso639Part5LanguageFamily primaryLanguageSubtag)
        {
#if NETCOREAPP3_0_OR_GREATER
            string? primaryLanguageSubtagString = primaryLanguageSubtag;
#else
            string primaryLanguageSubtagString = primaryLanguageSubtag;
#endif
            if (primaryLanguageSubtagString is null) throw new ArgumentException(string.Format(ErrorMessage.UnspecifiedCodeUndefinedTemplate, "ISO 639-5"), nameof(primaryLanguageSubtag));
            if (!StringEnum.IsDefined<Iso639Part5LanguageFamily>(primaryLanguageSubtagString)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "ISO 639-5", primaryLanguageSubtagString), nameof(primaryLanguageSubtag));

            _primaryLanguageSubtag = primaryLanguageSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The Primary Language subtag is <c>null</c>.</exception>
        public IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(string primaryLanguageSubtag)
        {
            if (primaryLanguageSubtag is null)
                throw new ArgumentNullException(nameof(primaryLanguageSubtag));
            if (!Bcp47LanguageTag.CheckPrimaryLanguageSubtag(primaryLanguageSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, primaryLanguageSubtag, "Primary Language subtag"), nameof(primaryLanguageSubtag));

            _primaryLanguageSubtag = primaryLanguageSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The Extended Language subtag is <c>null</c>.</exception>
        IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtags.UsingExtendedLanguageSubtags(string firstExtendedLanguageSubtag)
        {
            if (firstExtendedLanguageSubtag is null)
                throw new ArgumentNullException(nameof(firstExtendedLanguageSubtag));
            if (!Bcp47LanguageTag.CheckExtendedLanguageSubtag(firstExtendedLanguageSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, firstExtendedLanguageSubtag, "Extended Language subtag"), nameof(firstExtendedLanguageSubtag));

            _extendedLanguageSubtags = new[] { firstExtendedLanguageSubtag };

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">An Extended Language subtag is <c>null</c>.</exception>
        IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtags.UsingExtendedLanguageSubtags(string firstExtendedLanguageSubtag, string secondExtendedLanguageSubtag)
        {
            const string subject = "Extended Language subtag";
            if (firstExtendedLanguageSubtag is null)
                throw new ArgumentNullException(nameof(firstExtendedLanguageSubtag));
            if (!Bcp47LanguageTag.CheckExtendedLanguageSubtag(firstExtendedLanguageSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, firstExtendedLanguageSubtag, subject), nameof(firstExtendedLanguageSubtag));
            if (secondExtendedLanguageSubtag is null)
                throw new ArgumentNullException(nameof(secondExtendedLanguageSubtag));
            if (!Bcp47LanguageTag.CheckExtendedLanguageSubtag(secondExtendedLanguageSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, secondExtendedLanguageSubtag, subject), nameof(secondExtendedLanguageSubtag));

            _extendedLanguageSubtags = new[] { firstExtendedLanguageSubtag, secondExtendedLanguageSubtag };

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">An Extended Language subtag is <c>null</c>.</exception>
        IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtags.UsingExtendedLanguageSubtags(string firstExtendedLanguageSubtag, string secondExtendedLanguageSubtag,
            string thirdExtendedLanguageSubtag)
        {
            const string subject = "Extended Language subtag";
            if (firstExtendedLanguageSubtag is null)
                throw new ArgumentNullException(nameof(firstExtendedLanguageSubtag));
            if (!Bcp47LanguageTag.CheckExtendedLanguageSubtag(firstExtendedLanguageSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, firstExtendedLanguageSubtag, subject), nameof(firstExtendedLanguageSubtag));
            if (secondExtendedLanguageSubtag is null)
                throw new ArgumentNullException(nameof(secondExtendedLanguageSubtag));
            if (!Bcp47LanguageTag.CheckExtendedLanguageSubtag(secondExtendedLanguageSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, secondExtendedLanguageSubtag, subject), nameof(secondExtendedLanguageSubtag));
            if (thirdExtendedLanguageSubtag is null)
                throw new ArgumentNullException(nameof(thirdExtendedLanguageSubtag));
            if (!Bcp47LanguageTag.CheckExtendedLanguageSubtag(thirdExtendedLanguageSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, thirdExtendedLanguageSubtag, subject), nameof(thirdExtendedLanguageSubtag));

            _extendedLanguageSubtags = new[] { firstExtendedLanguageSubtag, secondExtendedLanguageSubtag, thirdExtendedLanguageSubtag };

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">The subtag code is undefined.</exception>
        IBcp47LanguageTagBuilderStepUsingScriptSubtagNext IBcp47LanguageTagBuilderStepUsingScriptSubtag.UsingScriptSubtag(Iso15924Script scriptSubtag)
        {
#if NET5_0_OR_GREATER
            if (!Enum.IsDefined(scriptSubtag)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "ISO 15924", scriptSubtag), nameof(scriptSubtag));
#else
            if (!Enum.IsDefined(typeof(Iso15924Script), scriptSubtag)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "ISO 15924", scriptSubtag), nameof(scriptSubtag));
#endif

            _scriptSubtag = scriptSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The <paramref name="scriptSubtag"/> is <c>null</c>.</exception>
        IBcp47LanguageTagBuilderStepUsingScriptSubtagNext IBcp47LanguageTagBuilderStepUsingScriptSubtag.UsingScriptSubtag(string scriptSubtag)
        {
            if (scriptSubtag is null)
                throw new ArgumentNullException(nameof(scriptSubtag));
            if (!Bcp47LanguageTag.CheckScriptSubtag(scriptSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, scriptSubtag, "Script subtag"), nameof(scriptSubtag));

            _scriptSubtag = scriptSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">The subtag code is undefined.</exception>
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext IBcp47LanguageTagBuilderStepUsingRegionSubtag.UsingRegionSubtag(Iso3166Part1Alpha2Country regionSubtag)
        {
#if NET5_0_OR_GREATER
            if (!Enum.IsDefined(regionSubtag)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "ISO 3166-1 Alpha-2", regionSubtag), nameof(regionSubtag));
#else
            if (!Enum.IsDefined(typeof(Iso3166Part1Alpha2Country), regionSubtag)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "ISO 3166-1 Alpha-2", regionSubtag), nameof(regionSubtag));
#endif

            _regionSubtag = regionSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">The subtag code is undefined, or is not a valid Region subtag.</exception>
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext IBcp47LanguageTagBuilderStepUsingRegionSubtag.UsingRegionSubtag(UnM49Area regionSubtag)
        {
            var code = ((ushort)regionSubtag).ToString("000");
#if NET5_0_OR_GREATER
            if (!Enum.IsDefined(regionSubtag)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "UN M49", code), nameof(regionSubtag));
#else
            if (!Enum.IsDefined(typeof(UnM49Area), regionSubtag)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "UN M49", code), nameof(regionSubtag));
#endif
            // A UN M.49 code of a country is not a Region subtag, whatever is chosen for Build (ref. RFC 5646 §2.2.4¶4).
            if (!Bcp47IsoCodeListClassifier.Instance.IsRegion(code.AsSpan())) throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, code, "Region subtag"), nameof(regionSubtag));

            _regionSubtag = regionSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The <paramref name="regionSubtag"/> is <c>null</c>.</exception>
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext IBcp47LanguageTagBuilderStepUsingRegionSubtag.UsingRegionSubtag(string regionSubtag)
        {
            if (regionSubtag is null)
                throw new ArgumentNullException(nameof(regionSubtag));
            if (!Bcp47LanguageTag.CheckRegionSubtag(regionSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, regionSubtag, "Region subtag"), nameof(regionSubtag));

            _regionSubtag = regionSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">A Variant subtag is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">A Variant subtag is not valid, or appears more than once (compared case-insensitively).</exception>
        IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext IBcp47LanguageTagBuilderStepUsingVariantSubtags.UsingVariantSubtags(string variantSubtag, params string[] variantSubtags)
        {
            CheckVariantSubtagArgument(variantSubtag, nameof(variantSubtag));
            if (variantSubtags is null)
                throw new ArgumentNullException(nameof(variantSubtags));

            _variantSubtags = CreateVariantSubtags(variantSubtag, variantSubtags);

            return this;
        }

#if NET9_0_OR_GREATER
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">A Variant subtag is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">A Variant subtag is not valid, or appears more than once (compared case-insensitively).</exception>
        IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext IBcp47LanguageTagBuilderStepUsingVariantSubtags.UsingVariantSubtags(string variantSubtag, params ReadOnlySpan<string> variantSubtags)
        {
            CheckVariantSubtagArgument(variantSubtag, nameof(variantSubtag));

            _variantSubtags = CreateVariantSubtags(variantSubtag, variantSubtags);

            return this;
        }
#endif

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">An Extension subtag is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">An Extension subtag is not valid, or its singleton appears more than once (compared case-insensitively).</exception>
        IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext IBcp47LanguageTagBuilderStepUsingExtensionSubtags.UsingExtensionSubtags(string extensionSubtag, params string[] extensionSubtags)
        {
            CheckExtensionSubtagArgument(extensionSubtag, nameof(extensionSubtag));
            if (extensionSubtags is null)
                throw new ArgumentNullException(nameof(extensionSubtags));

            _extensionSubtags = CreateExtensionSubtags(extensionSubtag, extensionSubtags);

            return this;
        }

#if NET9_0_OR_GREATER
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">An Extension subtag is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">An Extension subtag is not valid, or its singleton appears more than once (compared case-insensitively).</exception>
        IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext IBcp47LanguageTagBuilderStepUsingExtensionSubtags.UsingExtensionSubtags(string extensionSubtag, params ReadOnlySpan<string> extensionSubtags)
        {
            CheckExtensionSubtagArgument(extensionSubtag, nameof(extensionSubtag));

            _extensionSubtags = CreateExtensionSubtags(extensionSubtag, extensionSubtags);

            return this;
        }
#endif

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The Private Use subtag is <c>null</c>.</exception>
        IBcp47LanguageTagBuilderStepUsingPrivateUseSubtagNext IBcp47LanguageTagBuilderStepUsingPrivateUseSubtag.UsingPrivateUseSubtag(string privateUseSubtag)
        {
            if (privateUseSubtag is null)
                throw new ArgumentNullException(nameof(privateUseSubtag));
            if (!Bcp47LanguageTag.CheckPrivateUseSubtag(privateUseSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, privateUseSubtag, "Private Use subtag"), nameof(privateUseSubtag));

            _privateUseSubtag = privateUseSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">
        /// More than three Extended Language subtags were specified.
        /// </exception>
        /// <exception cref="LanguageTagFormatException">
        /// The language tag is not valid against the Subtag Registry, if one was given.
        /// -or-
        /// The language tag does not satisfy the conformance, if one was chosen.
        /// -or-
        /// Otherwise, the language tag is not well-formed.
        /// </exception>
        Bcp47LanguageTag IBcp47LanguageTagBuilderStepBuild.Build()
        {
            // Use full language tag, if specified.
            if (_languageTag != null)
            {
                return CreateLanguageTag(_languageTag);
            }

            // Resolve each subtag to its text, in the order the tag is written.
            if (_primaryLanguageSubtag is null) throw new InvalidOperationException(ErrorMessage.InternalStateErrorTryAgain);
            var primaryLanguageSubtag = GetPrimaryLanguageSubtag(_primaryLanguageSubtag);
            if (_extendedLanguageSubtags.Length > 3) throw new InvalidOperationException("A maximum of 3 Extended Language subtags are permitted.");
            var scriptSubtag = _scriptSubtag is null ? null : GetScriptSubtag(_scriptSubtag);
            var numericRegionSubtag = -1;
            var regionSubtag = _regionSubtag is null ? null : GetRegionSubtag(_regionSubtag, out numericRegionSubtag);

            // Measure the tag, so that it is written into a buffer of exactly its length.
            var length = primaryLanguageSubtag.Length + GetSubtagsLength(_extendedLanguageSubtags) + GetSubtagsLength(_variantSubtags) + GetSubtagsLength(_extensionSubtags);
            if (scriptSubtag != null) length += 1 + scriptSubtag.Length;
            if (regionSubtag != null) length += 1 + regionSubtag.Length;
            else if (numericRegionSubtag >= 0) length += 1 + NumericRegionSubtagLength;
            if (_privateUseSubtag != null) length += 1 + _privateUseSubtag.Length;

            // Return new instance of language tag.
#if NETCOREAPP2_1_OR_GREATER
            var languageTag = string.Create(length, (Builder: this, PrimaryLanguageSubtag: primaryLanguageSubtag, ScriptSubtag: scriptSubtag, RegionSubtag: regionSubtag, NumericRegionSubtag: numericRegionSubtag),
                static (destination, state) =>
                {
                    var position = Write(destination, 0, state.PrimaryLanguageSubtag);
                    position = WriteSubtags(destination, position, state.Builder._extendedLanguageSubtags);
                    if (state.ScriptSubtag != null) position = WriteSubtag(destination, position, state.ScriptSubtag);
                    if (state.RegionSubtag != null) position = WriteSubtag(destination, position, state.RegionSubtag);
                    else if (state.NumericRegionSubtag >= 0) position = WriteNumericSubtag(destination, position, state.NumericRegionSubtag);
                    position = WriteSubtags(destination, position, state.Builder._variantSubtags);
                    position = WriteSubtags(destination, position, state.Builder._extensionSubtags);
                    if (state.Builder._privateUseSubtag != null) WriteSubtag(destination, position, state.Builder._privateUseSubtag);
                });

            return CreateLanguageTag(languageTag);
#else
            var languageTagBuilder = new StringBuilder(length);
            languageTagBuilder.Append(primaryLanguageSubtag);
            AppendSubtags(languageTagBuilder, _extendedLanguageSubtags);
            if (scriptSubtag != null) languageTagBuilder.Append('-').Append(scriptSubtag);
            if (regionSubtag != null) languageTagBuilder.Append('-').Append(regionSubtag);
            else if (numericRegionSubtag >= 0) AppendNumericSubtag(languageTagBuilder, numericRegionSubtag);
            AppendSubtags(languageTagBuilder, _variantSubtags);
            AppendSubtags(languageTagBuilder, _extensionSubtags);
            if (_privateUseSubtag != null) languageTagBuilder.Append('-').Append(_privateUseSubtag);

            return CreateLanguageTag(languageTagBuilder.ToString());
#endif
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Number of digits in a UN M.49 Region subtag (ref. RFC 5646 §2.2.4¶4).
        /// </summary>
        private const int NumericRegionSubtagLength = 3;

#if NETCOREAPP3_0_OR_GREATER
        private static void CheckVariantSubtagArgument(string? variantSubtag, string parameterName)
#else
        private static void CheckVariantSubtagArgument([CanBeNull] string variantSubtag, [NotNull] string parameterName)
#endif
        {
            if (variantSubtag is null)
                throw new ArgumentNullException(parameterName);
            if (!Bcp47LanguageTag.CheckVariantSubtag(variantSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, variantSubtag, "Variant subtag"), parameterName);
        }

#if NETCOREAPP3_0_OR_GREATER
        private static void CheckExtensionSubtagArgument(string? extensionSubtag, string parameterName)
#else
        private static void CheckExtensionSubtagArgument([CanBeNull] string extensionSubtag, [NotNull] string parameterName)
#endif
        {
            if (extensionSubtag is null)
                throw new ArgumentNullException(parameterName);
            if (!Bcp47LanguageTag.CheckExtensionSubtag(extensionSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, extensionSubtag, "Extension subtag"), parameterName);
        }

#if NET9_0_OR_GREATER
        private static string[] CreateVariantSubtags(string variantSubtag, ReadOnlySpan<string> variantSubtags)
#elif NETCOREAPP3_0_OR_GREATER
        private static string[] CreateVariantSubtags(string variantSubtag, string[] variantSubtags)
#else
        [NotNull]
        private static string[] CreateVariantSubtags([NotNull] string variantSubtag, [NotNull] string[] variantSubtags)
#endif
        {
            var useVariantSubtags = new string[variantSubtags.Length + 1];
            useVariantSubtags[0] = variantSubtag;
            for (var index = 0; index < variantSubtags.Length; index++)
            {
                CheckVariantSubtagArgument(variantSubtags[index], nameof(variantSubtags));
                useVariantSubtags[index + 1] = variantSubtags[index];
            }

            var repeatedIndex = Bcp47LanguageTag.IndexOfRepeatedVariantSubtag(useVariantSubtags);
            if (repeatedIndex >= 0)
                throw new ArgumentException(string.Format(ErrorMessage.SubtagRepeatedTemplate, "Variant subtag", useVariantSubtags[repeatedIndex]), nameof(variantSubtags));

            return useVariantSubtags;
        }

#if NET9_0_OR_GREATER
        private static string[] CreateExtensionSubtags(string extensionSubtag, ReadOnlySpan<string> extensionSubtags)
#elif NETCOREAPP3_0_OR_GREATER
        private static string[] CreateExtensionSubtags(string extensionSubtag, string[] extensionSubtags)
#else
        [NotNull]
        private static string[] CreateExtensionSubtags([NotNull] string extensionSubtag, [NotNull] string[] extensionSubtags)
#endif
        {
            var useExtensionSubtags = new string[extensionSubtags.Length + 1];
            useExtensionSubtags[0] = extensionSubtag;
            for (var index = 0; index < extensionSubtags.Length; index++)
            {
                CheckExtensionSubtagArgument(extensionSubtags[index], nameof(extensionSubtags));
                useExtensionSubtags[index + 1] = extensionSubtags[index];
            }

            var repeatedIndex = Bcp47LanguageTag.IndexOfRepeatedExtensionSingleton(useExtensionSubtags);
            if (repeatedIndex >= 0)
                throw new ArgumentException(string.Format(ErrorMessage.SubtagRepeatedTemplate, "Extension singleton", useExtensionSubtags[repeatedIndex][0]), nameof(extensionSubtags));

            return useExtensionSubtags;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string GetPrimaryLanguageSubtag(object primaryLanguageSubtag)
#else
        [NotNull]
        private static string GetPrimaryLanguageSubtag([NotNull] object primaryLanguageSubtag)
#endif
        {
#if NETCOREAPP3_0_OR_GREATER
            string? primaryLanguageSubtagString;
#else
            string primaryLanguageSubtagString;
#endif
            if (primaryLanguageSubtag is Iso639Part1Language primaryLanguageSubtagFromIso639Part1)
            {
                primaryLanguageSubtagString = primaryLanguageSubtagFromIso639Part1;
            }
            else if (primaryLanguageSubtag is Iso639Part2TLanguage primaryLanguageSubtagFromIso639Part2T)
            {
                primaryLanguageSubtagString = primaryLanguageSubtagFromIso639Part2T;
            }
            else if (primaryLanguageSubtag is Iso639Part3Language primaryLanguageSubtagFromIso639Part3)
            {
                primaryLanguageSubtagString = primaryLanguageSubtagFromIso639Part3;
            }
            else if (primaryLanguageSubtag is Iso639Part5LanguageFamily primaryLanguageSubtagFromIso639Part5)
            {
                primaryLanguageSubtagString = primaryLanguageSubtagFromIso639Part5;
            }
            else
            {
                primaryLanguageSubtagString = primaryLanguageSubtag as string;
            }

            return primaryLanguageSubtagString ?? throw new InvalidOperationException("The Primary Language subtag is invalid.");
        }

        /// <summary>
        /// Get the text of the Region subtag, or its UN M.49 code.
        /// </summary>
        /// <param name="regionSubtag">Region subtag.</param>
        /// <param name="numericRegionSubtag">UN M.49 code of the Region subtag, if it is one; otherwise -1.</param>
        /// <returns>Text of the Region subtag; <c>null</c> if it is a UN M.49 code.</returns>
#if NETCOREAPP3_0_OR_GREATER
        private static string? GetRegionSubtag(object regionSubtag, out int numericRegionSubtag)
#else
        [CanBeNull]
        private static string GetRegionSubtag([NotNull] object regionSubtag, out int numericRegionSubtag)
#endif
        {
            numericRegionSubtag = -1;
            if (regionSubtag is Iso3166Part1Alpha2Country regionSubtagFromIso3166Part1)
            {
#if NET5_0_OR_GREATER
                return Enum.GetName(regionSubtagFromIso3166Part1) ?? throw new InvalidOperationException("The Region subtag is invalid.");
#else
                return Enum.GetName(typeof(Iso3166Part1Alpha2Country), regionSubtagFromIso3166Part1) ?? throw new InvalidOperationException("The Region subtag is invalid.");
#endif
            }

            if (regionSubtag is string regionSubtagFromRegistry)
            {
                return regionSubtagFromRegistry;
            }

            if (regionSubtag is UnM49Area regionSubtagFromUnM49Area)
            {
                numericRegionSubtag = (ushort)regionSubtagFromUnM49Area;
            }

            // The setters admit only UN M.49 codes of three digits.
            if (numericRegionSubtag < 0 || numericRegionSubtag > 999) throw new InvalidOperationException("The Region subtag is invalid.");

            return null;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string GetScriptSubtag(object scriptSubtag)
#else
        [NotNull]
        private static string GetScriptSubtag([NotNull] object scriptSubtag)
#endif
        {
            if (scriptSubtag is Iso15924Script scriptSubtagFromIso15924)
            {
#if NET5_0_OR_GREATER
                return Enum.GetName(scriptSubtagFromIso15924) ?? throw new InvalidOperationException("The Script subtag is invalid.");
#else
                return Enum.GetName(typeof(Iso15924Script), scriptSubtagFromIso15924) ?? throw new InvalidOperationException("The Script subtag is invalid.");
#endif
            }

            return scriptSubtag as string ?? throw new InvalidOperationException("The Script subtag is invalid.");
        }

        private static int GetSubtagsLength(string[] subtags)
        {
            var length = 0;
            foreach (var subtag in subtags)
            {
                length += 1 + subtag.Length;
            }

            return length;
        }

#if NETCOREAPP2_1_OR_GREATER
        private static int Write(Span<char> destination, int position, string value)
        {
            value.AsSpan().CopyTo(destination.Slice(position));
            return position + value.Length;
        }

        private static int WriteNumericSubtag(Span<char> destination, int position, int numericSubtag)
        {
            destination[position] = '-';
            destination[position + 1] = (char)('0' + numericSubtag / 100);
            destination[position + 2] = (char)('0' + numericSubtag / 10 % 10);
            destination[position + 3] = (char)('0' + numericSubtag % 10);
            return position + 1 + NumericRegionSubtagLength;
        }

        private static int WriteSubtag(Span<char> destination, int position, string subtag)
        {
            destination[position] = '-';
            return Write(destination, position + 1, subtag);
        }

        private static int WriteSubtags(Span<char> destination, int position, string[] subtags)
        {
            foreach (var subtag in subtags)
            {
                position = WriteSubtag(destination, position, subtag);
            }

            return position;
        }
#else
        private static void AppendNumericSubtag(StringBuilder languageTagBuilder, int numericSubtag)
        {
            languageTagBuilder.Append('-')
                .Append((char)('0' + numericSubtag / 100))
                .Append((char)('0' + numericSubtag / 10 % 10))
                .Append((char)('0' + numericSubtag % 10));
        }

        private static void AppendSubtags(StringBuilder languageTagBuilder, string[] subtags)
        {
            foreach (var subtag in subtags)
            {
                languageTagBuilder.Append('-').Append(subtag);
            }
        }
#endif

#if NETCOREAPP3_0_OR_GREATER
        private Bcp47LanguageTag CreateLanguageTag(string languageTag)
#else
        private Bcp47LanguageTag CreateLanguageTag([NotNull] string languageTag)
#endif
        {
            return _subtagRegistry != null ? Bcp47LanguageTag.Create(languageTag, _subtagRegistry) : Bcp47LanguageTag.Create(languageTag, _conformance);
        }

        #endregion
    }
}