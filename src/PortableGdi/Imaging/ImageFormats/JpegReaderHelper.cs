using System;
using System.Buffers;
using System.IO;
using Portable.Drawing.Imaging.ImageFormats.Jpeg;

namespace Portable.Drawing.Imaging.ImageFormats
{
    public class JpegReaderHelper
    {
        public static void Load(Stream stream, PortableImage image)
        {
            // This just reads enough headers to set size.

            var ms = new MemoryStream();
            stream.CopyTo(ms);
            var data = new ReadOnlySequence<byte>(ms.ToArray());
            
            var decoder = new JpegDecoder();
            decoder.SetInput(data);
            decoder.Identify();

            if (decoder.NumberOfComponents != 1 && decoder.NumberOfComponents != 3)
            {
                // We only support Grayscale and YCbCr.
                throw new NotSupportedException("This color space is not supported");
            }

            image.Width = decoder.Width;
            image.Height = decoder.Height;
            image.LoadFormat = PortableImage.Jpeg;
            image.imageFlags = decoder.NumberOfComponents == 1 ? ImageFlags.ColorSpaceGray : ImageFlags.ColorSpaceRgb;
            
            /*
            // TODO: bring in the huge complexity that is the JPEG read/write format.
            // Add a frame to the image object.
            Frame frame = image.AddFrame(image.Width, image.Height, PixelFormat.Format24bppRgb);
            
            if (decoder.Precision == 8)
            {
                // This is the most common case for JPEG, and we have an optimised implementation.
                decoder.SetOutputWriter(new JpegBufferOutputWriter8Bit(image.Width, image.Height, 3, frame.data));
            }
            else if (decoder.Precision < 8)
            {
                decoder.SetOutputWriter(new JpegBufferOutputWriterLessThan8Bit(image.Width, image.Height, decoder.Precision, 3, frame.data));
            }
            else
            {
                decoder.SetOutputWriter(new JpegBufferOutputWriterGreaterThan8Bit(image.Width, image.Height, decoder.Precision, 3, frame.data));
            }

            decoder.Decode();
            // TODO: our frame buffer should now be populated with Y,Cr,Cb data. It should get converted to RGB.
            */
        }
    }
}