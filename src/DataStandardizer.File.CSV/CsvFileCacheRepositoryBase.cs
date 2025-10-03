using System;
using System.Collections.Generic;
using System.ComponentModel;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.File.CSV
{
    public abstract class CsvFileCacheRepositoryBase
    {
        protected internal static readonly Dictionary<Type, ICsvFileMapper> DeclarativeMapperCache = new Dictionary<Type, ICsvFileMapper>();
        protected internal static readonly Dictionary<Type, ICsvFileMapper> ImperativeMapperCache = new Dictionary<Type, ICsvFileMapper>();
        protected static readonly string[] StandardLineBreaks = new[] { "\n", "\r", "\r\n" };
        private static readonly Dictionary<Type, TypeConverter> TypeConverterCache = new Dictionary<Type, TypeConverter>();

#if NETCOREAPP3_0_OR_GREATER
        protected TypeConverter? GetTypeConverter(string typeName)
#else
        [CanBeNull]
        protected TypeConverter GetTypeConverter(string typeName)
#endif
        {
            var typeConverterType = Type.GetType(typeName);
            if (typeConverterType is null)
            {
                return null;
            }

            return GetTypeConverter(typeConverterType);
        }

#if NETCOREAPP3_0_OR_GREATER
        protected TypeConverter? GetTypeConverter(Type typeConverterType)
#else
        [CanBeNull]
        protected TypeConverter GetTypeConverter(Type typeConverterType)
#endif
        {
            if (!TypeConverterCache.TryGetValue(typeConverterType, out var typeConverter))
            {
                typeConverter = Activator.CreateInstance(typeConverterType) as TypeConverter;

                if (typeConverter != null)
                {
                    TypeConverterCache.Add(typeConverterType, typeConverter);
                }
            }

            return typeConverter;
        }
    }
}