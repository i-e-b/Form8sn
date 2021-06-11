using System;
using System.Collections.Generic;
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
        private static readonly Dictionary<string,string> _fontCache = new Dictionary<string, string>();
        private readonly Font _fontSpec;
        private readonly float _dpi;

        public PortableFont(Font fontSpec, float dpi)
        {
            _fontSpec = fontSpec;
            _dpi = dpi;
            
            EnsureFontCache();
            
            BaseTrueTypeFont = new TrueTypeFont(GetFontFilePath(fontSpec));
        }

        public TrueTypeFont BaseTrueTypeFont { get; }

        private static void EnsureFontCache()
        {
            if (_fontCache.Count < 1)
            {
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts, Environment.SpecialFolderOption.DoNotVerify);
                ReadAvailableFonts(basePath);
            }
        }

        public void Dispose() { }

        public void Select(IToolkitGraphics graphics)
        {
            graphics.Font = this;
        }

        public IntPtr GetHfont() => IntPtr.Zero;

        public void ToLogFont(object lf, IToolkitGraphics graphics) { }
        
        public static void ReadAvailableFonts(string basePath)
        {
            // Font file names usually bear little resemblance to the family and style names.
            // Our portable GDI reads the whole font library and jumps into headers to find the names.
            // This is time and memory expensive, so we cache the results aggressively
            var allFiles = Directory.EnumerateFiles(basePath, "*.ttf").ToList();

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
        
        // ReSharper disable once InconsistentNaming
        const double FUDGE_FACTOR = 1.5; // This is used to scale the fonts. Set to 1.0 to get the real size

        public int GetLineHeight()
        {
            // TODO: cache this out
            var scale = GetScale();
            var g1 = BaseTrueTypeFont.ReadGlyph('|') ?? BaseTrueTypeFont.ReadGlyph('$') ?? throw new Exception("Couldn't read sample glyph");
            return (int) ((g1.yMax - g1.yMin) * scale * 1.5 * FUDGE_FACTOR);
        }

        public double GetScale()
        {
            // DPI is normally 96 in the Windows world and 72 in the Mac world.
            return (_fontSpec.Size * FUDGE_FACTOR * (_dpi/72.0)) / BaseTrueTypeFont.Height();
        }
    }
}