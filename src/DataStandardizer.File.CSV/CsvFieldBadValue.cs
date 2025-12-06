namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Delegate for a method to be called when a bad CSV field value is encountered.
    /// </summary>
    /// <typeparam name="TModel">Type of the CSV line model.</typeparam>
    /// <param name="context">Context of the field being read or written.</param>
    public delegate void CsvFieldBadValue<TModel>(CsvFieldContext<TModel> context) where TModel : class;
}