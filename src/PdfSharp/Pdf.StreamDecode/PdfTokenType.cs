namespace PdfSharp.Pdf.StreamDecode
{
    public enum PdfTokenType
    {
        /// <summary>
        /// An unknown instruction type.
        /// This will generally be preceded by some raw values
        /// </summary>
        Unknown = 0,
        
        /// <summary>
        /// `Do` (name --) : Draw an XImage from the page dictionary
        /// </summary>
        DrawImage
        
        
        /*
         < >        ; array
         << >>      ; dictionary
         w
         W*
         Do
         Q
         q
         re
         cm
         n
         rg
         BT / ET
            Td Tf Tj
         */
    }
}