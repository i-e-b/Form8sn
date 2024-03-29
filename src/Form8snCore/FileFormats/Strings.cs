namespace Form8snCore.FileFormats
{
    public static class Strings
    {
        public const char Separator = '.';
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