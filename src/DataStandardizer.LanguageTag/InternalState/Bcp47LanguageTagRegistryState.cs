namespace DataStandardizer.LanguageTag.InternalState
{
    internal class Bcp47LanguageTagRegistryState : IInternalState, IBcp47LanguageTagState
    {
        internal Bcp47LanguageTagRegistryState(SubtagRegistry.SubtagRegistry subtagRegistry)
        {
            PrimaryLanguageSubtagExpression = new Bcp47TagExpression<Bcp47RegistryBasedPrimaryLanguageSubtagExpressionFactory>(subtagRegistry);
            ExtendedLanguageSubtagExpression = new Bcp47TagExpression<Bcp47RegistryBasedExtendedLanguageSubtagExpressionFactory>(subtagRegistry);
            ScriptSubtagExpression = new Bcp47TagExpression<Bcp47RegistryBasedScriptSubtagExpressionFactory>(subtagRegistry);
            RegionSubtagExpression = new Bcp47TagExpression<Bcp47RegistryBasedRegionSubtagExpressionFactory>(subtagRegistry);
            VariantSubtagExpression = new Bcp47TagExpression<Bcp47RegistryBasedVariantSubtagExpressionFactory>(subtagRegistry);
            LanguageTagExpression = new Bcp47TagExpression<Bcp47RegistryBasedLanguageTagExpressionFactory>(subtagRegistry);
        }

        public void Activated()
        {
        }

        public void Deactivated()
        {
        }

        public IBcp47TagExpression PrimaryLanguageSubtagExpression { get; }

        public IBcp47TagExpression ExtendedLanguageSubtagExpression { get; }

        public IBcp47TagExpression ScriptSubtagExpression { get; }

        public IBcp47TagExpression RegionSubtagExpression { get; }

        public IBcp47TagExpression VariantSubtagExpression { get; }

        public IBcp47TagExpression ExtensionSubtagExpression { get; } = new Bcp47TagExpression<Bcp47RulesBasedExtensionSubtagExpressionFactory>();

        public IBcp47TagExpression PrivateUseSubtagExpression { get; } = new Bcp47TagExpression<Bcp47RulesBasedPrivateUseSubtagExpressionFactory>();

        public IBcp47TagExpression LanguageTagExpression { get; }
    }
}