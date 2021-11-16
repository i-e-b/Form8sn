using System;
using System.Globalization;
using System.Text;
using Form8snCore.FileFormats;

namespace Form8snCore.DataExtraction
{
    public class DisplayFormatter
    {
        public static string? ApplyFormat(DisplayFormatFilter? format, string? str)
        {
            if (str is null) return null;
            if (format is null) return str;
            var type = format.Type;

            switch (type)
            {
                case DisplayFormatType.None:
                case DisplayFormatType.RenderImage:
                    return str;

                case DisplayFormatType.DateFormat:
                    return ReformatDate(format, str);

                case DisplayFormatType.NumberFormat:
                    return ReformatNumber(format, str);
                
                case DisplayFormatType.Integral:
                    return ReformatNumberToInt(str);
                    
                case DisplayFormatType.Fractional:
                    return ReformatNumberToFrac(format, str);

                default:
                    return null;
            }
        }

        
        private static string? ReformatNumberToInt(string str)
        {
            if (!decimal.TryParse(str, out var value)) return null;
            
            var intPart = (long)Math.Truncate(value);
            
            return intPart.ToString();
        }
        
        private static string? ReformatNumberToFrac(DisplayFormatFilter format, string str)
        {
            var param = format.FormatParameters;
            
            if (!decimal.TryParse(str, out var value)) return null;
            
            var dpKey = nameof(FractionalDisplayParams.DecimalPlaces);
            var dpStr = param.ContainsKey(dpKey) ? param[dpKey]??"" : "2";
            if (!int.TryParse(dpStr, out var decimalPlaces)) decimalPlaces = 2;
            if (decimalPlaces < 0 || decimalPlaces > 20) decimalPlaces = 2;
            
            var scale = (decimal)Math.Pow(10.0, decimalPlaces);
            var intPart = Math.Truncate(value);
            var fracPart = value - intPart;
            
            var final = Math.Round((1+fracPart)*scale, 0); // add 1 to get leading zeros
            return final.ToString(CultureInfo.InvariantCulture)!.Substring(1); // remove the added 1
        }

        private static string? ReformatNumber(DisplayFormatFilter format, string str)
        {
            var param = format.FormatParameters;

            if (!decimal.TryParse(str, out var value)) return null;

            var dpKey = nameof(NumberDisplayParams.DecimalPlaces);
            var dpStr = param.ContainsKey(dpKey) ? param[dpKey]??"" : "2";
            if (!int.TryParse(dpStr, out var decimalPlaces)) decimalPlaces = 2;
            if (decimalPlaces < 0 || decimalPlaces > 20) decimalPlaces = 2;

            var tsKey = nameof(NumberDisplayParams.ThousandsSeparator);
            var thousands = param.ContainsKey(tsKey) ? param[tsKey]??"" : "";

            var dcKey = nameof(NumberDisplayParams.DecimalSeparator);
            var decimalSeparator = param.ContainsKey(dcKey) ? param[dcKey]??"" : ".";
            if (string.IsNullOrEmpty(decimalSeparator)) decimalSeparator = ".";

            var preKey = nameof(NumberDisplayParams.Prefix);
            var prefix = param.ContainsKey(preKey) ? param[preKey] : "";

            var postKey = nameof(NumberDisplayParams.Postfix);
            var postfix = param.ContainsKey(postKey) ? param[postKey] : "";


            var result = FloatToString(value, decimalPlaces, decimalPlaces, decimalSeparator, thousands);


            return prefix + result + postfix;
        }

        public static string FloatToString(decimal floatValue, int minDecimalPlaces, int maxDecimalPlaces, string decimalPlace, string thousandsPlace)
        {
            var result = new StringBuilder();
            var dotted = false;
            var unsignedValue = floatValue;

            // sign if required
            if (unsignedValue < 0)
            {
                result.Append('-');
                unsignedValue = -unsignedValue;
            }

            var scale = (decimal)Math.Pow(10.0, maxDecimalPlaces);
            var rounded = Math.Round(unsignedValue * scale, MidpointRounding.AwayFromZero) / scale;

            var intPart = Math.Truncate(rounded);
            var fracPart = rounded - intPart;

            // integral part
            result.Append(intPart.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(thousandsPlace)) result = new StringBuilder(InjectThousands(result.ToString(), thousandsPlace));
            if (maxDecimalPlaces < 1) return result.ToString();

            // do fractional part
            var tail = minDecimalPlaces;
            var head = maxDecimalPlaces;

            while (head > 0 // we haven't hit max dp
                   && fracPart > 0 // there is still value in the fraction
            )
            {
                fracPart *= 10; // shift
                var digit = Math.Truncate(fracPart); // truncate
                if (head == 1) digit = Math.Round(fracPart);
                fracPart -= digit;

                head--;
                tail--;

                if (!dotted)
                {
                    result.Append(decimalPlace);
                    dotted = true;
                }

                result.Append(digit);
            }

            while (tail > 0)
            {
                if (!dotted)
                {
                    result.Append(decimalPlace);
                    dotted = true;
                }

                result.Append("0");
                tail--;
            }

            return result.ToString();
        }

        private static string InjectThousands(string str, string sep)
        {
            var sb = new StringBuilder();
            var i = str.Length % 3;
            var first = true;
            foreach (var c in str)
            {
                if (i-- <= 0)
                {
                    if (!first) sb.Append(sep);
                    i = 2;
                }

                sb.Append(c);
                first = false;
            }

            return sb.ToString();
        }

        private static string? ReformatDate(DisplayFormatFilter format, string str)
        {
            var param = format.FormatParameters;

            var key = nameof(DateDisplayParams.FormatString);
            var fmt = (param.ContainsKey(key) ? param[key] : null) ?? "yyyy-MM-dd";

            try
            {
                // first, try the exact format that *should* be used
                if (DateTime.TryParseExact(str, "yyyy-MM-dd", null!, DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces, out var dt))
                    return dt.ToString(fmt);

                // try a more general search
                if (DateTime.TryParse(str, out dt)) return dt.ToString(fmt);

                // abandon
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}