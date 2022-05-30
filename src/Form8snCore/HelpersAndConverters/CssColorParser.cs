using System;
using System.Collections.Generic;

namespace Form8snCore.HelpersAndConverters
{
    /// <summary>
    /// Parse a css color string into an ARGB byte set.
    /// This tries to be as accepting of non-standard input as possible.
    /// </summary>
    /// <remarks>
    /// Coded based on https://github.com/deanm/css-color-parser-js
    /// but our output order and format is different: int[a,r,g,b] all in range 0..255; Deanm's code is [int r, int g, int b, float a]
    /// </remarks>
    public class CssColorParser
    {
        private static void AddColor(string name, params int[] argb) => _kCssColorTable.Add(name, argb);

        private const int AlphaSolid = 255;
        private const int AlphaZero = 0;
        
        static CssColorParser() // set up named colors
        {
            AddColor("transparent"          , AlphaZero  , 0   , 0   , 0   );
            AddColor("aliceblue"            , AlphaSolid , 240 , 248 , 255 );
            AddColor("antiquewhite"         , AlphaSolid , 250 , 235 , 215 );
            AddColor("aqua"                 , AlphaSolid , 0   , 255 , 255 );
            AddColor("aquamarine"           , AlphaSolid , 127 , 255 , 212 );
            AddColor("azure"                , AlphaSolid , 240 , 255 , 255 );
            AddColor("beige"                , AlphaSolid , 245 , 245 , 220 );
            AddColor("bisque"               , AlphaSolid , 255 , 228 , 196 );
            AddColor("black"                , AlphaSolid , 0   , 0   , 0   );
            AddColor("blanchedalmond"       , AlphaSolid , 255 , 235 , 205 );
            AddColor("blue"                 , AlphaSolid , 0   , 0   , 255 );
            AddColor("blueviolet"           , AlphaSolid , 138 , 43  , 226 );
            AddColor("brown"                , AlphaSolid , 165 , 42  , 42  );
            AddColor("burlywood"            , AlphaSolid , 222 , 184 , 135 );
            AddColor("cadetblue"            , AlphaSolid , 95  , 158 , 160 );
            AddColor("chartreuse"           , AlphaSolid , 127 , 255 , 0   );
            AddColor("chocolate"            , AlphaSolid , 210 , 105 , 30  );
            AddColor("coral"                , AlphaSolid , 255 , 127 , 80  );
            AddColor("cornflowerblue"       , AlphaSolid , 100 , 149 , 237 );
            AddColor("cornsilk"             , AlphaSolid , 255 , 248 , 220 );
            AddColor("crimson"              , AlphaSolid , 220 , 20  , 60  );
            AddColor("cyan"                 , AlphaSolid , 0   , 255 , 255 );
            AddColor("darkblue"             , AlphaSolid , 0   , 0   , 139 );
            AddColor("darkcyan"             , AlphaSolid , 0   , 139 , 139 );
            AddColor("darkgoldenrod"        , AlphaSolid , 184 , 134 , 11  );
            AddColor("darkgray"             , AlphaSolid , 169 , 169 , 169 );
            AddColor("darkgreen"            , AlphaSolid , 0   , 100 , 0   );
            AddColor("darkgrey"             , AlphaSolid , 169 , 169 , 169 );
            AddColor("darkkhaki"            , AlphaSolid , 189 , 183 , 107 );
            AddColor("darkmagenta"          , AlphaSolid , 139 , 0   , 139 );
            AddColor("darkolivegreen"       , AlphaSolid , 85  , 107 , 47  );
            AddColor("darkorange"           , AlphaSolid , 255 , 140 , 0   );
            AddColor("darkorchid"           , AlphaSolid , 153 , 50  , 204 );
            AddColor("darkred"              , AlphaSolid , 139 , 0   , 0   );
            AddColor("darksalmon"           , AlphaSolid , 233 , 150 , 122 );
            AddColor("darkseagreen"         , AlphaSolid , 143 , 188 , 143 );
            AddColor("darkslateblue"        , AlphaSolid , 72  , 61  , 139 );
            AddColor("darkslategray"        , AlphaSolid , 47  , 79  , 79  );
            AddColor("darkslategrey"        , AlphaSolid , 47  , 79  , 79  );
            AddColor("darkturquoise"        , AlphaSolid , 0   , 206 , 209 );
            AddColor("darkviolet"           , AlphaSolid , 148 , 0   , 211 );
            AddColor("deeppink"             , AlphaSolid , 255 , 20  , 147 );
            AddColor("deepskyblue"          , AlphaSolid , 0   , 191 , 255 );
            AddColor("dimgray"              , AlphaSolid , 105 , 105 , 105 );
            AddColor("dimgrey"              , AlphaSolid , 105 , 105 , 105 );
            AddColor("dodgerblue"           , AlphaSolid , 30  , 144 , 255 );
            AddColor("firebrick"            , AlphaSolid , 178 , 34  , 34  );
            AddColor("floralwhite"          , AlphaSolid , 255 , 250 , 240 );
            AddColor("forestgreen"          , AlphaSolid , 34  , 139 , 34  );
            AddColor("fuchsia"              , AlphaSolid , 255 , 0   , 255 );
            AddColor("gainsboro"            , AlphaSolid , 220 , 220 , 220 );
            AddColor("ghostwhite"           , AlphaSolid , 248 , 248 , 255 );
            AddColor("gold"                 , AlphaSolid , 255 , 215 , 0   );
            AddColor("goldenrod"            , AlphaSolid , 218 , 165 , 32  );
            AddColor("gray"                 , AlphaSolid , 128 , 128 , 128 );
            AddColor("green"                , AlphaSolid , 0   , 128 , 0   );
            AddColor("greenyellow"          , AlphaSolid , 173 , 255 , 47  );
            AddColor("grey"                 , AlphaSolid , 128 , 128 , 128 );
            AddColor("honeydew"             , AlphaSolid , 240 , 255 , 240 );
            AddColor("hotpink"              , AlphaSolid , 255 , 105 , 180 );
            AddColor("indianred"            , AlphaSolid , 205 , 92  , 92  );
            AddColor("indigo"               , AlphaSolid , 75  , 0   , 130 );
            AddColor("ivory"                , AlphaSolid , 255 , 255 , 240 );
            AddColor("khaki"                , AlphaSolid , 240 , 230 , 140 );
            AddColor("lavender"             , AlphaSolid , 230 , 230 , 250 );
            AddColor("lavenderblush"        , AlphaSolid , 255 , 240 , 245 );
            AddColor("lawngreen"            , AlphaSolid , 124 , 252 , 0   );
            AddColor("lemonchiffon"         , AlphaSolid , 255 , 250 , 205 );
            AddColor("lightblue"            , AlphaSolid , 173 , 216 , 230 );
            AddColor("lightcoral"           , AlphaSolid , 240 , 128 , 128 );
            AddColor("lightcyan"            , AlphaSolid , 224 , 255 , 255 );
            AddColor("lightgoldenrodyellow" , AlphaSolid , 250 , 250 , 210 );
            AddColor("lightgray"            , AlphaSolid , 211 , 211 , 211 );
            AddColor("lightgreen"           , AlphaSolid , 144 , 238 , 144 );
            AddColor("lightgrey"            , AlphaSolid , 211 , 211 , 211 );
            AddColor("lightpink"            , AlphaSolid , 255 , 182 , 193 );
            AddColor("lightsalmon"          , AlphaSolid , 255 , 160 , 122 );
            AddColor("lightseagreen"        , AlphaSolid , 32  , 178 , 170 );
            AddColor("lightskyblue"         , AlphaSolid , 135 , 206 , 250 );
            AddColor("lightslategray"       , AlphaSolid , 119 , 136 , 153 );
            AddColor("lightslategrey"       , AlphaSolid , 119 , 136 , 153 );
            AddColor("lightsteelblue"       , AlphaSolid , 176 , 196 , 222 );
            AddColor("lightyellow"          , AlphaSolid , 255 , 255 , 224 );
            AddColor("lime"                 , AlphaSolid , 0   , 255 , 0   );
            AddColor("limegreen"            , AlphaSolid , 50  , 205 , 50  );
            AddColor("linen"                , AlphaSolid , 250 , 240 , 230 );
            AddColor("magenta"              , AlphaSolid , 255 , 0   , 255 );
            AddColor("maroon"               , AlphaSolid , 128 , 0   , 0   );
            AddColor("mediumaquamarine"     , AlphaSolid , 102 , 205 , 170 );
            AddColor("mediumblue"           , AlphaSolid , 0   , 0   , 205 );
            AddColor("mediumorchid"         , AlphaSolid , 186 , 85  , 211 );
            AddColor("mediumpurple"         , AlphaSolid , 147 , 112 , 219 );
            AddColor("mediumseagreen"       , AlphaSolid , 60  , 179 , 113 );
            AddColor("mediumslateblue"      , AlphaSolid , 123 , 104 , 238 );
            AddColor("mediumspringgreen"    , AlphaSolid , 0   , 250 , 154 );
            AddColor("mediumturquoise"      , AlphaSolid , 72  , 209 , 204 );
            AddColor("mediumvioletred"      , AlphaSolid , 199 , 21  , 133 );
            AddColor("midnightblue"         , AlphaSolid , 25  , 25  , 112 );
            AddColor("mintcream"            , AlphaSolid , 245 , 255 , 250 );
            AddColor("mistyrose"            , AlphaSolid , 255 , 228 , 225 );
            AddColor("moccasin"             , AlphaSolid , 255 , 228 , 181 );
            AddColor("navajowhite"          , AlphaSolid , 255 , 222 , 173 );
            AddColor("navy"                 , AlphaSolid , 0   , 0   , 128 );
            AddColor("oldlace"              , AlphaSolid , 253 , 245 , 230 );
            AddColor("olive"                , AlphaSolid , 128 , 128 , 0   );
            AddColor("olivedrab"            , AlphaSolid , 107 , 142 , 35  );
            AddColor("orange"               , AlphaSolid , 255 , 165 , 0   );
            AddColor("orangered"            , AlphaSolid , 255 , 69  , 0   );
            AddColor("orchid"               , AlphaSolid , 218 , 112 , 214 );
            AddColor("palegoldenrod"        , AlphaSolid , 238 , 232 , 170 );
            AddColor("palegreen"            , AlphaSolid , 152 , 251 , 152 );
            AddColor("paleturquoise"        , AlphaSolid , 175 , 238 , 238 );
            AddColor("palevioletred"        , AlphaSolid , 219 , 112 , 147 );
            AddColor("papayawhip"           , AlphaSolid , 255 , 239 , 213 );
            AddColor("peachpuff"            , AlphaSolid , 255 , 218 , 185 );
            AddColor("peru"                 , AlphaSolid , 205 , 133 , 63  );
            AddColor("pink"                 , AlphaSolid , 255 , 192 , 203 );
            AddColor("plum"                 , AlphaSolid , 221 , 160 , 221 );
            AddColor("powderblue"           , AlphaSolid , 176 , 224 , 230 );
            AddColor("purple"               , AlphaSolid , 128 , 0   , 128 );
            AddColor("rebeccapurple"        , AlphaSolid , 102 , 51  , 153 );
            AddColor("red"                  , AlphaSolid , 255 , 0   , 0   );
            AddColor("rosybrown"            , AlphaSolid , 188 , 143 , 143 );
            AddColor("royalblue"            , AlphaSolid , 65  , 105 , 225 );
            AddColor("saddlebrown"          , AlphaSolid , 139 , 69  , 19  );
            AddColor("salmon"               , AlphaSolid , 250 , 128 , 114 );
            AddColor("sandybrown"           , AlphaSolid , 244 , 164 , 96  );
            AddColor("seagreen"             , AlphaSolid , 46  , 139 , 87  );
            AddColor("seashell"             , AlphaSolid , 255 , 245 , 238 );
            AddColor("sienna"               , AlphaSolid , 160 , 82  , 45  );
            AddColor("silver"               , AlphaSolid , 192 , 192 , 192 );
            AddColor("skyblue"              , AlphaSolid , 135 , 206 , 235 );
            AddColor("slateblue"            , AlphaSolid , 106 , 90  , 205 );
            AddColor("slategray"            , AlphaSolid , 112 , 128 , 144 );
            AddColor("slategrey"            , AlphaSolid , 112 , 128 , 144 );
            AddColor("snow"                 , AlphaSolid , 255 , 250 , 250 );
            AddColor("springgreen"          , AlphaSolid , 0   , 255 , 127 );
            AddColor("steelblue"            , AlphaSolid , 70  , 130 , 180 );
            AddColor("tan"                  , AlphaSolid , 210 , 180 , 140 );
            AddColor("teal"                 , AlphaSolid , 0   , 128 , 128 );
            AddColor("thistle"              , AlphaSolid , 216 , 191 , 216 );
            AddColor("tomato"               , AlphaSolid , 255 , 99  , 71  );
            AddColor("turquoise"            , AlphaSolid , 64  , 224 , 208 );
            AddColor("violet"               , AlphaSolid , 238 , 130 , 238 );
            AddColor("wheat"                , AlphaSolid , 245 , 222 , 179 );
            AddColor("white"                , AlphaSolid , 255 , 255 , 255 );
            AddColor("whitesmoke"           , AlphaSolid , 245 , 245 , 245 );
            AddColor("yellow"               , AlphaSolid , 255 , 255 , 0   );
            AddColor("yellowgreen"          , AlphaSolid , 154 , 205 , 50  );
        }

