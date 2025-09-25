namespace DataStandardizer.LanguageTag.InternalState
{
    internal interface IInternalState
    {
        /// <summary>
        /// Occurs when the state is selected.
        /// </summary>
        void Activated();

        /// <summary>
        /// Occurs when the state is unselected.
        /// </summary>
        void Deactivated();
    }
}