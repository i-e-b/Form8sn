/*
 * StringFormat.cs - Implementation of the "System.Drawing.StringFormat" class.
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

namespace Portable.Drawing
{
	public sealed class StringFormat
	: IDisposable
{
	// Internal state.
	internal CharacterRange[] ranges;
	private float firstTabOffset;
	private float[] tabStops;


	// Constructors.
	public StringFormat()
			{
				Trimming = StringTrimming.Character;
			}
	public StringFormat(StringFormat format)
			{
				if(format == null)
				{
					throw new ArgumentNullException("format");
				}
				FormatFlags = format.FormatFlags;
				DigitSubstitutionLanguage = format.DigitSubstitutionLanguage;
				Alignment = format.Alignment;
				DigitSubstitutionMethod = format.DigitSubstitutionMethod;
				HotkeyPrefix = format.HotkeyPrefix;
				LineAlignment = format.LineAlignment;
				Trimming = format.Trimming;
				ranges = format.ranges;
				firstTabOffset = format.firstTabOffset;
				tabStops = format.tabStops;
			}
	public StringFormat(StringFormatFlags options)
			{
				FormatFlags = options;
				Trimming = StringTrimming.Character;
				
			}
	public StringFormat(StringFormatFlags options, int language)
			{
				FormatFlags = options;
				DigitSubstitutionLanguage = language;
				Trimming = StringTrimming.Character;
			}
	private StringFormat(bool typographic)
			{
				if(typographic)
				{
					FormatFlags = (StringFormatFlags.LineLimit |
					                StringFormatFlags.NoClip);
				}
				else
				{
					Trimming = StringTrimming.Character;
				}
			}


	// Get or set this object's properties.
	public StringAlignment Alignment { get; set; }

	public int DigitSubstitutionLanguage { get; private set; }

	public StringDigitSubstitute DigitSubstitutionMethod { get; set; }

	public StringFormatFlags FormatFlags { get; set; }

	public HotkeyPrefix HotkeyPrefix { get; set; }

	public StringAlignment LineAlignment { get; set; }

	public StringTrimming Trimming { get; set; }

	// Get the generic default string format.
	public static StringFormat GenericDefault
			{
				get { return new StringFormat(false); }
			}

	// Get the generic typographic string format.
	public static StringFormat GenericTypographic
			{
				get { return new StringFormat(true); }
			}


	// Clone this object.
	public object Clone()
			{
				return new StringFormat(this);
			}

	// Dispose of this object.
	public void Dispose()
			{
				// Nothing to do here in this implementation.
			}

	// Get tab stop information for this format.
	public float[] GetTabStops(out float firstTabOffset)
			{
			#if CONFIG_EXTENDED_NUMERICS
				if(this.tabStops == null)
				{
					this.firstTabOffset = 8.0f;
					this.tabStops = new float [] {8.0f};
				}
			#endif
				firstTabOffset = this.firstTabOffset;
				return tabStops;
			}

	// Set the digit substitution properties.
	public void SetDigitSubstitution
				(int language, StringDigitSubstitute substitute)
			{
				DigitSubstitutionLanguage = language;
				DigitSubstitutionMethod = DigitSubstitutionMethod;
			}

	// Set the measurable character ranges.
	public void SetMeasurableCharacterRanges(CharacterRange[] ranges)
			{
				this.ranges = ranges;
			}

	// Set the tab stops for this format.
	public void SetTabStops(float firstTabOffset, float[] tabStops)
			{
				this.firstTabOffset = firstTabOffset;
				this.tabStops = tabStops;
			}

	// Convert this object into a string.
	public override string ToString()
			{
				return "[StringFormat, FormatFlags=" +
					   FormatFlags.ToString() + "]";
			}

}; // class StringFormat

}; // namespace System.Drawing
