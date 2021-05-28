namespace Form8snCore.FileFormats
{
    public static class Strings
    {
        // TODO: replace literal uses with these constants
        public const char Separator = '\x1F';
        public const string FilterMarker = "#";
        public const string PageDataMarker = "P";
        
        public static string? CleanKeyName(string? name) => name?.Replace('.', '·'); // prevent '.' in names from breaking paths.
    }
}