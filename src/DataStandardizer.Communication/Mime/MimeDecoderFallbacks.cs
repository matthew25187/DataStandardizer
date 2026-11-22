using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
#if NETSTANDARD1_1
using System.Reflection;
#endif

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Gives a copy of an encoding the decoder fallback that reads octets the encoding can't decode as U+FFFD, or the one that throws
    /// <see cref="DecoderFallbackException"/> for them.
    /// </summary>
    /// <remarks>
    /// .NET Standard 1.1 has no <c>Encoding.Clone</c>, <c>Encoding.DecoderFallback</c> or decoder fallback types, though every .NET runtime
    /// that loads it does, so on that build they are found at run time. A runtime whose reflection metadata leaves them out, such as .NET
    /// Native, has no copy to give.
    /// </remarks>
    internal static class MimeDecoderFallbacks
    {
        // ref. The Unicode Standard §23.8: U+FFFD REPLACEMENT CHARACTER.
        internal const string ReplacementString = "\uFFFD";

#if NETSTANDARD1_1
        // Each is null if the runtime's reflection metadata leaves it out.
        private static readonly MethodInfo CloneMethod = typeof(Encoding).GetRuntimeMethod("Clone", new Type[0]);
        private static readonly PropertyInfo DecoderFallbackProperty = typeof(Encoding).GetRuntimeProperty("DecoderFallback");
        private static readonly object ReplacementFallback = CreateFallback("System.Text.DecoderReplacementFallback", ReplacementString);
        private static readonly object ExceptionFallback = CreateFallback("System.Text.DecoderExceptionFallback");

        /// <summary>
        /// Gets whether the runtime lets this build give an encoding a decoder fallback.
        /// </summary>
        internal static bool IsAvailable => CloneMethod != null && DecoderFallbackProperty != null && DecoderFallbackProperty.CanWrite &&
            ReplacementFallback != null && ExceptionFallback != null;
#else
        private static readonly DecoderFallback ReplacementFallback = new DecoderReplacementFallback(ReplacementString);

        /// <summary>
        /// Gets whether the runtime lets this build give an encoding a decoder fallback.
        /// </summary>
        internal static bool IsAvailable => true;
#endif

        /// <summary>
        /// Gets a copy of an encoding that reads octets it can't decode as U+FFFD, or throws <see cref="DecoderFallbackException"/> for
        /// them.
        /// </summary>
        /// <param name="encoding">The encoding to copy, which isn't changed.</param>
        /// <param name="replace">Whether the copy reads octets it can't decode as U+FFFD, rather than throwing.</param>
        /// <param name="copy">The copy; or <c>null</c> if the runtime doesn't let this build give an encoding a decoder fallback.</param>
        /// <returns><c>true</c> if there is a copy; otherwise <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryCopy(Encoding encoding, bool replace, [NotNullWhen(true)] out Encoding? copy)
#else
        internal static bool TryCopy(Encoding encoding, bool replace, [MaybeNullWhen(false)] out Encoding copy)
#endif
        {
#if NETSTANDARD1_1
            if (!IsAvailable)
            {
                copy = null;
                return false;
            }

            copy = (Encoding)CloneMethod.Invoke(encoding, null);
            DecoderFallbackProperty.SetValue(copy, replace ? ReplacementFallback : ExceptionFallback);
#else
            copy = (Encoding)encoding.Clone();
            copy.DecoderFallback = replace ? ReplacementFallback : DecoderFallback.ExceptionFallback;
#endif
            return true;
        }

#if NETSTANDARD1_1
        private static object CreateFallback(string typeName, params object[] arguments)
        {
            var type = typeof(Encoding).GetTypeInfo().Assembly.GetType(typeName);
            return type is null ? null : Activator.CreateInstance(type, arguments);
        }
#endif
    }
}
