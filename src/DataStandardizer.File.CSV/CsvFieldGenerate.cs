namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Delegate for a method to be called when a value is generated for a CSV field.
    /// </summary>
    /// <typeparam name="T">Type of the property representing the CSV field.</typeparam>
    /// <returns>A CSV field value.</returns>
    public delegate T CsvFieldGenerate<out T>();
}