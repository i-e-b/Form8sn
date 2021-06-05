﻿/*
 * PaperSource.cs - Implementation of the
 *			"System.Drawing.Printing.PaperSource" class.
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

using System.Text;

namespace Portable.Drawing.Printing
{
    public enum PaperSourceKind
    {
        None			= 0,
        Upper			= 1,
        Lower			= 2,
        Middle			= 3,
        Manual			= 4,
        Envelope		= 5,
        ManualFeed		= 6,
        AutomaticFeed	= 7,
        TractorFeed		= 8,
        SmallFormat		= 9,
        LargeFormat		= 10,
        LargeCapacity	= 11,
        Cassette		= 14,
        FormSource		= 15,
        Custom			= 257

    };

    public class PaperSource
    {
        // Internal state.

        // Constructor.
        internal PaperSource(PaperSourceKind kind, string name)
        {
            Kind = kind;
            if(name == null)
            {
                SourceName = kind.ToString();
            }
            else
            {
                SourceName = name;
            }
        }

        // Get the paper kind.
        public PaperSourceKind Kind { get; }

        public string SourceName { get; }

        // Convert this object into a string.
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[PaperSource ");
            builder.Append(SourceName);
            builder.Append(" Kind=");
            builder.Append(Kind.ToString());
            builder.Append(']');
            return builder.ToString();
        }

    }; // class PaperSource

}; // namespace System.Drawing.Printing