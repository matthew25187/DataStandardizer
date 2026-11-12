using System;
using System.Collections.Generic;
using System.Reflection;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// A property that mapping considers on a record line or custom model type: one the type declares, or one it inherits.
    /// </summary>
    internal sealed class CsvMappableProperty
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private CsvMappableProperty(PropertyInfo declaration, PropertyInfo? getter, PropertyInfo? setter)
#else
        private CsvMappableProperty(PropertyInfo declaration, PropertyInfo getter, PropertyInfo setter)
#endif
        {
            Declaration = declaration;
            Getter = getter;
            Setter = setter;
        }

        /// <summary>
        /// Gets the most derived declaration of the property.  Its attributes, with those it inherits from the property it overrides, configure
        /// the mapping.
        /// </summary>
        internal PropertyInfo Declaration { get; }

        /// <summary>
        /// Gets the declaration whose getter reads the property, or <c>null</c> if it can't be read.
        /// </summary>
        /// <remarks>
        /// Where an override declares only a setter, this is the declaration it overrides.  The getter is virtual, so calling it on an instance
        /// runs the most derived getter.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal PropertyInfo? Getter { get; }
#else
        internal PropertyInfo Getter { get; }
#endif

        /// <summary>
        /// Gets the declaration whose setter writes the property, or <c>null</c> if it can't be written.
        /// </summary>
        /// <remarks>
        /// Where an override declares only a getter, this is the declaration it overrides.  The setter is virtual, so calling it on an instance
        /// runs the most derived setter.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal PropertyInfo? Setter { get; }
#else
        internal PropertyInfo Setter { get; }
#endif

        /// <summary>
        /// Gets the properties that mapping considers on a type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The properties are those the type declares and those it inherits, so a base type's mapping applies to its derived types.  A base
        /// type's property whose accessors are all private isn't inherited, and indexers can't be mapped, so neither is included.  The walk up the
        /// hierarchy stops at the types this library defines, which declare no mappable properties.
        /// </para>
        /// <para>
        /// Where a type redeclares a base type's property, by overriding or hiding it, the property is included once, so a property name has one
        /// key on a type.  The properties are in declaration order, with a base type's ahead of its derived type's, and a redeclared property
        /// keeps the place of its first declaration.
        /// </para>
        /// </remarks>
        internal static CsvMappableProperty[] GetAll(Type type)
        {
            var typeChain = GetTypeChain(type);
            var declarationsByName = new Dictionary<string, List<PropertyInfo>>(StringComparer.Ordinal);
            var propertyNames = new List<string>();

            // From the base type down, so each list of declarations ends up most derived first.
            for (var level = typeChain.Count - 1; level >= 0; level--)
            {
                foreach (var pi in typeChain[level].DeclaredProperties)
                {
                    if (IsIndexer(pi) || (level > 0 && IsPrivate(pi)))
                    {
                        continue;
                    }

                    if (!declarationsByName.TryGetValue(pi.Name, out var declarations))
                    {
                        declarations = new List<PropertyInfo>();
                        declarationsByName.Add(pi.Name, declarations);
                        propertyNames.Add(pi.Name);
                    }

                    declarations.Insert(0, pi);
                }
            }

            var properties = new CsvMappableProperty[propertyNames.Count];
            for (var i = 0; i < properties.Length; i++)
            {
                var declarations = declarationsByName[propertyNames[i]];
                properties[i] = new CsvMappableProperty(
                    declarations[0],
                    FindAccessorDeclaration(declarations, isGetter: true),
                    FindAccessorDeclaration(declarations, isGetter: false));
            }

            return properties;
        }

        /// <summary>
        /// Gets the most derived declaration of the property with the given name on a type, or <c>null</c> if it has none.
        /// </summary>
        /// <remarks>
        /// It is the <see cref="Declaration"/> of the property with that name that <see cref="GetAll"/> returns.  Where there is none, it is the
        /// most derived declaration of a base type's private property with that name, since such a property reads and writes its own field on a
        /// derived type's instance too.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static PropertyInfo? FindDeclaration(Type type, string propertyName)
#else
        internal static PropertyInfo FindDeclaration(Type type, string propertyName)
#endif
        {
            foreach (var property in GetAll(type))
            {
                if (string.Equals(property.Declaration.Name, propertyName, StringComparison.Ordinal))
                {
                    return property.Declaration;
                }
            }

            foreach (var typeInfo in GetTypeChain(type))
            {
                foreach (var pi in typeInfo.DeclaredProperties)
                {
                    if (string.Equals(pi.Name, propertyName, StringComparison.Ordinal) && !IsIndexer(pi))
                    {
                        return pi;
                    }
                }
            }

            return null;
        }

        // The type and its base types, most derived first, up to the first that is object or a type this library defines.
        private static List<TypeInfo> GetTypeChain(Type type)
        {
            var libraryAssembly = typeof(CsvMappableProperty).GetTypeInfo().Assembly;
            var typeChain = new List<TypeInfo>();
            var currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                var typeInfo = currentType.GetTypeInfo();
                if (typeInfo.Assembly == libraryAssembly)
                {
                    break;
                }

                typeChain.Add(typeInfo);
                currentType = typeInfo.BaseType;
            }

            return typeChain;
        }

        // The most derived declaration with the accessor, following overrides only: a declaration that hides one with `new` ends the search.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static PropertyInfo? FindAccessorDeclaration(List<PropertyInfo> declarations, bool isGetter)
#else
        private static PropertyInfo FindAccessorDeclaration(List<PropertyInfo> declarations, bool isGetter)
#endif
        {
            foreach (var pi in declarations)
            {
                var accessor = isGetter ? pi.GetMethod : pi.SetMethod;
                if (accessor != null)
                {
                    return pi;
                }

                if (!IsOverride(pi))
                {
                    return null;
                }
            }

            return null;
        }

        private static bool IsOverride(PropertyInfo pi)
        {
            var accessor = pi.GetMethod ?? pi.SetMethod;
            return accessor != null && accessor.IsVirtual && (accessor.Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.ReuseSlot;
        }

        private static bool IsPrivate(PropertyInfo pi)
        {
            return (pi.GetMethod == null || pi.GetMethod.IsPrivate) && (pi.SetMethod == null || pi.SetMethod.IsPrivate);
        }

        private static bool IsIndexer(PropertyInfo pi)
        {
            return pi.GetIndexParameters().Length > 0;
        }
    }
}
