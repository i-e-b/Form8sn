namespace Form8snCore.Rendering
{
    public enum DocumentBoxType
    {
        // Normal text output
        Normal = 0,
        
        // Special meta-data text output
        PageGenerationDate,
        CurrentPageNumber,
        TotalPageCount,
        RepeatingPageNumber,
        RepeatingPageTotalCount,
        
        // Super special rendering modes
        EmbedJpegImage,
        ColorBox,
        QrCode,
        CustomRenderer
    }
}