        private static readonly Dictionary<string, int[]> _kCssColorTable = new();

        /// <summary>
        /// Clamp and round double to integer 0 .. 255.
        /// </summary>
        private static int ClampCssByte(double v)
        {
            v = Math.Round(v);
            return v < 0 ? 0 : v > 255 ? 255 : (int)v;
        }

        /// <summary>
        /// Parse a numeric or percent string into an int in range 0..255
        /// </summary>
        private static int ParseCssInt(string str)
        {
            // check for percentage
            if (str[str.Length - 1] == '%')
            {
                var num = str.Substring(0, str.Length - 2);
                if (!double.TryParse(num, out var d)) return 0;
                return ClampCssByte(d / 100 * 255);
            }

            if (!double.TryParse(str, out var di)) return 0;
            return ClampCssByte(di);
        }

        /// <summary>
        /// Parse a percent string into an int in range 0..255; or numeric string in range 0.0..1.0 into an int in range 0..255
        /// </summary>
        private static int ParseCssFloat(string str)
        {
            // check for percentage
            if (str[str.Length -1] == '%')
            {
                var num = str.Substring(0, str.Length - 2);
                if (!double.TryParse(num, out var d)) return 0;
                return ClampCssByte(d / 100.0 * 255.0);
            }

            if (!double.TryParse(str, out var di)) return 0;
            return ClampCssByte(di * 255.0);
        }

