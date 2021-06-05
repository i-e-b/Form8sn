/*
 * Image.cs - Implementation of the "DotGNU.Images.Image" class.
 *
 * Copyright (C) 2003  Southern Storm Software, Pty Ltd.
 *
 * This program is free software, you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY, without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program, if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using System.IO;

namespace Portable.Drawing.Imaging.ImageFormats
{
	
	public class PortableImage : MarshalByRefObject, ICloneable, IDisposable
{
	// Internal state.
	internal PixelFormat pixelFormat;
	internal ImageFlags imageFlags;
	private Frame[] frames;

	// Standard image formats.
	public const string Png = "png";
	public const string Jpeg = "jpeg";
	public const string Gif = "gif";
	public const string Tiff = "tiff";
	public const string Bmp = "bmp";
	public const string Icon = "icon";
	public const string Cursor = "cursor";
	public const string Exif = "exif";

	// Constructors.
	public PortableImage()
			{
				Width = 0;
				Height = 0;
				pixelFormat = PixelFormat.Undefined;
				NumFrames = 0;
				frames = null;
				LoadFormat = null;
				Palette = null;
				TransparentPixel = -1;
			}

	public PortableImage(int width, int height, PixelFormat pixelFormat)
			{
				Width = width;
				Height = height;
				this.pixelFormat = pixelFormat;
				NumFrames = 0;
				frames = null;
				LoadFormat = null;
				Palette = null;
				TransparentPixel = -1;
			}

	private PortableImage(PortableImage image, Frame thisFrameOnly) :
		this(image, image.PixelFormat)
			{
				if(thisFrameOnly != null)
				{
					NumFrames = 1;
					frames = new Frame [1];
					frames[0] = thisFrameOnly.CloneFrame(this);
				}
				else
				{
					NumFrames = image.NumFrames;
					if(image.frames != null)
					{
						int frame;
						frames = new Frame [NumFrames];
						for(frame = 0; frame < NumFrames; ++frame)
						{
							frames[frame] =
								image.frames[frame].CloneFrame(this);
						}
					}
				}
				
			}

	private PortableImage(PortableImage image, PixelFormat format)
			{
				Width = image.Width;
				Height = image.Height;
				pixelFormat = image.pixelFormat;
				LoadFormat = image.LoadFormat;
				if(image.Palette != null)
				{
					Palette = (int[])(image.Palette.Clone());
				}
				TransparentPixel = image.TransparentPixel;
			}

	// Destructor.
	~PortableImage()
			{
				Dispose(false);
			}

	// Get or set the image's overall properties.  The individual frames
	// may have different properties from the ones stated here.
	public int Width { get; set; }

	public int Height { get; set; }

	public int NumFrames { get; private set; }

	public PixelFormat PixelFormat
			{
				get
				{
					return pixelFormat;
				}
				set
				{
					pixelFormat = value;
				}
			}

	public string LoadFormat { get; set; }

	public int[] Palette { get; set; }

	public int TransparentPixel { get; set; }

	// Add a new frame to this image.
	public Frame AddFrame()
			{
				return AddFrame(Width, Height, pixelFormat);
			}

	public Frame AddFrame(int width, int height, PixelFormat pixelFormat)
			{
				Frame frame = new Frame(this, width, height, pixelFormat);
				frame.Palette = Palette;
				frame.TransparentPixel = TransparentPixel;
				return AddFrame(frame);
			}

	public Frame AddFrame(Frame frame)
			{
				if(frames == null)
				{
					frames = new Frame[] {frame};
					NumFrames = 1;
				}
				else
				{
					Frame[] newFrames = new Frame [NumFrames + 1];
					Array.Copy(frames, 0, newFrames, 0, NumFrames);
					frames = newFrames;
					frames[NumFrames] = frame;
					++NumFrames;
				}
				return frame;
			}

	// Clone this object.
	public object Clone()
			{
				return new PortableImage(this, null);
			}

	// Dispose of this object.
	public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

	protected virtual void Dispose(bool disposing)
			{
				if(frames != null)
				{
					int frame;
					for(frame = 0; frame < NumFrames; ++frame)
					{
						frames[frame].Dispose();
					}
					frames = null;
					NumFrames = 0;
				}
				Palette = null;
				TransparentPixel = -1;
			}

	// Get a particular frame within this image.
	public Frame GetFrame(int frame)
			{
				if(frame >= 0 && frame < NumFrames && frames != null)
				{
					return frames[frame];
				}
				else
				{
					return null;
				}
			}

	public void SetFrame(int frame, Frame newFrame)
			{
				if(frame >= 0 && frame < NumFrames && newFrame != null)
				{
					newFrame.NewImage(this);
					frames[frame] = newFrame;
				}
			}

	// Determine if it is possible to load a particular format.
	public static bool CanLoadFormat(string format)
			{
				return (format == Bmp || format == Icon ||
				        format == Cursor || format == Png ||
						format == Gif || format == Jpeg ||
						format == Exif);
			}

	// Determine if it is possible to save a particular format.
	public static bool CanSaveFormat(string format)
			{
				return (format == Bmp || format == Icon ||
				        format == Cursor || format == Png ||
						format == Gif || format == Jpeg);
			}

	// Load an image from a stream into this object.  This will
	// throw "FormatException" if the format could not be loaded.
	public void Load(string filename)
			{
				if(filename == null)
				{
					throw new ArgumentNullException("filename", "Argument cannot be null");
				}

				Stream stream = new FileStream
					(filename, FileMode.Open, FileAccess.Read);
				try
				{
					Load(stream);
				}
				finally
				{
					stream.Close();
				}
			}

	public void Load(Stream stream)
			{
				// Read the first 4 bytes from the stream to determine
				// what kind of image we are loading.
				byte[] magic = new byte [4];
				stream.Read(magic, 0, 4);
				if(magic[0] == (byte)'B' && magic[1] == (byte)'M')
				{
					// Windows bitmap image.
					BmpReader.Load(stream, this);
				}
				else if(magic[0] == 0 && magic[1] == 0 &&
						magic[2] == 1 && magic[3] == 0)
				{
					// Windows icon image.
					IconReader.Load(stream, this, false);
				}
				else if(magic[0] == 0 && magic[1] == 0 &&
						magic[2] == 2 && magic[3] == 0)
				{
					// Windows cursor image (same as icon, with hotspots).
					IconReader.Load(stream, this, true);
				}
				else if(magic[0] == 137 && magic[1] == 80 &&
						magic[2] == 78 && magic[3] == 71)
				{
					// PNG image.
					PngReader.Load(stream, this);
				}
				else if(magic[0] == (byte)'G' && magic[1] == (byte)'I' &&
						magic[2] == (byte)'F' && magic[3] == (byte)'8')
				{
					// GIF image.
					GifReader.Load(stream, this);
				}
				else if(magic[0] == (byte)0xFF && magic[1] == (byte)0xD8)
				{
					// JPEG or EXIF image.
					//JpegReader.Load(stream, this, magic, 4);
					// TODO: implement JPEG reading
					stream.Seek(0, SeekOrigin.Begin);
					JpegReaderHelper.Load(stream, this);
				}
				else
				{
					// Don't know how to load this kind of file.
					throw new FormatException();
				}
			}

	// Save this image to a stream, in a particular format.
	// If the format is not specified, it defaults to "png".
	public void Save(string filename)
			{
				Stream stream = new FileStream
					(filename, FileMode.Create, FileAccess.Write);
				try
				{
					Save(stream, null);
				}
				finally
				{
					stream.Close();
				}
			}

	public void Save(string filename, string format)
			{
				Stream stream = new FileStream
					(filename, FileMode.Create, FileAccess.Write);
				try
				{
					Save(stream, format);
				}
				finally
				{
					stream.Close();
				}
			}

	public void Save(Stream stream)
			{
				Save(stream, null);
			}

	public void Save(Stream stream, string format)
			{
				// Select a default format for the save.
				if(format == null)
				{
					format = Png;
				}

				// Determine how to save the image.
				if(format == Bmp)
				{
					// Windows bitmap image.
					BmpWriter.Save(stream, this);
				}
				else if(format == Icon)
				{
					// Windows icon image.
					IconWriter.Save(stream, this, false);
				}
				else if(format == Cursor)
				{
					// Windows cursor image (same as icon, with hotspots).
					IconWriter.Save(stream, this, true);
				}
				else if(format == Png)
				{
					// PNG image.
					PngWriter.Save(stream, this);
				}
				else if(format == Gif)
				{
					// GIF image.  If the image is RGB, then we encode
					// as a PNG instead and hope that the image viewer
					// is smart enough to check the magic number before
					// decoding the image.
					if(GifWriter.IsGifEncodable(this))
					{
						GifWriter.Save(stream, this);
					}
					else
					{
						PngWriter.Save(stream, this);
					}
				}
				else if(format == Jpeg)
				{
					// JPEG image.
					//JpegWriter.Save(stream, this);
					// TODO: implement portable jpeg
					throw new NotImplementedException("PORTABLE JPEG IS NOT YET IMPLEMENTED");
				}
			}

	// Stretch this image to a new size.
	public PortableImage Stretch(int width, int height)
			{
				Width = width;
				Height = height;
				PortableImage newImage = null;
				for (int i = 0; i < frames.Length; i++)
				{
					if (i == 0)
						newImage = new PortableImage(this, frames[0].Scale(width, height));
					else
						newImage.AddFrame(frames[i].Scale(width, height));
				}
				return newImage;
			}

	// Create a new image that contains a copy of one frame from this image.
	public PortableImage ImageFromFrame(int frame)
			{
				return new PortableImage(this, GetFrame(frame));
			}

	public PortableImage Reformat(PixelFormat newFormat)
			{
				PortableImage newImage = new PortableImage(this, newFormat);
				for (int i = 0; i < frames.Length; i++)
					newImage.AddFrame(frames[i].Reformat(newFormat));
				return newImage;
			}

	public ImageFlags GetFlags()
	{
		return imageFlags;
	}
};

	// class Image

}; // namespace DotGNU.Images
