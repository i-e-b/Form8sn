#region PDFsharp - A .NET library for processing PDF
//
// Authors:
//   Stefan Lange
//
// Copyright (c) 2005-2019 empira Software GmbH, Cologne Area (Germany)
//
// http://www.pdfsharp.com
// http://sourceforge.net/projects/pdfsharp
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using PdfSharp.Fonts;
#if CORE || GDI
using GdiFont = Portable.Drawing.Font;
using GdiFontStyle = Portable.Drawing.FontStyle;
#endif
#if WPF
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfTypeface = System.Windows.Media.Typeface;
using WpfGlyphTypeface = System.Windows.Media.GlyphTypeface;
#endif
using PdfSharp.Internal;
using PdfSharp.Fonts.OpenType;
using Portable.Drawing.Toolkit.Fonts;
using Portable.Drawing.Toolkit.Portable;

namespace PdfSharp.Drawing
{
    /// <summary>
    /// The bytes of a font file.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay}")]
    internal class XFontSource
    {
        // Implementation Notes
        // 
        // * XFontSource represents a single font (file) in memory.
        // * An XFontSource hold a reference to it OpenTypeFontface.
        // * To prevent large heap fragmentation this class must exists only once.
        // * TODO: ttcf

        // Signature of a true type collection font.
        const uint ttcf = 0x66637474;

        XFontSource(byte[] bytes, ulong key)
        {
            _fontName = null;
            _bytes = bytes;
            _key = key;
        }

        /// <summary>
        /// Gets an existing font source or creates a new one.
        /// A new font source is cached in font factory.
        /// </summary>
        public static XFontSource GetOrCreateFrom(byte[] bytes)
        {
            ulong key = FontHelper.CalcChecksum(bytes);
            XFontSource fontSource;
            if (!FontFactory.TryGetFontSourceByKey(key, out fontSource))
            {
                fontSource = new XFontSource(bytes, key);
                // Theoretically the font source could be created by a different thread in the meantime.
                fontSource = FontFactory.CacheFontSource(fontSource);
            }
            return fontSource;
        }

        
        public static XFontSource GetOrCreateFromFile(string typefaceKey, GdiFont gdiFont)
        {
            byte[] bytes;
            if (gdiFont.Name == GlobalFontSettings.DefaultFontName)
            {
                // Load our special 'fall-back' font from resources and load that
                bytes = LoadFallbackFont();
            }
            else
            {
                bytes = PortableFont.ReadFontFileBytes(gdiFont);
            }

            XFontSource fontSource = GetOrCreateFrom(typefaceKey, bytes);
            return fontSource;
        }

        private static byte[] LoadFallbackFont()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var ms = new MemoryStream();
            using var stream = assembly.GetManifestResourceStream(GlobalFontSettings.FallbackFontResourceName);
            stream?.CopyTo(ms);
            
            ms.Seek(0, SeekOrigin.Begin);
            return ms.ToArray();
        }

        static XFontSource GetOrCreateFrom(string typefaceKey, byte[] fontBytes)
        {
            var key = FontHelper.CalcChecksum(fontBytes);
            if (FontFactory.TryGetFontSourceByKey(key, out XFontSource fontSource))
            {
                // The font source already exists, but is not yet cached under the specified typeface key.
                FontFactory.CacheExistingFontSourceWithNewTypefaceKey(typefaceKey, fontSource);
            }
            else
            {
                // No font source exists. Create new one and cache it.
                fontSource = new XFontSource(fontBytes, key);
                FontFactory.CacheNewFontSource(typefaceKey, fontSource);
            }
            return fontSource;
        }

        public static XFontSource CreateCompiledFont(byte[] bytes)
        {
            XFontSource fontSource = new(bytes, 0);
            return fontSource;
        }

        /// <summary>
        /// Gets or sets the fontface.
        /// </summary>
        internal OpenTypeFontface Fontface
        {
            get => _fontface;
            set
            {
                _fontface = value;
                _fontName = value.name.FullFontName;
            }
        }
        OpenTypeFontface _fontface;

        /// <summary>
        /// Gets the key that uniquely identifies this font source.
        /// </summary>
        internal ulong Key
        {
            get
            {
                if (_key == 0)
                    _key = FontHelper.CalcChecksum(Bytes);
                return _key;
            }
        }
        ulong _key;

        public void IncrementKey()
        {
            // HACK: Depends on implementation of CalcChecksum.
            // Increment check sum and keep length untouched.
            _key += 1ul << 32;
        }

        /// <summary>
        /// Gets the name of the font's name table.
        /// </summary>
        public string? FontName => _fontName;
        string? _fontName;

        /// <summary>
        /// Gets the bytes of the font.
        /// </summary>
        public byte[] Bytes => _bytes;
        readonly byte[] _bytes;

        public override int GetHashCode()
        {
            return (int)((Key >> 32) ^ Key);
        }

        public override bool Equals(object obj)
        {
            XFontSource fontSource = obj as XFontSource;
            if (fontSource == null)
                return false;
            return Key == fontSource.Key;
        }

        /// <summary>
        /// Gets the DebuggerDisplayAttribute text.
        /// </summary>
        // ReSharper disable UnusedMember.Local
        // The key is converted to a value a human can remember during debugging.
        internal string DebuggerDisplay => string.Format(CultureInfo.InvariantCulture, "XFontSource: '{0}', keyhash={1}", FontName, Key % 99991 /* largest prime number less than 100000 */); // ReShar per restore UnusedMember.Local
    }
}
