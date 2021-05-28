namespace Form8snCore.FileFormats
{
    public static class Strings
    {
        // TODO: replace literal uses with these constants
        public const char Separator = '\x1F';
        public const string FilterMarker = "#";
        public const string PageDataMarker = "P";
        
        /// <summary>
        /// Apply this to user inputs for Box names and Filter names.
        /// Prevents accidental use of path elements
        /// </summary>
        public static string? CleanKeyName(string? name)
        {
            if (name == "#") return "＃"; // 'fullwidth' octothorpe
            return name?.Replace('.', '·'); // mid-dot
        }
    }
}