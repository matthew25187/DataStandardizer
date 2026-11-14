using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Creates the parser a reader uses for a mapped property that has no converter configured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The parser of a type is chosen in this order, the same on every target, so that the same field value reads as the same property value
    /// wherever the package runs:
    /// </para>
    /// <list type="number">
    /// <item><description>The integer and floating-point types, <see cref="decimal"/>, <see cref="bool"/>, <see cref="char"/>, <see cref="DateTime"/>,
    /// <see cref="DateTimeOffset"/>, <see cref="TimeSpan"/> and <see cref="Guid"/>, with the number and date styles their
    /// <c>IParsable&lt;TSelf&gt;</c> implementations use on .NET 7 and later, except that a <see cref="DateTime"/> keeps the
    /// <see cref="DateTime.Kind"/> its value is written with (<see cref="DateTimeStyles.RoundtripKind"/>), so that a UTC time ending in <c>Z</c> or
    /// <c>GMT</c> reads as that UTC time and not as the machine's local time.  On .NET 6 and later, a <c>DateOnly</c> is read with the round-trip
    /// pattern (<c>"o"</c>) before the culture, so that a date a writer wrote reads back as the same date in a culture whose calendar isn't the
    /// Gregorian calendar.</description></item>
    /// <item><description>Enumerations, with <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>, case-sensitive.</description></item>
    /// <item><description>On .NET 7 and later, any other type that implements <c>ISpanParsable&lt;TSelf&gt;</c> or <c>IParsable&lt;TSelf&gt;</c>.</description></item>
    /// <item><description>The <see cref="TypeConverter"/> that <see cref="TypeDescriptor.GetConverter(Type)"/> gives for the type, if it converts from
    /// a string.</description></item>
    /// </list>
    /// <para>
    /// A <see cref="Nullable{T}"/> type uses the parser of its underlying type.  A type with none of these has no parser.
    /// </para>
    /// </remarks>
    internal static class CsvFieldParser
    {
        private const NumberStyles IntegerStyles = NumberStyles.Integer;
        private const NumberStyles FloatingPointStyles = NumberStyles.Float | NumberStyles.AllowThousands;
        private const NumberStyles DecimalStyles = NumberStyles.Number;
#if NET6_0_OR_GREATER
        private const string RoundTripFormat = "o";
#endif

        /// <summary>
        /// Gets the parser of a type, or <c>null</c> if the type has none.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static CsvFieldParser<T>? For<T>()
#else
        [return: MaybeNull]
        internal static CsvFieldParser<T> For<T>()
#endif
        {
            var type = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType is null)
            {
                return CreateParser(type) as CsvFieldParser<T>;
            }

            var underlyingParser = CreateParser(underlyingType);
            if (underlyingParser is null)
            {
                return null;
            }

            return Activator.CreateInstance(typeof(NullableParser<>).MakeGenericType(underlyingType), underlyingParser) as CsvFieldParser<T>;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static object? CreateParser(Type type)
#else
        [return: MaybeNull]
        private static object CreateParser(Type type)
#endif
        {
            var knownParser = CreateKnownParser(type);
            if (knownParser != null)
            {
                return knownParser;
            }

            if (type.GetTypeInfo().IsEnum)
            {
                return Activator.CreateInstance(typeof(EnumParser<>).MakeGenericType(type));
            }

#if NET7_0_OR_GREATER
            var parsableParserType = GetParsableParserType(type);
            if (parsableParserType != null)
            {
                return Activator.CreateInstance(parsableParserType.MakeGenericType(type));
            }
#endif

            var typeConverter = TypeDescriptor.GetConverter(type);
            if (typeConverter.CanConvertFrom(typeof(string)))
            {
                return Activator.CreateInstance(typeof(TypeConverterParser<>).MakeGenericType(type), typeConverter);
            }

            return null;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static object? CreateKnownParser(Type type)
#else
        [return: MaybeNull]
        private static object CreateKnownParser(Type type)
#endif
        {
            if (type == typeof(int)) return new DelegateParser<int>((string value, CultureInfo culture, out int result) => int.TryParse(value, IntegerStyles, culture, out result));
            if (type == typeof(long)) return new DelegateParser<long>((string value, CultureInfo culture, out long result) => long.TryParse(value, IntegerStyles, culture, out result));
            if (type == typeof(short)) return new DelegateParser<short>((string value, CultureInfo culture, out short result) => short.TryParse(value, IntegerStyles, culture, out result));
            if (type == typeof(sbyte)) return new DelegateParser<sbyte>((string value, CultureInfo culture, out sbyte result) => sbyte.TryParse(value, IntegerStyles, culture, out result));
            if (type == typeof(uint)) return new DelegateParser<uint>((string value, CultureInfo culture, out uint result) => uint.TryParse(value, IntegerStyles, culture, out result));
            if (type == typeof(ulong)) return new DelegateParser<ulong>((string value, CultureInfo culture, out ulong result) => ulong.TryParse(value, IntegerStyles, culture, out result));
            if (type == typeof(ushort)) return new DelegateParser<ushort>((string value, CultureInfo culture, out ushort result) => ushort.TryParse(value, IntegerStyles, culture, out result));
            if (type == typeof(byte)) return new DelegateParser<byte>((string value, CultureInfo culture, out byte result) => byte.TryParse(value, IntegerStyles, culture, out result));
            if (type == typeof(decimal)) return new DelegateParser<decimal>((string value, CultureInfo culture, out decimal result) => decimal.TryParse(value, DecimalStyles, culture, out result));
            if (type == typeof(double)) return new DelegateParser<double>(TryParseDouble);
            if (type == typeof(float)) return new DelegateParser<float>(TryParseSingle);
            if (type == typeof(bool)) return new DelegateParser<bool>((string value, CultureInfo culture, out bool result) => bool.TryParse(value, out result));
            if (type == typeof(char)) return new DelegateParser<char>((string value, CultureInfo culture, out char result) => char.TryParse(value, out result));
            if (type == typeof(DateTime)) return new DelegateParser<DateTime>((string value, CultureInfo culture, out DateTime result) => DateTime.TryParse(value, culture, DateTimeStyles.RoundtripKind, out result));
            if (type == typeof(DateTimeOffset)) return new DelegateParser<DateTimeOffset>((string value, CultureInfo culture, out DateTimeOffset result) => DateTimeOffset.TryParse(value, culture, DateTimeStyles.None, out result));
            if (type == typeof(TimeSpan)) return new DelegateParser<TimeSpan>((string value, CultureInfo culture, out TimeSpan result) => TimeSpan.TryParse(value, culture, out result));
            if (type == typeof(Guid)) return new DelegateParser<Guid>((string value, CultureInfo culture, out Guid result) => Guid.TryParse(value, out result));
#if NET6_0_OR_GREATER
            // A writer writes a DateOnly with the round-trip pattern, which is always in the Gregorian calendar.  Parsed with a culture whose calendar
            // isn't Gregorian, such as th-TH, the same text would be a date in that calendar.
            if (type == typeof(DateOnly)) return new DelegateParser<DateOnly>((string value, CultureInfo culture, out DateOnly result) =>
                DateOnly.TryParseExact(value, RoundTripFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
                || DateOnly.TryParse(value, culture, DateTimeStyles.None, out result));
#endif

            return null;
        }

#if NET7_0_OR_GREATER
        private static Type? GetParsableParserType(Type type)
        {
            Type? parsableParserType = null;
            foreach (var implementedInterface in type.GetInterfaces())
            {
                if (!implementedInterface.IsGenericType || implementedInterface.GetGenericArguments()[0] != type)
                {
                    continue;
                }

                var genericInterface = implementedInterface.GetGenericTypeDefinition();
                if (genericInterface == typeof(ISpanParsable<>))
                {
                    return typeof(SpanParsableParser<>);
                }

                if (genericInterface == typeof(IParsable<>))
                {
                    parsableParserType = typeof(ParsableParser<>);
                }
            }

            return parsableParserType;
        }
#endif

        // .NET Core 3.0 and later parse a number too large for the type as infinity, where .NET Framework fails, and they match the infinity and NaN
        // symbols without regard to case, where .NET Framework doesn't.  Accepting infinity or NaN only for the culture's own symbol, as written,
        // gives the same result on every runtime.  .NET Framework's parse isn't correctly rounded, so the targets that may run on it read the
        // nearest value themselves.
        private static bool TryParseDouble(string value, CultureInfo culture, out double result)
        {
#if NETCOREAPP3_0_OR_GREATER
            return double.TryParse(value, FloatingPointStyles, culture, out result)
#else
            return CsvFloatingPointParser.TryParseDouble(value, FloatingPointStyles, culture.NumberFormat, out result)
#endif
                   && ((!double.IsInfinity(result) && !double.IsNaN(result)) || IsSpecialValueSymbol(value, culture.NumberFormat));
        }

        private static bool TryParseSingle(string value, CultureInfo culture, out float result)
        {
#if NETCOREAPP3_0_OR_GREATER
            return float.TryParse(value, FloatingPointStyles, culture, out result)
#else
            return CsvFloatingPointParser.TryParseSingle(value, FloatingPointStyles, culture.NumberFormat, out result)
#endif
                   && ((!float.IsInfinity(result) && !float.IsNaN(result)) || IsSpecialValueSymbol(value, culture.NumberFormat));
        }

        private static bool IsSpecialValueSymbol(string value, NumberFormatInfo numberFormat)
        {
            var trimmedValue = value.Trim();
            return string.Equals(trimmedValue, numberFormat.PositiveInfinitySymbol, StringComparison.Ordinal)
                   || string.Equals(trimmedValue, numberFormat.NegativeInfinitySymbol, StringComparison.Ordinal)
                   || string.Equals(trimmedValue, numberFormat.NaNSymbol, StringComparison.Ordinal);
        }

        private delegate bool TryParseFunc<T>(string value, CultureInfo culture, out T result);

        private sealed class DelegateParser<T> : CsvFieldParser<T>
        {
            private readonly TryParseFunc<T> _tryParse;

            internal DelegateParser(TryParseFunc<T> tryParse)
            {
                _tryParse = tryParse;
            }

            internal override bool TryParse(string value, CultureInfo culture, [MaybeNullWhen(false)] out T result) => _tryParse(value, culture, out result);
        }

        private sealed class NullableParser<T> : CsvFieldParser<T?> where T : struct
        {
            private readonly CsvFieldParser<T> _underlyingParser;

            // Public, so that Activator.CreateInstance finds it on every target.
            public NullableParser(CsvFieldParser<T> underlyingParser)
            {
                _underlyingParser = underlyingParser;
            }

            internal override bool TryParse(string value, CultureInfo culture, [MaybeNullWhen(false)] out T? result)
            {
                if (_underlyingParser.TryParse(value, culture, out var underlyingResult))
                {
                    result = underlyingResult;
                    return true;
                }

                result = null;
                return false;
            }
        }

        private sealed class EnumParser<T> : CsvFieldParser<T> where T : struct
        {
            internal override bool TryParse(string value, CultureInfo culture, [MaybeNullWhen(false)] out T result) => Enum.TryParse(value, false, out result);
        }

#if NET7_0_OR_GREATER
        private sealed class SpanParsableParser<T> : CsvFieldParser<T> where T : ISpanParsable<T>
        {
            internal override bool TryParse(string value, CultureInfo culture, [MaybeNullWhen(false)] out T result) => T.TryParse(value.AsSpan(), culture, out result);
        }

        private sealed class ParsableParser<T> : CsvFieldParser<T> where T : IParsable<T>
        {
            internal override bool TryParse(string value, CultureInfo culture, [MaybeNullWhen(false)] out T result) => T.TryParse(value, culture, out result);
        }
#endif

        /// <summary>
        /// Parses with a <see cref="TypeConverter"/>, which reports a value it can't convert by throwing.  The caller handles the exception.
        /// </summary>
        private sealed class TypeConverterParser<T> : CsvFieldParser<T>
        {
            private readonly TypeConverter _typeConverter;

            // Public, so that Activator.CreateInstance finds it on every target.
            public TypeConverterParser(TypeConverter typeConverter)
            {
                _typeConverter = typeConverter;
            }

            internal override bool TryParse(string value, CultureInfo culture, [MaybeNullWhen(false)] out T result)
            {
                var convertedValue = _typeConverter.ConvertFromString(null, culture, value);
                if (convertedValue is T typedValue)
                {
                    result = typedValue;
                    return true;
                }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                result = default!;
#else
                result = default;
#endif

                // Null is a converted value for a type that can hold it, as it is for a converter configured on the mapping.
                return convertedValue is null && default(T) == null;
            }
        }
    }

    /// <summary>
    /// Parses a field value to a property type.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <remarks>
    /// Parsers hold no state that changes, so one instance is shared by every reader.
    /// </remarks>
    internal abstract class CsvFieldParser<T>
    {
        /// <summary>
        /// Parses a non-empty field value.
        /// </summary>
        /// <returns><c>true</c> if the value was parsed; <c>false</c> if it is not a valid value of the type.  A parser may instead throw.</returns>
        internal abstract bool TryParse(string value, CultureInfo culture, [MaybeNullWhen(false)] out T result);
    }
}
