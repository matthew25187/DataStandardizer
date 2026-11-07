using System;
using System.Linq;
using System.Reflection;

namespace DataStandardizer.Core
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Searches the runtime type of an object, and then each of its base types in turn, for a method with the specified name, and
        /// returns the first one found that matches the visibility flags.
        /// </summary>
        /// <remarks>
        /// With no visibility flag set, a method of any visibility matches, including a private method of a base type.  Each flag that is
        /// set further restricts the visibility a method must have, so setting more than one flag matches no method.
        /// </remarks>
        /// <typeparam name="T">Type of the object on which the method is implemented.</typeparam>
        /// <param name="obj">Object on which the method is implemented.</param>
        /// <param name="methodName">Name of the method to find.</param>
        /// <param name="isPublic"><c>true</c> to match only a public method; otherwise, <c>false</c>.</param>
        /// <param name="isProtected"><c>true</c> to match only a protected method; otherwise, <c>false</c>.</param>
        /// <param name="isPrivate"><c>true</c> to match only a private method; otherwise, <c>false</c>.</param>
        /// <returns>Information about the method found, or <c>null</c> if no method matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> or <paramref name="methodName"/> is <c>null</c>.</exception>
        [Obsolete("TypeExtensions.GetMethod will be removed in a future major version.  Search the type's TypeInfo.DeclaredMethods instead.")]
#if NETCOREAPP3_0_OR_GREATER
        public static MethodInfo? GetMethod<T>(this T obj, string methodName, bool isPublic = false, bool isProtected = false, bool isPrivate = false)
#else
        public static MethodInfo GetMethod<T>(this T obj, string methodName, bool isPublic = false, bool isProtected = false, bool isPrivate = false)
#endif
            where T : class
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));
            if (methodName is null)
                throw new ArgumentNullException(nameof(methodName));

#if NETCOREAPP3_0_OR_GREATER
            MethodInfo? result = null;
#else
            MethodInfo result = null;
#endif

            var hostType = obj.GetType();
            while (result is null && hostType != null)
            {
                var method = hostType.GetTypeInfo().DeclaredMethods
                    .FirstOrDefault(m => m.Name == methodName && (!isPublic || m.IsPublic) && (!isProtected || m.IsFamily) && (!isPrivate || m.IsPrivate));
                if (method != null)
                {
                    result = method;
                }

                hostType = hostType.GetTypeInfo().BaseType;
            }

            return result;
        }
    }
}
