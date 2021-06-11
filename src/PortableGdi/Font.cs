/*
 * Font.cs - Implementation of the "System.Drawing.Font" class.
 *
 * Copyright (C) 2003  Southern Storm Software, Pty Ltd.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using System.Runtime.InteropServices;
using System.Text;
using Portable.Drawing.Drawing2D;
using Portable.Drawing.Toolkit;

namespace Portable.Drawing
{
    // enum GraphicsUnit


    [Serializable]
    [ComVisible(true)]
    public sealed class Font : MarshalByRefObject, ICloneable, IDisposable
    {
        // Internal state.
        private IToolkit? _toolkit;
        private IToolkitFont? _toolkitFont;

        // Cached height of the font in pixels.
        private int _pixelHeight;

        // Constructors.
        public Font(Font prototype, FontStyle newStyle)
            : this(prototype.FontFamily, prototype.Size, newStyle,
                prototype.Unit, prototype.GdiCharSet,
                prototype.GdiVerticalFont)
        {
        }

        public Font(FontFamily family, float emSize) : this(family, emSize, FontStyle.Regular, GraphicsUnit.Point, 1, false)
        {
        }

        public Font(FontFamily family, float emSize, FontStyle style) : this(family, emSize, style, GraphicsUnit.Point, 1, false)
        {
        }

        public Font(FontFamily family, float emSize, GraphicsUnit unit) : this(family, emSize, FontStyle.Regular, unit, 1, false)
        {
        }

        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit) : this(family, emSize, style, unit, 1, false)
        {
        }

        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet) : this(family, emSize, style, unit, gdiCharSet, false)
        {
        }

        public Font(string familyName, float emSize) : this(new FontFamily(familyName), emSize, FontStyle.Regular, GraphicsUnit.Point, 1, false)
        {
        }

        public Font(string familyName, float emSize, FontStyle style) : this(new FontFamily(familyName), emSize, style, GraphicsUnit.Point, 1, false)
        {
        }

        public Font(string familyName, float emSize, GraphicsUnit unit) : this(new FontFamily(familyName), emSize, FontStyle.Regular, unit, 1, false)
        {
        }

        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit) : this(new FontFamily(familyName), emSize, style, unit, 1, false)
        {
        }

        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet) : this(new FontFamily(familyName), emSize, style, unit, gdiCharSet, false)
        {
        }

        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont) : this(new FontFamily(familyName), emSize, style, unit, gdiCharSet, gdiVerticalFont)
        {
        }

        public Font(FontFamily? family, float emSize, FontStyle style,
            GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont)
        {
            FontFamily = family ?? throw new ArgumentNullException(nameof(family));
            OriginalFontName = family.Name;
            SystemFontName = family.Name;
            Size = emSize;
            Style = style;
            Unit = unit;
            GdiCharSet = gdiCharSet;
            GdiVerticalFont = gdiVerticalFont;
        }

        private Font(IToolkitFont font)
        {
            _toolkit = _toolkit;
            _toolkitFont = font;
            // TODO: load the font information from the IToolkitFont
        }

        // Destructor.
        ~Font()
        {
            Dispose();
        }

        // Font properties.
        public bool Bold
        {
            get { return ((Style & FontStyle.Bold) != 0); }
        }

        public FontFamily FontFamily { get; }
        public byte GdiCharSet { get; }
        public bool GdiVerticalFont { get; }
        public int Height => (int) (GetHeight() + 0.99f);

        public bool Italic
        {
            get { return ((Style & FontStyle.Italic) != 0); }
        }

        public string Name
        {
            get { return FontFamily.Name; }
        }

        public float Size { get; }
        public float SizeInPoints
        {
            get
            {
                float adjust;
                switch (Unit)
                {
                    case GraphicsUnit.World:
                    case GraphicsUnit.Pixel:
                    default:
                    {
                        adjust = 72.0f / ToolkitManager.Toolkit.GetDefaultGraphics().DpiY;
                    }
                        break;

                    case GraphicsUnit.Display:
                    {
                        adjust = 72.0f / 75.0f;
                    }
                        break;

                    case GraphicsUnit.Point:
                    {
                        return Size;
                    }
                    // Not reached.

                    case GraphicsUnit.Inch:
                    {
                        adjust = 72.0f;
                    }
                        break;

                    case GraphicsUnit.Document:
                    {
                        adjust = 72.0f / 300.0f;
                    }
                        break;

                    case GraphicsUnit.Millimeter:
                    {
                        adjust = 72.0f / (75.0f / 25.4f);
                    }
                        break;
                }

                return Size * adjust;
            }
        }
#if CONFIG_COMPONENT_MODEL
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
        public bool Strikeout
        {
            get { return ((Style & FontStyle.Strikeout) != 0); }
        }
#if CONFIG_COMPONENT_MODEL
	[Browsable(false)]
#endif
        public FontStyle Style { get; }
#if CONFIG_COMPONENT_MODEL
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
        public bool Underline => ((Style & FontStyle.Underline) != 0);
#if CONFIG_COMPONENT_MODEL
	[TypeConverter(typeof(FontConverter.FontUnitConverter))]
#endif
        public GraphicsUnit Unit { get; }
        public string OriginalFontName { get; set; }
        public string SystemFontName { get; set; }

        // Clone this object.
        public object Clone()
        {
            lock (this)
            {
                Font font = (Font) (MemberwiseClone());
                font._toolkit = null;
                font._toolkitFont = null;
                return font;
            }
        }

        // Dispose of this object.
        public void Dispose()
        {
            lock (this)
            {
                if (_toolkitFont != null)
                {
                    _toolkitFont.Dispose();
                    _toolkitFont = null;
                }

                _toolkit = null;
            }
        }

        // Determine if two objects are equal.
        public override bool Equals(object obj)
        {
            Font other = (obj as Font);
            if (other != null)
            {
                return (other.Name == Name &&
                        other.Size == Size &&
                        other.Style == Style &&
                        other.GdiCharSet == GdiCharSet &&
                        other.GdiVerticalFont == GdiVerticalFont);
            }
            else
            {
                return false;
            }
        }

        // Get a hash code for this object.
        public override int GetHashCode()
        {
            return Name.GetHashCode() + (((int) Size) << 4) +
                   (int) Style;
        }

        // Extract the active font from a native device context.
        public static Font FromHdc(IntPtr hdc)
        {
            IToolkitFont font;
            font = ToolkitManager.Toolkit.GetFontFromHdc(hdc);
            if (font != null)
            {
                return new Font(font);
            }
            else
            {
                return null;
            }
        }

        // Convert a native font handle into a font object.
        public static Font FromHfont(IntPtr hfont)
        {
            IToolkitFont font;
            font = ToolkitManager.Toolkit.GetFontFromHfont(hfont);
            if (font != null)
            {
                return new Font(font);
            }
            else
            {
                return null;
            }
        }

        // Convert a native logical font descriptor into a font object.
        public static Font FromLogFont(object lf)
        {
            return FromLogFont(lf, IntPtr.Zero);
        }

        public static Font FromLogFont(object lf, IntPtr hdc)
        {
            IToolkitFont font;
            font = ToolkitManager.Toolkit.GetFontFromLogFont(lf, hdc);
            if (font != null)
            {
                return new Font(font);
            }
            else
            {
                return null;
            }
        }

        // Get the height of this font.
        public float GetHeight()
        {
            return GetHeight(Graphics.DefaultGraphics);
        }

        public float GetHeight(Graphics graphics)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            // Get the font size in raw pixels.
            if (_pixelHeight < 1)
                _pixelHeight = graphics.GetLineSpacing(this);

            // If the graphics object uses pixels (the most common case),
            // then return the pixel value directly.
            if (graphics.IsPixelUnits())
            {
                return _pixelHeight;
            }

            // Get the dpi of the screen for the y axis.
            float dpiY = graphics.DpiY;

            // Convert the pixel value back into points.
            float points = _pixelHeight / (dpiY / 72.0f);

            // Convert the points into the graphics object's unit.
            switch (graphics.PageUnit)
            {
                case GraphicsUnit.World:
                case GraphicsUnit.Pixel:
                {
                    points *= (dpiY / 72.0f);
                }
                    break;

                case GraphicsUnit.Display:
                {
                    points *= (75.0f / 72.0f);
                }
                    break;

                case GraphicsUnit.Point: break;

                case GraphicsUnit.Inch:
                {
                    points /= 72.0f;
                }
                    break;

                case GraphicsUnit.Document:
                {
                    points *= (300.0f / 72.0f);
                }
                    break;

                case GraphicsUnit.Millimeter:
                {
                    points *= (25.4f / 72.0f);
                }
                    break;
            }

            return points;
        }

        public float GetHeight(float dpi)
        {
            if (Unit == GraphicsUnit.World || Unit == GraphicsUnit.Pixel)
            {
                return GetHeight(Graphics.DefaultGraphics);
            }
            else
            {
                return FontFamily.GetLineSpacing(Style) *
                    (Size / FontFamily.GetEmHeight(Style)) * dpi / 72f;
            }
        }

        // Convert this object into a native font handle.
        public IntPtr ToHfont()
        {
            if (_toolkitFont == null)
            {
                IToolkit toolkit = ToolkitManager.Toolkit;
                // TODO: this is the next place we need to extend portable GDI. Use the FontReaderCs stuff
                float dpiY = toolkit.GetDefaultGraphics().DpiY;
                GetFont(toolkit, dpiY);
            }

            return _toolkitFont.GetHfont();
        }

        // Fill in a native font information structure with info about this font.
        public void ToLogFont(object lf)
        {
            ToLogFont(lf, null);
        }

        public void ToLogFont(object lf, Graphics graphics)
        {
            IToolkitGraphics g;
            if (graphics != null)
            {
                g = graphics.ToolkitGraphics;
            }
            else
            {
                g = null;
            }

            lock (this)
            {
                if (_toolkitFont == null)
                {
                    if (g != null)
                    {
                        GetFont(g.Toolkit, graphics.DpiY);
                    }
                    else
                    {
                        IToolkit toolkit = ToolkitManager.Toolkit;
                        float dpiY = toolkit.GetDefaultGraphics().DpiY;
                        GetFont(toolkit, dpiY);
                    }
                }

                _toolkitFont.ToLogFont(lf, g);
            }
        }

        // Convert this object into a string.
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[Font: Name=");
            builder.Append(Name);
            builder.Append(" Size=");
#if CONFIG_EXTENDED_NUMERICS
				builder.Append(Size);
#else
            builder.Append((int) Size);
#endif
            builder.Append(" Units=");
            builder.Append((int) Unit);
            builder.Append(" GdiCharSet=");
            builder.Append(GdiCharSet);
            builder.Append(" GdiVerticalFont=");
            builder.Append(GdiVerticalFont);
            if (Style != FontStyle.Regular)
            {
                builder.Append(" Style=");
                builder.Append(Style);
            }

            builder.Append("]");
            return builder.ToString();
        }

#if CONFIG_SERIALIZATION
	// Get the serialization information for this object.
	void ISerializable.GetObjectData(SerializationInfo info,
									 StreamingContext context)
			{
				if(info == null)
				{
					throw new ArgumentNullException("info");
				}
				info.AddValue("Name", Name);
				info.AddValue("Size", Size);
				info.AddValue("Style", Style);
				info.AddValue("Unit", Unit);
			}

#endif

        // Get the toolkit version of this font for a specific toolkit.
        internal IToolkitFont GetFont(IToolkit toolkit, float dpi)
        {
            lock (this)
            {
                if (_toolkitFont == null)
                {
                    // We don't yet have a toolkit font yet.
                    _toolkitFont = toolkit.CreateFont(this, dpi);
                    _toolkit = toolkit;
                    return _toolkitFont;
                }
                else if (_toolkit == toolkit)
                {
                    // Same toolkit - return the cached font.
                    return _toolkitFont;
                }
                else
                {
                    // We have a font object for another toolkit,
                    // so dispose it and create for this toolkit.
                    // We null out "toolkitFont" before calling
                    // "CreateFont()" just in case an exception
                    // is thrown while creating the toolkit font.
                    _toolkitFont.Dispose();
                    _toolkitFont = null;
                    _toolkitFont = toolkit.CreateFont(this, dpi);
                    _toolkit = toolkit;
                    return _toolkitFont;
                }
            }
        }
    }; // class Font
}; // namespace System.Drawing