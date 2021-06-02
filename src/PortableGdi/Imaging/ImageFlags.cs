namespace System.Drawing.Imaging
{
    [Flags]
    public enum ImageFlags
    {
        Scalable			= 0x00000001,
        None				= 0x00000000,
        HasAlpha			= 0x00000002,
        HasTranslucent		= 0x00000004,
        PartiallyScalable	= 0x00000008,
        ColorSpaceRgb		= 0x00000010,
        ColorSpaceCmyk		= 0x00000020,
        ColorSpaceGray		= 0x00000040,
        ColorSpaceYcbcr		= 0x00000080,
        ColorSpaceYcck		= 0x00000100,
        HasRealDpi			= 0x00001000,
        HasRealPixelSize	= 0x00002000,
        ReadOnly			= 0x00010000,
        Caching				= 0x00020000

    };
}