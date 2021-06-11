/*
 * Bitmap.cs - Implementation of the "System.Drawing.Bitmap" class.
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
using System.Security;
using Portable.Drawing.Imaging;
using Portable.Drawing.Imaging.ImageFormats;

namespace Portable.Drawing
{
	public enum ImageLockMode
	{
		None			= 0,
		ReadOnly		= 1,
		WriteOnly		= 2,
		ReadWrite		= 3,
		UserInputBuffer	= 4

	};

[Serializable]
[ComVisible(true)]
public sealed class Bitmap : Image
{
	// Constructors.
	public Bitmap(Image original)
			: this(original, original.Width, original.Height) {}
	public Bitmap(Image original, Size newSize)
			: this (original, newSize.Width, newSize.Height) {}
	public Bitmap(Stream stream) : this(stream, false) {}
	public Bitmap(Stream stream, bool useIcm)
			{
				PortableImage dgImage = new PortableImage();
				dgImage.Load(stream);
				SetDGImage(dgImage);
			}
	public Bitmap(string filename) : this(filename, false) {}
	public Bitmap(string filename, bool useIcm)
			{
				PortableImage dgImage = new PortableImage();
				dgImage.Load(filename);
				SetDGImage(dgImage);
			}
	public Bitmap(int width, int height)
			: this(width, height, PixelFormat.Format24bppRgb) {}
	public Bitmap(int width, int height,
				  PixelFormat format)
			{
				SetDGImage(new PortableImage
					(width, height, (PixelFormat)format));
				dgImage.AddFrame();
			}
	public Bitmap(int width, int height, Graphics g)
			{
				if(g == null)
				{
					throw new ArgumentNullException("g");
				}
				SetDGImage(new PortableImage
					(width, height, PixelFormat.Format24bppRgb));
				dgImage.AddFrame();
			}
	public Bitmap(Type type, string resource)
			{
				Stream stream = GetManifestResourceStream(type, resource);
				if(stream == null)
				{
					throw new ArgumentException("Arg_UnknownResource");
				}
				try
				{
					var dgImage = new PortableImage();
					dgImage.Load(stream);
					SetDGImage(dgImage);
				}
				finally
				{
					stream.Close();
				}
			}
	public Bitmap(Image original, int width, int height)
			{
				if(original.dgImage != null)
				{
					SetDGImage(original.dgImage.Stretch(width, height));
				}
			}
	public Bitmap(int width, int height, int stride,
				  PixelFormat format, IntPtr scan0)
			{
				// We don't support loading bitmaps from unmanaged buffers.
				throw new SecurityException();
			}
	internal Bitmap(PortableImage image) : base(image) {}
#if CONFIG_SERIALIZATION
	internal Bitmap(SerializationInfo info, StreamingContext context)
			: base(info, context) {}
#endif

	// Get a manifest resource stream.  Profile-safe version.
	internal static Stream GetManifestResourceStream(Type type, string name)
			{
			#if !ECMA_COMPAT
				return type.Module.Assembly.GetManifestResourceStream
						(type, name);
			#else
				if(type.Namespace != null && type.Namespace != String.Empty)
				{
					return type.Module.Assembly.GetManifestResourceStream
						(type.Namespace + "." + name);
				}
				return type.Module.Assembly.GetManifestResourceStream(name);
			#endif
			}

	// Clone this bitmap and transform it into a new pixel format
	[TODO]
	public Bitmap Clone
				(Rectangle rect, PixelFormat format)
			{
				// TODO : There has to be a better way !!
				Bitmap b = new Bitmap(rect.Width, rect.Height, format);
				for(int x = 0 ; x < rect.Width ; x++)
				{
					for(int y = 0 ; y < rect.Height ; y++)
					{
						b.SetPixel(x,y, GetPixel(rect.Left+x,rect.Top+y));
					}
				}
				return b;
			}
	[TODO]
	public Bitmap Clone
				(RectangleF rect, PixelFormat format)
			{
				// TODO : There has to be a better way !!
				Bitmap b = new Bitmap((int)rect.Width, (int)rect.Height, format);
				for(int x = 0 ; x < rect.Width ; x++)
				{
					for(int y = 0 ; y < rect.Height ; y++)
					{
						b.SetPixel(x,y,
								GetPixel((int)rect.Left+x, (int)rect.Top+y));
					}
				}
				return b;
			}

	// Create a bitmap from a native icon handle.
	public static Bitmap FromHicon(IntPtr hicon)
			{
				throw new SecurityException();
			}

	// Create a bitmap from a Windows resource name.
	public static Bitmap FromResource(IntPtr hinstance, string bitmapName)
			{
				throw new SecurityException();
			}

	// Convert this bitmap into a native bitmap handle.
#if CONFIG_COMPONENT_MODEL
	[EditorBrowsable(EditorBrowsableState.Advanced)]
#endif
	public IntPtr GetHbitmap()
			{
				return GetHbitmap(Color.LightGray);
			}
	[TODO]
#if CONFIG_COMPONENT_MODEL
	[EditorBrowsable(EditorBrowsableState.Advanced)]
#endif
	public IntPtr GetHbitmap(Color background)
			{
				throw new SecurityException();
			}

	// Convert this bitmap into a native icon handle.
	[TODO]
#if CONFIG_COMPONENT_MODEL
	[EditorBrowsable(EditorBrowsableState.Advanced)]
#endif
	public IntPtr GetHicon()
			{
				throw new SecurityException();
			}

	// Get the color of a specific pixel.
	public Color GetPixel(int x, int y)
			{
				if(dgImage != null)
				{
					int pix = dgImage.GetFrame(0).GetPixel(x, y);
					return Color.FromArgb((pix >> 16) & 0xFF,
										  (pix >> 8) & 0xFF,
										  pix & 0xFF);
				}
				return Color.Empty;
			}

	// Lock a region of this bitmap.  Use of this method is discouraged.
	// It assumes that managed arrays are fixed in place in memory,
	// which is true for ilrun, but maybe not other CLR implementations.
	// We also assume that "format" is the same as the bitmap's real format.
	public unsafe BitmapData LockBits
					(Rectangle rect, ImageLockMode flags,
					 PixelFormat format)
			{
				BitmapData bitmapData = new BitmapData();
				bitmapData.Width = rect.Width;
				bitmapData.Height = rect.Height;
				bitmapData.PixelFormat = format;
				if(dgImage != null)
				{
					Frame frame = dgImage.GetFrame(0);
					if(frame != null)
					{
						if (format != PixelFormat)
						{
							frame = frame.Reformat(format);
						}
						bitmapData.Stride = frame.Stride;
						byte[] data = frame.Data;
						bitmapData.dataHandle = GCHandle.Alloc(data);

						int offset = rect.X * GetPixelFormatSize(format) / 8;
						// TODO: will GCHandle.AddrOfPinnedObject work more 
						//       portably across GCs ?
						fixed (byte *pixel = &(data[rect.Y * frame.Stride]))
						{
							bitmapData.Scan0 = (IntPtr)(void *)(pixel + offset);
						}
					}
				}
				return bitmapData;
			}

	// Make a particular color transparent within this bitmap.
	public void MakeTransparent()
			{
				Color transparentColor = Color.LightGray;
				if(Width > 1 && Height > 1)
				{
					transparentColor = GetPixel(0, Height - 1);
					if(transparentColor.A == 0xFF)
					{
						// Use light grey
						transparentColor = Color.LightGray;
					}
				}
				MakeTransparent(transparentColor);
			}
	public void MakeTransparent(Color transparentColor)
			{
				// Make all the frames transparent.
				for (int f = 0; f < dgImage.NumFrames; f++)
				{
					Frame frame = dgImage.GetFrame(f);
					int color = transparentColor.ToArgb();
					if(!IsAlphaPixelFormat(PixelFormat))
					{
						// Remove the alpha component.
						color = color & 0x00FFFFFF;
					}
					frame.MakeTransparent(color);
				}
			}

	// Set a pixel within this bitmap.
	public void SetPixel(int x, int y, Color color)
			{
				if(dgImage != null)
				{
					dgImage.GetFrame(0).SetPixel
						(x, y, (color.R << 16) | (color.G << 8) | color.B);
				}
			}

	// Set the resolution for this bitmap.
	public void SetResolution(float dpiX, float dpiY)
			{
				horizontalResolution = dpiX;
				verticalResolution = dpiY;
			}

	// Unlock the bits within this bitmap.
	public void UnlockBits(BitmapData bitmapData)
			{
				// Nothing to do in this implementation.
				bitmapData.dataHandle.Free();
			}

}; // class Bitmap

}; // namespace System.Drawing