        private static double HslToRgbFrag(double m1, double m2, double h)
        {
            if (h < 0.0) h += 1.0;
            else if (h > 1.0) h -= 1.0;

            if (h * 6.0 < 1.0) return m1 + (m2 - m1) * h * 6.0;
            if (h * 2.0 < 1.0) return m2;
            if (h * 3.0 < 2.0) return m1 + (m2 - m1) * (2.0 / 3.0 - h) * 6.0;
            return m1;
        }

        public static int[] ParseCssColor(string? cssStr)
        {
            var result = new[] { 255, 0, 0, 0 };
            if (cssStr is null) return result;
            
            // Remove all whitespace, not compliant, but should just be more accepting.
            var str = cssStr.Trim().ToLowerInvariant();
            
            if (str.Length < 2) return result;

            // Color keywords (and transparent) lookup.
            if (_kCssColorTable.ContainsKey(str)) return _kCssColorTable[str]!;

            
            // #abc and #abc123 syntax.
            if (str[0] == '#')
            {
                if (str.Length == 4) // #rgb
                {
                    result[1] = ParseNybble(str[1]) * 17;
                    result[2] = ParseNybble(str[2]) * 17;
                    result[3] = ParseNybble(str[3]) * 17;
                    return result;
                }
                if (str.Length == 5) // #argb
                {
                    result[0] = ParseNybble(str[1]) * 17;
                    result[1] = ParseNybble(str[2]) * 17;
                    result[2] = ParseNybble(str[3]) * 17;
                    result[3] = ParseNybble(str[4]) * 17;
                    return result;
                }
                if (str.Length == 7) // #RrGgBb
                {
                    result[1] = ParseByte(str[1], str[2]);
                    result[2] = ParseByte(str[3], str[4]);
                    result[3] = ParseByte(str[5], str[6]);
                    return result;
                }
                if (str.Length == 9) // #AaRrGgBb
                {
                    result[0] = ParseByte(str[1], str[2]);
                    result[1] = ParseByte(str[3], str[4]);
                    result[2] = ParseByte(str[5], str[6]);
                    result[3] = ParseByte(str[7], str[8]);
                    return result;
                }

                // Not a valid input
                return result;
            }

            // rgb(), rgba(), hsl(), hsla()
            var op = str.IndexOf('(');
            var ep = str.IndexOf(')');
            if (op == -1 || ep + 1 != str.Length) return result; // not something we understand
            
            var colorTypeName = str.Substring(0, op);
            var args = str.Substring(op + 1, ep - (op + 1)).Split(',');
            switch (colorTypeName)
            {
                case "rgba":
                    if (args.Length >= 4) result[0] = ParseCssFloat(args[3]);
                    goto case "rgb"; // Fall through.
                    
                case "rgb":
                    if (args.Length < 3) return result;
                    result[1] = ParseCssInt(args[0]);
                    result[2] = ParseCssInt(args[1]);
                    result[3] = ParseCssInt(args[2]);
                    return result;
                    
                case "hsla":
                    if (args.Length >= 4) result[0] = ParseCssFloat(args[3]);
                    goto case "hsl"; // Fall through.

                case "hsl":
                    if (args.Length < 3) return result;
                    double.TryParse(args[0], out var hue);
                    hue = ((hue % 360.0) + 360.0 % 360.0) / 360.0; // fit in range 0..1

                    // According to the CSS spec s/l should only be percentages, but we allow float or percentage
                    var s = ParseCssFloat(args[1]);
                    var l = ParseCssFloat(args[2]);
                    var m2 = l <= 0.5 ? l * (s + 1) : l + s - l * s;
                    var m1 = l * 2 - m2;
                    result[1] = ClampCssByte(HslToRgbFrag(m1, m2, hue + 1.0 / 3.0) * 255.0);
                    result[2] = ClampCssByte(HslToRgbFrag(m1, m2, hue) * 255.0);
                    result[3] = ClampCssByte(HslToRgbFrag(m1, m2, hue - 1.0 / 3.0) * 255.0);
                    break;
            }

            return result;
        }

        private static int ParseNybble(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return 10 + (c - 'a');
            return 0;
        }

        private static int ParseByte(char l, char r)
        {
            return ParseNybble(l) * 16 + ParseNybble(r);
        }
    }
}