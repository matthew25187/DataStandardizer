using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using DataStandardizer.Core;
using DataStandardizer.Geography;
using DataStandardizer.Language;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// Builder for constructing an IETF BCP 47 language tag.
    /// </summary>
    public class Bcp47LanguageTagBuilder : IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistryNext, IBcp47LanguageTagBuilderStepWithTimeoutNext, IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext,
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
            internal const string UnspecifiedCodeUndefinedTemplate = "The {0} code is undefined.";
        }

        #endregion

        #region Instance Declarations

        private TimeSpan? _matchTimeout;

#if NETSTANDARD1_3_OR_GREATER||NET
        private string[] _extendedLanguageSubtags = Array.Empty<string>();
        private string[] _extensionSubtags = Array.Empty<string>();
        private string[] _variantSubtags = Array.Empty<string>();
#else
        private string[] _extendedLanguageSubtags = { };
        private string[] _extensionSubtags = { };
        private string[] _variantSubtags = { };
#endif
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
        public IBcp47LanguageTagBuilderStepWithTimeoutNext WithTimeout(TimeSpan matchTimeout)
        {
            _matchTimeout = matchTimeout;

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
            if (!Enum.IsDefined(scriptSubtag.GetType(), scriptSubtag)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "ISO 15924", scriptSubtag), nameof(scriptSubtag));

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
            if (!Enum.IsDefined(regionSubtag.GetType(), regionSubtag)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "ISO 3166-1 Alpha-2", regionSubtag), nameof(regionSubtag));

            _regionSubtag = regionSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">The subtag code is undefined.</exception>
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext IBcp47LanguageTagBuilderStepUsingRegionSubtag.UsingRegionSubtag(UnM49AreaByAlpha2CountryCode regionSubtag)
        {
            if (!IsValidRegionCodeFromUnM49((ushort)regionSubtag)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "UN M49", ((ushort)regionSubtag).ToString("000")), nameof(regionSubtag));

            _regionSubtag = regionSubtag;

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">The subtag code is undefined.</exception>
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext IBcp47LanguageTagBuilderStepUsingRegionSubtag.UsingRegionSubtag(UnM49AreaByAlpha3CountryCode regionSubtag)
        {
            if (!IsValidRegionCodeFromUnM49((ushort)regionSubtag)) throw new ArgumentException(string.Format(ErrorMessage.SpecifiedCodeUndefinedTemplate, "UN M49", ((ushort)regionSubtag).ToString("000")), nameof(regionSubtag));

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
        IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext IBcp47LanguageTagBuilderStepUsingVariantSubtags.UsingVariantSubtags(string variantSubtag, params string[] variantSubtags)
        {
            const string subject = "Variant subtag";
            if (variantSubtag is null)
                throw new ArgumentNullException(nameof(variantSubtag));
            if (!Bcp47LanguageTag.CheckVariantSubtag(variantSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, variantSubtag, subject), nameof(variantSubtag));
            if (variantSubtags is null)
                throw new ArgumentNullException(nameof(variantSubtags));
            if (!variantSubtags.All(Bcp47LanguageTag.CheckVariantSubtag))
            {
                var invalidSubtag = variantSubtags.First(subtag => !Bcp47LanguageTag.CheckVariantSubtag(subtag));
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, invalidSubtag, subject), nameof(invalidSubtag));
            }

            var useVariantSubtags = new List<string> { variantSubtag };
            useVariantSubtags.AddRange(variantSubtags);
            _variantSubtags = useVariantSubtags.ToArray();

            return this;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">An Extension subtag is <c>null</c>.</exception>
        IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext IBcp47LanguageTagBuilderStepUsingExtensionSubtags.UsingExtensionSubtags(string extensionSubtag, params string[] extensionSubtags)
        {
            const string subject = "Extension subtag";
            if (extensionSubtag is null)
                throw new ArgumentNullException(nameof(extensionSubtag));
            if (!Bcp47LanguageTag.CheckExtensionSubtag(extensionSubtag))
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, extensionSubtag, subject), nameof(extensionSubtag));
            if (extensionSubtags is null)
                throw new ArgumentNullException(nameof(extensionSubtags));
            if (!extensionSubtags.All(Bcp47LanguageTag.CheckExtensionSubtag))
            {
                var invalidSubtag = extensionSubtags.First(subtag => !Bcp47LanguageTag.CheckExtensionSubtag(subtag));
                throw new ArgumentException(string.Format(ErrorMessage.SubtagInvalidTemplate, invalidSubtag, subject), nameof(invalidSubtag));
            }

            var useExtensionSubtags = new List<string> { extensionSubtag };
            useExtensionSubtags.AddRange(extensionSubtags);
            _extensionSubtags = useExtensionSubtags.ToArray();

            return this;
        }

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
        /// The Primary Language subtag is not an ISO 639-1 code, an ISO 639-2 code, an ISO 639-3 code, an ISO 639-5 code, or a registered subtag.
        /// -or-
        /// More than three Extended Language subtags were specified.
        /// -or-
        /// The Script subtag is not an ISO 15924 code, nor a registered subtag.
        /// -or-
        /// The Region subtag is not an ISO 3166-1 Alpha-2 code, a UN M49 code, or a registered subtag.
        /// -or-
        /// A Variant subtag is not a registered subtag.
        /// </exception>
        /// <exception cref="LanguageTagFormatException">
        /// The Primary Language subtag is not correctly formatted.
        /// -or-
        /// An Extended Language subtag is not correctly formatted.
        /// -or-
        /// The Script subtag is not correctly formatted.
        /// -or-
        /// The Region subtag is not correctly formatted.
        /// -or-
        /// A Variant subtag is not correctly formatted.
        /// -or-
        /// An Extension subtag is not correctly formatted.
        /// -or-
        /// The Private Use subtag is not correctly formatted.
        /// </exception>
        Bcp47LanguageTag IBcp47LanguageTagBuilderStepBuild.Build()
        {
            var languageTagBuilder = new StringBuilder();

            // Use full language tag, if specified.
            if (_languageTag != null)
            {
                return CreateLanguageTag(_languageTag);
            }

            // Add Primary Language subtag.
            if (_primaryLanguageSubtag is null) throw new InvalidOperationException(ErrorMessage.InternalStateErrorTryAgain);
            AppendPrimaryLanguageSubtag(languageTagBuilder, _primaryLanguageSubtag);

            // Add Extended Language subtags.
            if (_extendedLanguageSubtags.Any())
            {
                AppendExtendedLanguageSubtags(languageTagBuilder, _extendedLanguageSubtags);
            }

            // Add Script subtag.
            if (_scriptSubtag != null)
            {
                AppendScriptSubtag(languageTagBuilder, _scriptSubtag);
            }

            // Add Region subtag.
            if (_regionSubtag != null)
            {
                AppendRegionSubtag(languageTagBuilder, _regionSubtag);
            }

            // Add Variant subtags.
            if (_variantSubtags.Any())
            {
                AppendVariantSubtags(languageTagBuilder, _variantSubtags);
            }

            // Add Extension subtags.
            if (_extensionSubtags.Any())
            {
                AppendExtensionSubtags(languageTagBuilder, _extensionSubtags);
            }

            // Add Private Use subtag.
            if (_privateUseSubtag != null)
            {
                AppendPrivateUseSubtag(languageTagBuilder, _privateUseSubtag);
            }

            // Return new instance of language tag.
            return CreateLanguageTag(languageTagBuilder.ToString());
        }

        #endregion

        #region Private Methods

        private void AppendExtendedLanguageSubtags(StringBuilder languageTagBuilder, string[] extendedLanguageSubtags)
        {
            if (extendedLanguageSubtags.Length >= 1 && extendedLanguageSubtags.Length <= 3)
            {
                foreach (var extendedLanguageSubtag in extendedLanguageSubtags)
                {
                    languageTagBuilder.AppendFormat("-{0}", extendedLanguageSubtag);
                }
            }
            else if (extendedLanguageSubtags.Length > 3)
            {
                throw new InvalidOperationException("A maximum of 3 Extended Language subtags are permitted.");
            }
        }

        private void AppendExtensionSubtags(StringBuilder languageTagBuilder, string[] extensionSubtags)
        {
            foreach (var extensionSubtag in extensionSubtags)
            {
                languageTagBuilder.AppendFormat("-{0}", extensionSubtag);
            }
        }

        private void AppendPrimaryLanguageSubtag(StringBuilder languageTagBuilder, object primaryLanguageSubtag)
        {
            if (primaryLanguageSubtag is Iso639Part1Language primaryLanguageSubtagFromIso639Part1)
            {
                languageTagBuilder.Append(primaryLanguageSubtagFromIso639Part1);
            }
            else if (primaryLanguageSubtag is Iso639Part2TLanguage primaryLanguageSubtagFromIso639Part2T)
            {
                languageTagBuilder.Append(primaryLanguageSubtagFromIso639Part2T);
            }
            else if (primaryLanguageSubtag is Iso639Part3Language primaryLanguageSubtagFromIso639Part3)
            {
                languageTagBuilder.Append(primaryLanguageSubtagFromIso639Part3);
            }
            else if (primaryLanguageSubtag is Iso639Part5LanguageFamily primaryLanguageSubtagFromIso639Part5)
            {
                languageTagBuilder.Append(primaryLanguageSubtagFromIso639Part5);
            }
            else if (primaryLanguageSubtag is string primaryLanguageSubtagFromRegistry)
            {
                languageTagBuilder.Append(primaryLanguageSubtagFromRegistry);
            }
            else
            {
                throw new InvalidOperationException("The Primary Language subtag is invalid.");
            }
        }

        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private void AppendPrivateUseSubtag(StringBuilder languageTagBuilder, string privateUseSubtag)
        {
            languageTagBuilder.AppendFormat("-{0}", privateUseSubtag);
        }

        private void AppendRegionSubtag(StringBuilder languageTagBuilder, object regionSubtag)
        {
            if (regionSubtag is Iso3166Part1Alpha2Country regionSubtagFromIso3166Part1)
            {
                languageTagBuilder.AppendFormat("-{0}", Enum.GetName(regionSubtagFromIso3166Part1.GetType(), regionSubtagFromIso3166Part1));
            }
            else if (regionSubtag is UnM49AreaByAlpha2CountryCode regionSubtagFromUnM49ByAlpha2Code)
            {
                languageTagBuilder.AppendFormat("-{0:000}", (ushort)regionSubtagFromUnM49ByAlpha2Code);
            }
            else if (regionSubtag is UnM49AreaByAlpha3CountryCode regionSubtagFromUnM49ByAlpha3Code)
            {
                languageTagBuilder.AppendFormat("-{0:000}", (ushort)regionSubtagFromUnM49ByAlpha3Code);
            }
            else if (regionSubtag is string regionSubtagFromRegistry)
            {
                languageTagBuilder.AppendFormat("-{0}", regionSubtagFromRegistry);
            }
            else
            {
                throw new InvalidOperationException("The Region subtag is invalid.");
            }
        }

        private void AppendScriptSubtag(StringBuilder languageTagBuilder, object scriptSubtag)
        {
            if (scriptSubtag is Iso15924Script scriptSubtagFromIso15924)
            {
                languageTagBuilder.AppendFormat("-{0}", Enum.GetName(scriptSubtagFromIso15924.GetType(), scriptSubtagFromIso15924));
            }
            else if (scriptSubtag is string scriptSubtagFromRegistry)
            {
                languageTagBuilder.AppendFormat("-{0}", scriptSubtagFromRegistry);
            }
            else
            {
                throw new InvalidOperationException("The Script subtag is invalid.");
            }
        }

        private void AppendVariantSubtags(StringBuilder languageTagBuilder, string[] variantSubtags)
        {
            foreach (var variantSubtag in variantSubtags)
            {
                languageTagBuilder.AppendFormat("-{0}", variantSubtag);
            }
        }

