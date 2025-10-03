namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Delegate for a method to be called when an inconsistent field count is detected.
    /// </summary>
    /// <typeparam name="TModel">Type of the CSV line model.</typeparam>
    /// <param name="context">Context for the CSV field being processed.</param>
    public delegate void CsvFieldCount<TModel>(CsvFieldContext<TModel> context) where TModel : class;
}