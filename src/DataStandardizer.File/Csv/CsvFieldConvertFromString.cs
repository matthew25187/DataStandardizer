namespace DataStandardizer.File.Csv
{
    /// <summary>
    /// Delegate for a method to be called when a CSV field value is deserialized.
    /// </summary>
    /// <typeparam name="TModel">Type of the CSV line model.</typeparam>
    /// <typeparam name="T">Type of the property representing the CSV field.</typeparam>
    /// <param name="context">Context for the CSV field being processed.</param>
    /// <returns>Deserialized CSV field value.</returns>
    public delegate T CsvFieldConvertFromString<TModel, out T>(CsvFieldContext<TModel> context)
        where TModel : class;
}