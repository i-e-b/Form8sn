using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Portable.Drawing.Drawing2D;
using Portable.Drawing.Toolkit.Fonts;

namespace Portable.Drawing.Toolkit.Portable
{
    /// <summary>
    /// Platform independent font reading.
    /// This handles ONLY true-type fonts.
    /// </summary>
    public class PortableFont : IToolkitFont
    {
        /// <summary>
        /// Display Name => Font File Path
        /// </summary>
        private static readonly Dictionary<string,string> _fontCache = new Dictionary<string, string>();
        private readonly Font _fontSpec;
        private readonly float _dpi;
        
        public TrueTypeFont BaseTrueTypeFont { get; }

        public PortableFont(Font fontSpec, float dpi)
        {
            _fontSpec = fontSpec;
            _dpi = dpi;
            
            EnsureFontCache();
            
            BaseTrueTypeFont = new TrueTypeFont(GetFontFilePath(fontSpec));
        }

        /// <summary>
        /// Lists the display names of all fonts found on the system
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> ListKnownFonts()
        {
            EnsureFontCache();
            return _fontCache.Keys.ToList();
        }

        private static void EnsureFontCache()
        {
            if (_fontCache.Count < 1)
            {
                // Try the path where dotnet thinks the fonts should be
                // and try some other locations, as the dotnet one is often just ~/.fonts on Linux
                var possiblePaths = new []{
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), "AppData/Local/Microsoft/Windows/Fonts/"),
                    Environment.GetFolderPath(Environment.SpecialFolder.Fonts, Environment.SpecialFolderOption.DoNotVerify),
                    "/usr/share/fonts/",
                    "/usr/local/share/fonts",
                    "/usr/X11R6/lib/X11/fonts/"
                };
                foreach (var path in possiblePaths)
                {
                    try
                    {
                        ReadAvailableFonts(path);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        public void Dispose() { }

        public void Select(IToolkitGraphics graphics)
        {
            graphics.Font = this;
        }

        public IntPtr GetHfont() => IntPtr.Zero;

        public void ToLogFont(object lf, IToolkitGraphics graphics) { }

        private static void ReadAvailableFonts(string basePath)
        {
            if (!Directory.Exists(basePath)) return;
            
            // Font file names usually bear little resemblance to the family and style names.
            // Our portable GDI reads the whole font library and jumps into headers to find the names.
            // This is time and memory expensive, so we cache the results aggressively
            var allFiles = Directory.EnumerateFiles(basePath, "*.ttf", SearchOption.AllDirectories).ToList();

            foreach (var file in allFiles)
            {
                try
                {
                    var ttf = new TrueTypeFont(file);
                    var nameTable = ttf.GetTable("name") as TtfTableName;
                    if (nameTable?.Names == null) continue;

                    var names = nameTable.Names
                        .Where(n => n.NameId == 1 || (n.NameId == 4 && EnglishLanguage(n)) ).ToList(); // 1 = 'family name' (like "Courier New"); 4 = style name (like "Courier New Bold");

                    foreach (var name in names)
                    {
                        var key = name.PlatformId == 3 && name.EncodingId == 1
                            ? Encoding.BigEndianUnicode.GetString(name.ByteStringValue)
                            : Encoding.UTF8.GetString(name.ByteStringValue);
                        
                        if (LooksLikeBadName(key)) continue;

                        if (!_fontCache.ContainsKey(key)) _fontCache.Add(key, file);
                    }
                }
                catch (BadImageFormatException)
                {
                    Console.WriteLine($"Not a true-type file: {file}");
                }
                catch (Exception ex)
                {
                    var str = ex.ToString();
                    Console.WriteLine(str);
                    // ignoring
                }
            }
        }

        /// <summary>
        /// Tries to detect incorrectly encoded names.
        /// If the first character is non-ascii, but the second is -- then we guess the encoding is wrong
        /// </summary>
        private static bool LooksLikeBadName(string key)
        {
            if (key.Length < 1) return true;
            if (key.Length < 2) return false;
            
            return !IsAscii(key[0]) && IsAscii(key[1]);
        }

        private static bool IsAscii(char c)
        {
            return c >= '0' && c <= 'z';
        }

        private static bool EnglishLanguage(NameRecord name)
        {
            return name.LanguageId == 0 // Macintosh
                   // Windows country-specific English variants:
                || name.LanguageId == 0x0C09
                || name.LanguageId == 0x2809
                || name.LanguageId == 0x1009
                || name.LanguageId == 0x2409
                || name.LanguageId == 0x4009
                || name.LanguageId == 0x1809
                || name.LanguageId == 0x2009
                || name.LanguageId == 0x4409
                || name.LanguageId == 0x1409
                || name.LanguageId == 0x3409
                || name.LanguageId == 0x4809
                || name.LanguageId == 0x1C09
                || name.LanguageId == 0x2C09
                || name.LanguageId == 0x0809
                || name.LanguageId == 0x0409
                || name.LanguageId == 0x3009;
        }


        private static string BuildStyledName(Font gdiFont)
        {
            var sb = new StringBuilder();
            sb.Append(gdiFont.FontFamily.Name);
            if (gdiFont.Bold) sb.Append(" Bold");
            if (gdiFont.Italic) sb.Append(" Italic");
            return sb.ToString();
        }

        private static string GetFontFilePath(Font gdiFont)
        {
            EnsureFontCache();
            
            var prefKey = BuildStyledName(gdiFont);
            var fallbackKey = gdiFont.FontFamily.Name;
            if (_fontCache.ContainsKey(prefKey)) return _fontCache[prefKey]!;
            if (_fontCache.ContainsKey(fallbackKey)) return _fontCache[fallbackKey]!;
            // TODO: pick a final fallback font when we build the cache

            throw new Exception("Font file is not available");
        }

        public static byte[] ReadFontFileBytes(Font gdiFont)
        {
            EnsureFontCache();
            return File.ReadAllBytes(GetFontFilePath(gdiFont));
        }

        public int GetLineHeight()
        {
            // IEB: Need to read font's vertical offsets tables
            var scale = GetScale();
            var g1 = BaseTrueTypeFont.ReadGlyph('|') ?? BaseTrueTypeFont.ReadGlyph('$') ?? throw new Exception("Couldn't read sample glyph");
            var g2 = BaseTrueTypeFont.ReadGlyph('y') ?? BaseTrueTypeFont.ReadGlyph('g') ?? throw new Exception("Couldn't read sample glyph");
            var fudge = 3.0;
            return (int) ((g1.yMax - g2.yMin) * scale * fudge * EmScale);
        }
        
        public double EmScale => 1000.0 / BaseTrueTypeFont.Header.UnitsPerEm;

        public double GetScale()
        {
            // DPI is normally 96 in the Windows world and 72 in the Mac world.
            //var dpiAdjust = _dpi/72.0;
            // Should be: pointSize * resolution / (72 points per inch * units_per_em)
            // see: https://developer.apple.com/fonts/TrueType-Reference-Manual/RM02/Chap2.html#converting
            return (_fontSpec.Size * _dpi) / (72 * BaseTrueTypeFont.Header.UnitsPerEm);
        }
    }
}