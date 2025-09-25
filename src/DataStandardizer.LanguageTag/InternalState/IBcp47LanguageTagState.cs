namespace DataStandardizer.LanguageTag.InternalState
{
    internal interface IBcp47LanguageTagState
    {
        /// <summary>
        /// Gets the operations available for evaluating a Primary Language subtag.
        /// </summary>
        IBcp47TagExpression PrimaryLanguageSubtagExpression { get; }

        /// <summary>
        /// Gets the operations available for evaluating an Extended Language subtag.
        /// </summary>
        IBcp47TagExpression ExtendedLanguageSubtagExpression { get; }

        /// <summary>
        /// Gets the operations available for evaluating a Script subtag.
        /// </summary>
        IBcp47TagExpression ScriptSubtagExpression { get; }

        /// <summary>
        /// Gets the operations available for evaluating a Region subtag.
        /// </summary>
        IBcp47TagExpression RegionSubtagExpression { get; }

        /// <summary>
        /// Gets the operations available for evaluating a Variant subtag.
        /// </summary>
        IBcp47TagExpression VariantSubtagExpression { get; }

        /// <summary>
        /// Gets the operations available for evaluating an Extension subtag.
        /// </summary>
        IBcp47TagExpression ExtensionSubtagExpression { get; }

        /// <summary>
        /// Gets the operations available for evaluating a Private-Use subtag.
        /// </summary>
        IBcp47TagExpression PrivateUseSubtagExpression { get; }

        /// <summary>
        /// Gets the operations available for evaluating a language tag.
        /// </summary>
        IBcp47TagExpression LanguageTagExpression { get; }
    }
}