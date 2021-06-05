/*
 * BitmapData.cs - Implementation of the
 *			"System.Drawing.Imaging.BitmapData" class.
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

namespace Portable.Drawing.Imaging
{
	public sealed class BitmapData
{
	// Internal state.
	internal GCHandle dataHandle;

	// Constructor.
	public BitmapData() {}

	// Get or set this object's properties.
	public int Height { get; set; }

	public PixelFormat PixelFormat { get; set; }

	public int Reserved { get; set; }

	public IntPtr Scan0 { get; set; }

	public int Stride { get; set; }

	public int Width { get; set; }
}; // class BitmapData

}; // namespace System.Drawing.Imaging
