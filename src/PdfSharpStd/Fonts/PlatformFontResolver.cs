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
using System.Diagnostics;
#if CORE || GDI
using System.Drawing;
using GdiFontFamily = Portable.Drawing.FontFamily;
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
using WpfStyleSimulations = System.Windows.Media.StyleSimulations;
#endif
using PdfSharp.Drawing;

#pragma warning disable 1591
// ReSharper disable RedundantNameQualifier

namespace PdfSharp.Fonts
{
    // TODO: replace platform font resolver with PortableGdi stuff
    /// <summary>
    /// Default platform specific font resolving.
    /// </summary>
    public static class PlatformFontResolver
    {
        /// <summary>
        /// Resolves the typeface by generating a font resolver info.
        /// </summary>
        /// <param name="familyName">Name of the font family.</param>
        /// <param name="isBold">Indicates whether a bold font is requested.</param>
        /// <param name="isItalic">Indicates whether an italic font is requested.</param>
        public static FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            FontResolvingOptions fontResolvingOptions = new FontResolvingOptions(FontHelper.CreateStyle(isBold, isItalic));
            return ResolveTypeface(familyName, fontResolvingOptions, XGlyphTypeface.ComputeKey(familyName, fontResolvingOptions));
        }

        /// <summary>
        /// Internal implementation.
        /// </summary>
        internal static FontResolverInfo ResolveTypeface(string familyName, FontResolvingOptions fontResolvingOptions, string typefaceKey)
        {
            // Internally we often have the typeface key already.
            if (string.IsNullOrEmpty(typefaceKey))
                typefaceKey = XGlyphTypeface.ComputeKey(familyName, fontResolvingOptions);

            // The user may call ResolveTypeface anytime from anywhere, so check cache in FontFactory in the first place.
            FontResolverInfo fontResolverInfo;
            if (FontFactory.TryGetFontResolverInfoByTypefaceKey(typefaceKey, out fontResolverInfo))
                return fontResolverInfo;

            // Let the platform create the requested font source and save both PlattformResolverInfo
            // and XFontSource in FontFactory cache.
            // It is possible that we already have the correct font source. E.g. we already have the regular typeface in cache
            // and looking now for the italic typeface, but no such font exists. In this case we get the regular font source
            // and cache again it with the italic typeface key. Furthermore in glyph typeface style simulation for italic is set.
            GdiFont gdiFont;
            XFontSource fontSource = CreateFontSource(familyName, fontResolvingOptions, out gdiFont, typefaceKey);

            // If no such font exists return null. PDFsharp will fail.
            if (fontSource == null)
                return null;

            if (fontResolvingOptions.OverrideStyleSimulations)
            {
#if (CORE || GDI) && !WPF
                // TODO: Support style simulation for GDI+ platform fonts.
                fontResolverInfo = new PlatformFontResolverInfo(typefaceKey, fontResolvingOptions.MustSimulateBold, fontResolvingOptions.MustSimulateItalic, gdiFont);
#endif

            }
            else
            {
                bool mustSimulateBold = gdiFont.Bold && !fontSource.Fontface.os2.IsBold;
                bool mustSimulateItalic = gdiFont.Italic && !fontSource.Fontface.os2.IsItalic;
                fontResolverInfo = new PlatformFontResolverInfo(typefaceKey, mustSimulateBold, mustSimulateItalic, gdiFont);
            }

            FontFactory.CacheFontResolverInfo(typefaceKey, fontResolverInfo);

            // Register font data under the platform specific face name.
            // Already done in CreateFontSource.
            // FontFactory.CacheNewFontSource(typefaceKey, fontSource);

            return fontResolverInfo;
        }

#if (CORE_WITH_GDI || GDI) && !WPF
        /// <summary>
        /// Create a GDI+ font and use its handle to retrieve font data using native calls.
        /// </summary>
        internal static XFontSource CreateFontSource(string familyName, FontResolvingOptions fontResolvingOptions, out GdiFont font, string typefaceKey)
        {
            if (string.IsNullOrEmpty(typefaceKey))
                typefaceKey = XGlyphTypeface.ComputeKey(familyName, fontResolvingOptions);

            GdiFontStyle gdiStyle = (GdiFontStyle)(fontResolvingOptions.FontStyle & XFontStyle.BoldItalic);

            // Create a 10 point GDI+ font as an exemplar.
            XFontSource fontSource;
            font = FontHelper.CreateFont(familyName, 10, gdiStyle, out fontSource);

            if (fontSource != null)
            {
                Debug.Assert(font != null);
                // Case: Font was created by a GDI+ private font collection.
#if true
#if DEBUG
                XFontSource existingFontSource;
                Debug.Assert(FontFactory.TryGetFontSourceByTypefaceKey(typefaceKey, out existingFontSource) &&
                    ReferenceEquals(fontSource, existingFontSource));
#endif
#else
                // Win32 API cannot get font data from fonts created by private font collection,
                // because this is handled internally in GDI+.
                // Therefore the font source was created when the private font is added to the private font collection.
                if (!FontFactory.TryGetFontSourceByTypefaceKey(typefaceKey, out fontSource))
                {
                    // Simplify styles.
                    // (The code is written for clarity - do not rearrange for optimization)
                    if (font.Bold && font.Italic)
                    {
                        if (FontFactory.TryGetFontSourceByTypefaceKey(XGlyphTypeface.ComputeKey(font.Name, true, false), out fontSource))
                        {
                            // Use bold font.
                            FontFactory.CacheExistingFontSourceWithNewTypefaceKey(typefaceKey, fontSource);
                        }
                        else if (FontFactory.TryGetFontSourceByTypefaceKey(XGlyphTypeface.ComputeKey(font.Name, false, true), out fontSource))
                        {
                            // Use italic font.
                            FontFactory.CacheExistingFontSourceWithNewTypefaceKey(typefaceKey, fontSource);
                        }
                        else if (FontFactory.TryGetFontSourceByTypefaceKey(XGlyphTypeface.ComputeKey(font.Name, false, false), out fontSource))
                        {
                            // Use regular font.
                            FontFactory.CacheExistingFontSourceWithNewTypefaceKey(typefaceKey, fontSource);
                        }
                    }
                    else if (font.Bold || font.Italic)
                    {
                        // Use regular font.
                        if (FontFactory.TryGetFontSourceByTypefaceKey(XGlyphTypeface.ComputeKey(font.Name, false, false), out fontSource))
                        {
                            FontFactory.CacheExistingFontSourceWithNewTypefaceKey(typefaceKey, fontSource);
                        }
                    }
                    else
                    {
                        if (FontFactory.TryGetFontSourceByTypefaceKey(XGlyphTypeface.ComputeKey(font.Name, false, false), out fontSource))
                        {
                            // Should never come here...
                            FontFactory.CacheExistingFontSourceWithNewTypefaceKey(typefaceKey, fontSource);
                        }
                    }
                }
#endif
            }
            else
            {
                // Get or create the font source and cache it under the specified typeface key.
                fontSource = XFontSource.GetOrCreateFromFile(typefaceKey, font);
            }
            return fontSource;
        }
#endif
    }
}
