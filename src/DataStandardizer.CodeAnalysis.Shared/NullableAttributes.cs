// The compiler recognises these attributes by their full name, whichever assembly declares them and whatever their accessibility, so
// internal copies give a nullable-enabled consumer the same warnings as the BCL's on the targets that lack them.  This file must compile
// as C# 7.3, the default language version of the netstandard targets, so it can't use #nullable or '?'.
namespace System.Diagnostics.CodeAnalysis
{
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
    /// <summary>
    /// Specifies that null is allowed as an input even if the corresponding type disallows it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class AllowNullAttribute : Attribute
    {
    }

    /// <summary>
    /// Specifies that null is disallowed as an input even if the corresponding type allows it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class DisallowNullAttribute : Attribute
    {
    }

    /// <summary>
    /// Specifies that an output may be null even if the corresponding type disallows it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class MaybeNullAttribute : Attribute
    {
    }

    /// <summary>
    /// Specifies that an output is not null even if the corresponding type allows it, and that an input argument is not null when the call returns.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute
    {
    }

    /// <summary>
    /// Specifies that when a method returns <see cref="ReturnValue"/>, the parameter may be null even if the corresponding type disallows it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class MaybeNullWhenAttribute : Attribute
    {
        /// <summary>
        /// Initializes the attribute with the specified return value condition.
        /// </summary>
        /// <param name="returnValue">The return value condition. If the method returns this value, the associated parameter may be null.</param>
        public MaybeNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }

        /// <summary>
        /// Gets the return value condition.
        /// </summary>
        public bool ReturnValue { get; }
    }

    /// <summary>
    /// Specifies that when a method returns <see cref="ReturnValue"/>, the parameter is not null even if the corresponding type allows it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        /// <summary>
        /// Initializes the attribute with the specified return value condition.
        /// </summary>
        /// <param name="returnValue">The return value condition. If the method returns this value, the associated parameter is not null.</param>
        public NotNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }

        /// <summary>
        /// Gets the return value condition.
        /// </summary>
        public bool ReturnValue { get; }
    }

    /// <summary>
    /// Specifies that the output is not null if the named parameter is not null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    internal sealed class NotNullIfNotNullAttribute : Attribute
    {
        /// <summary>
        /// Initializes the attribute with the associated parameter name.
        /// </summary>
        /// <param name="parameterName">The associated parameter name. The output is not null if the argument to the parameter with this name is not null.</param>
        public NotNullIfNotNullAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        /// <summary>
        /// Gets the associated parameter name.
        /// </summary>
        public string ParameterName { get; }
    }

    /// <summary>
    /// Specifies that a method will never return under any circumstance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class DoesNotReturnAttribute : Attribute
    {
    }

    /// <summary>
    /// Specifies that the method will not return if the associated <see cref="bool"/> parameter is passed the specified value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class DoesNotReturnIfAttribute : Attribute
    {
        /// <summary>
        /// Initializes the attribute with the specified parameter value.
        /// </summary>
        /// <param name="parameterValue">The condition parameter value. Code after the method is unreachable if the argument to the associated parameter matches this value.</param>
        public DoesNotReturnIfAttribute(bool parameterValue)
        {
            ParameterValue = parameterValue;
        }

        /// <summary>
        /// Gets the condition parameter value.
        /// </summary>
        public bool ParameterValue { get; }
    }
#endif

#if !NET5_0_OR_GREATER
    /// <summary>
    /// Specifies that the method or property will ensure that the listed field and property members have values that aren't null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    internal sealed class MemberNotNullAttribute : Attribute
    {
        /// <summary>
        /// Initializes the attribute with a field or property member.
        /// </summary>
        /// <param name="member">The field or property member that is promised to be not null.</param>
        public MemberNotNullAttribute(string member)
        {
            Members = new[] { member };
        }

        /// <summary>
        /// Initializes the attribute with the list of field and property members.
        /// </summary>
        /// <param name="members">The list of field and property members that are promised to be not null.</param>
        public MemberNotNullAttribute(params string[] members)
        {
            Members = members;
        }

        /// <summary>
        /// Gets the field and property members.
        /// </summary>
        public string[] Members { get; }
    }

    /// <summary>
    /// Specifies that the method or property will ensure that the listed field and property members have values that aren't null when returning with the specified return value condition.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    internal sealed class MemberNotNullWhenAttribute : Attribute
    {
        /// <summary>
        /// Initializes the attribute with the specified return value condition and a field or property member.
        /// </summary>
        /// <param name="returnValue">The return value condition. If the method returns this value, the associated field or property member is not null.</param>
        /// <param name="member">The field or property member that is promised to be not null.</param>
        public MemberNotNullWhenAttribute(bool returnValue, string member)
        {
            ReturnValue = returnValue;
            Members = new[] { member };
        }

        /// <summary>
        /// Initializes the attribute with the specified return value condition and list of field and property members.
        /// </summary>
        /// <param name="returnValue">The return value condition. If the method returns this value, the associated field and property members are not null.</param>
        /// <param name="members">The list of field and property members that are promised to be not null.</param>
        public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
        {
            ReturnValue = returnValue;
            Members = members;
        }

        /// <summary>
        /// Gets the return value condition.
        /// </summary>
        public bool ReturnValue { get; }

        /// <summary>
        /// Gets the field and property members.
        /// </summary>
        public string[] Members { get; }
    }
#endif
}
