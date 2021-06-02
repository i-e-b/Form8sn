/*
 * ColorMatrix.cs - Implementation of the
 *			"System.Drawing.Imaging.ColorMatrix" class.
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
	public enum ColorChannelFlag
	{
		ColorChannelC    = 0,
		ColorChannelM    = 1,
		ColorChannelY    = 2,
		ColorChannelK    = 3,
		ColorChannelLast = 4,

	};
	public enum ColorMatrixFlag
	{
		Default   = 0,
		SkipGrays = 1,
		AltGrays  = 2

	};
public sealed class ColorMatrix
{
	// Internal state.

	// Constructor.
	public ColorMatrix()
			{
				// Set up the 5x5 identity matrix as the default value.
				Matrix00 = 1.0f; Matrix01 = 0.0f; Matrix02 = 0.0f; Matrix03 = 0.0f; Matrix04 = 0.0f;
				Matrix10 = 0.0f; Matrix11 = 1.0f; Matrix12 = 0.0f; Matrix13 = 0.0f; Matrix14 = 0.0f;
				Matrix20 = 0.0f; Matrix21 = 0.0f; Matrix22 = 1.0f; Matrix23 = 0.0f; Matrix24 = 0.0f;
				Matrix30 = 0.0f; Matrix31 = 0.0f; Matrix32 = 0.0f; Matrix33 = 1.0f; Matrix34 = 0.0f;
				Matrix40 = 0.0f; Matrix41 = 0.0f; Matrix42 = 0.0f; Matrix43 = 0.0f; Matrix44 = 1.0f;
			}
	
	public ColorMatrix(float[][] newColorMatrix)
			{
				Matrix00 = newColorMatrix[0][0];
				Matrix01 = newColorMatrix[0][1];
				Matrix02 = newColorMatrix[0][2];
				Matrix03 = newColorMatrix[0][3];
				Matrix04 = newColorMatrix[0][4];
				Matrix10 = newColorMatrix[1][0];
				Matrix11 = newColorMatrix[1][1];
				Matrix12 = newColorMatrix[1][2];
				Matrix13 = newColorMatrix[1][3];
				Matrix14 = newColorMatrix[1][4];
				Matrix20 = newColorMatrix[2][0];
				Matrix21 = newColorMatrix[2][1];
				Matrix22 = newColorMatrix[2][2];
				Matrix23 = newColorMatrix[2][3];
				Matrix24 = newColorMatrix[2][4];
				Matrix30 = newColorMatrix[3][0];
				Matrix31 = newColorMatrix[3][1];
				Matrix32 = newColorMatrix[3][2];
				Matrix33 = newColorMatrix[3][3];
				Matrix34 = newColorMatrix[3][4];
				Matrix40 = newColorMatrix[4][0];
				Matrix41 = newColorMatrix[4][1];
				Matrix42 = newColorMatrix[4][2];
				Matrix43 = newColorMatrix[4][3];
				Matrix44 = newColorMatrix[4][4];
			}

	// Get or set this object's properties.
	public float this[int row, int column]
			{
				get
				{
					switch(row * 5 + column)
					{
						case 0 * 5 + 0:		return Matrix00;
						case 0 * 5 + 1:		return Matrix01;
						case 0 * 5 + 2:		return Matrix02;
						case 0 * 5 + 3:		return Matrix03;
						case 0 * 5 + 4:		return Matrix04;
						case 1 * 5 + 0:		return Matrix10;
						case 1 * 5 + 1:		return Matrix11;
						case 1 * 5 + 2:		return Matrix12;
						case 1 * 5 + 3:		return Matrix13;
						case 1 * 5 + 4:		return Matrix14;
						case 2 * 5 + 0:		return Matrix20;
						case 2 * 5 + 1:		return Matrix21;
						case 2 * 5 + 2:		return Matrix22;
						case 2 * 5 + 3:		return Matrix23;
						case 2 * 5 + 4:		return Matrix24;
						case 3 * 5 + 0:		return Matrix30;
						case 3 * 5 + 1:		return Matrix31;
						case 3 * 5 + 2:		return Matrix32;
						case 3 * 5 + 3:		return Matrix33;
						case 3 * 5 + 4:		return Matrix34;
						case 4 * 5 + 0:		return Matrix40;
						case 4 * 5 + 1:		return Matrix41;
						case 4 * 5 + 2:		return Matrix42;
						case 4 * 5 + 3:		return Matrix43;
						case 4 * 5 + 4:		return Matrix44;
						default:			return 0.0f;
					}
				}
				set
				{
					switch(row * 5 + column)
					{
						case 0 * 5 + 0:		Matrix00 = value; break;
						case 0 * 5 + 1:		Matrix01 = value; break;
						case 0 * 5 + 2:		Matrix02 = value; break;
						case 0 * 5 + 3:		Matrix03 = value; break;
						case 0 * 5 + 4:		Matrix04 = value; break;
						case 1 * 5 + 0:		Matrix10 = value; break;
						case 1 * 5 + 1:		Matrix11 = value; break;
						case 1 * 5 + 2:		Matrix12 = value; break;
						case 1 * 5 + 3:		Matrix13 = value; break;
						case 1 * 5 + 4:		Matrix14 = value; break;
						case 2 * 5 + 0:		Matrix20 = value; break;
						case 2 * 5 + 1:		Matrix21 = value; break;
						case 2 * 5 + 2:		Matrix22 = value; break;
						case 2 * 5 + 3:		Matrix23 = value; break;
						case 2 * 5 + 4:		Matrix24 = value; break;
						case 3 * 5 + 0:		Matrix30 = value; break;
						case 3 * 5 + 1:		Matrix31 = value; break;
						case 3 * 5 + 2:		Matrix32 = value; break;
						case 3 * 5 + 3:		Matrix33 = value; break;
						case 3 * 5 + 4:		Matrix34 = value; break;
						case 4 * 5 + 0:		Matrix40 = value; break;
						case 4 * 5 + 1:		Matrix41 = value; break;
						case 4 * 5 + 2:		Matrix42 = value; break;
						case 4 * 5 + 3:		Matrix43 = value; break;
						case 4 * 5 + 4:		Matrix44 = value; break;
					}
				}
			}
	public float Matrix00 { get; set; }

	public float Matrix01 { get; set; }

	public float Matrix02 { get; set; }

	public float Matrix03 { get; set; }

	public float Matrix04 { get; set; }

	public float Matrix10 { get; set; }

	public float Matrix11 { get; set; }

	public float Matrix12 { get; set; }

	public float Matrix13 { get; set; }

	public float Matrix14 { get; set; }

	public float Matrix20 { get; set; }

	public float Matrix21 { get; set; }

	public float Matrix22 { get; set; }

	public float Matrix23 { get; set; }

	public float Matrix24 { get; set; }

	public float Matrix30 { get; set; }

	public float Matrix31 { get; set; }

	public float Matrix32 { get; set; }

	public float Matrix33 { get; set; }

	public float Matrix34 { get; set; }

	public float Matrix40 { get; set; }

	public float Matrix41 { get; set; }

	public float Matrix42 { get; set; }

	public float Matrix43 { get; set; }

	public float Matrix44 { get; set; }
}; // class ColorMatrix

}; // namespace System.Drawing.Imaging
