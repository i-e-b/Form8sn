/*
 * ImageCodecInfo.cs - Implementation of the
 *			"System.Drawing.Imaging.ImageCodecInfo" class.
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

namespace Portable.Drawing.Imaging
{
    [Flags]
    public enum ImageCodecFlags
    {
        Encoder = 0x00000001,
        Decoder = 0x00000002,
        SupportBitmap = 0x00000004,
        SupportVector = 0x00000008,
        SeekableEncode = 0x00000010,
        BlockingDecoder = 0x00000020,
        Builtin = 0x00010000,
        System = 0x00020000,
        User = 0x00040000
    };
#if !ECMA_COMPAT
    [ComVisible(false)]
#endif
    public sealed class ImageCodecInfo
    {
        // Internal state.
#if !ECMA_COMPAT
#endif

        // Constructor.
        internal ImageCodecInfo()
        {
            SignatureMasks = new byte[][]{};
            SignaturePatterns = new byte[][]{};
        }

        // Get or set this object's properties.
#if !ECMA_COMPAT
        public Guid Clsid { get; set; }
#endif
        public string CodecName { get; set; }="";

        public string DllName { get; set; }="";

        public string FilenameExtension { get; set; }="";

        public ImageCodecFlags Flags { get; set; }

        public string FormatDescription { get; set; }="";
#if !ECMA_COMPAT
        // ReSharper disable once InconsistentNaming
        public Guid FormatID { get; set; }
#endif
        public string MimeType { get; set; }="";

        public byte[][] SignatureMasks { get; set; }

        public byte[][] SignaturePatterns { get; set; }

        public int Version { get; set; }

        // Find all image decoders.
        [TODO]
        public static ImageCodecInfo[] GetImageDecoders()
        {
            // TODO
            return new ImageCodecInfo[0];
        }

        // Find all image encoders.
        [TODO]
        public static ImageCodecInfo[] GetImageEncoders()
        {
            // TODO
            return new ImageCodecInfo[0];
        }
    }; // class ImageCodecInfo
}; // namespace System.Drawing.Imaging