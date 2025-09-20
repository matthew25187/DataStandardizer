using System;
using System.Linq;
using System.Reflection;

namespace DataStandardizer.File
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Searches for the public method with the specified name.
        /// </summary>
        /// <typeparam name="T">Type of the object on which the method is implemented.</typeparam>
        /// <param name="obj">Object on which the method is implemented.</param>
        /// <param name="methodName">Name of the method to find.</param>
        /// <param name="isPublic">Flag indicating if the method should be declared public.</param>
        /// <param name="isProtected">Flag indicating if the method should be declared protected.</param>
        /// <param name="isPrivate">Flag indicating if the method should be declared private.</param>
        /// <returns>Information about the method found.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static MethodInfo? GetMethod<T>(this T obj, string methodName, bool isPublic = false, bool isProtected = false, bool isPrivate = false)
#else
        public static MethodInfo GetMethod<T>(this T obj, string methodName, bool isPublic = false, bool isProtected = false, bool isPrivate = false)
#endif
            where T : class
        {
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