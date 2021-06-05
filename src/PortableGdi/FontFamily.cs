/*
 * FontFamily.cs - Implementation of the "System.Drawing.FontFamily" class.
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
using Portable.Drawing.Text;
using Portable.Drawing.Toolkit;

namespace Portable.Drawing
{
	public sealed class FontFamily : MarshalByRefObject, IDisposable
{
	// Internal state.
	private readonly GenericFontFamilies _genericFamily;
	private FontCollection? _fontCollection;
	private FontStyle _metricsStyle;
	private int _ascent;
	private int _descent;
	private int _emHeight;
	private int _lineSpacing;

	// Constructors.
	public FontFamily(GenericFontFamilies genericFamily)
			{
				_genericFamily = genericFamily;
				_fontCollection = null;
				_metricsStyle = (FontStyle)(-1);
				switch(genericFamily)
				{
					case GenericFontFamilies.Serif:
					default:
					{
						Name = "Times New Roman";
					}
					break;

					case GenericFontFamilies.SansSerif:
					{
						Name = "Microsoft Sans Serif";
					}
					break;

					case GenericFontFamilies.Monospace:
					{
						Name = "Courier New";
					}
					break;
				}
			}
	public FontFamily(string name) : this(name, null) {}
	public FontFamily(string name, FontCollection fontCollection)
			{
				if(name == null)
				{
					throw new ArgumentNullException("name");
				}
				Name = name;
				_fontCollection = fontCollection;
				_metricsStyle = (FontStyle)(-1);

				// Intuit the generic family based on common font names.
				if(string.Compare(name, "Times", true) == 0 ||
				   string.Compare(name, "Times New Roman", true) == 0)
				{
					_genericFamily = GenericFontFamilies.Serif;
				}
				else if(string.Compare(name, "Helvetica", true) == 0 ||
				        string.Compare(name, "Helv", true) == 0 ||
				        string.Compare
							(name, "Microsoft Sans Serif", true) == 0 ||
				        string.Compare(name, "Arial", true) == 0 ||
						(name.Length >= 6 &&
				        	string.Compare(name, 0, "Arial ", 0, 6, true) == 0))
				{
					_genericFamily = GenericFontFamilies.SansSerif;
				}
				else if(string.Compare(name, "Courier", true) == 0 ||
				        string.Compare(name, "Courier New", true) == 0)
				{
					_genericFamily = GenericFontFamilies.Monospace;
				}
				else
				{
					_genericFamily = GenericFontFamilies.Serif;
				}
			}

	// Get a list of all font families on this system.
	public static FontFamily[] Families
			{
				get
				{
					return GetFamilies(null);
				}
			}

	// Get a generic monospace object.
	public static FontFamily GenericMonospace
			{
				get
				{
					return new FontFamily(GenericFontFamilies.Monospace);
				}
			}

	// Get a generic sans-serif object.
	public static FontFamily GenericSansSerif
			{
				get
				{
					return new FontFamily(GenericFontFamilies.SansSerif);
				}
			}

	// Get a generic serif object.
	public static FontFamily GenericSerif
			{
				get
				{
					return new FontFamily(GenericFontFamilies.Serif);
				}
			}

	// Get the name of this font family.
	public string Name { get; }

	// Dispose of this object.
	public void Dispose()
			{
				// Nothing to do here in this implementation.
			}

	// Determine if two objects are equal.
	public override bool Equals(object obj)
			{
				FontFamily other = (obj as FontFamily);
				if(other != null)
				{
					return (other.Name == Name);
				}
				else
				{
					return false;
				}
			}

	// Get the font family metrics.
	private void GetMetrics(FontStyle style)
			{
				if(style != _metricsStyle)
				{
					ToolkitManager.Toolkit.GetFontFamilyMetrics
						(_genericFamily, Name, style,
						 out _ascent, out _descent,
						 out _emHeight, out _lineSpacing);
					_metricsStyle = style;
				}
			}

	// Get the cell ascent for a particular style.
	public int GetCellAscent(FontStyle style)
			{
				GetMetrics(style);
				return _ascent;
			}

	// Get the cell descent for a particular style.
	public int GetCellDescent(FontStyle style)
			{
				GetMetrics(style);
				return _descent;
			}

	// Get the em height for a particular style.
	public int GetEmHeight(FontStyle style)
			{
				GetMetrics(style);
				return _emHeight;
			}

	// Get a list of all font families with a specified graphics context.
	public static FontFamily[] GetFamilies(Graphics? graphics)
			{
				if(graphics != null)
				{
					return ToolkitManager.Toolkit.GetFontFamilies
						(graphics.ToolkitGraphics);
				}
				else
				{
					return ToolkitManager.Toolkit.GetFontFamilies(null);
				}
			}

	// Get a hash code for this object.
	public override int GetHashCode()
			{
				return Name.GetHashCode();
			}

	// Get the line spacing for a particular style.
	public int GetLineSpacing(FontStyle style)
			{
				GetMetrics(style);
				return _lineSpacing;
			}

	// Get the name of this font in a specified language.
	public string GetName(int language)
			{
				// We don't support language identifiers.
				return Name;
			}

	// Determine if a particular font style is available.
	public bool IsStyleAvailable(FontStyle style)
			{
				// We assume that all styles are available and that it is
				// up to the toolkit handlers to emulate unknown styles.
				return true;
			}

	// Convert this object into a string.
	public override string ToString()
			{
				return "[FontFamily: Name=" + Name + "]";
			}

}; // class FontFamily

}; // namespace System.Drawing
