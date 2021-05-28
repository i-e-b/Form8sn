namespace BasicImageFormFiller.EditForms.PropertyGridSpecialTypes
{
    /// <summary>
    /// A marker class to get string values to store in the index.json.
    /// This lets us have .ToString() be user-friendly, but have a good serialisable value for the file.
    /// </summary>
    public interface ISpecialString
    {
        public string? StringValue { get; }
    }
}