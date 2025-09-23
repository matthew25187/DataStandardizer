using System;
using System.Collections.Generic;
using System.Linq;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    internal class Bcp47LanguageTagContext : IBcp47LanguageTagState
    {
#if NETCOREAPP3_0_OR_GREATER
        private string? _currentStateName;
#else
        [CanBeNull] private string _currentStateName;
#endif
        private readonly IDictionary<string, IBcp47LanguageTagState> _states;

        internal Bcp47LanguageTagContext(IDictionary<string, IBcp47LanguageTagState> states)
        {
            _states = states ?? throw new ArgumentNullException(nameof(states));

            _currentStateName = _states.Keys.FirstOrDefault();
            if (_states.TryGetValue(_currentStateName ?? string.Empty, out var state) && state is IInternalState newState)
            {
                newState.Activated();
            }
        }

        #region Public Methods

#if NETCOREAPP3_0_OR_GREATER
        public void SelectState(string stateName)
#else
        public void SelectState([NotNull] string stateName)
#endif
        {
            if (!_states.ContainsKey(stateName))
            {
                throw new KeyNotFoundException($"State '{stateName}' not found.");
            }

            if (_states.TryGetValue(_currentStateName ?? string.Empty, out var state) && state is IInternalState oldState)
            {
                oldState.Deactivated();
            }

            _currentStateName = stateName;
            (_states[_currentStateName] as IInternalState)?.Activated();
        }

        #endregion

        #region Public Properties

#if NETCOREAPP3_0_OR_GREATER
#nullable disable annotations  
#endif
        public IBcp47TagExpression PrimaryLanguageSubtagExpression => DoGetPrimaryLanguageSubtagExpression();

        public IBcp47TagExpression ExtendedLanguageSubtagExpression => DoGetExtendedLanguageSubtagExpression();

        public IBcp47TagExpression ScriptSubtagExpression => DoGetScriptSubtagExpression();

        public IBcp47TagExpression RegionSubtagExpression => DoGetRegionSubtagExpression();

        public IBcp47TagExpression VariantSubtagExpression => DoGetVariantSubtagExpression();

        public IBcp47TagExpression ExtensionSubtagExpression => DoGetExtensionSubtagExpression();

        public IBcp47TagExpression PrivateUseSubtagExpression => DoGetPrivateUseSubtagExpression();

        public IBcp47TagExpression LanguageTagExpression => DoGetLanguageTagExpression();
#if NETCOREAPP3_0_OR_GREATER
#nullable enable annotations  
#endif

        #endregion

        #region Private Methods

#if NETCOREAPP3_0_OR_GREATER
        private IBcp47TagExpression? DoGetExtendedLanguageSubtagExpression()
#else
        [CanBeNull]
        private IBcp47TagExpression DoGetExtendedLanguageSubtagExpression()
#endif
        {
            if (!_states.TryGetValue(_currentStateName ?? string.Empty, out var currentState))
            {
                return null;
            }

            return currentState.ExtendedLanguageSubtagExpression;
        }

#if NETCOREAPP3_0_OR_GREATER
        private IBcp47TagExpression? DoGetExtensionSubtagExpression()
#else
        [CanBeNull]
        private IBcp47TagExpression DoGetExtensionSubtagExpression()
#endif
        {
            if (!_states.TryGetValue(_currentStateName ?? string.Empty, out var currentState))
            {
                return null;
            }

            return currentState.ExtensionSubtagExpression;
        }

#if NETCOREAPP3_0_OR_GREATER
        private IBcp47TagExpression? DoGetLanguageTagExpression()
#else
        [CanBeNull]
        private IBcp47TagExpression DoGetLanguageTagExpression()
#endif
        {
            if (!_states.TryGetValue(_currentStateName ?? string.Empty, out var currentState))
            {
                return null;
            }

            return currentState.LanguageTagExpression;
        }
#if NETCOREAPP3_0_OR_GREATER
        private IBcp47TagExpression? DoGetPrimaryLanguageSubtagExpression()
#else
        [CanBeNull]
        private IBcp47TagExpression DoGetPrimaryLanguageSubtagExpression()
#endif
        {
            if (!_states.TryGetValue(_currentStateName ?? string.Empty, out var currentState))
            {
                return null;
            }

            return currentState.PrimaryLanguageSubtagExpression;
        }

#if NETCOREAPP3_0_OR_GREATER
        private IBcp47TagExpression? DoGetPrivateUseSubtagExpression()
#else
        [CanBeNull]
        private IBcp47TagExpression DoGetPrivateUseSubtagExpression()
#endif
        {
            if (!_states.TryGetValue(_currentStateName ?? string.Empty, out var currentState))
            {
                return null;
            }

            return currentState.PrivateUseSubtagExpression;
        }

#if NETCOREAPP3_0_OR_GREATER
        private IBcp47TagExpression? DoGetRegionSubtagExpression()
#else
        [CanBeNull]
        private IBcp47TagExpression DoGetRegionSubtagExpression()
#endif
        {
            if (!_states.TryGetValue(_currentStateName ?? string.Empty, out var currentState))
            {
                return null;
            }

            return currentState.RegionSubtagExpression;
        }

#if NETCOREAPP3_0_OR_GREATER
        private IBcp47TagExpression? DoGetScriptSubtagExpression()
#else
        [CanBeNull]
        private IBcp47TagExpression DoGetScriptSubtagExpression()
#endif
        {
            if (!_states.TryGetValue(_currentStateName ?? string.Empty, out var currentState))
            {
                return null;
            }

            return currentState.ScriptSubtagExpression;
        }

#if NETCOREAPP3_0_OR_GREATER
        private IBcp47TagExpression? DoGetVariantSubtagExpression()
#else
        [CanBeNull]
        private IBcp47TagExpression DoGetVariantSubtagExpression()
#endif
        {
            if (!_states.TryGetValue(_currentStateName ?? string.Empty, out var currentState))
            {
                return null;
            }

            return currentState.VariantSubtagExpression;
        }

        #endregion
    }
}