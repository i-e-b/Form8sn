﻿/*
 * EncoderParameters.cs - Implementation of the
 *			"System.Drawing.Imaging.EncoderParameters" class.
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

    public sealed class EncoderParameters : IDisposable
    {
        // Internal state.

        // Constructors.
        public EncoderParameters()
        {
            Param = new EncoderParameter [1];
        }
        public EncoderParameters(int count)
        {
            Param = new EncoderParameter [count];
        }

        // Get or set the parameter array.
        public EncoderParameter[] Param { get; set; }

        // Dispose this object.
        public void Dispose()
        {
            Param = null;
            GC.SuppressFinalize(this);
        }

    }; // class EncoderParameters

#endif // !ECMA_COMPAT

}; // namespace System.Drawing.Imaging