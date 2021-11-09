namespace Form8snCore.FileFormats
{
    public class RepeatMode
    {
        /// <summary>
        /// If true, the data path must be set.
        /// </summary>
        public bool Repeats { get; set; }

        /// <summary>
        /// Path into data, or name of filter.
        /// If the path points to a single item or object, you will get only one page.
        /// If the path doesn't exist in the data set, this page will be skipped.
        /// </summary>
        public string[]? DataPath { get; set; }
    }
}