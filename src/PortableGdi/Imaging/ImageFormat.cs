/*
 * ImageFormat.cs - Implementation of the
 *			"System.Drawing.Imaging.ImageFormat" class.
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

namespace Portable.Drawing.Imaging
{

#if !ECMA_COMPAT

public sealed class ImageFormat
{
	// Internal state.
	private Guid guid;

	// Constructor.
	public ImageFormat(Guid guid)
			{
				this.guid = guid;
			}

	// Get the GUID for this image format.
	public Guid Guid
			{
				get
				{
					return guid;
				}
			}

	// Standard image formats.
	public static ImageFormat Bmp { get; } = new ImageFormat
		(new Guid("{b96b3cab-0728-11d3-9d7b-0000f81ef32e}"));

	public static ImageFormat Emf { get; } = new ImageFormat
		(new Guid("{b96b3cac-0728-11d3-9d7b-0000f81ef32e}"));

	public static ImageFormat Exif { get; } = new ImageFormat
		(new Guid("{b96b3cb2-0728-11d3-9d7b-0000f81ef32e}"));

	public static ImageFormat Gif { get; } = new ImageFormat
		(new Guid("{b96b3cb0-0728-11d3-9d7b-0000f81ef32e}"));

	public static ImageFormat Icon { get; } = new ImageFormat
		(new Guid("{b96b3cb5-0728-11d3-9d7b-0000f81ef32e}"));

	public static ImageFormat Jpeg { get; } = new ImageFormat
		(new Guid("{b96b3cae-0728-11d3-9d7b-0000f81ef32e}"));

	public static ImageFormat MemoryBmp { get; } = new ImageFormat
		(new Guid("{b96b3caa-0728-11d3-9d7b-0000f81ef32e}"));

	public static ImageFormat Png { get; } = new ImageFormat
		(new Guid("{b96b3caf-0728-11d3-9d7b-0000f81ef32e}"));

	public static ImageFormat Tiff { get; } = new ImageFormat
		(new Guid("{b96b3cb1-0728-11d3-9d7b-0000f81ef32e}"));

	public static ImageFormat Wmf { get; } = new ImageFormat
		(new Guid("{b96b3cad-0728-11d3-9d7b-0000f81ef32e}"));

	// Determine if two objects are equal.
	public override bool Equals(object obj)
			{
				ImageFormat other = (obj as ImageFormat);
				if(other != null)
				{
					return (other.guid.Equals(guid));
				}
				else
				{
					return false;
				}
			}

	// Get the hash code for this object.
	public override int GetHashCode()
			{
				return guid.GetHashCode();
			}

	// Convert this object into a string.
	public override string ToString()
			{
				if(this == Bmp)
				{
					return "Bmp";
				}
				else if(this == Emf)
				{
					return "Emf";
				}
				else if(this == Exif)
				{
					return "Exif";
				}
				else if(this == Gif)
				{
					return "Gif";
				}
				else if(this == Icon)
				{
					return "Icon";
				}
				else if(this == Jpeg)
				{
					return "Jpeg";
				}
				else if(this == MemoryBmp)
				{
					return "MemoryBMP";
				}
				else if(this == Png)
				{
					return "Png";
				}
				else if(this == Tiff)
				{
					return "Tiff";
				}
				else if(this == Wmf)
				{
					return "Wmf";
				}
				else
				{
					return "[ImageFormat: " + guid.ToString() + "]";
				}
			}

}; // class ImageFormat

#endif // !ECMA_COMPAT

}; // namespace System.Drawing.Imaging
