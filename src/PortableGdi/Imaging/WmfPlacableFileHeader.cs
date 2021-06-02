/*
 * WmfPlaceableFileHeader.cs - Implementation of the
 *			"System.Drawing.Imaging.WmfPlaceableFileHeader" class.
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

namespace System.Drawing.Imaging
{

public sealed class WmfPlaceableFileHeader
{
	// Internal state.

	// Constructor.
	public WmfPlaceableFileHeader()
			{
				Key = unchecked((int)0x9AC6CDD7);
			}

	// Get or set this object's properties.
	public short BboxBottom { get; set; }

	public short BboxLeft { get; set; }

	public short BboxRight { get; set; }

	public short BboxTop { get; set; }

	public short Checksum { get; set; }

	public short Hmf { get; set; }

	public short Inch { get; set; }

	public int Key { get; set; }

	public int Reserved { get; set; }
}; // class WmfPlaceableFileHeader

}; // namespace System.Drawing.Imaging
