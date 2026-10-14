namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// The content of a line, taken either from an <see cref="IcalContentLine"/> or field by field through a view's own accessors.
/// </summary>
internal sealed record LineSnapshot(string Name, ParameterSnapshot[] Parameters, string Value, long LineNumber, long PhysicalLineNumber)
{
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
}

internal sealed record ParameterSnapshot(string Name, string[] Values);
