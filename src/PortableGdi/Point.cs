/*
 * Point.cs - Implementation of the "System.Drawing.Point" class.
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

namespace Portable.Drawing
{
#if !ECMA_COMPAT
    
    [ComVisible(true)]
#endif
    public struct Point
    {
        // Internal state.
        private int _x;
        private int _y;

        // The empty point.
        public static readonly Point Empty = new Point(0, 0);

        // Constructors.
        public Point(int dw)
        {
            _x = (short) dw;
            _y = (dw >> 16);
        }

        public Point(Size sz)
        {
            _x = sz.Width;
            _y = sz.Height;
        }

        public Point(int x, int y)
        {
            this._x = x;
            this._y = y;
        }

        // Determine if this point is empty.
        public bool IsEmpty => (_x == 0 && _y == 0);

        // Get or set the X co-ordinate.
        public int X
        {
            get => _x;
            set => _x = value;
        }

        // Get or set the Y co-ordinate.
        public int Y
        {
            get => _y;
            set => _y = value;
        }

        // Convert a PointF object into a Point object using ceiling conversion.
        public static Point Ceiling(PointF value)
        {
            return new Point((int) (Math.Ceiling(value.X)),
                (int) (Math.Ceiling(value.Y)));
        }


        // Determine if two points are equal.
        public override bool Equals(Object obj)
        {
            if (!(obj is Point other)) return false;
            return (_x == other._x && _y == other._y);
        }

        // Get a hash code for this object.
        public override int GetHashCode()
        {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            return _x ^ _y;
            // ReSharper restore NonReadonlyMemberInGetHashCode
        }

        // Offset this point by a specified amount.
        public void Offset(int dx, int dy)
        {
            _x += dx;
            _y += dy;
        }

        // Convert a PointF object into a Point object using rounding conversion.
        public static Point Round(PointF value)
        {
            return new Point((int) (Math.Round(value.X)),
                (int) (Math.Round(value.Y)));
        }

        // Convert this object into a string.
        public override String ToString()
        {
            return $"{{X={_x},Y={_y}}}";
        }

        // Convert a PointF object into a Point object using truncating conversion.
        public static Point Truncate(PointF value)
        {
            return new Point((int) (value.X), (int) (value.Y));
        }

        // Overloaded operators.
        public static Point operator +(Point pt, Size sz)
        {
            return new Point(pt._x + sz.Width, pt._y + sz.Height);
        }

        public static Point operator -(Point pt, Size sz)
        {
            return new Point(pt._x - sz.Width, pt._y - sz.Height);
        }

        public static bool operator ==(Point left, Point right)
        {
            return (left._x == right._x && left._y == right._y);
        }

        public static bool operator !=(Point left, Point right)
        {
            return (left._x != right._x || left._y != right._y);
        }

        public static explicit operator Size(Point p)
        {
            return new Size(p._x, p._y);
        }

        public static implicit operator PointF(Point p)
        {
            return new PointF(p._x, p._y);
        }
    }; // struct Point
}; // namespace System.Drawing