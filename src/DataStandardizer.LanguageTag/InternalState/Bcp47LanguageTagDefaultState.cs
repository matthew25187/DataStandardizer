namespace DataStandardizer.LanguageTag.InternalState
{
    internal class Bcp47LanguageTagDefaultState : IInternalState, IBcp47LanguageTagState
    {
        public void Activated()
        {
        }

        public void Deactivated()
        {
        }

        public IBcp47TagExpression PrimaryLanguageSubtagExpression { get; } = new Bcp47TagExpression<Bcp47RulesBasedPrimaryLanguageSubtagExpressionFactory>();
        public IBcp47TagExpression ExtendedLanguageSubtagExpression { get; } = new Bcp47TagExpression<Bcp47RulesBasedExtendedLanguageSubtagExpressionFactory>();
        public IBcp47TagExpression ScriptSubtagExpression { get; } = new Bcp47TagExpression<Bcp47RulesBasedScriptSubtagExpressionFactory>();
        public IBcp47TagExpression RegionSubtagExpression { get; } = new Bcp47TagExpression<Bcp47RulesBasedRegionSubtagExpressionFactory>();
        public IBcp47TagExpression VariantSubtagExpression { get; } = new Bcp47TagExpression<Bcp47RulesBasedVariantSubtagExpressionFactory>();
        public IBcp47TagExpression ExtensionSubtagExpression { get; } = new Bcp47TagExpression<Bcp47RulesBasedExtensionSubtagExpressionFactory>();
        public IBcp47TagExpression PrivateUseSubtagExpression { get; } = new Bcp47TagExpression<Bcp47RulesBasedPrivateUseSubtagExpressionFactory>();
        public IBcp47TagExpression LanguageTagExpression { get; } = new Bcp47TagExpression<Bcp47RulesBasedLanguageTagExpressionFactory>();
    }
}