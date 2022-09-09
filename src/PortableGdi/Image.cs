/*
 * Image.cs - Implementation of the "System.Drawing.Image" class.
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
using System.IO;
using System.Runtime.InteropServices;
using Portable.Drawing.Imaging;
using Portable.Drawing.Imaging.ImageFormats;

namespace Portable.Drawing
{
#if !ECMA_COMPAT
    [Serializable]
    [ComVisible(true)]
#endif
    public abstract class Image
        : MarshalByRefObject, ICloneable, IDisposable
#if CONFIG_SERIALIZATION
	, ISerializable
#endif
    {
        // Internal state.
        internal int flags;
#if !ECMA_COMPAT
        internal Guid[] frameDimensionsList;
        internal ImageFormat rawFormat;
#endif
        internal int height;
        internal float horizontalResolution;
        internal ColorPalette palette;
        internal PixelFormat pixelFormat;
        internal int[] propertyIdList;
        internal PropertyItem[] propertyItems;
        internal float verticalResolution;
        internal int width;
        internal PortableImage? dgImage;
        internal Toolkit.IToolkitImage? toolkitImage;

        /// <summary>
        /// File format this image was loaded from (if any)
        /// See constants in <see cref="Portable.Drawing.Imaging.ImageFormats.PortableImage"/>
        /// </summary>
        public string? LoadFormat => dgImage?.LoadFormat;

        // Constructors.
        internal Image()
        {
        }

        internal Image(PortableImage dgImage)
        {
            SetDGImage(dgImage);
        }
#if CONFIG_SERIALIZATION
	internal Image(SerializationInfo info, StreamingContext context)
			{
				// Do we need to Handle PixelFormats ?.
				byte[] data = null;
				data = (byte[])info.GetValue("Data", typeof(byte[]));
				MemoryStream stream = new MemoryStream(data,false);
				DotGNU.Images.Image dgImage = new DotGNU.Images.Image();
				dgImage.Load(stream);
				SetDGImage(dgImage);
			}
#endif

        // Destructor.
        ~Image()
        {
            Dispose(false);
        }

        // Get this object's properties.
        public int Flags => flags;
#if !ECMA_COMPAT
        public Guid[] FrameDimensionsList
        {
            get { return frameDimensionsList; }
        }
#endif
        public int Height
        {
            get { return height; }
        }

        public float HorizontalResolution
        {
            get { return horizontalResolution; }
        }

        public ColorPalette Palette
        {
            get { return palette; }
            set { palette = value; }
        }

        public SizeF PhysicalDimension
        {
            get { return new SizeF(width, height); }
        }

        public PixelFormat PixelFormat
        {
            get { return pixelFormat; }
        }

        public int[] PropertyIdList
        {
            get
            {
                if (propertyIdList == null)
                {
                    propertyIdList = new int [0];
                }

                return propertyIdList;
            }
        }

        public PropertyItem[] PropertyItems
        {
            get
            {
                if (propertyItems == null)
                {
                    propertyItems = new PropertyItem [0];
                }

                return propertyItems;
            }
        }
#if !ECMA_COMPAT
        public ImageFormat RawFormat
        {
            get { return rawFormat; }
        }
#endif
        public Size Size
        {
            get { return new Size(width, height); }
        }

        public float VerticalResolution
        {
            get { return verticalResolution; }
        }

        public int Width
        {
            get { return width; }
        }

        // Make a copy of this object.
        public virtual object Clone()
        {
            // TODO
            return null;
        }

        // Dispose of this object.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (dgImage != null)
            {
                dgImage.Dispose();
                dgImage = null;
            }
        }

        // Load an image from a file.
        public static Image FromFile(string filename)
        {
            return FromFile(filename, false);
        }

        public static Image FromFile
            (string filename, bool useEmbeddedColorManagement)
        {
            var image = new PortableImage();
            image.Load(filename);
            return new Bitmap(image);
        }

        // Convert a HBITMAP object into an image.
        public static Image FromHbitmap(IntPtr hbitmap)
        {
            return FromHbitmap(hbitmap, IntPtr.Zero);
        }

        [TODO]
        public static Image FromHbitmap(IntPtr hbitmap, IntPtr hpalette)
        {
            // TODO
            return null;
        }

        // Load an image from a particular stream.
        public static Image FromStream(Stream stream)
        {
            return FromStream(stream, false);
        }

        public static Image FromStream
            (Stream stream, bool useEmbeddedColorManagement)
        {
            var image = new PortableImage();
            image.Load(stream);
            return new Bitmap(image);
        }

        // Get a bounding rectangle for this image.
        [TODO]
        public RectangleF GetBounds(ref GraphicsUnit pageUnit)
        {
            // TODO
            return RectangleF.Empty;
        }

#if !ECMA_COMPAT

        // Get parameter information for a specific encoder.
        [TODO]
        public EncoderParameters GetEncoderParameterList(Guid encoder)
        {
            // TODO
            return null;
        }

        // Get the number of frames in a specific dimension.
        [TODO]
        public int GetFrameCount(FrameDimension dimension)
        {
            // TODO
            return 1;
        }

        // Select a new frame and make it the active one.
        [TODO]
        public int SelectActiveFrame(FrameDimension dimension, int frameIndex)
        {
            // TODO
            return frameIndex;
        }

#endif

        // Get the number of bits per pixel in a specific format.
        public static int GetPixelFormatSize
            (PixelFormat pixfmt)
        {
            switch (pixfmt)
            {
                case PixelFormat.Format1bppIndexed:
                    return 1;
                case PixelFormat.Format4bppIndexed:
                    return 4;
                case PixelFormat.Format8bppIndexed:
                    return 8;
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                case PixelFormat.Format16bppArgb1555:
                case PixelFormat.Format16bppGrayScale: return 16;
                case PixelFormat.Format24bppRgb:
                    return 24;
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppArgb:
                    return 32;
                case PixelFormat.Format48bppRgb:
                    return 48;
                case PixelFormat.Format64bppPArgb:
                case PixelFormat.Format64bppArgb:
                    return 64;
            }

            return 0;
        }

        // Get a specific property item
        [TODO]
        public PropertyItem GetPropertyItem(int propid)
        {
            // TODO
            return null;
        }

        // Get a thumbnail version of this image.
        [TODO]
        public Image GetThumbnailImage(int thumbWidth, int thumbHeight,
            GetThumbnailImageAbort callback,
            IntPtr callbackData)
        {
            // TODO
            return null;
        }

        // Delegate for aborting "GetThumbnailImage" prematurely.
        public delegate bool GetThumbnailImageAbort();

        // Check for specific kinds of pixel formats.
        public static bool IsAlphaPixelFormat
            (PixelFormat pixfmt)
        {
            return ((pixfmt &
                     PixelFormat.Alpha) != 0);
        }

        public static bool IsCanonicalPixelFormat
            (PixelFormat pixfmt)
        {
            return ((pixfmt &
                     PixelFormat.Canonical) != 0);
        }

        public static bool IsExtendedPixelFormat
            (PixelFormat pixfmt)
        {
            return ((pixfmt &
                     PixelFormat.Extended) != 0);
        }

        // Remove a specific property.
        [TODO]
        public void RemoveProperty(int propid)
        {
            // TODO
        }

        // Rotate and/or flip this image.
        [TODO]
        public void RotateFlip(RotateFlipType rotateFlipType)
        {
            // TODO
        }

        // Save this image to a file.
        public void Save(string filename)
        {
            dgImage.Save(filename);
        }

        public void Save(string filename, ImageFormat format)
        {
            dgImage.Save(filename, ImageFormatToDG(format));
        }

        [TODO]
        public void Save(string filename, ImageCodecInfo encoder,
            EncoderParameters encoderParameters)
        {
            // TODO
        }

        // Save this image to a stream.
        public void Save(Stream stream, ImageFormat format)
        {
            dgImage.Save(stream, ImageFormatToDG(format));
        }

        private string ImageFormatToDG(ImageFormat format)
        {
            if (format == ImageFormat.Bmp)
                return "bmp";
            if (format == ImageFormat.Gif)
                return "gif";
            if (format == ImageFormat.Icon)
                return "icon";
            if (format == ImageFormat.Jpeg)
                return "jpeg";
            if (format == ImageFormat.Png)
                return "png";
            if (format == ImageFormat.Tiff)
                return "tiff";
            if (format == ImageFormat.Exif)
                return "exif";
            else
                throw new NotSupportedException(format.ToString());
        }

        [TODO]
        public void Save(Stream stream, ImageCodecInfo encoder,
            EncoderParameters encoderParameters)
        {
            // TODO
        }

        // Add a frame to the previously saved image file.
        [TODO]
        public void SaveAdd(EncoderParameters encoderParamers)
        {
            // TODO
        }

        [TODO]
        public void SaveAdd(Image image, EncoderParameters encoderParamers)
        {
            // TODO
        }


        // Set a property on this image.
        [TODO]
        public void SetPropertyItem(PropertyItem propitem)
        {
            // TODO
        }


        // Set the dgImage field within this object.
        internal void SetDGImage(PortableImage dgImage)
        {
            flags = (int) dgImage.GetFlags();
#if !ECMA_COMPAT
            switch (dgImage.LoadFormat)
            {
                case PortableImage.Png:
                    rawFormat = ImageFormat.Png;
                    break;
                case PortableImage.Jpeg:
                    rawFormat = ImageFormat.Jpeg;
                    break;
                case PortableImage.Gif:
                    rawFormat = ImageFormat.Gif;
                    break;
                case PortableImage.Tiff:
                    rawFormat = ImageFormat.Tiff;
                    break;
                case PortableImage.Bmp:
                    rawFormat = ImageFormat.Bmp;
                    break;
                case PortableImage.Icon:
                    rawFormat = ImageFormat.Icon;
                    break;
                case PortableImage.Exif:
                    rawFormat = ImageFormat.Exif;
                    break;

                default: // Not loaded from a source?
                    rawFormat = ImageFormat.MemoryBmp;
                    break;
            }

            frameDimensionsList = new Guid [0];
            this.dgImage = dgImage;
            // If we are loading an icon, set the size of the image
            // to the size of the first icon
            if (rawFormat == ImageFormat.Icon)
            {
                width = dgImage.GetFrame(0).Width;
                height = dgImage.GetFrame(0).Height;
            }
            else
            {
                width = dgImage.Width;
                height = dgImage.Height;
            }
#else
				this.dgImage = dgImage;
				width = dgImage.GetFrame(0).Width;
				height = dgImage.GetFrame(0).Height;
#endif
            horizontalResolution = Graphics.DefaultScreenDpi;
            verticalResolution = Graphics.DefaultScreenDpi;
            pixelFormat = (PixelFormat)
                (dgImage.PixelFormat);
        }

        // This is an internal member and should not be used.
        // Returns a bitmap which is the reformatted (to newFormat) of the image.
        public Image Reformat(PixelFormat newFormat)
        {
            return new Bitmap(dgImage.Reformat((PixelFormat) newFormat));
        }

        // This is an internal member and should not be used.
        // Returns a bitmap which is the resized (to newWidth and newHeight) of the first frame.
        public Image Resize(int newWidth, int newHeight)
        {
            Frame frame = dgImage.GetFrame(0);
            Frame newFrame = frame.AdjustImage(0, 0, width, 0, 0, height, 0, 0, newWidth, 0, 0, newHeight);
            var newImage = new PortableImage(newWidth, newHeight, newFrame.PixelFormat);
            newImage.AddFrame(newFrame);
            return new Bitmap(newImage);
        }
    }; // class Image
}; // namespace System.Drawing