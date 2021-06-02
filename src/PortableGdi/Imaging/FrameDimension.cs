/*
 * FrameDimension.cs - Implementation of the
 *			"System.Drawing.Imaging.FrameDimension" class.
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

#if !ECMA_COMPAT

public sealed class FrameDimension
{
	// Internal state.
	private Guid guid;

	// Constructor.
	public FrameDimension(Guid guid)
			{
				this.guid = guid;
			}

	// Get the GUID for this object.
	public Guid Guid
			{
				get
				{
					return guid;
				}
			}

	// Get standard frame dimension objects.
	public static FrameDimension Page { get; } = new FrameDimension
		(new Guid("{7462dc86-6180-4c7e-8e3f-ee7333a7a483}"));

	public static FrameDimension Resolution { get; } = new FrameDimension
		(new Guid("{84236f7b-3bd3-428f-8dab-4ea1439ca315}"));

	public static FrameDimension Time { get; } = new FrameDimension
		(new Guid("{6aedbd6d-3fb5-418a-83a6-7f45229dc872}"));

	// Determine if two objects are equal.
	public override bool Equals(object obj)
			{
				FrameDimension other = (obj as FrameDimension);
				if(other != null)
				{
					return guid.Equals(other.guid);
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
				if(this == Page)
				{
					return "Page";
				}
				else if(this == Resolution)
				{
					return "Resolution";
				}
				else if(this == Time)
				{
					return "Time";
				}
				else
				{
					return "[FrameDimension: " + guid.ToString() + "]";
				}
			}

}; // class FrameDimension

#endif // !ECMA_COMPAT

}; // namespace System.Drawing.Imaging
