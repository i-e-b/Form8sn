#nullable enable

namespace Portable.Drawing.Imaging.ImageFormats.Jpeg
{
    /// <summary>
    /// Element precision of quantization tablse.
    /// </summary>
    public enum JpegElementPrecision : byte
    {
        /// <summary>
        /// 8 bit precision.
        /// </summary>
        Precision8Bit = 0,

        /// <summary>
        /// 12 bit precision.
        /// </summary>
        Precision12Bit = 1,
    }
}
