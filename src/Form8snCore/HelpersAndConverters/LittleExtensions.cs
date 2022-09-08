using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Form8snCore.HelpersAndConverters;

public static class LittleExtensions
{
    /// <summary>
    /// Encode a string as UTF-8 and wrap in a stream
    /// </summary>
    public static Stream ToStream(this string? str)
    {
        var ms = new MemoryStream();
        if (str is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            ms.Write(bytes, 0, bytes.Length);
            ms.Seek(0, SeekOrigin.Begin);
        }
        return ms;
    }

    /// <summary>
    /// Remove non-alpha numeric characters from a string.
    /// If input is null, this will return an empty string.
    /// </summary>
    public static string Safe(this string? str)
    {
        var sb = new StringBuilder();
        if (str is null) return sb.ToString();

        foreach (char c in str)
        {
            if (char.IsLetter(c) || char.IsDigit(c)) sb.Append(c);
            else if (c == ' ' || c == '_' || c == '-') sb.Append('_');
        }
        return sb.ToString();
    }

    /// <summary>
    /// Return an IList as an IEnumerable&lt;object>
    /// </summary>
    public static IEnumerable<object?> AsObjectList(this IList? src)
    {
        if (src is null) return new object?[0];
        if (src is IEnumerable<object?> native) return native;
        
        var dst = new List<object?>(src.Count);
        dst.AddRange(src.Cast<object?>());

        return dst;
    }
}