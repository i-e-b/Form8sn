﻿/*
 * ColorPalette.cs - Implementation of the
 *			"System.Drawing.Imaging.ColorPalette" class.
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

using System.Drawing;

namespace Portable.Drawing.Imaging
{

    public sealed class ColorPalette
    {
        // Internal state.

        // Constructor.
        internal ColorPalette(Color[] entries, int flags)
        {
            Entries = entries;
            Flags = flags;
        }

        // Get this object's properties.
        public Color[] Entries { get; }

        public int Flags { get; }
    }; // class ColorPalette

}; // namespace System.Drawing.Imaging