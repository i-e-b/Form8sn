/*
 * PaperSize.cs - Implementation of the
 *			"System.Drawing.Printing.PaperSize" class.
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
using System.Text;

namespace Portable.Drawing.Printing
{
	public class PaperSize
{
	// Internal state.
	private string name;
	private int width;
	private int height;

	// Constructors.
	public PaperSize(string name, int width, int height)
			{
				Kind = PaperKind.Custom;
				this.name = name;
				this.width = width;
				this.height = height;
			}
	internal PaperSize(PaperKind kind)
			{
				Kind = kind;
				name = kind.ToString();
				// TODO: need to add all of the rest of the paper sizes.
				switch(kind)
				{
					case PaperKind.Letter:
					{
						width = 850;
						height = 1100;
					}
					break;

					case PaperKind.A4:
					{
						width = 857;
						height = 1212;
					}
					break;

					case PaperKind.Executive:
					{
						width = 725;
						height = 1050;
					}
					break;

					case PaperKind.Legal:
					{
						width = 850;
						height = 1400;
					}
					break;

					default:
					{
						// Unknown paper size, so switch to "Letter".
						Kind = PaperKind.Letter;
						width = 850;
						height = 1100;
					}
					break;
				}
			}

	// Get or set this object's properties.
	public int Height
			{
				get
				{
					return height;
				}
				set
				{
					if(Kind != PaperKind.Custom)
					{
						throw new ArgumentException("Arg_PaperSizeNotCustom");
					}
					height = value;
				}
			}
	public PaperKind Kind { get; }

	public string PaperName
			{
				get
				{
					return name;
				}
				set
				{
					if(Kind != PaperKind.Custom)
					{
						throw new ArgumentException("Arg_PaperSizeNotCustom");
					}
					name = value;
				}
			}
	public int Width
			{
				get
				{
					return width;
				}
				set
				{
					if(Kind != PaperKind.Custom)
					{
						throw new ArgumentException("Arg_PaperSizeNotCustom");
					}
					width = value;
				}
			}

	// Convert this object into a string.
	public override string ToString()
			{
				StringBuilder builder = new StringBuilder();
				builder.Append("[PaperSize ");
				builder.Append(name);
				builder.Append(" Kind=");
				builder.Append(Kind.ToString());
				builder.Append(" Height=");
				builder.Append(height.ToString());
				builder.Append(" Width=");
				builder.Append(width.ToString());
				builder.Append(']');
				return builder.ToString();
			}

}; // class PaperSize

}; // namespace System.Drawing.Printing
