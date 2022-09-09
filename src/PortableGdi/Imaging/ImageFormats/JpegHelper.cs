using System;
using System.Buffers;
using System.IO;
using Portable.Drawing.Imaging.ImageFormats.Jpeg;

namespace Portable.Drawing.Imaging.ImageFormats
{
    /// <summary>
    /// Simple interface to the Jpeg codec.
    /// </summary>
    public class JpegHelper
    {
        /// <summary>
        /// Load enough of a Jpeg image to read size and color type.
        /// </summary>
        public static void ReadHeaders(Stream srcStream, PortableImage dstImage)
        {
            // This just reads enough headers to set size.

            var ms = new MemoryStream();
            srcStream.CopyTo(ms);
            var data = new ReadOnlySequence<byte>(ms.ToArray());

            var decoder = new JpegDecoder();
            decoder.SetInput(data);
            decoder.Identify();

            if (decoder.NumberOfComponents != 1 && decoder.NumberOfComponents != 3)
            {
                // We only support Grayscale and YCbCr.
                throw new NotSupportedException("This color space is not supported");
            }

            dstImage.Width = decoder.Width;
            dstImage.Height = decoder.Height;
            dstImage.LoadFormat = PortableImage.Jpeg;
            dstImage.imageFlags = decoder.NumberOfComponents == 1 ? ImageFlags.ColorSpaceGray : ImageFlags.ColorSpaceRgb;
        }

        /// <summary>
        /// Read a Jpeg source file into a portable image container
        /// </summary>
        public static void Read(Stream srcStream, PortableImage dstImage)
        {
        }

        /// <summary>
        /// Write a portable image into a Jpeg file stream
        /// </summary>
        public static void Write(PortableImage srcImage, Stream dstStream, int quality = 95)
        {
            if (quality <= 0 || quality > 100) quality = 95;
            
            if (srcImage.NumFrames < 1) throw new Exception("Source image contains no pixel data (frame count is zero)");
            var frame = srcImage.GetFrame(0);
            if (frame is null) throw new Exception("Invalid frame zero in image");

            // Prepare a buffer for Jpeg data
            var yCbCr = new byte[srcImage.Width * srcImage.Height * 3];

            // Copy image pixel into YCbCr buffer, converting RGB to YCbCr.
            if (frame.pixelFormat == PixelFormat.Format32bppArgb)
            {
                for (int i = 0; i < srcImage.Height; i++)
                {
                    JpegRgbToYCbCrConverter.Shared.ConvertRgba32ToYCbCr8(
                        frame.GetRowBytes(i),
                        yCbCr.AsSpan(3 * srcImage.Width * i, 3 * srcImage.Width),
                        srcImage.Width
                    );
                }
            }
            else if (frame.pixelFormat == PixelFormat.Format24bppRgb)
            {
                for (int i = 0; i < srcImage.Height; i++)
                {
                    JpegRgbToYCbCrConverter.Shared.ConvertRgb24ToYCbCr8(
                        frame.GetRowBytes(i),
                        yCbCr.AsSpan(3 * srcImage.Width * i, 3 * srcImage.Width),
                        srcImage.Width
                    );
                }
            }
            else throw new Exception("Can't convert to JPEG - not a full color image");

            var encoder = new JpegEncoder();
            encoder.SetQuantizationTable(JpegStandardQuantizationTable.ScaleByQuality(JpegStandardQuantizationTable.GetLuminanceTable(JpegElementPrecision.Precision8Bit, 0), quality));
            encoder.SetQuantizationTable(JpegStandardQuantizationTable.ScaleByQuality(JpegStandardQuantizationTable.GetChrominanceTable(JpegElementPrecision.Precision8Bit, 1), quality));

            /* BUG: Green/purple dots in each block
            encoder.SetHuffmanTable(true, 0, JpegStandardHuffmanEncodingTable.GetLuminanceDCTable());
            encoder.SetHuffmanTable(false, 0, JpegStandardHuffmanEncodingTable.GetLuminanceACTable());
            encoder.SetHuffmanTable(true, 1, JpegStandardHuffmanEncodingTable.GetChrominanceDCTable());
            encoder.SetHuffmanTable(false, 1, JpegStandardHuffmanEncodingTable.GetChrominanceACTable());
            */
            
            // BUG: green at right edge of image
            encoder.SetHuffmanTable(true, 0);
            encoder.SetHuffmanTable(false, 0);
            encoder.SetHuffmanTable(true, 1);
            encoder.SetHuffmanTable(false, 1);
            
            encoder.AddComponent(1, 0, 0, 0, 2, 2); // Y component
            encoder.AddComponent(2, 1, 1, 1, 1, 1); // Cb component
            encoder.AddComponent(3, 1, 1, 1, 1, 1); // Cr component

            encoder.SetInputReader(new JpegBufferInputReader(srcImage.Width, srcImage.Height, 3, yCbCr));

            var writer = new ArrayBufferWriter<byte>();
            encoder.SetOutput(writer);

            encoder.Encode();

            var buf = writer.WrittenSpan.ToArray();
            if (buf is null) throw new Exception("Jpeg buffer failed to produce data");
            
            dstStream.Write(buf, 0, buf.Length);
        }
    }
}