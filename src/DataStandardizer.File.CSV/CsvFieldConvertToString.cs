namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Delegate for a method to be called when a CSV field value is serialized.
    /// </summary>
    /// <typeparam name="TModel">Type of the CSV line model.</typeparam>
    /// <param name="context">Context for the CSV field being processed.</param>
    /// <returns>Serialized CSV field value.</returns>
    public delegate string CsvFieldConvertToString<TModel>(CsvFieldContext<TModel> context)
        where TModel : class;
}