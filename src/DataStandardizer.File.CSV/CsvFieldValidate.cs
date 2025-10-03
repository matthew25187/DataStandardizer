namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Delegate for a method to be called when a CSV field value is validated.
    /// </summary>
    /// <typeparam name="TModel">Type of the CSV line model.</typeparam>
    /// <param name="context">Context for the CSV field being processed.</param>
    /// <returns><c>true</c> if the field value is valid; <c>false</c> if not.</returns>
    public delegate bool CsvFieldValidate<TModel>(CsvFieldContext<TModel> context) where TModel : class;
}