#if NETCOREAPP3_0_OR_GREATER
        private Bcp47LanguageTag CreateLanguageTag(string languageTag)
#else
        private Bcp47LanguageTag CreateLanguageTag([NotNull] string languageTag)
#endif
        {
            if (_subtagRegistry != null)
            {
                return _matchTimeout is null ? Bcp47LanguageTag.Create(languageTag, _subtagRegistry) : Bcp47LanguageTag.Create(languageTag, _subtagRegistry, _matchTimeout.Value);
            }

            return _matchTimeout is null ? Bcp47LanguageTag.Create(languageTag) : Bcp47LanguageTag.Create(languageTag, _matchTimeout.Value);
        }

        private bool IsValidRegionCodeFromUnM49(ushort candidateCode)
        {
            var globalCodes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetGlobalCode()).Where(code => code.HasValue).Cast<ushort>();
            var regionCodes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetRegionCode()).Where(code => code.HasValue).Cast<ushort>();
            var subRegionCodes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetSubRegionCode()).Where(code => code.HasValue).Cast<ushort>();
            var intermediateRegionCodes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetIntermediateRegionCode()).Where(code => code.HasValue).Cast<ushort>();
            var m49Codes = globalCodes.Union(regionCodes).Union(subRegionCodes).Union(intermediateRegionCodes);
            return m49Codes.Contains(candidateCode);
        }

        #endregion
    }
}