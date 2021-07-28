﻿namespace Portable.Drawing.Toolkit.Portable.Rasteriser
{
    public readonly struct Size2D
    {
        public int Width { get; }
        public int Height { get; }

        public Size2D(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}