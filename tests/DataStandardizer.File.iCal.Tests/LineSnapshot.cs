using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// The content of a line, taken either from an <see cref="IcalContentLine"/> or field by field through a view's own accessors.
    /// </summary>
    internal sealed class LineSnapshot
    {
        public LineSnapshot(string name, ParameterSnapshot[] parameters, string value, long lineNumber, long physicalLineNumber)
        {
            Name = name;
            Parameters = parameters;
            Value = value;
            LineNumber = lineNumber;
            PhysicalLineNumber = physicalLineNumber;
        }

        public string Name { get; }

        public ParameterSnapshot[] Parameters { get; }

        public string Value { get; }

        public long LineNumber { get; }

        public long PhysicalLineNumber { get; }

        public static LineSnapshot FromLine(IcalContentLine line)
        {
            var parameters = line.Parameters.Select(parameter => new ParameterSnapshot(parameter.Name, parameter.Values.ToArray())).ToArray();
            return new LineSnapshot(line.Name, parameters, line.Value, line.LineNumber, line.PhysicalLineNumber);
        }

        public static LineSnapshot FromView(IcalContentLineView view)
        {
            var parameters = new ParameterSnapshot[view.ParameterCount];
            for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                var values = new string[view.GetParameterValueCount(parameterIndex)];
                for (var valueIndex = 0; valueIndex < values.Length; valueIndex++)
                {
                    values[valueIndex] = view.GetParameterValue(parameterIndex, valueIndex).ToString();
                }

                parameters[parameterIndex] = new ParameterSnapshot(view.GetParameterName(parameterIndex).ToString(), values);
            }

            return new LineSnapshot(view.Name.ToString(), parameters, view.RawValue.ToString(), view.LineNumber, view.PhysicalLineNumber);
        }

        public LineSnapshot WithoutLineNumbers() => new LineSnapshot(Name, Parameters, Value, 0, 0);
    }

    internal sealed class ParameterSnapshot
    {
        public ParameterSnapshot(string name, string[] values)
        {
            Name = name;
            Values = values;
        }

        public string Name { get; }

        public string[] Values { get; }
    }
}
